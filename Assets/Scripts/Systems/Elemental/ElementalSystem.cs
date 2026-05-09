using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ZombieDefense.Upgrade.Data;

namespace ZombieDefense.Upgrade.Systems.Elemental
{
    /// <summary>
    /// 元素反应结果
    /// </summary>
    public class ElementalReactionResult
    {
        public ElementalReactionType ReactionType;
        public float FinalDamage;
        public float AreaRadius;
        public ElementalStatusEffect AppliedEffect;
        public Vector3 Position;
        public bool IsCritical;
    }

    /// <summary>
    /// 元素系统 - 管理元素反应和状态效果
    /// </summary>
    public class ElementalSystem : MonoBehaviour
    {
        public static ElementalSystem Instance { get; private set; }

        [Header("配置")]
        public ElementalTowerConfig Config;

        [Header("调试")]
        public bool ShowDebugLogs = true;

        // 元素附着记录（敌人 -> 元素 -> 剩余时间）
        private Dictionary<int, Dictionary<ElementType, float>> enemyElementTimers;
        private Dictionary<int, ElementalStatusEffect> enemyStatusEffects;

        // 事件
        public event Action<ElementalReactionResult, int> OnElementalReaction;
        public event Action<int, ElementType, float> OnElementApplied;
        public event Action<int, ElementalStatusEffect> OnStatusEffectApplied;
        public event Action<int, ElementalStatusEffect> OnStatusEffectRemoved;

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
            enemyElementTimers = new Dictionary<int, Dictionary<ElementType, float>>();
            enemyStatusEffects = new Dictionary<int, ElementalStatusEffect>();

            if (Config == null)
            {
                Config = ScriptableObject.CreateInstance<ElementalTowerConfig>();
                InitializeDefaultConfig();
            }
        }

        private void Update()
        {
            UpdateElementTimers();
            UpdateStatusEffects();
        }

        #region 元素附着

        /// <summary>
        /// 对敌人施加元素
        /// </summary>
        public void ApplyElement(int enemyId, ElementType element, float duration = 5f)
        {
            if (element == ElementType.None) return;

            // 检查是否已存在其他元素，触发反应
            if (enemyElementTimers.TryGetValue(enemyId, out var elements))
            {
                foreach (var existingElement in elements.Keys.ToList())
                {
                    if (existingElement != element)
                    {
                        var reaction = TriggerReaction(enemyId, existingElement, element);
                        if (reaction != null)
                        {
                            // 反应后清除原有元素
                            elements.Remove(existingElement);
                        }
                    }
                }
            }
            else
            {
                enemyElementTimers[enemyId] = new Dictionary<ElementType, float>();
            }

            // 添加或更新元素附着
            enemyElementTimers[enemyId][element] = duration;

            OnElementApplied?.Invoke(enemyId, element, duration);

            if (ShowDebugLogs)
                Debug.Log($"[Elemental] 敌人 {enemyId} 被施加 {element} 元素，持续 {duration} 秒");
        }

        /// <summary>
        /// 检查敌人是否有元素附着
        /// </summary>
        public bool HasElement(int enemyId, ElementType element)
        {
            return enemyElementTimers.TryGetValue(enemyId, out var elements) && elements.ContainsKey(element);
        }

        /// <summary>
        /// 获取敌人的所有元素附着
        /// </summary>
        public Dictionary<ElementType, float> GetEnemyElements(int enemyId)
        {
            if (enemyElementTimers.TryGetValue(enemyId, out var elements))
                return new Dictionary<ElementType, float>(elements);
            return new Dictionary<ElementType, float>();
        }

        /// <summary>
        /// 清除敌人的元素附着
        /// </summary>
        public void ClearElements(int enemyId)
        {
            enemyElementTimers.Remove(enemyId);
        }

        #endregion

        #region 元素反应

