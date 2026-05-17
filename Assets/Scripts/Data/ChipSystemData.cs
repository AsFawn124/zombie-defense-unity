using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 芯片/符文系统数据结构 - Phase 2.2
/// </summary>

#region 芯片类型与品质

/// <summary>
/// 芯片大类
/// </summary>
public enum ChipCategory
{
    Attack,     // 攻击类芯片
    Defense,    // 防御类芯片
    Utility     // 功能类芯片
}

/// <summary>
/// 芯片子类型
/// </summary>
public enum ChipSubType
{
    // 攻击类
    PhysicalAttack,         // 物理攻击
    ElementalAttack,        // 元素攻击
    CritChance,             // 暴击率
    CritDamage,             // 暴击伤害
    AttackSpeed,            // 攻击速度
    DamageAmp,              // 伤害增幅

    // 防御类
    MaxHealth,              // 最大生命
    Defense,                // 防御力
    DamageReduction,        // 伤害减免
    HealthRegen,            // 生命恢复
    Shield,                 // 护盾
    ElementalResistance,    // 元素抗性

    // 功能类
    CooldownReduction,      // 冷却缩减
    RangeIncrease,          // 射程提升
    GoldBonus,              // 金币加成
    ExpBonus,               // 经验加成
    Lifesteal,              // 生命偷取
    SplashDamage            // 溅射伤害
}

/// <summary>
/// 芯片品质
/// </summary>
public enum ChipQuality
{
    Normal = 1,     // 普通 - 绿色
    Rare = 2,       // 稀有 - 蓝色
    Epic = 3,       // 史诗 - 紫色
    Legendary = 4   // 传说 - 橙色
}

#endregion

#region 套装

/// <summary>
/// 套装定义
/// </summary>
[Serializable]
public class ChipSetBonus
{
    public string SetId;                    // 套装ID
    public string SetName;                  // 套装名称
    public ChipCategory SetCategory;        // 所属大类
    public string SetDescription;           // 套装描述
    public int[] RequiredPieces;            // 激活所需件数（如{2,4,6}）

    // 各级套装效果
    [Serializable]
    public class SetEffect
    {
        public int Pieces;                  // 触发件数
        public string EffectDescription;    // 效果描述
        public AffixValueType ValueType;    // 数值类型
        public float Value;                 // 效果数值
        public AffixType EffectType;        // 效果属性类型
    }

    public List<SetEffect> Effects;         // 套装效果列表

    /// <summary>
    /// 获取指定件数的效果
    /// </summary>
    public SetEffect GetEffect(int pieces)
    {
        foreach (var effect in Effects)
        {
            if (effect.Pieces == pieces)
                return effect;
        }
        return null;
    }

    /// <summary>
    /// 获取可触发的最大效果
    /// </summary>
    public SetEffect GetMaxEffect(int currentPieces)
    {
        SetEffect maxEffect = null;
        foreach (var effect in Effects)
        {
            if (effect.Pieces <= currentPieces)
                maxEffect = effect;
        }
        return maxEffect;
    }
}

#endregion

#region 芯片实例

/// <summary>
/// 芯片基础定义（配置表用）
/// </summary>
[Serializable]
public class ChipDef
{
    public string ChipId;                   // 芯片ID
    public string ChipName;                 // 芯片名称
    public ChipCategory Category;           // 大类
    public ChipSubType SubType;             // 子类型
    public ChipQuality Quality;             // 品质
    public string SetId;                    // 所属套装ID
    public string Description;              // 描述

    // 基础数值
    public float BaseValue;                 // 基础数值
    public float GrowthPerLevel;            // 每级成长
    public AffixValueType ValueType;        // 数值类型
    public AffixType AffectAttribute;       // 影响的属性

    // 限制
    public int MaxLevel;                    // 最大等级
    public List<EquipmentSlotType> AllowedSlots;  // 可镶嵌的装备部位

    public Sprite Icon;                     // 图标
}

