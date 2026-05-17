using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 装备系统数据结构 - Phase 2.1
/// </summary>

#region 装备品质与部位

/// <summary>
/// 装备品质（7级：白绿蓝紫橙红彩）
/// </summary>
public enum EquipmentQuality
{
    Common = 1,         // 白 - ★
    Uncommon = 2,       // 绿 - ★★
    Rare = 3,           // 蓝 - ★★★
    Epic = 4,           // 紫 - ★★★★
    Legendary = 5,      // 橙 - ★★★★★
    Mythic = 6,         // 红 - ★★★★★★
    Prismatic = 7       // 彩 - 七彩变换
}

/// <summary>
/// 装备部位
/// </summary>
public enum EquipmentSlotType
{
    Weapon,             // 武器
    Armor,              // 护甲
    Accessory           // 饰品
}

/// <summary>
/// 词条稀有度
/// </summary>
public enum AffixRarity
{
    Normal = 1,         // 普通
    Rare = 2,           // 稀有
    Epic = 3            // 史诗
}

/// <summary>
/// 词条数值范围类型
/// </summary>
public enum AffixValueType
{
    Fixed,              // 固定值
    Percentage,         // 百分比
    Special             // 特殊效果
}

#endregion

#region 词条定义

/// <summary>
/// 装备词条基础定义（配置表用）
/// </summary>
[Serializable]
public class EquipmentAffixDef
{
    public string AffixId;                  // 词条ID
    public string AffixName;                // 词条名称
    public AffixType AffixType;             // 词条类型
    public AffixRarity AffixRarity;         // 词条稀有度
    public AffixValueType ValueType;        // 数值类型
    public string Description;              // 词条描述模板（{0}为数值占位符）
    public float MinValue;                  // 最小值（品质白时）
    public float MaxValue;                  // 最大值（品质彩时）
    public int RequiredQuality;             // 最低品质要求
}

/// <summary>
/// 词条实例（装备上的实际词条）
/// </summary>
[Serializable]
public class EquipmentAffixInstance
{
    public string AffixId;                  // 词条定义ID
    public string AffixName;                // 词条名称
    public AffixType AffixType;             // 词条类型
    public AffixRarity AffixRarity;         // 词条稀有度
    public AffixValueType ValueType;        // 数值类型
    public float Value;                     // 当前数值
    public bool IsLocked;                   // 是否锁定（洗练时保留）

    public string GetDescription()
    {
        if (ValueType == AffixValueType.Percentage)
            return $"{AffixName} +{Value * 100:F1}%";
        else if (ValueType == AffixValueType.Fixed)
            return $"{AffixName} +{Value:F0}";
        else
            return $"{AffixName}";
    }

    public EquipmentAffixInstance Clone()
    {
        return new EquipmentAffixInstance
        {
            AffixId = this.AffixId,
            AffixName = this.AffixName,
            AffixType = this.AffixType,
            AffixRarity = this.AffixRarity,
            ValueType = this.ValueType,
            Value = this.Value,
            IsLocked = false
        };
    }
}

#endregion

#region 装备实例

/// <summary>
/// 装备实例数据
/// </summary>
[Serializable]
public class EquipmentItem
{
    public string InstanceId;               // 唯一实例ID
    public string EquipmentId;              // 配置表ID
    public string EquipmentName;            // 装备名称
    public EquipmentSlotType SlotType;      // 部位
    public EquipmentQuality Quality;        // 品质
    public int Level;                       // 等级（暂时不分级，预留）
    public int EnhancementLevel;            // 强化等级

    // 基础属性
    public float AttackBonus;               // 攻击加成
    public float DefenseBonus;              // 防御加成
    public float HealthBonus;               // 生命加成
    public float CritChanceBonus;           // 暴击率加成
    public float CritDamageBonus;           // 暴击伤害加成
    public float AttackSpeedBonus;          // 攻速加成

    // 词条
    public List<EquipmentAffixInstance> Affixes;    // 词条列表

    // 来源
    public string SourceStageId;            // 掉落关卡
    public DateTime AcquiredTime;           // 获取时间

