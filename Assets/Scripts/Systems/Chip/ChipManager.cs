using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

/// <summary>
/// 芯片管理器 - 单例
/// 管理芯片获取、升级、背包
/// 对应 TASK-018
/// </summary>
public class ChipManager : MonoBehaviour
{
    public static ChipManager Instance { get; private set; }

    [Header("配置")]
    public ChipSystemConfig Config;

    // 运行时数据
    private List<ChipInstance> ownedChips;                       // 拥有的芯片
    private Dictionary<string, List<string>> embeddedChips;     // 装备ID -> 芯片实例ID列表

    // 事件
    public event Action<ChipInstance> OnChipAcquired;           // 获得芯片
    public event Action<ChipInstance> OnChipUpgraded;           // 芯片升级
    public event Action<ChipInstance> OnChipEmbedded;           // 芯片镶嵌
    public event Action<ChipInstance> OnChipRemoved;            // 芯片拆卸
    public event Action<ChipInstance> OnChipDestroyed;          // 芯片销毁
    public event Action OnChipChanged;                          // 芯片变更

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
        ownedChips = new List<ChipInstance>();
        embeddedChips = new Dictionary<string, List<string>>();
    }

    #region 芯片生成

    /// <summary>
    /// 生成随机芯片
    /// </summary>
    public ChipInstance GenerateChip(ChipQuality? forcedQuality = null, string stageId = "")
    {
        if (Config == null || Config.ChipDefinitions == null || Config.ChipDefinitions.Count == 0)
        {
            Debug.LogWarning("[ChipManager] 无芯片定义");
            return null;
        }

        // 随机选择芯片定义
        var chipDef = Config.ChipDefinitions[Random.Range(0, Config.ChipDefinitions.Count)];

        // 确定品质
        ChipQuality quality = forcedQuality ?? RollChipQuality();

        // 创建芯片实例
        var chip = new ChipInstance
        {
            ChipId = chipDef.ChipId,
            ChipName = chipDef.ChipName,
            Category = chipDef.Category,
            SubType = chipDef.SubType,
            Quality = quality,
            SetId = chipDef.SetId,
            MaxLevel = chipDef.MaxLevel,
            GrowthPerLevel = chipDef.GrowthPerLevel,
            ValueType = chipDef.ValueType,
            AffectAttribute = chipDef.AffectAttribute
        };

        // 根据品质缩放数值
        float qualityMultiplier = quality switch
        {
            ChipQuality.Normal => 1.0f,
            ChipQuality.Rare => 1.5f,
            ChipQuality.Epic => 2.2f,
            ChipQuality.Legendary => 3.0f,
            _ => 1.0f
        };

        chip.GrowthPerLevel *= qualityMultiplier;
        chip.CurrentValue = chip.GrowthPerLevel; // Level 1

        return chip;
    }

    /// <summary>
    /// 随机品质
    /// </summary>
    private ChipQuality RollChipQuality()
    {
        float roll = Random.value;
        if (roll < 0.50f) return ChipQuality.Normal;
        if (roll < 0.80f) return ChipQuality.Rare;
        if (roll < 0.95f) return ChipQuality.Epic;
        return ChipQuality.Legendary;
    }

    /// <summary>
    /// 生成芯片（按类别过滤）
    /// </summary>
    public ChipInstance GenerateChipByCategory(ChipCategory category, ChipQuality? forcedQuality = null)
    {
        if (Config == null) return null;

        var candidates = Config.ChipDefinitions.Where(c => c.Category == category).ToList();
        if (candidates.Count == 0)
            candidates = Config.ChipDefinitions.ToList();

        var chipDef = candidates[Random.Range(0, candidates.Count)];
        ChipQuality quality = forcedQuality ?? RollChipQuality();

        return new ChipInstance
        {
            ChipId = chipDef.ChipId,
            ChipName = chipDef.ChipName,
            Category = chipDef.Category,
            SubType = chipDef.SubType,
            Quality = quality,
            SetId = chipDef.SetId,
            MaxLevel = chipDef.MaxLevel,
            GrowthPerLevel = chipDef.GrowthPerLevel,
            ValueType = chipDef.ValueType,
            AffectAttribute = chipDef.AffectAttribute,
            CurrentValue = chipDef.GrowthPerLevel
        };
    }

    #endregion

    #region 芯片管理

    /// <summary>
    /// 添加芯片到背包
    /// </summary>
    public bool AddChip(ChipInstance chip)
    {
        if (chip == null) return false;

        ownedChips.Add(chip);
        OnChipAcquired?.Invoke(chip);
        OnChipChanged?.Invoke();

        Debug.Log($"[ChipManager] 获得芯片: {chip.ChipName} ({ChipInstance.GetQualityName(chip.Quality)})");
        return true;
    }

    /// <summary>
    /// 移除芯片
    /// </summary>
    public bool RemoveChip(string instanceId)
    {
        var chip = GetChipById(instanceId);
        if (chip == null) return false;

        if (chip.IsEmbedded)
        {
            Debug.LogWarning("[ChipManager] 无法移除已镶嵌的芯片，请先拆卸");
            return false;
        }

        ownedChips.Remove(chip);
        OnChipDestroyed?.Invoke(chip);
        OnChipChanged?.Invoke();
        return true;
    }

    #endregion

    #region 芯片升级

    /// <summary>
    /// 升级芯片
    /// </summary>
    public ChipUpgradeResult UpgradeChip(string instanceId, List<string> materialChipIds)
    {
        var result = new ChipUpgradeResult();
        var chip = GetChipById(instanceId);

        if (chip == null)
        {
            result.Message = "芯片不存在";
            return result;
        }

        if (chip.Level >= chip.MaxLevel)
        {
            result.Message = $"已达到最高等级 Lv.{chip.MaxLevel}";
            return result;
        }

        if (chip.IsEmbedded)
        {
            // 允许镶嵌时升级，但需要先拆卸？设定：允许
        }

        // 查找升级配方
        var recipe = Config?.GetUpgradeRecipe(chip.Quality, chip.Level);
        if (recipe == null)
        {
            // 默认升级需求
            recipe = new ChipUpgradeRecipe
            {
                GoldCost = Config?.BaseUpgradeGoldCost ?? 500 + chip.Level * 200,
                ChipsRequired = chip.Level,
                FromLevel = chip.Level,
                ToLevel = chip.Level + 1,
                SuccessRate = Mathf.Max(0.3f, 1.0f - chip.Level * 0.1f),
                Quality = chip.Quality
            };
        }

        // 检查材料
        if (materialChipIds.Count < recipe.ChipsRequired)
        {
            result.Message = $"材料不足，需要 {recipe.ChipsRequired} 个同品质芯片";
            return result;
        }

        // 验证材料品质
        var materials = materialChipIds
            .Select(id => GetChipById(id))
            .Where(m => m != null && m.Quality == chip.Quality && !m.IsEmbedded)
            .Take(recipe.ChipsRequired)
            .ToList();

        if (materials.Count < recipe.ChipsRequired)
        {
            result.Message = $"材料不足，需要 {recipe.ChipsRequired} 个{chip.Quality}品质芯片";
            return result;
        }

        result.OldLevel = chip.Level;
        result.OldValue = chip.CurrentValue;
        result.GoldCost = recipe.GoldCost;
        result.ChipsConsumed = materials.Count;

        // 成功率判定
        if (Random.value <= recipe.SuccessRate)
        {
            // 升级成功：消费材料
            foreach (var mat in materials)
                RemoveChip(mat.InstanceId);

            chip.Level++;
            chip.CurrentValue = chip.GrowthPerLevel * chip.Level;
            result.Success = true;
            result.NewLevel = chip.Level;
            result.NewValue = chip.CurrentValue;
            result.Message = $"升级成功！{chip.ChipName} Lv.{result.OldLevel} → Lv.{result.NewLevel}";

            OnChipUpgraded?.Invoke(chip);
            OnChipChanged?.Invoke();
        }
        else
        {
            // 升级失败：消费一半材料
            int lossCount = Mathf.Max(1, materials.Count / 2);
            for (int i = 0; i < lossCount; i++)
                RemoveChip(materials[i].InstanceId);

            result.Success = false;
            result.Message = $"升级失败，损失 {lossCount} 个材料";
        }

        return result;
    }

    /// <summary>
    /// 获取升级预览
    /// </summary>
    public ChipUpgradeResult GetUpgradePreview(string instanceId)
    {
        var chip = GetChipById(instanceId);
        if (chip == null) return new ChipUpgradeResult { Message = "芯片不存在" };

        var recipe = Config?.GetUpgradeRecipe(chip.Quality, chip.Level);
        if (recipe == null)
        {
            return new ChipUpgradeResult
            {
                OldLevel = chip.Level,
                OldValue = chip.CurrentValue,
                NewLevel = chip.Level + 1,
                NewValue = chip.GrowthPerLevel * (chip.Level + 1),
                GoldCost = (Config?.BaseUpgradeGoldCost ?? 500) + chip.Level * 200,
                ChipsConsumed = chip.Level,
                Success = true,
                Message = $"预计: Lv.{chip.Level} → Lv.{chip.Level + 1}"
            };
        }

        return new ChipUpgradeResult
        {
            OldLevel = chip.Level,
            OldValue = chip.CurrentValue,
            NewLevel = recipe.ToLevel,
            NewValue = chip.GrowthPerLevel * recipe.ToLevel,
            GoldCost = recipe.GoldCost,
            ChipsConsumed = recipe.ChipsRequired,
            Success = true,
            Message = $"成功率: {recipe.SuccessRate:P0}"
        };
    }

    #endregion

    #region 查询

    /// <summary>
    /// 获取所有芯片
    /// </summary>
    public List<ChipInstance> GetAllChips()
    {
        return new List<ChipInstance>(ownedChips);
    }

    /// <summary>
    /// 获取未镶嵌的芯片
    /// </summary>
    public List<ChipInstance> GetUnembeddedChips()
    {
        return ownedChips.Where(c => !c.IsEmbedded).ToList();
    }

    /// <summary>
    /// 获取指定类别的芯片
    /// </summary>
    public List<ChipInstance> GetChipsByCategory(ChipCategory category)
    {
        return ownedChips.Where(c => c.Category == category).ToList();
    }

    /// <summary>
    /// 获取指定套装的芯片
    /// </summary>
    public List<ChipInstance> GetChipsBySet(string setId)
    {
        return ownedChips.Where(c => c.SetId == setId).ToList();
    }

    /// <summary>
    /// 通过ID获取芯片
    /// </summary>
    public ChipInstance GetChipById(string instanceId)
    {
        return ownedChips.Find(c => c.InstanceId == instanceId);
    }

    /// <summary>
    /// 获取装备上镶嵌的芯片
    /// </summary>
    public List<ChipInstance> GetEmbeddedChips(string equipmentId)
    {
        var result = new List<ChipInstance>();
        if (embeddedChips.TryGetValue(equipmentId, out var chipIds))
        {
            foreach (var id in chipIds)
            {
                var chip = GetChipById(id);
                if (chip != null)
                    result.Add(chip);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取芯片总数
    /// </summary>
    public int GetChipCount()
    {
        return ownedChips.Count;
    }

    /// <summary>
    /// 计算激活的套装效果
    /// </summary>
    public List<ActiveSetBonus> GetActiveSetBonuses()
    {
        var activeSetBonuses = new List<ActiveSetBonus>();

        // 统计所有已镶嵌芯片的套装
        var setCountDict = new Dictionary<string, int>();
        foreach (var chip in ownedChips)
        {
            if (!chip.IsEmbedded || string.IsNullOrEmpty(chip.SetId)) continue;

            if (!setCountDict.ContainsKey(chip.SetId))
                setCountDict[chip.SetId] = 0;
            setCountDict[chip.SetId]++;
        }

        // 检查套装效果
        foreach (var kvp in setCountDict)
        {
            var setBonus = Config?.GetSetBonus(kvp.Key);
            if (setBonus == null) continue;

            var activatedEffect = setBonus.GetMaxEffect(kvp.Value);
            if (activatedEffect != null)
            {
                activeSetBonuses.Add(new ActiveSetBonus
                {
                    SetBonus = setBonus,
                    ActivatedPieces = kvp.Value,
                    Effect = activatedEffect
                });
            }
        }

        return activeSetBonuses;
    }

    #endregion

    #region 保存加载

    public ChipSaveData GetSaveData()
    {
        return new ChipSaveData
        {
            OwnedChips = new List<ChipInstance>(ownedChips),
            EmbeddedChips = new Dictionary<string, List<string>>(embeddedChips),
            LastSaveTime = DateTime.Now
        };
    }

    public void LoadSaveData(ChipSaveData saveData)
    {
        if (saveData == null) return;

        Initialize();
        ownedChips = saveData.OwnedChips ?? new List<ChipInstance>();
        embeddedChips = saveData.EmbeddedChips ?? new Dictionary<string, List<string>>();

        OnChipChanged?.Invoke();
    }

    #endregion

    // Socket methods are delegated to ChipSocketSystem
    public Dictionary<string, List<string>> GetEmbeddedMap() => embeddedChips;

    public void SetEmbeddedMap(Dictionary<string, List<string>> map)
    {
        embeddedChips = map ?? new Dictionary<string, List<string>>();
        OnChipChanged?.Invoke();
    }
}

/// <summary>
/// 激活的套装效果
/// </summary>
[Serializable]
public class ActiveSetBonus
{
    public ChipSetBonus SetBonus;
    public int ActivatedPieces;
    public ChipSetBonus.SetEffect Effect;

    public string GetDescription()
    {
        return $"{SetBonus.SetName} [{ActivatedPieces}/{SetBonus.RequiredPieces[SetBonus.RequiredPieces.Length - 1]}] - {Effect.EffectDescription}";
    }
}
