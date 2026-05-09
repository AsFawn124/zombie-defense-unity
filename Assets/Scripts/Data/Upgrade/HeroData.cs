using UnityEngine;
using System;
using System.Collections.Generic;

namespace ZombieDefense.Upgrade.Data
{
    /// <summary>
    /// 英雄类型
    /// </summary>
    public enum HeroType
    {
        Warrior,    // 重装战士（坦克型）
        Sniper,     // 狙击手（输出型）
        Engineer,   // 工程师（辅助型）
        Mage        // 法师（群攻型）
    }

    /// <summary>
    /// 英雄属性
    /// </summary>
    [Serializable]
    public class HeroStats
    {
        public float MaxHealth;
        public float CurrentHealth;
        public float AttackDamage;
        public float AttackRange;
        public float AttackSpeed;
        public float MoveSpeed;
        public float Defense;
        public float HealthRegen;           // 生命恢复
        public float CritChance;            // 暴击率
        public float CritDamage;            // 暴击伤害
        public float CooldownReduction;     // 冷却缩减

        // 元素属性
        public ElementType PrimaryElement;
        public float ElementalMastery;      // 元素精通
        public float ElementalResistance;   // 元素抗性

        public HeroStats Clone()
        {
            return new HeroStats
            {
                MaxHealth = this.MaxHealth,
                CurrentHealth = this.CurrentHealth,
                AttackDamage = this.AttackDamage,
                AttackRange = this.AttackRange,
                AttackSpeed = this.AttackSpeed,
                MoveSpeed = this.MoveSpeed,
                Defense = this.Defense,
                HealthRegen = this.HealthRegen,
                CritChance = this.CritChance,
                CritDamage = this.CritDamage,
                CooldownReduction = this.CooldownReduction,
                PrimaryElement = this.PrimaryElement,
                ElementalMastery = this.ElementalMastery,
                ElementalResistance = this.ElementalResistance
            };
        }
    }

    /// <summary>
    /// 英雄技能数据
    /// </summary>
    [Serializable]
    public class HeroSkillData
    {
        public string SkillId;
        public string SkillName;
        public string Description;
        public Sprite SkillIcon;

        [Header("技能属性")]
        public SkillType SkillType;
        public float Damage;
        public float Range;
        public float Cooldown;
        public float Duration;
        public float ManaCost;
        public int MaxCharges;              // 最大充能次数

        [Header("元素相关")]
        public ElementType ElementType;
        public bool ApplyElementalEffect;
        public ElementalStatusEffect StatusEffect;

        [Header("特效")]
        public GameObject CastEffect;
        public GameObject HitEffect;
        public AudioClip CastSound;

        [Header("升级")]
        public int MaxLevel = 5;
        public float DamagePerLevel = 1.2f;
        public float CooldownReductionPerLevel = 0.95f;
    }

    /// <summary>
    /// 英雄装备槽位
    /// </summary>
    public enum EquipmentSlot
    {
        Weapon,     // 武器
        Armor,      // 护甲
        Helmet,     // 头盔
        Boots,      // 靴子
        Accessory1, // 饰品1
        Accessory2  // 饰品2
    }

    /// <summary>
    /// 装备数据
    /// </summary>
    [Serializable]
    public class EquipmentData
    {
        public string EquipmentId;
        public string EquipmentName;
        public EquipmentSlot Slot;
        public int Quality;                 // 品质（1-6：白绿蓝紫橙红）
        public int Level;

        [Header("基础属性")]
        public float HealthBonus;
        public float AttackBonus;
        public float DefenseBonus;
        public float SpeedBonus;

        [Header("特殊属性")]
        public float CritChanceBonus;
        public float CritDamageBonus;
        public float CooldownReductionBonus;
        public float ElementalMasteryBonus;

        [Header("词条")]
        public List<EquipmentAffix> Affixes;