/// <summary>
/// 芯片实例（玩家拥有的实际芯片）
/// </summary>
[Serializable]
public class ChipInstance
{
    public string InstanceId;               // 实例ID
    public string ChipId;                   // 芯片定义ID
    public string ChipName;                 // 芯片名称
    public ChipCategory Category;           // 大类
    public ChipSubType SubType;             // 子类型
    public ChipQuality Quality;             // 品质
    public string SetId;                    // 所属套装ID
    public int Level;                       // 当前等级
    public int MaxLevel;                    // 最大等级
    public float CurrentValue;              // 当前数值
    public float GrowthPerLevel;            // 每级成长值
    public AffixValueType ValueType;        // 数值类型
    public AffixType AffectAttribute;       // 影响的属性

    // 镶嵌状态
    public bool IsEmbedded;                 // 是否已镶嵌
    public string EmbeddedEquipmentId;      // 镶嵌的装备ID
    public int EmbeddedSlotIndex;           // 镶嵌的槽位索引

    public ChipInstance()
    {
        InstanceId = Guid.NewGuid().ToString("N");
        Level = 1;
        IsEmbedded = false;
        EmbeddedEquipmentId = string.Empty;
        EmbeddedSlotIndex = -1;
    }

    /// <summary>
    /// 获取升级后的数值
    /// </summary>
    public float GetValueAtLevel(int level)
    {
        return GameMath.CalculateChipValue(BaseValue: 0, GrowthPerLevel, level);
    }

    public static class GameMath
    {
        public static float CalculateChipValue(float BaseValue, float growthPerLevel, int level)
        {
            return growthPerLevel * level;
        }
    }

    /// <summary>
    /// 获取当前效果描述
    /// </summary>
    public string GetEffectDescription()
    {
        string valStr = ValueType == AffixValueType.Percentage
            ? $"{CurrentValue * 100:F1}%"
            : $"{CurrentValue:F1}";

        return $"{GetSubTypeName(SubType)} +{valStr}";
    }

    /// <summary>
    /// 获取品质颜色
    /// </summary>
    public static Color GetQualityColor(ChipQuality quality)
    {
        switch (quality)
        {
            case ChipQuality.Normal: return Color.green;
            case ChipQuality.Rare: return new Color(0.3f, 0.5f, 1.0f);
            case ChipQuality.Epic: return new Color(0.7f, 0.3f, 1.0f);
            case ChipQuality.Legendary: return new Color(1.0f, 0.6f, 0.0f);
            default: return Color.white;
        }
    }

    public static string GetQualityName(ChipQuality quality)
    {
        switch (quality)
        {
            case ChipQuality.Normal: return "普通";
            case ChipQuality.Rare: return "稀有";
            case ChipQuality.Epic: return "史诗";
            case ChipQuality.Legendary: return "传说";
            default: return "未知";
        }
    }

    public static string GetSubTypeName(ChipSubType subType)
    {
        switch (subType)
        {
            case ChipSubType.PhysicalAttack: return "物理攻击";
            case ChipSubType.ElementalAttack: return "元素攻击";
            case ChipSubType.CritChance: return "暴击率";
            case ChipSubType.CritDamage: return "暴击伤害";
            case ChipSubType.AttackSpeed: return "攻击速度";
            case ChipSubType.DamageAmp: return "伤害增幅";
            case ChipSubType.MaxHealth: return "最大生命";
            case ChipSubType.Defense: return "防御力";
            case ChipSubType.DamageReduction: return "伤害减免";
            case ChipSubType.HealthRegen: return "生命恢复";
            case ChipSubType.Shield: return "护盾";
            case ChipSubType.ElementalResistance: return "元素抗性";
            case ChipSubType.CooldownReduction: return "冷却缩减";
            case ChipSubType.RangeIncrease: return "射程提升";
            case ChipSubType.GoldBonus: return "金币加成";
            case ChipSubType.ExpBonus: return "经验加成";
            case ChipSubType.Lifesteal: return "生命偷取";
            case ChipSubType.SplashDamage: return "溅射伤害";
            default: return "未知";
        }
    }

