using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ZombieDefense.Upgrade.Data;

namespace ZombieDefense.Upgrade.Systems.Hero
{
    /// <summary>
    /// 英雄系统 - 管理英雄单位
    /// </summary>
    public class HeroSystem : MonoBehaviour
    {
        public static HeroSystem Instance { get; private set; }

        [Header("配置")]
        public HeroConfig Config;

        [Header("当前英雄")]
        public HeroData CurrentHero;

        [Header("运行时")]
        public HeroController ActiveHeroController;

        // 事件
        public event Action<HeroData> OnHeroSelected;
        public event Action<HeroData> OnHeroLevelUp;
        public event Action<EquipmentData, EquipmentSlot> OnEquipmentChanged;
        public event Action<HeroSkillData> OnSkillUsed;
        public event Action<float, float> OnHeroHealthChanged;
        public event Action<float, float> OnHeroManaChanged;

        // 运行时属性（计算装备加成后）
        private HeroStats currentStats;
        private float currentHealth;
        private float currentMana;
        private Dictionary<string, float> skillCooldowns;

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
            skillCooldowns = new Dictionary<string, float>();

            if (Config == null)
            {
                Config = ScriptableObject.CreateInstance<HeroConfig>();
            }
        }

        private void Update()
        {
            UpdateCooldowns();
            UpdateHealthRegen();
        }

        #region 英雄选择

        /// <summary>
        /// 选择英雄
        /// </summary>
        public void SelectHero(string heroId)
        {
            var heroData = Config?.GetHeroData(heroId);
            if (heroData == null)
            {
                Debug.LogError($"[Hero] 找不到英雄: {heroId}");
                return;
            }

            CurrentHero = heroData;
            RecalculateStats();
            currentHealth = currentStats.MaxHealth;
            currentMana = 100f; // 默认满蓝

            OnHeroSelected?.Invoke(CurrentHero);

            Debug.Log($"[Hero] 选择英雄: {heroData.HeroName}");
        }

        /// <summary>
        /// 创建英雄控制器
        /// </summary>
        public HeroController SpawnHero(Vector3 position)
        {
            if (CurrentHero?.HeroPrefab == null)
            {
                Debug.LogError("[Hero] 没有设置英雄预制体");
                return null;
            }

            GameObject heroObj = Instantiate(CurrentHero.HeroPrefab, position, Quaternion.identity);
            ActiveHeroController = heroObj.GetComponent<HeroController>();

            if (ActiveHeroController != null)
            {
                ActiveHeroController.Initialize(CurrentHero, currentStats);
            }

            return ActiveHeroController;
        }

        #endregion

        #region 属性计算

        /// <summary>
        /// 重新计算属性（应用装备加成）
        /// </summary>
        public void RecalculateStats()
        {
            if (CurrentHero == null) return;

            currentStats = CurrentHero.BaseStats.Clone();

            // 应用装备加成
            foreach (var kvp in CurrentHero.EquippedItems)
            {
                ApplyEquipmentStats(kvp.Value);
            }

            // 应用等级加成
            float levelBonus = 1 + (CurrentHero.CurrentLevel - 1) * 0.1f;
            currentStats.MaxHealth *= levelBonus;
            currentStats.AttackDamage *= levelBonus;
            currentStats.Defense *= levelBonus;
        }

        /// <summary>
        /// 应用装备属性
        /// </summary>
        private void ApplyEquipmentStats(EquipmentData equipment)
        {
            if (equipment == null) return;

            currentStats.MaxHealth += equipment.HealthBonus;
            currentStats.AttackDamage += equipment.AttackBonus;
            currentStats.Defense += equipment.DefenseBonus;
            currentStats.MoveSpeed += equipment.SpeedBonus;
            currentStats.CritChance += equipment.CritChanceBonus;
            currentStats.CritDamage += equipment.CritDamageBonus;
            currentStats.CooldownReduction += equipment.CooldownReductionBonus;
            currentStats.ElementalMastery += equipment.ElementalMasteryBonus;
        }

        /// <summary>
        /// 获取当前属性
        /// </summary>
        public HeroStats GetCurrentStats()
        {
            return currentStats;
        }

        #endregion

        #region 装备系统

