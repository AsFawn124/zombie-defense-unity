using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 天赋树管理器 - 单例
/// 管理天赋树、天赋点、方案保存、重置
/// 对应 TASK-020, TASK-021
/// </summary>
public class TalentTreeManager : MonoBehaviour
{
    public static TalentTreeManager Instance { get; private set; }

    [Header("配置")]
    public TalentTreeConfig Config;

    // 运行时数据
    private Dictionary<string, int> activeNodeLevels;           // 当前方案：节点ID -> 等级
    private int availableTalentPoints;                          // 可用天赋点
    private int totalPointsSpent;                               // 已消耗天赋点
    private List<TalentBuild> savedBuilds;                      // 保存的方案
    private List<TalentPointAcquisition> pointHistory;          // 获取记录
    private int freeResetCountRemaining;                        // 剩余免费重置次数
    private int dailyResetCount;                                // 今日重置次数
    private DateTime lastResetDate;                             // 上次重置日期

    // 事件
    public event Action<string, int, int> OnNodeLevelChanged;   // 节点等级变化 (nodeId, oldLevel, newLevel)
    public event Action<int, int> OnTalentPointsChanged;        // 天赋点变化 (before, after)
    public event Action OnTalentReset;                          // 天赋重置
    public event Action<TalentBuild> OnBuildSaved;              // 方案保存
    public event Action<TalentBuild> OnBuildLoaded;             // 方案加载
    public event Action<string> OnBuildDeleted;                 // 方案删除
    public event Action OnTalentTreeChanged;                    // 天赋树变更

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        activeNodeLevels = new Dictionary<string, int>();
        savedBuilds = new List<TalentBuild>();
        pointHistory = new List<TalentPointAcquisition>();
        availableTalentPoints = 0;
        totalPointsSpent = 0;
        freeResetCountRemaining = Config?.ResetConfig.FirstFreeResetCount ?? 3;
        dailyResetCount = 0;
        lastResetDate = DateTime.MinValue;
    }

    #region 天赋点管理

    /// <summary>
    /// 获取天赋点
    /// </summary>
    public void AddTalentPoints(int points, TalentPointSource source, string sourceId = "")
    {
        if (points <= 0) return;

        int before = availableTalentPoints;
        availableTalentPoints += points;

        pointHistory.Add(new TalentPointAcquisition
        {
            Source = source,
            SourceId = sourceId,
            PointsEarned = points,
            EarnedTime = DateTime.Now
        });

        OnTalentPointsChanged?.Invoke(before, availableTalentPoints);
        OnTalentTreeChanged?.Invoke();

        Debug.Log($"[TalentTree] 获得 {points} 天赋点（来源: {source}）剩余: {availableTalentPoints}");
    }

    /// <summary>
    /// 通过关卡通关获取天赋点
    /// </summary>
    public void GrantStageReward(string stageId)
    {
        if (Config?.PointRewards == null) return;

        var reward = Config.PointRewards.Find(r => r.Source == TalentPointSource.StageClear && r.SourceId == stageId);
        if (reward != null)
        {
            AddTalentPoints(reward.PointsGranted, TalentPointSource.StageClear, stageId);
        }
    }

    /// <summary>
    /// 通过升级获取天赋点
    /// </summary>
    public void GrantLevelUpReward(int newLevel)
    {
        int points = Config?.PointsPerLevelUp ?? 1;
        AddTalentPoints(points, TalentPointSource.LevelUp, $"level_{newLevel}");
    }

    /// <summary>
    /// 获取当前可用天赋点
    /// </summary>
    public int GetAvailablePoints()
    {
        return availableTalentPoints;
    }

    /// <summary>
    /// 获取已消耗天赋点
    /// </summary>
    public int GetTotalPointsSpent()
    {
        return totalPointsSpent;
    }

    /// <summary>
    /// 获取总获得天赋点
    /// </summary>
    public int GetTotalPointsEarned()
    {
        return pointHistory.Sum(p => p.PointsEarned);
    }

    #endregion

    #region 节点操作

    /// <summary>
    /// 升级天赋节点
    /// </summary>
    public bool UpgradeNode(string nodeId)
    {
        var nodeDef = Config?.GetNodeDef(nodeId);
        if (nodeDef == null)
        {
            Debug.LogWarning($"[TalentTree] 节点定义不存在: {nodeId}");
            return false;
        }

        // 获取当前等级
        int currentLevel = activeNodeLevels.ContainsKey(nodeId) ? activeNodeLevels[nodeId] : 0;

        // 检查是否满级
        if (currentLevel >= nodeDef.MaxLevel)
        {
            Debug.LogWarning($"[TalentTree] 节点已满级: {nodeDef.NodeName}");
            return false;
        }

        // 检查前置条件
        if (!Config.ArePrerequisitesMet(nodeDef, activeNodeLevels))
        {
            Debug.LogWarning($"[TalentTree] 前置条件不满足: {nodeDef.NodeName}");
            return false;
        }

        // 检查天赋点
        if (availableTalentPoints <= 0)
        {
            Debug.LogWarning("[TalentTree] 天赋点不足");
            return false;
        }

        // 消耗天赋点
        availableTalentPoints--;
        totalPointsSpent++;

        // 升级节点
        int oldLevel = currentLevel;
        activeNodeLevels[nodeId] = currentLevel + 1;

        OnNodeLevelChanged?.Invoke(nodeId, oldLevel, currentLevel + 1);
        OnTalentTreeChanged?.Invoke();

        float value = nodeDef.GetValueAtLevel(currentLevel + 1);
        Debug.Log($"[TalentTree] {nodeDef.NodeName} Lv.{oldLevel} → Lv.{currentLevel + 1} (+{value})");

        return true;
    }

    /// <summary>
    /// 降级天赋节点（退回天赋点）
    /// </summary>
    public bool DowngradeNode(string nodeId)
    {
        if (!activeNodeLevels.ContainsKey(nodeId) || activeNodeLevels[nodeId] <= 0)
        {
            Debug.LogWarning($"[TalentTree] 节点未激活: {nodeId}");
            return false;
        }

        // 检查是否有其他节点依赖此节点
        if (Config != null)
        {
            foreach (var node in Config.AllNodes)
            {
                if (node.Prerequisites != null && node.Prerequisites.Contains(nodeId))
                {
                    if (activeNodeLevels.ContainsKey(node.NodeId) && activeNodeLevels[node.NodeId] > 0)
                    {
                        Debug.LogWarning($"[TalentTree] 节点 [{node.NodeName}] 依赖此节点，无法降级");
                        return false;
                    }
                }
            }
        }

        int oldLevel = activeNodeLevels[nodeId];
        activeNodeLevels[nodeId]--;

        if (activeNodeLevels[nodeId] <= 0)
            activeNodeLevels.Remove(nodeId);

        // 退回天赋点
        availableTalentPoints++;
        totalPointsSpent--;

        OnNodeLevelChanged?.Invoke(nodeId, oldLevel, activeNodeLevels.ContainsKey(nodeId) ? activeNodeLevels[nodeId] : 0);
        OnTalentTreeChanged?.Invoke();

        Debug.Log($"[TalentTree] 降级节点: {nodeId} Lv.{oldLevel} → Lv.{Mathf.Max(0, oldLevel - 1)}");
        return true;
    }

    /// <summary>
    /// 获取节点等级
    /// </summary>
    public int GetNodeLevel(string nodeId)
    {
        return activeNodeLevels.ContainsKey(nodeId) ? activeNodeLevels[nodeId] : 0;
    }

    /// <summary>
    /// 节点是否激活
    /// </summary>
    public bool IsNodeActive(string nodeId)
    {
        return activeNodeLevels.ContainsKey(nodeId) && activeNodeLevels[nodeId] > 0;
    }

    /// <summary>
    /// 是否可以升级节点
    /// </summary>
    public bool CanUpgradeNode(string nodeId)
    {
        var nodeDef = Config?.GetNodeDef(nodeId);
        if (nodeDef == null) return false;

        // 有可用天赋点
        if (availableTalentPoints <= 0) return false;

        // 未满级
        int currentLevel = activeNodeLevels.ContainsKey(nodeId) ? activeNodeLevels[nodeId] : 0;
        if (currentLevel >= nodeDef.MaxLevel) return false;

        // 前置条件满足
        if (!Config.ArePrerequisitesMet(nodeDef, activeNodeLevels)) return false;

        return true;
    }

    /// <summary>
    /// 获取所有节点状态
    /// </summary>
    public List<TalentNodeInstance> GetAllNodeStates()
    {
        var states = new List<TalentNodeInstance>();

        if (Config?.AllNodes == null) return states;

        foreach (var nodeDef in Config.AllNodes)
        {
            int level = activeNodeLevels.ContainsKey(nodeDef.NodeId) ? activeNodeLevels[nodeDef.NodeId] : 0;

            states.Add(new TalentNodeInstance
            {
                NodeId = nodeDef.NodeId,
                NodeName = nodeDef.NodeName,
                Branch = nodeDef.Branch,
                NodeType = nodeDef.NodeType,
                Tier = nodeDef.Tier,
                CurrentLevel = level,
                MaxLevel = nodeDef.MaxLevel,
                IsActivated = level > 0
            });
        }

        return states;
    }

    /// <summary>
    /// 获取指定系别的节点状态
    /// </summary>
    public List<TalentNodeInstance> GetNodeStatesByBranch(TalentBranch branch)
    {
        return GetAllNodeStates().Where(n => n.Branch == branch).ToList();
    }

    #endregion

    #region 重置

    /// <summary>
    /// 重置天赋树
    /// </summary>
    public TalentResetResult ResetTalentTree(bool useFreeReset = true)
    {
        var result = new TalentResetResult();

        // 检查今日重置次数
        if (lastResetDate.Date != DateTime.Now.Date)
        {
            dailyResetCount = 0;
            lastResetDate = DateTime.Now;
        }

        if (useFreeReset && freeResetCountRemaining > 0)
        {
            freeResetCountRemaining--;
            result.Cost = 0;
            result.IsFree = true;
        }
        else
        {
            int maxDaily = Config?.ResetConfig.MaxDailyResets ?? 3;
            if (dailyResetCount >= maxDaily)
            {
                result.Success = false;
                result.Message = $"今日重置次数已用完 ({dailyResetCount}/{maxDaily})";
                return result;
            }

            int baseCost = Config?.ResetConfig.BaseResetGoldCost ?? 1000;
            int perPointCost = Config?.ResetConfig.ResetCostPerPoint ?? 100;
            result.Cost = baseCost + totalPointsSpent * perPointCost;
            result.IsFree = false;
            dailyResetCount++;
        }

        // 退回所有天赋点
        int pointsReturned = totalPointsSpent;
        availableTalentPoints += pointsReturned;
        totalPointsSpent = 0;
        activeNodeLevels.Clear();

        result.Success = true;
        result.PointsReturned = pointsReturned;
        result.Message = result.IsFree
            ? $"免费重置成功！退回 {pointsReturned} 天赋点（剩余免费次数: {freeResetCountRemaining}）"
            : $"重置成功！消耗 {result.Cost} 金币，退回 {pointsReturned} 天赋点";

        OnTalentReset?.Invoke();
        OnTalentTreeChanged?.Invoke();

        Debug.Log($"[TalentTree] 天赋重置: {result.Message}");
        return result;
    }

    /// <summary>
    /// 获取重置信息
    /// </summary>
    public TalentResetResult GetResetPreview()
    {
        if (lastResetDate.Date != DateTime.Now.Date)
            dailyResetCount = 0;

        if (freeResetCountRemaining > 0)
        {
            return new TalentResetResult
            {
                Success = true,
                IsFree = true,
                Cost = 0,
                PointsReturned = totalPointsSpent,
                Message = $"免费重置（剩余 {freeResetCountRemaining} 次）"
            };
        }

        int maxDaily = Config?.ResetConfig.MaxDailyResets ?? 3;
        if (dailyResetCount >= maxDaily)
        {
            return new TalentResetResult
            {
                Success = false,
                Message = $"今日重置次数已用完"
            };
        }

        int baseCost = Config?.ResetConfig.BaseResetGoldCost ?? 1000;
        int perPointCost = Config?.ResetConfig.ResetCostPerPoint ?? 100;
        int totalCost = baseCost + totalPointsSpent * perPointCost;

        return new TalentResetResult
        {
            Success = true,
            IsFree = false,
            Cost = totalCost,
            PointsReturned = totalPointsSpent,
            Message = $"消耗 {totalCost} 金币，退回 {totalPointsSpent} 天赋点"
        };
    }

    #endregion

    #region 方案管理

    /// <summary>
    /// 保存当前天赋配置为方案
    /// </summary>
    public TalentBuild SaveCurrentBuild(string buildName)
    {
        if (Config != null && savedBuilds.Count >= Config.MaxSavedBuilds)
        {
            Debug.LogWarning($"[TalentTree] 方案数量已达上限 ({Config.MaxSavedBuilds})");
            return null;
        }

        // 去激活其他方案
        foreach (var build in savedBuilds)
            build.IsActive = false;

        var newBuild = new TalentBuild
        {
            BuildName = buildName,
            NodeLevels = new Dictionary<string, int>(activeNodeLevels),
            TotalPointsSpent = totalPointsSpent,
            IsActive = true,
            CreatedTime = DateTime.Now,
            LastModifiedTime = DateTime.Now
        };

        savedBuilds.Add(newBuild);
        OnBuildSaved?.Invoke(newBuild);
        OnTalentTreeChanged?.Invoke();

        Debug.Log($"[TalentTree] 保存方案: {buildName}（{totalPointsSpent} 点）");
        return newBuild;
    }

    /// <summary>
    /// 加载天赋方案
    /// </summary>
    public bool LoadBuild(string buildId)
    {
        var build = savedBuilds.Find(b => b.BuildId == buildId);
        if (build == null)
        {
            Debug.LogWarning($"[TalentTree] 方案不存在: {buildId}");
            return false;
        }

        // 去激活所有方案
        foreach (var b in savedBuilds)
            b.IsActive = false;

        // 应用方案
        activeNodeLevels = new Dictionary<string, int>(build.NodeLevels);
        availableTalentPoints += totalPointsSpent;  // 退回当前点数
        totalPointsSpent = build.TotalPointsSpent;
        availableTalentPoints -= totalPointsSpent;   // 扣除方案点数

        build.IsActive = true;
        build.LastModifiedTime = DateTime.Now;

        OnBuildLoaded?.Invoke(build);
        OnTalentTreeChanged?.Invoke();

        Debug.Log($"[TalentTree] 加载方案: {build.BuildName}（{totalPointsSpent} 点）");
        return true;
    }

    /// <summary>
    /// 删除方案
    /// </summary>
    public bool DeleteBuild(string buildId)
    {
        var build = savedBuilds.Find(b => b.BuildId == buildId);
        if (build == null) return false;

        bool wasActive = build.IsActive;
        savedBuilds.Remove(build);

        if (wasActive && savedBuilds.Count > 0)
        {
            // 激活第一个方案
            savedBuilds[0].IsActive = true;
        }

        OnBuildDeleted?.Invoke(buildId);
        OnTalentTreeChanged?.Invoke();

        Debug.Log($"[TalentTree] 删除方案: {build.BuildName}");
        return true;
    }

    /// <summary>
    /// 重命名方案
    /// </summary>
    public bool RenameBuild(string buildId, string newName)
    {
        var build = savedBuilds.Find(b => b.BuildId == buildId);
        if (build == null) return false;

        if (Config != null && newName.Length > Config.MaxBuildNameLength)
            newName = newName.Substring(0, Config.MaxBuildNameLength);

        build.BuildName = newName;
        build.LastModifiedTime = DateTime.Now;
        OnTalentTreeChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 获取所有方案
    /// </summary>
    public List<TalentBuild> GetAllBuilds()
    {
        return new List<TalentBuild>(savedBuilds);
    }

    /// <summary>
    /// 获取当前激活的方案
    /// </summary>
    public TalentBuild GetActiveBuild()
    {
        return savedBuilds.Find(b => b.IsActive);
    }

    /// <summary>
    /// 导出当前方案（用于分享）
    /// </summary>
    public string ExportCurrentBuild()
    {
        var data = new TalentTreeSaveData
        {
            ActiveNodeLevels = new Dictionary<string, int>(activeNodeLevels),
            TotalPointsSpent = totalPointsSpent,
            AvailableTalentPoints = availableTalentPoints
        };

        return JsonUtility.ToJson(data);
    }

    #endregion

    #region 属性计算

    /// <summary>
    /// 计算天赋树属性的总加成
    /// </summary>
    public EquipmentStats CalculateTalentBonus()
    {
        var stats = new EquipmentStats();
        if (Config?.AllNodes == null) return stats;

        foreach (var nodeDef in Config.AllNodes)
        {
            int level = activeNodeLevels.ContainsKey(nodeDef.NodeId) ? activeNodeLevels[nodeDef.NodeId] : 0;
            if (level <= 0 || nodeDef.NodeType != TalentNodeType.StatBonus) continue;

            float value = nodeDef.GetValueAtLevel(level);
            ApplyTalentStat(ref stats, nodeDef.BonusAttribute, value, nodeDef.ValueType);
        }

        return stats;
    }

    private void ApplyTalentStat(ref EquipmentStats stats, AffixType type, float value, AffixValueType valueType)
    {
        float actualValue = valueType == AffixValueType.Percentage ? value * 100f : value;

        switch (type)
        {
            case AffixType.Attack: stats.AttackBonus += actualValue; break;
            case AffixType.Defense: stats.DefenseBonus += actualValue; break;
            case AffixType.Health: stats.HealthBonus += actualValue; break;
            case AffixType.CritChance: stats.CritChanceBonus += actualValue; break;
            case AffixType.CritDamage: stats.CritDamageBonus += actualValue; break;
            case AffixType.Speed: stats.AttackSpeedBonus += actualValue; break;
            case AffixType.CooldownReduction: stats.CooldownReduction += actualValue; break;
            case AffixType.ElementalMastery: stats.ElementalMastery += actualValue; break;
            case AffixType.ElementalResistance: stats.ElementalResistance += actualValue; break;
            case AffixType.LifeSteal: stats.LifeSteal += actualValue; break;
            case AffixType.DamageReduction: stats.DamageReduction += actualValue; break;
        }
    }

    #endregion

    #region 保存加载

    public TalentTreeSaveData GetSaveData()
    {
        return new TalentTreeSaveData
        {
            AvailableTalentPoints = availableTalentPoints,
            ActiveNodeLevels = new Dictionary<string, int>(activeNodeLevels),
            TotalPointsSpent = totalPointsSpent,
            SavedBuilds = savedBuilds.Select(b => b.Clone()).ToList(),
            PointHistory = new List<TalentPointAcquisition>(pointHistory),
            FreeResetCountRemaining = freeResetCountRemaining,
            DailyResetCount = dailyResetCount,
            LastResetDate = lastResetDate,
            LastSaveTime = DateTime.Now
        };
    }

    public void LoadSaveData(TalentTreeSaveData saveData)
    {
        if (saveData == null) return;

        Initialize();
        availableTalentPoints = saveData.AvailableTalentPoints;
        activeNodeLevels = saveData.ActiveNodeLevels ?? new Dictionary<string, int>();
        totalPointsSpent = saveData.TotalPointsSpent;
        savedBuilds = saveData.SavedBuilds ?? new List<TalentBuild>();
        pointHistory = saveData.PointHistory ?? new List<TalentPointAcquisition>();
        freeResetCountRemaining = saveData.FreeResetCountRemaining;
        dailyResetCount = saveData.DailyResetCount;
        lastResetDate = saveData.LastResetDate;

        OnTalentTreeChanged?.Invoke();
    }

    #endregion
}

/// <summary>
/// 天赋重置结果
/// </summary>
[Serializable]
public class TalentResetResult
{
    public bool Success;
    public bool IsFree;
    public int Cost;
    public int PointsReturned;
    public string Message;
}
