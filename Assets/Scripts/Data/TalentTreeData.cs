using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 天赋/科技树系统数据结构 - Phase 2.3
/// </summary>

#region 天赋系别

/// <summary>
/// 天赋系别
/// </summary>
public enum TalentBranch
{
    Attack,     // 攻击系 - 15节点
    Defense,    // 防御系 - 15节点
    Economy,    // 经济系 - 10节点
    Special     // 特殊系 - 10节点
}

/// <summary>
/// 天赋节点类型
/// </summary>
public enum TalentNodeType
{
    StatBonus,      // 属性加成
    SkillUnlock,    // 技能解锁
    Passive,        // 被动效果
    Special         // 特殊节点
}

#endregion

#region 天赋节点

/// <summary>
/// 天赋节点定义（配置表用）
/// </summary>
[Serializable]
public class TalentNodeDef
{
    public string NodeId;                   // 节点ID
    public string NodeName;                 // 节点名称
    public TalentBranch Branch;             // 所属系别
    public TalentNodeType NodeType;         // 节点类型
    public int Tier;                        // 层级（1-5）
    public int PositionIndex;               // 同层位置索引
    public List<string> Prerequisites;      // 前置节点ID列表（空表示初始节点）
    public int MaxLevel;                    // 最大等级
    public string Description;              // 描述（{0}为数值占位符）

    // 属性加成（StatBonus类型）
    public AffixType BonusAttribute;        // 加成的属性
    public AffixValueType ValueType;        // 数值类型
    public float[] ValuesPerLevel;          // 每级加成值

    // 技能解锁（SkillUnlock类型）
    public string UnlockSkillId;            // 解锁的技能ID

    // 特殊效果（Passive/Special类型）
    public string SpecialEffectId;          // 特殊效果ID

    // 视觉
    public Sprite Icon;                     // 节点图标
    public Vector2 Position;                // 在树中的位置（编辑器用）

    /// <summary>
    /// 获取指定等级的值
    /// </summary>
    public float GetValueAtLevel(int level)
    {
        if (ValuesPerLevel == null || level < 1 || level > ValuesPerLevel.Length)
            return 0f;
        return ValuesPerLevel[level - 1];
    }
}

/// <summary>
/// 天赋节点实例（玩家的实际天赋状态）
/// </summary>
[Serializable]
public class TalentNodeInstance
{
    public string NodeId;                   // 节点定义ID
    public string NodeName;                 // 节点名称
    public TalentBranch Branch;             // 所属系别
    public TalentNodeType NodeType;         // 节点类型
    public int Tier;                        // 层级
    public int CurrentLevel;                // 当前等级
    public int MaxLevel;                    // 最大等级
    public bool IsActivated;                // 是否激活

    /// <summary>
    /// 是否已达到最大等级
    /// </summary>
    public bool IsMaxLevel => CurrentLevel >= MaxLevel;

    /// <summary>
    /// 是否可以升级
    /// </summary>
    public bool CanUpgrade => !IsMaxLevel && IsActivated;
}

#endregion

#region 天赋方案

/// <summary>
/// 天赋方案（一套完整的天赋配置）
/// </summary>
[Serializable]
public class TalentBuild
{
    public string BuildId;                              // 方案ID
    public string BuildName;                            // 方案名称
    public Dictionary<string, int> NodeLevels;          // 节点ID -> 等级
    public int TotalPointsSpent;                        // 已消耗天赋点
    public bool IsActive;                               // 是否当前激活的方案
    public DateTime CreatedTime;                        // 创建时间
    public DateTime LastModifiedTime;                   // 最后修改时间

    public TalentBuild()
    {
        BuildId = Guid.NewGuid().ToString("N");
        NodeLevels = new Dictionary<string, int>();
        CreatedTime = DateTime.Now;
        LastModifiedTime = DateTime.Now;
    }

    public TalentBuild Clone()
    {
        return new TalentBuild
        {
            BuildId = this.BuildId,
            BuildName = this.BuildName,
            NodeLevels = new Dictionary<string, int>(this.NodeLevels),
            TotalPointsSpent = this.TotalPointsSpent,
            IsActive = this.IsActive,
            CreatedTime = this.CreatedTime,
            LastModifiedTime = this.LastModifiedTime
        };
    }
}

#endregion

#region 天赋点获取

/// <summary>
/// 天赋点获取途径
/// </summary>
public enum TalentPointSource
{
    LevelUp,            // 玩家升级
    StageClear,         // 关卡通关
    Achievement,        // 成就奖励
    Purchase,           // 购买
    Quest               // 任务奖励
}

/// <summary>
/// 天赋点奖励配置
/// </summary>
[Serializable]
public class TalentPointReward
{
    public TalentPointSource Source;
    public string SourceId;                 // 来源ID（关卡ID/成就ID等）
    public int PointsGranted;               // 奖励点数
    public string Description;              // 奖励描述
}

/// <summary>
/// 天赋点获取记录
/// </summary>
[Serializable]
public class TalentPointAcquisition
{
    public TalentPointSource Source;
    public string SourceId;
    public int PointsEarned;
    public DateTime EarnedTime;
}

