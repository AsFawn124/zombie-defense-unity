using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

/// <summary>
/// 装备洗练系统
/// 重置词条、锁定词条、消耗材料
/// 对应 TASK-016
/// </summary>
public class EquipmentReforgeSystem : MonoBehaviour
{
    public static EquipmentReforgeSystem Instance { get; private set; }

    [Header("配置引用")]
    public EquipmentSystemConfig Config;

    [Header("洗练事件")]
    public event Action<EquipmentItem, ReforgeResult> OnReforgeCompleted;   // 洗练完成
    public event Action<EquipmentItem, int> OnAffixLocked;                  // 词条锁定
    public event Action<EquipmentItem, int> OnAffixUnlocked;                // 词条解锁

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 执行洗练
    /// </summary>
    public ReforgeResult Reforge(EquipmentItem equipment)
    {
        var result = new ReforgeResult();

        if (equipment == null)
        {
            result.Success = false;
            result.Message = "装备无效";
            return result;
        }

        if (Config?.ReforgeConfig == null)
        {
            result.Success = false;
            result.Message = "洗练配置未加载";
            return result;
        }

        if (equipment.Affixes == null || equipment.Affixes.Count == 0)
        {
            result.Success = false;
            result.Message = "该装备没有可洗练的词条";
            return result;
        }

        var reforgeConfig = Config.ReforgeConfig;

        // 计算消耗
        int lockedCount = equipment.Affixes.Count(a => a.IsLocked);
        result.GoldCost = reforgeConfig.BaseGoldCost + lockedCount * reforgeConfig.LockAffixCost;
        result.MaterialCost = reforgeConfig.Materials?.FirstOrDefault()?.CostPerUse ?? 1;

        // 保存旧词条
        result.OldAffixes = equipment.Affixes.Select(a => a.Clone()).ToList();

        // 重新生成词条（保留锁定的）
        var newAffixes = new List<EquipmentAffixInstance>();
        foreach (var oldAffix in equipment.Affixes)
        {
            if (oldAffix.IsLocked)
            {
                // 保留锁定的词条
                newAffixes.Add(oldAffix);
            }
            else
            {
                // 生成新词条
                var newAffix = GenerateNewAffix(equipment, oldAffix, newAffixes);
                if (newAffix != null)
                    newAffixes.Add(newAffix);
                else
                    newAffixes.Add(oldAffix); // 如果生成失败，保留原词条
            }
        }

        equipment.Affixes = newAffixes;
        result.NewAffixes = new List<EquipmentAffixInstance>(newAffixes);
        result.Success = true;
        result.Message = "洗练完成！";

        OnReforgeCompleted?.Invoke(equipment, result);
        Debug.Log($"[EquipmentReforge] 洗练完成，锁定 {lockedCount} 个词条，消耗 {result.GoldCost} 金币");

        return result;
    }

    /// <summary>
    /// 生成新词条
    /// </summary>
    private EquipmentAffixInstance GenerateNewAffix(EquipmentItem equipment, EquipmentAffixInstance oldAffix, List<EquipmentAffixInstance> existingAffixes)
    {
        if (Config.AffixDefinitions == null || Config.AffixDefinitions.Count == 0)
            return null;

        AffixRarity maxRarity = EquipmentItem.GetMaxAffixRarity(equipment.Quality);

        // 洗练有概率出现更高稀有度词条
        float highQualityBonus = Config.ReforgeConfig.HigherQualityChance;
        if (Random.value < highQualityBonus)
        {
            maxRarity = (AffixRarity)Mathf.Min((int)maxRarity + 1, (int)AffixRarity.Epic);
        }

        var availableAffixes = Config.AffixDefinitions
            .Where(a => (int)a.AffixRarity <= (int)maxRarity)
            .ToList();

        if (availableAffixes.Count == 0)
            return null;

        // 排除已存在的词条类型
        var existingTypes = new HashSet<AffixType>(existingAffixes.Select(a => a.AffixType));
        var candidates = availableAffixes.Where(a => !existingTypes.Contains(a.AffixType)).ToList();

        if (candidates.Count == 0)
            candidates = availableAffixes; // 如果没有不同类型，允许重复

        var affixDef = candidates[Random.Range(0, candidates.Count)];

        float qualityFactor = ((int)equipment.Quality) / 7f;
        float value = Mathf.Lerp(affixDef.MinValue, affixDef.MaxValue, qualityFactor);
        value *= Random.Range(0.85f, 1.15f);

        return new EquipmentAffixInstance
        {
            AffixId = affixDef.AffixId,
            AffixName = affixDef.AffixName,
            AffixType = affixDef.AffixType,
            AffixRarity = affixDef.AffixRarity,
            ValueType = affixDef.ValueType,
            Value = Mathf.Round(value * 100f) / 100f,
            IsLocked = false
        };
    }