    public static string GetCategoryName(ChipCategory category)
    {
        switch (category)
        {
            case ChipCategory.Attack: return "攻击";
            case ChipCategory.Defense: return "防御";
            case ChipCategory.Utility: return "功能";
            default: return "未知";
        }
    }

    public ChipInstance Clone()
    {
        return new ChipInstance
        {
            InstanceId = this.InstanceId,
            ChipId = this.ChipId,
            ChipName = this.ChipName,
            Category = this.Category,
            SubType = this.SubType,
            Quality = this.Quality,
            SetId = this.SetId,
            Level = this.Level,
            MaxLevel = this.MaxLevel,
            CurrentValue = this.CurrentValue,
            GrowthPerLevel = this.GrowthPerLevel,
            ValueType = this.ValueType,
            AffectAttribute = this.AffectAttribute,
            IsEmbedded = this.IsEmbedded,
            EmbeddedEquipmentId = this.EmbeddedEquipmentId,
            EmbeddedSlotIndex = this.EmbeddedSlotIndex
        };
    }
}

#endregion

#region 芯片升级

/// <summary>
/// 芯片升级配方
/// </summary>
[Serializable]
public class ChipUpgradeRecipe
{
    public ChipQuality Quality;             // 芯片品质
    public int FromLevel;                   // 起始等级
    public int ToLevel;                     // 目标等级
    public int GoldCost;                    // 金币消耗
    public int ChipsRequired;               // 需要同类芯片数
    public float SuccessRate;               // 成功率（1.0=100%）
}

/// <summary>
/// 芯片升级结果
/// </summary>
[Serializable]
public class ChipUpgradeResult
{
    public bool Success;
    public int OldLevel;
    public int NewLevel;
    public float OldValue;
    public float NewValue;
    public int GoldCost;
    public int ChipsConsumed;
    public string Message;
}

#endregion

#region 芯片背包保存

/// <summary>
/// 芯片背包保存数据
/// </summary>
[Serializable]
public class ChipSaveData
{
    public List<ChipInstance> OwnedChips;       // 拥有的芯片
    public Dictionary<string, List<string>> EmbeddedChips; // 装备ID -> 芯片实例ID列表
    public DateTime LastSaveTime;

    public ChipSaveData()
    {
        OwnedChips = new List<ChipInstance>();
        EmbeddedChips = new Dictionary<string, List<string>>();
    }
}

#endregion

#region 芯片配置 ScriptableObject

/// <summary>
/// 芯片系统总配置
/// </summary>
[CreateAssetMenu(fileName = "ChipConfig", menuName = "Game/Chip/Chip Config")]
public class ChipSystemConfig : ScriptableObject
{
    [Header("芯片定义")]
    public List<ChipDef> ChipDefinitions;

    [Header("套装定义")]
    public List<ChipSetBonus> SetBonuses;

    [Header("升级配置")]
    public List<ChipUpgradeRecipe> UpgradeRecipes;
    public int BaseUpgradeGoldCost = 500;

    [Header("掉落配置")]
    public float ChipDropRate = 0.15f;          // 芯片掉落概率(15%)
    public int MaxDropPerWave = 1;

    [Header("镶嵌配置")]
    public int EmbedCost = 200;                 // 镶嵌金币消耗
    public int RemoveCost = 100;                // 拆卸金币消耗
    public bool DestroyOnRemove = false;        // 拆卸是否销毁芯片

    /// <summary>
    /// 获取芯片定义
    /// </summary>
    public ChipDef GetChipDef(string chipId)
    {
        return ChipDefinitions.Find(c => c.ChipId == chipId);
    }

    /// <summary>
    /// 获取套装定义
    /// </summary>
    public ChipSetBonus GetSetBonus(string setId)
    {
        return SetBonuses.Find(s => s.SetId == setId);
    }

    /// <summary>
    /// 获取升级配方
    /// </summary>
    public ChipUpgradeRecipe GetUpgradeRecipe(ChipQuality quality, int currentLevel)
    {
        return UpgradeRecipes.Find(r => r.Quality == quality && r.FromLevel == currentLevel);
    }
}

#endregion