#endregion

#region 天赋重置

/// <summary>
/// 重置配置
/// </summary>
[Serializable]
public class TalentResetConfig
{
    public int FirstFreeResetCount;         // 首次免费重置次数
    public int BaseResetGoldCost;           // 基础重置金币
    public int ResetCostPerPoint;           // 每个已分配天赋点额外金币
    public int MaxDailyResets;              // 每日最大重置次数
}

#endregion

#region 天赋保存

/// <summary>
/// 天赋树保存数据
/// </summary>
[Serializable]
public class TalentTreeSaveData
{
    public int AvailableTalentPoints;                               // 可用天赋点
    public Dictionary<string, int> ActiveNodeLevels;                // 当前方案：节点ID -> 等级
    public int TotalPointsSpent;                                    // 已消耗总数
    public List<TalentBuild> SavedBuilds;                           // 保存的方案列表
    public List<TalentPointAcquisition> PointHistory;               // 获取记录
    public int FreeResetCountRemaining;                             // 剩余免费重置次数
    public int DailyResetCount;                                     // 今日重置次数
    public DateTime LastResetDate;                                  // 上次重置日期
    public DateTime LastSaveTime;                                   // 最后保存时间

    public TalentTreeSaveData()
    {
        ActiveNodeLevels = new Dictionary<string, int>();
        SavedBuilds = new List<TalentBuild>();
        PointHistory = new List<TalentPointAcquisition>();
        FreeResetCountRemaining = 0;
        DailyResetCount = 0;
    }
}

#endregion

#region 天赋树配置 ScriptableObject

/// <summary>
/// 天赋树总配置
/// </summary>
[CreateAssetMenu(fileName = "TalentTreeConfig", menuName = "Game/Talent/Talent Tree Config")]
public class TalentTreeConfig : ScriptableObject
{
    [Header("节点定义")]
    public List<TalentNodeDef> AllNodes;

    [Header("系别配置")]
    public int AttackNodeCount = 15;
    public int DefenseNodeCount = 15;
    public int EconomyNodeCount = 10;
    public int SpecialNodeCount = 10;

    [Header("天赋点获取")]
    public int PointsPerLevelUp = 1;                    // 每次升级获得点数
    public List<TalentPointReward> PointRewards;        // 奖励列表

    [Header("重置配置")]
    public TalentResetConfig ResetConfig;

    [Header("方案配置")]
    public int MaxSavedBuilds = 5;                      // 最多保存方案数
    public int MaxBuildNameLength = 12;                 // 方案名称最大长度

    [Header("系别颜色")]
    public Color AttackColor = new Color(1f, 0.3f, 0.3f, 1f);     // 红色
    public Color DefenseColor = new Color(0.3f, 0.5f, 1f, 1f);    // 蓝色
    public Color EconomyColor = new Color(0.3f, 0.9f, 0.3f, 1f);  // 绿色
    public Color SpecialColor = new Color(0.7f, 0.3f, 1f, 1f);    // 紫色

    /// <summary>
    /// 获取系别名称
    /// </summary>
    public static string GetBranchName(TalentBranch branch)
    {
        switch (branch)
        {
            case TalentBranch.Attack: return "攻击系";
            case TalentBranch.Defense: return "防御系";
            case TalentBranch.Economy: return "经济系";
            case TalentBranch.Special: return "特殊系";
            default: return "未知";
        }
    }

    /// <summary>
    /// 获取系别颜色
    /// </summary>
    public Color GetBranchColor(TalentBranch branch)
    {
        switch (branch)
        {
            case TalentBranch.Attack: return AttackColor;
            case TalentBranch.Defense: return DefenseColor;
            case TalentBranch.Economy: return EconomyColor;
            case TalentBranch.Special: return SpecialColor;
            default: return Color.white;
        }
    }

    /// <summary>
    /// 获取系别节点列表
    /// </summary>
    public List<TalentNodeDef> GetNodesByBranch(TalentBranch branch)
    {
        return AllNodes.FindAll(n => n.Branch == branch);
    }

    /// <summary>
    /// 获取节点定义
    /// </summary>
    public TalentNodeDef GetNodeDef(string nodeId)
    {
        return AllNodes.Find(n => n.NodeId == nodeId);
    }

    /// <summary>
    /// 获取某层级的所有节点
    /// </summary>
    public List<TalentNodeDef> GetNodesByTier(TalentBranch branch, int tier)
    {
        return AllNodes.FindAll(n => n.Branch == branch && n.Tier == tier);
    }

    /// <summary>
    /// 检查前置条件是否满足
    /// </summary>
    public bool ArePrerequisitesMet(TalentNodeDef node, Dictionary<string, int> nodeLevels)
    {
        if (node.Prerequisites == null || node.Prerequisites.Count == 0)
            return true;

        foreach (var prereqId in node.Prerequisites)
        {
            if (!nodeLevels.ContainsKey(prereqId) || nodeLevels[prereqId] < 1)
                return false;
        }
        return true;
    }
}

#endregion
