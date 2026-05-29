using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 扩展内容库 - 新增防御塔、技能、敌人类型
/// 塔分支: 基础5系 → 扩展至8系，每系3分支9级
/// 技能池: 12个 → 扩展至36个
/// 敌人: 7种 → 扩展至15种
/// </summary>
[CreateAssetMenu(fileName = "ContentExpansionData", menuName = "ZombieDefense/Content Expansion")]
public class ContentExpansionData : ScriptableObject
{
    public List<TowerBranchData> ExpandedTowers = new List<TowerBranchData>();
    public List<ExpandedSkillData> ExpandedSkills = new List<ExpandedSkillData>();
    public List<EnemyVariantData> NewEnemies = new List<EnemyVariantData>();
    public List<SynergyData> TowerSynergies = new List<TowerSynergyData>();
}

#region === 扩展防御塔 ===

/// <summary>
/// 8大塔系: 每个3分支9级
/// </summary>
[Serializable]
public class TowerBranchData
{
    public string BranchId;
    public TowerElement Element;
    public string BranchName;
    public string Description;
    public int GridSize = 1; // 1/2/4/L型
    public TowerGridShape GridShape;

    // 3条分支路线
    public TowerSubBranch OffensiveBranch;  // 攻击路线
    public TowerSubBranch DefensiveBranch;  // 控制/防御路线
    public TowerSubBranch UtilityBranch;    // 功能/辅助路线

    public int BaseCost = 100;
    public float BaseDamage = 50f;
    public float BaseRange = 5f;
    public float BaseFireRate = 1f;
}

public enum TowerElement
{
    Fire,       // 火 - 高伤灼烧
    Ice,        // 冰 - 减速冻结
    Lightning,  // 电 - 连锁弹射
    Poison,     // 毒 - 持续伤害
    Wind,       // 风 - 击退聚拢
    Light,      // 光 - 治疗增益  [新增]
    Shadow,     // 暗 - 偷取削弱  [新增]
    Earth       // 土 - 召唤阻挡  [新增]
}

public enum TowerGridShape
{
    Single,      // 1格 □
    Double,      // 2格 □□
    Quad,        // 4格 □□/□□
    LShape       // L型
}

[Serializable]
public class TowerSubBranch
{
    public string BranchName;
    public string Description;
    public List<TowerLevelData> Levels;

    [Serializable]
    public class TowerLevelData
    {
        public int Level;
        public string LevelName;
        public int UpgradeCost;
        public float DamageMultiplier;
        public float RangeMultiplier;
        public float FireRateMultiplier;
        public string SpecialEffect;    // 特殊效果描述
        public string SpecialEffectId;  // 对应EffectManager中的效果ID
    }
}

#endregion

#region === 扩展技能 ===

[Serializable]
public class ExpandedSkillData
{
    public string SkillId;
    public string SkillName;
    public SkillRarity Rarity;
    public SkillCategory Category;
    public int MaxStacks = 3;
    public string IconName;
    public string Description;

    // 基础效果
    public float BaseValue;
    public float PerStackValue;
}

public enum SkillRarity
{
    Common,     // 白色 60%出现率
    Rare,       // 蓝色 25%
    Epic,       // 紫色 10%
    Legendary,  // 橙色 4%
    Mythic      // 红色 1%
}

public enum SkillCategory
{
    // 伤害类
    DamageUp,
    CriticalStrike,
    AttackSpeed,
    PiercingShot,
    SplashDamage,
    Execute,

    // 防御类
    TowerHealth,
    RepairRate,
    Shield,
    DodgeChance,

    // 元素类
    FireMastery,
    IceMastery,
    LightningMastery,
    PoisonMastery,
    WindMastery,
    ElementalFusion,    // 元素融合 [新增]

    // 经济类
    GoldUp,
    Discount,
    DoubleReward,
    InterestRate,
    SellBonus,

    // 功能类
    ExtraTower,
    RangeUp,
    ProjectileSpeed,
    BounceShot,
    HomingShot,

    // 特殊类 (新增)
    Necromancy,         // 击杀敌人复活为己方
    TimeSlow,           // 时间减缓
    ChainLightning,     // 闪电链
    PoisonCloud,        // 毒雾
    Blizzard,           // 暴风雪
    Meteor,             // 陨石召唤
    CloneTower,         // 复制一座塔
    LifeSteal,          // 攻击吸血
    ThornArmor,         // 反伤
    LastStand           // 背水一战
}

#endregion

#region === 新敌人类型 ===

[Serializable]
public class EnemyVariantData
{
    public string EnemyId;
    public string EnemyName;
    public EnemyArchetype Archetype;
    public int WaveMinAppear;   // 最小出现波次
    public float BaseHp;
    public float Speed;
    public float Damage;
    public int GoldReward;
    public List<EnemyAbility> Abilities = new List<EnemyAbility>();
}

public enum EnemyArchetype
{
    // 已有
    Normal, Fast, Tank, Exploder, Healer, Splitter, Boss,

    // 新增
    Stealth,        // 潜行 - 隐身接近，防御塔无法锁定 [需探测塔]
    Shielder,       // 护盾 - 为周围敌人施加护盾 [优先击杀]
    Summoner,       // 召唤师 - 持续召唤小怪 [高威胁]
    Dasher,         // 冲刺 - 周期性加速冲刺 [需要减速]
    Regenerator,    // 回复 - 快速回血 [需要爆发]
    Reflector,      // 反射 - 反射子弹伤害 [需要近战/元素]
    Spliter,        // 分裂 - 死亡分裂成2个小型 [需要AOE]
    Hijacker        // 劫持 - 接近防御塔时暂时控制 [需要远程击杀]
}

