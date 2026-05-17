using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

/// <summary>
/// 装备管理器 - 单例
/// 管理装备的获取、装备、卸载、背包等
/// 对应 TASK-014, TASK-015 (部分), TASK-017
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [Header("配置")]
    public EquipmentSystemConfig Config;

    // 运行时数据
    private List<EquipmentItem> ownedEquipments;                // 拥有的所有装备
    private Dictionary<EquipmentSlotType, string> equippedIds;  // 已装备的实例ID

    // 事件
    public event Action<EquipmentItem> OnEquipmentAcquired;     // 获得装备
    public event Action<EquipmentItem> OnEquipmentEquipped;     // 装备
    public event Action<EquipmentItem> OnEquipmentUnequipped;   // 卸载
    public event Action<EquipmentItem> OnEquipmentSold;         // 出售
    public event Action OnEquipmentChanged;                     // 装备变更

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
        ownedEquipments = new List<EquipmentItem>();
        equippedIds = new Dictionary<EquipmentSlotType, string>();
    }

    #region 装备生成

    /// <summary>
    /// 生成随机装备（由掉落系统调用）
    /// </summary>
    public EquipmentItem GenerateEquipment(EquipmentQuality quality, string stageId = "")
    {
        if (Config == null || Config.EquipmentTemplates == null || Config.EquipmentTemplates.Count == 0)
        {
            Debug.LogWarning("[EquipmentManager] 无装备模板可用");
            return null;
        }

        // 随机选择部位
        EquipmentSlotType slotType = (EquipmentSlotType)Random.Range(0, 3);

        // 从该部位的模板中随机选择
        var slotTemplates = Config.EquipmentTemplates
            .Where(t => t.SlotType == slotType)
            .ToList();

        if (slotTemplates.Count == 0)
            slotTemplates = Config.EquipmentTemplates.ToList();

        var template = slotTemplates[Random.Range(0, slotTemplates.Count)];

        // 创建装备实例
        var equipment = new EquipmentItem
        {
            EquipmentId = template.EquipmentId,
            EquipmentName = template.EquipmentName,
            SlotType = template.SlotType,
            Quality = quality,
            ChipSlotCount = EquipmentItem.GetChipSlotCount(quality),
            SourceStageId = stageId,
            AcquiredTime = DateTime.Now
        };

        // 根据品质缩放基础属性
        float qualityMultiplier = GetQualityStatMultiplier(quality);
        equipment.AttackBonus = Random.Range(template.BaseAttackMin, template.BaseAttackMax) * qualityMultiplier;
        equipment.DefenseBonus = Random.Range(template.BaseDefenseMin, template.BaseDefenseMax) * qualityMultiplier;
        equipment.HealthBonus = Random.Range(template.BaseHealthMin, template.BaseHealthMax) * qualityMultiplier;
        equipment.CritChanceBonus = template.BaseCritChance * qualityMultiplier;
        equipment.CritDamageBonus = template.BaseCritDamage * qualityMultiplier;
        equipment.AttackSpeedBonus = template.BaseAttackSpeed * qualityMultiplier;

        // 生成词条
        GenerateAffixes(equipment, quality);

        return equipment;
    }

    /// <summary>
    /// 获取品质属性倍率
    /// </summary>
    private float GetQualityStatMultiplier(EquipmentQuality quality)
    {
        switch (quality)
        {
            case EquipmentQuality.Common: return 0.4f;
            case EquipmentQuality.Uncommon: return 0.6f;
            case EquipmentQuality.Rare: return 0.8f;
            case EquipmentQuality.Epic: return 1.0f;
            case EquipmentQuality.Legendary: return 1.3f;
            case EquipmentQuality.Mythic: return 1.7f;
            case EquipmentQuality.Prismatic: return 2.2f;
            default: return 1.0f;
        }
    }

    /// <summary>
    /// 为装备生成词条
    /// </summary>
    public List<EquipmentAffixInstance> GenerateAffixes(EquipmentItem equipment, EquipmentQuality quality)
    {
        equipment.Affixes = new List<EquipmentAffixInstance>();
        int affixCount = EquipmentItem.GetAffixCountByQuality(quality);

        if (affixCount <= 0 || Config.AffixDefinitions == null || Config.AffixDefinitions.Count == 0)
            return equipment.Affixes;

        // 筛选可用词条（按品质限制稀有度）
        AffixRarity maxRarity = EquipmentItem.GetMaxAffixRarity(quality);
        var availableAffixes = Config.AffixDefinitions
            .Where(a => (int)a.AffixRarity <= (int)maxRarity)
            .ToList();

        if (availableAffixes.Count == 0) return equipment.Affixes;

        // 随机选择不重复的词条
        var usedTypes = new HashSet<AffixType>();
        int attempts = 0;

        while (equipment.Affixes.Count < affixCount && attempts < 50)
        {
            attempts++;
            var affixDef = availableAffixes[Random.Range(0, availableAffixes.Count)];

            // 避免相同类型的词条
            if (usedTypes.Contains(affixDef.AffixType))
                continue;

            usedTypes.Add(affixDef.AffixType);

            // 根据品质计算词条数值
            float qualityFactor = ((int)quality) / 7f; // 0~1
            float value = Mathf.Lerp(affixDef.MinValue, affixDef.MaxValue, qualityFactor);
            // 添加一些随机波动
            value *= Random.Range(0.85f, 1.15f);

            var instance = new EquipmentAffixInstance
            {
                AffixId = affixDef.AffixId,
                AffixName = affixDef.AffixName,
                AffixType = affixDef.AffixType,
                AffixRarity = affixDef.AffixRarity,
                ValueType = affixDef.ValueType,
                Value = Mathf.Round(value * 100f) / 100f
            };

            equipment.Affixes.Add(instance);
        }

        return equipment.Affixes;
    }

    #endregion

    #region 装备管理

    /// <summary>
    /// 添加装备到背包
    /// </summary>
    public bool AddEquipment(EquipmentItem equipment)
    {
        if (equipment == null) return false;

        ownedEquipments.Add(equipment);
        OnEquipmentAcquired?.Invoke(equipment);
        OnEquipmentChanged?.Invoke();

        Debug.Log($"[EquipmentManager] 获得装备: {equipment.EquipmentName} ({EquipmentItem.GetQualityName(equipment.Quality)})");
        return true;
    }

    /// <summary>
    /// 装备物品
    /// </summary>
    public bool EquipItem(string instanceId)
    {
        var equipment = GetEquipmentById(instanceId);
        if (equipment == null) return false;

        // 检查该部位是否已有装备
        if (equippedIds.TryGetValue(equipment.SlotType, out string oldId) && !string.IsNullOrEmpty(oldId))
        {
            // 卸载旧装备
            var oldEquipment = GetEquipmentById(oldId);
            if (oldEquipment != null)
            {
                OnEquipmentUnequipped?.Invoke(oldEquipment);
            }
        }

        equippedIds[equipment.SlotType] = instanceId;
        OnEquipmentEquipped?.Invoke(equipment);
        OnEquipmentChanged?.Invoke();

        Debug.Log($"[EquipmentManager] 装备: {equipment.EquipmentName} → {equipment.SlotType}");
        return true;
    }

    /// <summary>
    /// 卸载装备
    /// </summary>
    public bool UnequipSlot(EquipmentSlotType slot)
    {
        if (!equippedIds.TryGetValue(slot, out string instanceId))
            return false;

        var equipment = GetEquipmentById(instanceId);
        equippedIds.Remove(slot);

        if (equipment != null)
        {
            OnEquipmentUnequipped?.Invoke(equipment);
            OnEquipmentChanged?.Invoke();
        }

        Debug.Log($"[EquipmentManager] 卸载装备: {slot}");
        return true;
    }

    /// <summary>
    /// 出售装备
    /// </summary>
    public int SellEquipment(string instanceId)
    {
        var equipment = GetEquipmentById(instanceId);
        if (equipment == null) return 0;

        // 不能出售已装备的装备
        if (IsEquipped(instanceId))
        {
            Debug.LogWarning("[EquipmentManager] 不能出售已装备的装备");
            return 0;
        }

        int sellPrice = CalculateSellPrice(equipment);
        ownedEquipments.Remove(equipment);
        OnEquipmentSold?.Invoke(equipment);
        OnEquipmentChanged?.Invoke();

        Debug.Log($"[EquipmentManager] 出售装备: {equipment.EquipmentName}，获得 {sellPrice} 金币");
        return sellPrice;
    }

    /// <summary>
    /// 计算出售价格
    /// </summary>
    public int CalculateSellPrice(EquipmentItem equipment)
    {
        int basePrice = (int)equipment.Quality * 50;
        int affixBonus = equipment.Affixes.Count * 25;
        return basePrice + affixBonus;
    }

    #endregion

    #region 查询

    /// <summary>
    /// 获取所有装备
    /// </summary>
    public List<EquipmentItem> GetAllEquipments()
    {
        return new List<EquipmentItem>(ownedEquipments);
    }

    /// <summary>
    /// 获取指定部位的装备列表
    /// </summary>
    public List<EquipmentItem> GetEquipmentsBySlot(EquipmentSlotType slotType)
    {
        return ownedEquipments.Where(e => e.SlotType == slotType).ToList();
    }

    /// <summary>
    /// 获取当前装备的物品
    /// </summary>
    public EquipmentItem GetEquippedItem(EquipmentSlotType slot)
    {
        if (equippedIds.TryGetValue(slot, out string id))
            return GetEquipmentById(id);
        return null;
    }

    /// <summary>
    /// 获取所有已装备的物品
    /// </summary>
    public Dictionary<EquipmentSlotType, EquipmentItem> GetAllEquippedItems()
    {
        var result = new Dictionary<EquipmentSlotType, EquipmentItem>();
        foreach (var kvp in equippedIds)
        {
            var equipment = GetEquipmentById(kvp.Value);
            if (equipment != null)
                result[kvp.Key] = equipment;
        }
        return result;
    }

    /// <summary>
    /// 通过ID获取装备
    /// </summary>
    public EquipmentItem GetEquipmentById(string instanceId)
    {
        return ownedEquipments.Find(e => e.InstanceId == instanceId);
    }

    /// <summary>
    /// 检查装备是否已装备
    /// </summary>
    public bool IsEquipped(string instanceId)
    {
        return equippedIds.ContainsValue(instanceId);
    }

    /// <summary>
    /// 获取装备背包大小
    /// </summary>
    public int GetEquipmentCount()
    {
        return ownedEquipments.Count;
    }

    /// <summary>
    /// 计算总属性加成
    /// </summary>
    public EquipmentStats CalculateTotalStats()
    {
        var stats = new EquipmentStats();

        foreach (var kvp in equippedIds)
        {
            var equipment = GetEquipmentById(kvp.Value);
            if (equipment == null) continue;

            stats.AttackBonus += equipment.AttackBonus;
            stats.DefenseBonus += equipment.DefenseBonus;
            stats.HealthBonus += equipment.HealthBonus;
            stats.CritChanceBonus += equipment.CritChanceBonus;
            stats.CritDamageBonus += equipment.CritDamageBonus;
            stats.AttackSpeedBonus += equipment.AttackSpeedBonus;

            // 词条加成
            foreach (var affix in equipment.Affixes)
            {
                ApplyAffixStat(ref stats, affix.AffixType, affix.Value, affix.ValueType);
            }
        }

        return stats;
    }

    /// <summary>
    /// 应用词条属性到统计
    /// </summary>
    private void ApplyAffixStat(ref EquipmentStats stats, AffixType type, float value, AffixValueType valueType)
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

    public EquipmentSaveData GetSaveData()
    {
        return new EquipmentSaveData
        {
            OwnedEquipments = new List<EquipmentItem>(ownedEquipments),
            EquippedInstanceIds = new Dictionary<EquipmentSlotType, string>(equippedIds),
            LastSaveTime = DateTime.Now
        };
    }

    public void LoadSaveData(EquipmentSaveData saveData)
    {
        if (saveData == null) return;

        Initialize();
        ownedEquipments = saveData.OwnedEquipments ?? new List<EquipmentItem>();
        equippedIds = saveData.EquippedInstanceIds ?? new Dictionary<EquipmentSlotType, string>();

        OnEquipmentChanged?.Invoke();
    }

    #endregion
}

/// <summary>
/// 装备属性统计
/// </summary>
[Serializable]
public struct EquipmentStats
{
    public float AttackBonus;
    public float DefenseBonus;
    public float HealthBonus;
    public float CritChanceBonus;
    public float CritDamageBonus;
    public float AttackSpeedBonus;
    public float CooldownReduction;
    public float ElementalMastery;
    public float ElementalResistance;
    public float LifeSteal;
    public float DamageReduction;

    public override string ToString()
    {
        return $"ATK+{AttackBonus:F0} DEF+{DefenseBonus:F0} HP+{HealthBonus:F0} " +
               $"CRIT+{CritChanceBonus:F1}% CDMG+{CritDamageBonus:F1}% AS+{AttackSpeedBonus:F1}%";
    }
}