    // 芯片槽（Phase 2.2使用）
    public int ChipSlotCount;               // 芯片槽数量
    public List<string> EmbeddedChipIds;    // 镶嵌的芯片ID列表

    // 构建唯一ID
    public EquipmentItem()
    {
        InstanceId = Guid.NewGuid().ToString("N");
        Affixes = new List<EquipmentAffixInstance>();
        EmbeddedChipIds = new List<string>();
        AcquiredTime = DateTime.Now;
        Level = 1;
        EnhancementLevel = 0;
        ChipSlotCount = 0;
    }

    /// <summary>
    /// 获取品质颜色
    /// </summary>
    public static Color GetQualityColor(EquipmentQuality quality)
    {
        switch (quality)
        {
            case EquipmentQuality.Common: return Color.white;
            case EquipmentQuality.Uncommon: return Color.green;
            case EquipmentQuality.Rare: return new Color(0.3f, 0.5f, 1.0f);  // 蓝色
            case EquipmentQuality.Epic: return new Color(0.7f, 0.3f, 1.0f);   // 紫色
            case EquipmentQuality.Legendary: return new Color(1.0f, 0.6f, 0.0f); // 橙色
            case EquipmentQuality.Mythic: return Color.red;
            case EquipmentQuality.Prismatic: return new Color(1.0f, 0.2f, 0.7f); // 彩
            default: return Color.white;
        }
    }

    /// <summary>
    /// 获取品质名称中文
    /// </summary>
    public static string GetQualityName(EquipmentQuality quality)
    {
        switch (quality)
        {
            case EquipmentQuality.Common: return "普通";
            case EquipmentQuality.Uncommon: return "精良";
            case EquipmentQuality.Rare: return "稀有";
            case EquipmentQuality.Epic: return "史诗";
            case EquipmentQuality.Legendary: return "传说";
            case EquipmentQuality.Mythic: return "神话";
            case EquipmentQuality.Prismatic: return "彩华";
            default: return "未知";
        }
    }

    /// <summary>
    /// 获取品质星级
    /// </summary>
    public static string GetQualityStars(EquipmentQuality quality)
    {
        int stars = (int)quality;
        return new string('★', stars);
    }

    /// <summary>
    /// 获取词条数量（按品质）
    /// </summary>
    public static int GetAffixCountByQuality(EquipmentQuality quality)
    {
        switch (quality)
        {
            case EquipmentQuality.Common: return 0;      // 白：无词条
            case EquipmentQuality.Uncommon: return 1;    // 绿：1词条
            case EquipmentQuality.Rare: return 2;        // 蓝：2词条
            case EquipmentQuality.Epic: return 2;        // 紫：2词条（可刷出稀有词条）
            case EquipmentQuality.Legendary: return 3;   // 橙：3词条
            case EquipmentQuality.Mythic: return 3;      // 红：3词条（可刷出史诗词条）
            case EquipmentQuality.Prismatic: return 4;   // 彩：4词条
            default: return 0;
        }
    }

    /// <summary>
    /// 获取词条稀有度上限（按品质）
    /// </summary>
    public static AffixRarity GetMaxAffixRarity(EquipmentQuality quality)
    {
        switch (quality)
        {
            case EquipmentQuality.Common:
            case EquipmentQuality.Uncommon:
            case EquipmentQuality.Rare:
                return AffixRarity.Normal;
            case EquipmentQuality.Epic:
            case EquipmentQuality.Legendary:
                return AffixRarity.Rare;
            case EquipmentQuality.Mythic:
            case EquipmentQuality.Prismatic:
                return AffixRarity.Epic;
            default: return AffixRarity.Normal;
        }
    }

    /// <summary>
    /// 获取芯片槽数量（按品质）
    /// </summary>
    public static int GetChipSlotCount(EquipmentQuality quality)
    {
        switch (quality)
        {
            case EquipmentQuality.Common: return 0;
            case EquipmentQuality.Uncommon: return 0;
            case EquipmentQuality.Rare: return 1;
            case EquipmentQuality.Epic: return 1;
            case EquipmentQuality.Legendary: return 2;
            case EquipmentQuality.Mythic: return 2;
            case EquipmentQuality.Prismatic: return 3;
            default: return 0;
        }
    }