        /// <summary>
        /// 装备物品
        /// </summary>
        public bool EquipItem(EquipmentData equipment)
        {
            if (CurrentHero == null || equipment == null)
                return false;

            // 卸下同槽位的装备
            if (CurrentHero.EquippedItems.ContainsKey(equipment.Slot))
            {
                UnequipItem(equipment.Slot);
            }

            CurrentHero.EquippedItems[equipment.Slot] = equipment;
            RecalculateStats();

            OnEquipmentChanged?.Invoke(equipment, equipment.Slot);

            Debug.Log($"[Hero] 装备 {equipment.EquipmentName} 到 {equipment.Slot}");
            return true;
        }

        /// <summary>
        /// 卸下装备
        /// </summary>
        public bool UnequipItem(EquipmentSlot slot)
        {
            if (CurrentHero == null)
                return false;

            if (CurrentHero.EquippedItems.TryGetValue(slot, out var equipment))
            {
                CurrentHero.EquippedItems.Remove(slot);
                RecalculateStats();

                OnEquipmentChanged?.Invoke(null, slot);

                Debug.Log($"[Hero] 卸下 {slot} 的装备");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取已装备物品
        /// </summary>
        public EquipmentData GetEquippedItem(EquipmentSlot slot)
        {
            if (CurrentHero?.EquippedItems != null &&
                CurrentHero.EquippedItems.TryGetValue(slot, out var equipment))
            {
                return equipment;
            }
            return null;
        }

        /// <summary>
        /// 获取所有已装备物品
        /// </summary>
        public Dictionary<EquipmentSlot, EquipmentData> GetAllEquippedItems()
        {
            return CurrentHero?.EquippedItems ?? new Dictionary<EquipmentSlot, EquipmentData>();
        }

        #endregion

        #region 技能系统

        /// <summary>
        /// 使用技能
        /// </summary>
        public bool UseSkill(int skillIndex, Vector3 targetPosition)
        {
            if (CurrentHero == null || skillIndex < 0 || skillIndex >= CurrentHero.Skills.Length)
                return false;

            var skill = CurrentHero.Skills[skillIndex];
            if (skill == null)
                return false;

            // 检查冷却
            if (IsSkillOnCooldown(skill.SkillId))
                return false;

            // 检查蓝量
            if (currentMana < skill.ManaCost)
                return false;

            // 消耗蓝量
            currentMana -= skill.ManaCost;
            OnHeroManaChanged?.Invoke(currentMana, 100f);

            // 设置冷却
            float cooldown = skill.Cooldown * (1 - currentStats.CooldownReduction);
            skillCooldowns[skill.SkillId] = cooldown;

            // 执行技能
            ExecuteSkill(skill, targetPosition);

            OnSkillUsed?.Invoke(skill);

            Debug.Log($"[Hero] 使用技能: {skill.SkillName}");
            return true;
        }

        /// <summary>
        /// 执行技能
        /// </summary>
        private void ExecuteSkill(HeroSkillData skill, Vector3 targetPosition)
        {
            // 播放特效
            if (skill.CastEffect != null && ActiveHeroController != null)
            {
                Instantiate(skill.CastEffect, ActiveHeroController.transform.position, Quaternion.identity);
            }

            // 播放音效
            if (skill.CastSound != null)
            {
                AudioSource.PlayClipAtPoint(skill.CastSound, targetPosition);
            }

            // TODO: 实现具体的技能效果（伤害、治疗、BUFF等）
        }

        /// <summary>
        /// 检查技能是否在冷却中
        /// </summary>
        public bool IsSkillOnCooldown(string skillId)
        {
            return skillCooldowns.ContainsKey(skillId) && skillCooldowns[skillId] > 0;
        }

        /// <summary>
        /// 获取技能冷却时间
        /// </summary>
        public float GetSkillCooldown(string skillId)
        {
            if (skillCooldowns.TryGetValue(skillId, out float cooldown))
                return cooldown;
            return 0;
        }

        /// <summary>
        /// 更新技能冷却
        /// </summary>
        private void UpdateCooldowns()
        {
            var keys = new List<string>(skillCooldowns.Keys);
            foreach (var key in keys)
            {
                skillCooldowns[key] -= Time.deltaTime;
                if (skillCooldowns[key] <= 0)
                {
                    skillCooldowns.Remove(key);
                }
            }
        }

        #endregion

        #region 生命值和蓝量

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (currentStats == null) return;

            float actualDamage = Mathf.Max(1, damage - currentStats.Defense);
            currentHealth -= actualDamage;
            currentHealth = Mathf.Max(0, currentHealth);

            OnHeroHealthChanged?.Invoke(currentHealth, currentStats.MaxHealth);

            if (currentHealth <= 0)
            {
                OnHeroDeath();
            }
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(float amount)
        {
            if (currentStats == null) return;

            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, currentStats.MaxHealth);

            OnHeroHealthChanged?.Invoke(currentHealth, currentStats.MaxHealth);
        }

        /// <summary>
        /// 恢复蓝量
        /// </summary>
        public void RestoreMana(float amount)
        {
            currentMana += amount;
            currentMana = Mathf.Min(currentMana, 100f);

            OnHeroManaChanged?.Invoke(currentMana, 100f);
        }

        /// <summary>
        /// 更新生命恢复
        /// </summary>
        private void UpdateHealthRegen()
        {
            if (currentStats != null && currentStats.HealthRegen > 0 && currentHealth < currentStats.MaxHealth)
            {
                currentHealth += currentStats.HealthRegen * Time.deltaTime;
                currentHealth = Mathf.Min(currentHealth, currentStats.MaxHealth);
                OnHeroHealthChanged?.Invoke(currentHealth, currentStats.MaxHealth);
            }
        }

        /// <summary>
        /// 英雄死亡
        /// </summary>
        private void OnHeroDeath()
        {
            Debug.Log($"[Hero] 英雄 {CurrentHero?.HeroName} 死亡");
            // TODO: 实现复活机制或游戏结束
        }

        /// <summary>
        /// 复活英雄
        /// </summary>
        public void ReviveHero(float healthPercent = 0.5f)
        {
            if (currentStats != null)
            {
                currentHealth = currentStats.MaxHealth * healthPercent;
                OnHeroHealthChanged?.Invoke(currentHealth, currentStats.MaxHealth);
            }
        }

        #endregion

        #region 经验值和升级

        /// <summary>
        /// 获得经验值
        /// </summary>
        public void GainExperience(int exp)
        {
            if (CurrentHero == null) return;

            CurrentHero.CurrentExp += exp;

            // 检查升级
            while (CurrentHero.CurrentLevel < CurrentHero.MaxLevel &&
                   CurrentHero.CurrentExp >= GetExpToNextLevel())
            {
                LevelUp();
            }
        }

        /// <summary>
        /// 升级
        /// </summary>
        private void LevelUp()
        {
            if (CurrentHero == null) return;

            CurrentHero.CurrentExp -= GetExpToNextLevel();
            CurrentHero.CurrentLevel++;

            RecalculateStats();

            // 回满生命和蓝量
            currentHealth = currentStats.MaxHealth;
            currentMana = 100f;

            OnHeroLevelUp?.Invoke(CurrentHero);

            Debug.Log($"[Hero] 英雄升级！当前等级: {CurrentHero.CurrentLevel}");
        }

        /// <summary>
        /// 获取下一级所需经验
        /// </summary>
        public int GetExpToNextLevel()
        {
            if (CurrentHero == null) return int.MaxValue;

            int index = CurrentHero.CurrentLevel - 1;
            if (CurrentHero.ExpToLevel != null && index < CurrentHero.ExpToLevel.Length)
            {
                return CurrentHero.ExpToLevel[index];
            }

            // 使用默认配置
            if (Config?.LevelExpRequirements != null && index < Config.LevelExpRequirements.Length)
            {
                return Config.LevelExpRequirements[index];
            }

            return 1000; // 默认值
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取当前生命值
        /// </summary>
        public float GetCurrentHealth()
        {
            return currentHealth;
        }

        /// <summary>
        /// 获取当前蓝量
        /// </summary>
        public float GetCurrentMana()
        {
            return currentMana;
        }

        /// <summary>
        /// 获取生命值百分比
        /// </summary>
        public float GetHealthPercent()
        {
            if (currentStats == null || currentStats.MaxHealth <= 0)
                return 0;
            return currentHealth / currentStats.MaxHealth;
        }

        /// <summary>
        /// 获取蓝量百分比
        /// </summary>
        public float GetManaPercent()
        {
            return currentMana / 100f;
        }

        /// <summary>
        /// 获取英雄信息
        /// </summary>
        public string GetHeroInfo()
        {
            if (CurrentHero == null) return "未选择英雄";

            return $"{CurrentHero.HeroName} (Lv.{CurrentHero.CurrentLevel})\n" +
                   $"生命: {currentHealth:F0}/{currentStats?.MaxHealth:F0}\n" +
                   $"攻击: {currentStats?.AttackDamage:F1}\n" +
                   $"防御: {currentStats?.Defense:F1}\n" +
                   $"移速: {currentStats?.MoveSpeed:F1}";
        }

        #endregion
    }
}