        /// <summary>
        /// 触发元素反应
        /// </summary>
        public ElementalReactionResult TriggerReaction(int enemyId, ElementType elem1, ElementType elem2, float baseDamage = 0, Vector3 position = default)
        {
            var reactionData = Config.GetReactionData(elem1, elem2);
            if (reactionData == null)
            {
                if (ShowDebugLogs)
                    Debug.Log($"[Elemental] {elem1} 和 {elem2} 之间没有反应");
                return null;
            }

            var result = new ElementalReactionResult
            {
                ReactionType = reactionData.ReactionType,
                FinalDamage = baseDamage * reactionData.DamageMultiplier,
                AreaRadius = reactionData.AreaRadius,
                Position = position,
                IsCritical = reactionData.DamageMultiplier >= 2f
            };

            // 根据反应类型应用特殊效果
            switch (reactionData.ReactionType)
            {
                case ElementalReactionType.Vaporize:
                case ElementalReactionType.Melt:
                    // 伤害倍率已在上面计算
                    break;

                case ElementalReactionType.Overload:
                    // 范围爆炸
                    ApplyAreaDamage(position, reactionData.AreaRadius, result.FinalDamage);
                    break;

                case ElementalReactionType.ElectroCharge:
                    // 连锁伤害
                    ApplyChainDamage(enemyId, 3, result.FinalDamage * 0.5f);
                    break;

                case ElementalReactionType.Superconduct:
                    // 减防效果
                    result.AppliedEffect = new ElementalStatusEffect
                    {
                        ElementType = ElementType.Ice,
                        Duration = reactionData.Duration,
                        DefenseModifier = 0.5f
                    };
                    ApplyStatusEffect(enemyId, result.AppliedEffect);
                    break;

                case ElementalReactionType.Swirl:
                    // 扩散效果
                    SpreadElement(enemyId, elem1 == ElementType.Wind ? elem2 : elem1, reactionData.AreaRadius);
                    break;

                case ElementalReactionType.Burning:
                    // 持续燃烧
                    result.AppliedEffect = new ElementalStatusEffect
                    {
                        ElementType = ElementType.Fire,
                        Duration = reactionData.Duration,
                        TickInterval = 0.5f,
                        DamagePerTick = result.FinalDamage * 0.1f
                    };
                    ApplyStatusEffect(enemyId, result.AppliedEffect);
                    break;

                case ElementalReactionType.Frozen:
                    // 冰冻定身
                    result.AppliedEffect = new ElementalStatusEffect
                    {
                        ElementType = ElementType.Ice,
                        Duration = reactionData.Duration,
                        MoveSpeedModifier = 0f,
                        IsStunned = true
                    };
                    ApplyStatusEffect(enemyId, result.AppliedEffect);
                    break;

                case ElementalReactionType.PoisonCloud:
                    // 毒雾范围
                    CreatePoisonCloud(position, reactionData.AreaRadius, reactionData.Duration);
                    break;
            }

            OnElementalReaction?.Invoke(result, enemyId);

            if (ShowDebugLogs)
                Debug.Log($"[Elemental] 触发反应: {reactionData.ReactionType}，伤害: {result.FinalDamage}");

            return result;
        }

        #endregion

        #region 状态效果

        /// <summary>
        /// 应用状态效果
        /// </summary>
        public void ApplyStatusEffect(int enemyId, ElementalStatusEffect effect)
        {
            enemyStatusEffects[enemyId] = effect;
            OnStatusEffectApplied?.Invoke(enemyId, effect);

            if (ShowDebugLogs)
                Debug.Log($"[Elemental] 敌人 {enemyId} 获得状态效果: {effect.ElementType}，持续 {effect.Duration} 秒");
        }

        /// <summary>
        /// 获取敌人的状态效果
        /// </summary>
        public ElementalStatusEffect GetStatusEffect(int enemyId)
        {
            enemyStatusEffects.TryGetValue(enemyId, out var effect);
            return effect;
        }

        /// <summary>
        /// 移除状态效果
        /// </summary>
        public void RemoveStatusEffect(int enemyId)
        {
            if (enemyStatusEffects.TryGetValue(enemyId, out var effect))
            {
                enemyStatusEffects.Remove(enemyId);
                OnStatusEffectRemoved?.Invoke(enemyId, effect);
            }
        }

        /// <summary>
        /// 更新状态效果
        /// </summary>
        private void UpdateStatusEffects()
        {
            var enemiesToRemove = new List<int>();

            foreach (var kvp in enemyStatusEffects)
            {
                var effect = kvp.Value;
                effect.Duration -= Time.deltaTime;

                // 持续伤害
                if (effect.DamagePerTick > 0 && effect.TickInterval > 0)
                {
                    // TODO: 实现DOT伤害
                }

                if (effect.Duration <= 0)
                {
                    enemiesToRemove.Add(kvp.Key);
                }
            }

            foreach (var enemyId in enemiesToRemove)
            {
                RemoveStatusEffect(enemyId);
            }
        }

        #endregion

        #region 特殊效果实现

        /// <summary>
        /// 范围伤害
        /// </summary>
        private void ApplyAreaDamage(Vector3 center, float radius, float damage)
        {
            // TODO: 实现范围伤害检测
            if (ShowDebugLogs)
                Debug.Log($"[Elemental] 范围伤害: 中心 {center}，半径 {radius}，伤害 {damage}");
        }

        /// <summary>
        /// 连锁伤害
        /// </summary>
        private void ApplyChainDamage(int startEnemyId, int chainCount, float damage)
        {
            // TODO: 实现连锁伤害
            if (ShowDebugLogs)
                Debug.Log($"[Elemental] 连锁伤害: 起始敌人 {startEnemyId}，连锁 {chainCount} 次，伤害 {damage}");
        }

        /// <summary>
        /// 扩散元素
        /// </summary>
        private void SpreadElement(int sourceEnemyId, ElementType element, float radius)
        {
            // TODO: 实现元素扩散
            if (ShowDebugLogs)
                Debug.Log($"[Elemental] 扩散元素: 来源 {sourceEnemyId}，元素 {element}，半径 {radius}");
        }