    /// <summary>
    /// 锁定词条
    /// </summary>
    public bool LockAffix(EquipmentItem equipment, int affixIndex)
    {
        if (equipment == null || affixIndex < 0 || affixIndex >= equipment.Affixes.Count)
            return false;

        // 检查锁定数量上限
        int lockedCount = equipment.Affixes.Count(a => a.IsLocked);
        int maxLocked = Config?.ReforgeConfig?.MaxLockedAffixes ?? 2;

        if (lockedCount >= maxLocked)
        {
            Debug.LogWarning($"[EquipmentReforge] 最多锁定 {maxLocked} 个词条");
            return false;
        }

        equipment.Affixes[affixIndex].IsLocked = true;
        OnAffixLocked?.Invoke(equipment, affixIndex);
        return true;
    }

    /// <summary>
    /// 解锁词条
    /// </summary>
    public bool UnlockAffix(EquipmentItem equipment, int affixIndex)
    {
        if (equipment == null || affixIndex < 0 || affixIndex >= equipment.Affixes.Count)
            return false;

        equipment.Affixes[affixIndex].IsLocked = false;
        OnAffixUnlocked?.Invoke(equipment, affixIndex);
        return true;
    }

    /// <summary>
    /// 计算洗练消耗预览
    /// </summary>
    public (int gold, int materials) GetReforgeCost(EquipmentItem equipment)
    {
        if (equipment == null || Config?.ReforgeConfig == null)
            return (0, 0);

        int lockedCount = equipment.Affixes?.Count(a => a.IsLocked) ?? 0;
        int gold = Config.ReforgeConfig.BaseGoldCost + lockedCount * Config.ReforgeConfig.LockAffixCost;
        int materials = Config.ReforgeConfig.Materials?.FirstOrDefault()?.CostPerUse ?? 1;

        return (gold, materials);
    }

    /// <summary>
    /// 获取洗练预览信息
    /// </summary>
    public ReforgePreview GetReforgePreview(EquipmentItem equipment)
    {
        if (equipment == null)
            return new ReforgePreview { Message = "请选择装备" };

        var preview = new ReforgePreview
        {
            EquipmentName = equipment.EquipmentName,
            CurrentAffixes = new List<EquipmentAffixInstance>(equipment.Affixes ?? new List<EquipmentAffixInstance>()),
            LockedCount = equipment.Affixes?.Count(a => a.IsLocked) ?? 0,
            MaxLocked = Config?.ReforgeConfig?.MaxLockedAffixes ?? 2
        };

        var (gold, materials) = GetReforgeCost(equipment);
        preview.GoldCost = gold;
        preview.MaterialCost = materials;
        preview.CanReforge = equipment.Affixes != null && equipment.Affixes.Count > 0;
        preview.Message = preview.CanReforge
            ? $"消耗: {gold} 金币 + {materials} 洗练石（已锁定 {preview.LockedCount}/{preview.MaxLocked}）"
            : "该装备无可洗练词条";

        return preview;
    }
}

/// <summary>
/// 洗练预览信息
/// </summary>
[Serializable]
public class ReforgePreview
{
    public string EquipmentName;
    public List<EquipmentAffixInstance> CurrentAffixes;
    public int LockedCount;
    public int MaxLocked;
    public int GoldCost;
    public int MaterialCost;
    public bool CanReforge;
    public string Message;
}