    public EquipmentItem Clone()
    {
        var clone = new EquipmentItem
        {
            InstanceId = this.InstanceId,
            EquipmentId = this.EquipmentId,
            EquipmentName = this.EquipmentName,
            SlotType = this.SlotType,
            Quality = this.Quality,
            Level = this.Level,
            EnhancementLevel = this.EnhancementLevel,
            AttackBonus = this.AttackBonus,
            DefenseBonus = this.DefenseBonus,
            HealthBonus = this.HealthBonus,
            CritChanceBonus = this.CritChanceBonus,
            CritDamageBonus = this.CritDamageBonus,
            AttackSpeedBonus = this.AttackSpeedBonus,
            SourceStageId = this.SourceStageId,
            AcquiredTime = this.AcquiredTime,
            ChipSlotCount = this.ChipSlotCount,
            Affixes = new List<EquipmentAffixInstance>(),
            EmbeddedChipIds = new List<string>(this.EmbeddedChipIds)
        };

        foreach (var affix in this.Affixes)
            clone.Affixes.Add(affix.Clone());

        return clone;
    }
}

#endregion

#region 装备配置

/// <summary>
/// 装备基础配置（配表用，定义装备模板）
/// </summary>
[Serializable]
public class EquipmentConfigEntry
{
    public string EquipmentId;              // 装备模板ID
    public string EquipmentName;            // 装备名称
    public EquipmentSlotType SlotType;      // 部位
    public EquipmentQuality Quality;        // 品质
    public string Description;              // 描述

    // 基础属性范围（按品质缩放）
    public float BaseAttackMin;
    public float BaseAttackMax;
    public float BaseDefenseMin;
    public float BaseDefenseMax;
    public float BaseHealthMin;
    public float BaseHealthMax;
    public float BaseCritChance;
    public float BaseCritDamage;
    public float BaseAttackSpeed;

    public Sprite Icon;                     // 装备图标
    public GameObject Model;                // 装备模型
}

/// <summary>
/// 装备掉落配置（关卡掉落表）
/// </summary>
[Serializable]
public class EquipmentDropConfig
{
    public string StageId;                  // 关卡ID
    public int DropCount;                   // 掉落数量（基础）
    public List<QualityWeight> QualityWeights;  // 品质权重
}

/// <summary>
/// 品质掉落权重
/// </summary>
[Serializable]
public class QualityWeight
{
    public EquipmentQuality Quality;
    public float Weight;                    // 权重（相对概率）
}

/// <summary>
/// 装备合成配方
/// </summary>
[Serializable]
public class EquipmentMergeRecipe
{
    public string RecipeId;                 // 配方ID
    public string TargetEquipmentId;        // 合成目标装备ID
    public EquipmentQuality TargetQuality;  // 合成目标品质
    public int RequiredSameEquipment;       // 需要相同装备数量（5合1=5）
    public int MaterialCost;                // 金币消耗
    public float SuccessRate;               // 成功率（1.0=100%）
}

#endregion

#region 装备洗练

/// <summary>
/// 洗练材料
/// </summary>
[Serializable]
public class ReforgeMaterial
{
    public string MaterialId;
    public string MaterialName;
    public int Quantity;
    public int CostPerUse;                  // 每次洗练消耗数量
}

/// <summary>
/// 洗练配置
/// </summary>
[Serializable]
public class ReforgeConfig
{
    public int BaseGoldCost;                // 基础金币消耗
    public int LockAffixCost;               // 每锁一个词条额外金币
    public List<ReforgeMaterial> Materials; // 洗练材料列表
    public int MaxLockedAffixes;            // 最多锁定词条数
    public float HigherQualityChance;       // 洗练出现更高品质词条的概率加成
}