[Serializable]
public class EnemyAbility
{
    public string AbilityId;
    public string AbilityName;
    public float Cooldown;
    public float Duration;
    public Dictionary<string, float> Params;
}

#endregion

#region === 塔协同系统 ===

/// <summary>
/// 防御塔协同效应 - 激励玩家组合不同塔系
/// </summary>
[Serializable]
public class TowerSynergyData
{
    public string SynergyId;
    public string SynergyName;
    public List<TowerElement> RequiredElements;
    public int RequiredCount; // 需要至少N座
    public string BonusDescription;
    public float BonusValue;
}

/// <summary>
/// 预定义协同组合
/// </summary>
public static class TowerSynergyPresets
{
    public static List<TowerSynergyData> Synergies = new List<TowerSynergyData>
    {
        // 2元素协同
        new TowerSynergyData
        {
            SynergyId = "flash_freeze",
            SynergyName = "急冻爆裂",
            RequiredElements = new List<TowerElement> { TowerElement.Fire, TowerElement.Ice },
            RequiredCount = 2,
            BonusDescription = "火+冰冻目标伤害翻倍",
            BonusValue = 2.0f
        },
        new TowerSynergyData
        {
            SynergyId = "storm_chain",
            SynergyName = "风暴连锁",
            RequiredElements = new List<TowerElement> { TowerElement.Wind, TowerElement.Lightning },
            RequiredCount = 2,
            BonusDescription = "连锁弹射范围+50%",
            BonusValue = 1.5f
        },
        new TowerSynergyData
        {
            SynergyId = "toxic_flame",
            SynergyName = "毒火燎原",
            RequiredElements = new List<TowerElement> { TowerElement.Poison, TowerElement.Fire },
            RequiredCount = 2,
            BonusDescription = "点燃中毒敌人造成扩散伤害",
            BonusValue = 1.3f
        },
        new TowerSynergyData
        {
            SynergyId = "light_shadow",
            SynergyName = "光暗交织",
            RequiredElements = new List<TowerElement> { TowerElement.Light, TowerElement.Shadow },
            RequiredCount = 2,
            BonusDescription = "治疗同时偷取敌人生命",
            BonusValue = 1.5f
        },
        new TowerSynergyData
        {
            SynergyId = "earth_wind",
            SynergyName = "飞沙走石",
            RequiredElements = new List<TowerElement> { TowerElement.Earth, TowerElement.Wind },
            RequiredCount = 2,
            BonusDescription = "召唤物死亡时造成范围击退",
            BonusValue = 2.0f
        },

        // 3元素协同
        new TowerSynergyData
        {
            SynergyId = "tri_element",
            SynergyName = "三元归一",
            RequiredElements = new List<TowerElement> { TowerElement.Fire, TowerElement.Ice, TowerElement.Lightning },
            RequiredCount = 3,
            BonusDescription = "全元素伤害+35%",
            BonusValue = 1.35f
        },
        new TowerSynergyData
        {
            SynergyId = "dark_triad",
            SynergyName = "暗黑三连",
            RequiredElements = new List<TowerElement> { TowerElement.Shadow, TowerElement.Poison, TowerElement.Earth },
            RequiredCount = 3,
            BonusDescription = "敌人全属性抗性-40%",
            BonusValue = 0.4f
        },

        // 4元素协同
        new TowerSynergyData
        {
            SynergyId = "elemental_harmony",
            SynergyName = "元素共鸣",
            RequiredElements = new List<TowerElement> { TowerElement.Fire, TowerElement.Ice, TowerElement.Lightning, TowerElement.Wind },
            RequiredCount = 4,
            BonusDescription = "所有塔攻击速度+50%",
            BonusValue = 1.5f
        },

        // 6元素协同 (全元素)
        new TowerSynergyData
        {
            SynergyId = "elemental_god",
            SynergyName = "元素之神",
            RequiredElements = new List<TowerElement>
            {
                TowerElement.Fire, TowerElement.Ice, TowerElement.Lightning,
                TowerElement.Poison, TowerElement.Wind, TowerElement.Light,
                TowerElement.Shadow, TowerElement.Earth
            },
            RequiredCount = 6,
            BonusDescription = "所有伤害翻倍，全元素反应触发概率+100%",
            BonusValue = 2.0f
        }
    };
}

#endregion

#region === 游戏难度等级 ===

[Serializable]
public class DifficultyConfig
{
    public enum Difficulty
    {
        Casual,     // 休闲 - 敌人血量×0.7
        Normal,     // 普通
        Hard,       // 困难 - 敌人血量×1.5, 更多敌人
        Nightmare,  // 噩梦 - 敌人血量×2, 更强AI, BOSS多阶段
        Hell        // 地狱 - 全敌人强化, BOSS狂暴, 资源减半
    }

    public float EnemyHpMultiplier;
    public float EnemySpeedMultiplier;
    public float EnemyCountMultiplier;
    public float GoldMultiplier;
    public float BossAbilityFrequency;
    public bool BossHasExtraPhase;
    public bool EnemiesCanHeal;
    public int StartingGold;
}

#endregion