        /// <summary>
        /// 创建毒雾
        /// </summary>
        private void CreatePoisonCloud(Vector3 position, float radius, float duration)
        {
            // TODO: 实现毒雾区域
            if (ShowDebugLogs)
                Debug.Log($"[Elemental] 创建毒雾: 位置 {position}，半径 {radius}，持续 {duration} 秒");
        }

        #endregion

        #region 更新和清理

        /// <summary>
        /// 更新元素附着计时器
        /// </summary>
        private void UpdateElementTimers()
        {
            var enemiesToRemove = new List<int>();

            foreach (var kvp in enemyElementTimers)
            {
                var elements = kvp.Value;
                var elementsToRemove = new List<ElementType>();

                foreach (var elemKvp in elements)
                {
                    float newTime = elemKvp.Value - Time.deltaTime;
                    if (newTime <= 0)
                    {
                        elementsToRemove.Add(elemKvp.Key);
                    }
                    else
                    {
                        elements[elemKvp.Key] = newTime;
                    }
                }

                foreach (var elem in elementsToRemove)
                {
                    elements.Remove(elem);
                }

                if (elements.Count == 0)
                {
                    enemiesToRemove.Add(kvp.Key);
                }
            }

            foreach (var enemyId in enemiesToRemove)
            {
                enemyElementTimers.Remove(enemyId);
            }
        }

        /// <summary>
        /// 清理所有数据
        /// </summary>
        public void ClearAll()
        {
            enemyElementTimers.Clear();
            enemyStatusEffects.Clear();
        }

        #endregion

        #region 配置初始化

        /// <summary>
        /// 初始化默认配置
        /// </summary>
        private void InitializeDefaultConfig()
        {
            Config.ReactionDataList = new List<ElementalReactionData>
            {
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.Vaporize,
                    PrimaryElement = ElementType.Fire,
                    SecondaryElement = ElementType.Ice,
                    DamageMultiplier = 2f,
                    Description = "蒸发反应：火+冰，伤害×2"
                },
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.Overload,
                    PrimaryElement = ElementType.Fire,
                    SecondaryElement = ElementType.Electric,
                    DamageMultiplier = 1.5f,
                    AreaRadius = 3f,
                    Description = "超载反应：火+电，范围爆炸"
                },
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.Melt,
                    PrimaryElement = ElementType.Fire,
                    SecondaryElement = ElementType.Poison,
                    DamageMultiplier = 1.5f,
                    Description = "融化反应：火+毒，伤害×1.5"
                },
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.ElectroCharge,
                    PrimaryElement = ElementType.Electric,
                    SecondaryElement = ElementType.Ice,
                    DamageMultiplier = 1.2f,
                    Description = "感电反应：电+冰，连锁伤害"
                },
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.Superconduct,
                    PrimaryElement = ElementType.Electric,
                    SecondaryElement = ElementType.Wind,
                    DamageMultiplier = 1f,
                    Duration = 5f,
                    Description = "超导反应：电+风，减防50%"
                },
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.Swirl,
                    PrimaryElement = ElementType.Wind,
                    SecondaryElement = ElementType.Fire,
                    DamageMultiplier = 0.8f,
                    AreaRadius = 4f,
                    Description = "扩散反应：风+火，范围传播"
                },
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.Burning,
                    PrimaryElement = ElementType.Fire,
                    SecondaryElement = ElementType.Poison,
                    DamageMultiplier = 1f,
                    Duration = 6f,
                    Description = "燃烧反应：火+毒，持续伤害"
                },
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.Frozen,
                    PrimaryElement = ElementType.Ice,
                    SecondaryElement = ElementType.Wind,
                    DamageMultiplier = 1f,
                    Duration = 3f,
                    Description = "冰冻反应：冰+风，定身"
                },
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.PoisonCloud,
                    PrimaryElement = ElementType.Poison,
                    SecondaryElement = ElementType.Wind,
                    DamageMultiplier = 1f,
                    AreaRadius = 5f,
                    Duration = 8f,
                    Description = "毒雾反应：毒+风，范围毒伤"
                }
            };

            Config.ColorConfigs = new ElementColorConfig[]
            {
                new ElementColorConfig { ElementType = ElementType.Fire, Color = new Color(1f, 0.3f, 0f) },
                new ElementColorConfig { ElementType = ElementType.Ice, Color = new Color(0.3f, 0.8f, 1f) },
                new ElementColorConfig { ElementType = ElementType.Electric, Color = new Color(1f, 1f, 0f) },
                new ElementColorConfig { ElementType = ElementType.Poison, Color = new Color(0.5f, 0f, 0.8f) },
                new ElementColorConfig { ElementType = ElementType.Wind, Color = new Color(0.5f, 1f, 0.5f) }
            };
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取元素颜色
        /// </summary>
        public Color GetElementColor(ElementType element)
        {
            return Config.GetElementColor(element);
        }

        /// <summary>
        /// 获取元素名称
        /// </summary>
        public string GetElementName(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return "火";
                case ElementType.Ice: return "冰";
                case ElementType.Electric: return "电";
                case ElementType.Poison: return "毒";
                case ElementType.Wind: return "风";
                default: return "无";
            }
        }

        #endregion
    }
}