/// <summary>
/// 洗练结果
/// </summary>
[Serializable]
public class ReforgeResult
{
    public bool Success;
    public List<EquipmentAffixInstance> OldAffixes;
    public List<EquipmentAffixInstance> NewAffixes;
    public int GoldCost;
    public int MaterialCost;
    public string Message;
}

#endregion

#region 装备背包保存

/// <summary>
/// 装备背包保存数据
/// </summary>
[Serializable]
public class EquipmentSaveData
{
    public List<EquipmentItem> OwnedEquipments;     // 拥有的装备
    public Dictionary<EquipmentSlotType, string> EquippedInstanceIds; // 已装备的实例ID
    public int ReforgeStoneCount;                   // 洗练石数量
    public int MergeStoneCount;                     // 合成石数量
    public DateTime LastSaveTime;

    public EquipmentSaveData()
    {
        OwnedEquipments = new List<EquipmentItem>();
        EquippedInstanceIds = new Dictionary<EquipmentSlotType, string>();
    }
}

#endregion

#region 装备配置 ScriptableObject

/// <summary>
/// 装备系统总配置
/// </summary>
[CreateAssetMenu(fileName = "EquipmentConfig", menuName = "Game/Equipment/Equipment Config")]
public class EquipmentSystemConfig : ScriptableObject
{
    [Header("装备模板配置")]
    public List<EquipmentConfigEntry> EquipmentTemplates;

    [Header("词条定义配置")]
    public List<EquipmentAffixDef> AffixDefinitions;

    [Header("掉落配置")]
    public List<EquipmentDropConfig> DropConfigs;
    public float GlobalDropRate = 0.3f;             // 全局掉落概率(30%)
    public int MaxDropPerWave = 3;                   // 每波最多掉落

    [Header("合成配置")]
    public int MergeRequiredCount = 5;               // 5合1
    public List<EquipmentMergeRecipe> MergeRecipes;
    public int BaseMergeGoldCost = 1000;
    public float QualityUpChance = 0.1f;             // 合成升品概率加成(10%)

    [Header("洗练配置")]
    public ReforgeConfig ReforgeConfig;

    [Header("部位说明")]
    public string[] SlotTypeNames = { "武器", "护甲", "饰品" };
    public string[] SlotTypeIcons = { "icon_weapon", "icon_armor", "icon_accessory" };

    [Header("颜色配置")]
    public Color[] QualityColors = new Color[]
    {
        Color.white,
        Color.green,
        new Color(0.3f, 0.5f, 1.0f),
        new Color(0.7f, 0.3f, 1.0f),
        new Color(1.0f, 0.6f, 0.0f),
        Color.red,
        new Color(1.0f, 0.2f, 0.7f)
    };

    /// <summary>
    /// 获取装备模板
    /// </summary>
    public EquipmentConfigEntry GetTemplate(string equipmentId)
    {
        return EquipmentTemplates.Find(e => e.EquipmentId == equipmentId);
    }

    /// <summary>
    /// 获取词条定义
    /// </summary>
    public EquipmentAffixDef GetAffixDef(string affixId)
    {
        return AffixDefinitions.Find(a => a.AffixId == affixId);
    }

    /// <summary>
    /// 获取彩色品质权重
    /// </summary>
    public List<QualityWeight> GetDefaultQualityWeights()
    {
        return new List<QualityWeight>
        {
            new QualityWeight { Quality = EquipmentQuality.Common, Weight = 0.40f },
            new QualityWeight { Quality = EquipmentQuality.Uncommon, Weight = 0.30f },
            new QualityWeight { Quality = EquipmentQuality.Rare, Weight = 0.15f },
            new QualityWeight { Quality = EquipmentQuality.Epic, Weight = 0.08f },
            new QualityWeight { Quality = EquipmentQuality.Legendary, Weight = 0.04f },
            new QualityWeight { Quality = EquipmentQuality.Mythic, Weight = 0.02f },
            new QualityWeight { Quality = EquipmentQuality.Prismatic, Weight = 0.01f }
        };
    }
}

#endregion