        [Header("视觉")]
        public Sprite EquipmentIcon;
        public GameObject EquipmentModel;
    }

    [Serializable]
    public class EquipmentAffix
    {
        public string AffixName;
        public AffixType Type;
        public float Value;
        public bool IsPercentage;
    }

    public enum AffixType
    {
        Health,
        Attack,
        Defense,
        Speed,
        CritChance,
        CritDamage,
        CooldownReduction,
        ElementalMastery,
        ElementalResistance,
        LifeSteal,
        DamageReduction
    }

    /// <summary>
    /// 英雄数据
    /// </summary>
    [Serializable]
    public class HeroData
    {
        public string HeroId;
        public string HeroName;
        public HeroType HeroType;
        public string Description;
        public Sprite HeroPortrait;
        public GameObject HeroPrefab;

        [Header("基础属性")]
        public HeroStats BaseStats;

        [Header("技能")]
        public HeroSkillData[] Skills;      // 4个技能（QWER）

        [Header("成长")]
        public float HealthGrowth;
        public float AttackGrowth;
        public float DefenseGrowth;
        public int MaxLevel;
        public int CurrentLevel;
        public int CurrentExp;
        public int[] ExpToLevel;            // 每级所需经验

        [Header("装备")]
        public Dictionary<EquipmentSlot, EquipmentData> EquippedItems;

        public HeroData()
        {
            Skills = new HeroSkillData[4];
            EquippedItems = new Dictionary<EquipmentSlot, EquipmentData>();
        }
    }

    /// <summary>
    /// 英雄配置
    /// </summary>
    [CreateAssetMenu(fileName = "HeroConfig", menuName = "Game/Upgrade/Hero Config")]
    public class HeroConfig : ScriptableObject
    {
        [Header("英雄配置")]
        public List<HeroData> HeroDataList;

        [Header("装备配置")]
        public List<EquipmentData> EquipmentDataList;

        [Header("经验配置")]
        public int[] LevelExpRequirements = new int[] { 100, 200, 400, 800, 1600, 3200, 6400 };

        [Header("品质颜色")]
        public Color[] QualityColors = new Color[]
        {
            Color.white,        // 白
            Color.green,        // 绿
            Color.blue,         // 蓝
            Color.magenta,      // 紫
            Color.yellow,       // 橙
            Color.red           // 红
        };

        /// <summary>
        /// 获取英雄数据
        /// </summary>
        public HeroData GetHeroData(string heroId)
        {
            return HeroDataList.Find(h => h.HeroId == heroId)?.Clone();
        }

        /// <summary>
        /// 获取装备数据
        /// </summary>
        public EquipmentData GetEquipmentData(string equipmentId)
        {
            return EquipmentDataList.Find(e => e.EquipmentId == equipmentId);
        }

        /// <summary>
        /// 获取品质颜色
        /// </summary>
        public Color GetQualityColor(int quality)
        {
            if (quality >= 0 && quality < QualityColors.Length)
                return QualityColors[quality];
            return Color.white;
        }
    }

    public static class HeroDataExtensions
    {
        public static HeroData Clone(this HeroData source)
        {
            return new HeroData
            {
                HeroId = source.HeroId,
                HeroName = source.HeroName,
                HeroType = source.HeroType,
                Description = source.Description,
                HeroPortrait = source.HeroPortrait,
                HeroPrefab = source.HeroPrefab,
                BaseStats = source.BaseStats.Clone(),
                Skills = source.Skills,
                HealthGrowth = source.HealthGrowth,
                AttackGrowth = source.AttackGrowth,
                DefenseGrowth = source.DefenseGrowth,
                MaxLevel = source.MaxLevel,
                CurrentLevel = source.CurrentLevel,
                CurrentExp = source.CurrentExp,
                ExpToLevel = source.ExpToLevel,
                EquippedItems = new Dictionary<EquipmentSlot, EquipmentData>()
            };
        }
    }
}