using UnityEngine;
using System;
using System.Collections.Generic;

namespace ZombieDefense.Upgrade.Data
{
    /// <summary>
    /// 元素类型
    /// </summary>
    public enum ElementType
    {
        None,       // 无元素
        Fire,       // 火
        Ice,        // 冰
        Electric,   // 电
        Poison,     // 毒
        Wind        // 风
    }

    /// <summary>
    /// 元素反应类型
    /// </summary>
    public enum ElementalReactionType
    {
        None,
        Vaporize,       // 蒸发（火+水/冰）- 伤害×2
        Overload,       // 超载（火+电）- 范围爆炸
        Melt,           // 融化（火+冰）- 伤害×1.5
        ElectroCharge,  // 感电（电+水）- 连锁伤害
        Superconduct,   // 超导（电+冰）- 减防
        Swirl,          // 扩散（风+任意）- 范围传播
        Crystallize,    // 结晶（岩+任意）- 护盾（暂未实现）
        Burning,        // 燃烧（火+毒）- 持续伤害
        Frozen,         // 冰冻（冰+水）- 定身
        PoisonCloud     // 毒雾（毒+风）- 范围毒伤
    }

    /// <summary>
    /// 元素反应数据
    /// </summary>
    [Serializable]
    public class ElementalReactionData
    {
        public ElementalReactionType ReactionType;
        public ElementType PrimaryElement;      // 主元素
        public ElementType SecondaryElement;    // 副元素
        public float DamageMultiplier;          // 伤害倍率
        public float AreaRadius;                // 范围半径
        public float Duration;                  // 持续时间
        public string Description;              // 描述
        public GameObject ReactionEffectPrefab; // 特效预制体
    }

    /// <summary>
    /// 元素状态效果
    /// </summary>
    [Serializable]
    public class ElementalStatusEffect
    {
        public ElementType ElementType;
        public float Duration;                  // 持续时间
        public float TickInterval;              // 伤害间隔
        public float DamagePerTick;             // 每次伤害
        public float MoveSpeedModifier;         // 移速修正
        public float DefenseModifier;           // 防御修正
        public bool IsStunned;                  // 是否定身
        public GameObject EffectPrefab;         // 特效预制体
    }

    /// <summary>
    /// 元素塔技能数据
    /// </summary>
    [Serializable]
    public class ElementalTowerSkillData
    {
        public string SkillId;
        public string SkillName;
        public ElementType ElementType;
        public int Tier;                        // 技能阶级（1-3）
        public int Branch;                      // 分支（1-3）
        public string Description;
        public float Damage;
        public float Range;
        public float Cooldown;
        public float Duration;
        public ElementalStatusEffect StatusEffect;
        public GameObject SkillEffectPrefab;
        public Sprite SkillIcon;
    }

    /// <summary>
    /// 元素塔配置
    /// </summary>
    [CreateAssetMenu(fileName = "ElementalTowerConfig", menuName = "Game/Upgrade/Elemental Tower Config")]
    public class ElementalTowerConfig : ScriptableObject
    {
        [Header("元素反应配置")]
        public List<ElementalReactionData> ReactionDataList;

        [Header("元素塔技能配置")]
        public List<ElementalTowerSkillData> SkillDataList;

        [Header("元素特效配置")]
        public ElementalEffectConfig[] EffectConfigs;

        [Header("元素颜色配置")]
        public ElementColorConfig[] ColorConfigs;

        /// <summary>
        /// 获取元素反应数据
        /// </summary>
        public ElementalReactionData GetReactionData(ElementType elem1, ElementType elem2)
        {
            foreach (var reaction in ReactionDataList)
            {
                if ((reaction.PrimaryElement == elem1 && reaction.SecondaryElement == elem2) ||
                    (reaction.PrimaryElement == elem2 && reaction.SecondaryElement == elem1))
                {
                    return reaction;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取元素塔技能
        /// </summary>
        public List<ElementalTowerSkillData> GetSkillsByElement(ElementType elementType, int tier)
        {
            return SkillDataList.FindAll(s => s.ElementType == elementType && s.Tier == tier);
        }

        /// <summary>
        /// 获取元素颜色
        /// </summary>
        public Color GetElementColor(ElementType elementType)
        {
            foreach (var config in ColorConfigs)
            {
                if (config.ElementType == elementType)
                    return config.Color;
            }
            return Color.white;
        }
    }

    [Serializable]
    public class ElementalEffectConfig
    {
        public ElementType ElementType;
        public GameObject ProjectileEffect;
        public GameObject HitEffect;
        public GameObject AuraEffect;
    }

    [Serializable]
    public class ElementColorConfig
    {
        public ElementType ElementType;
        public Color Color;
    }
}