using UnityEngine;
using NUnit.Framework;
using ZombieDefense.Upgrade.Systems.Hero;
using ZombieDefense.Upgrade.Data;
using System.Collections.Generic;

namespace ZombieDefense.Upgrade.Tests
{
    /// <summary>
    /// 英雄系统单元测试
    /// </summary>
    public class HeroSystemTests
    {
        private HeroSystem heroSystem;
        private HeroConfig config;

        [SetUp]
        public void Setup()
        {
            GameObject go = new GameObject("HeroSystem");
            heroSystem = go.AddComponent<HeroSystem>();
            config = ScriptableObject.CreateInstance<HeroConfig>();

            // 初始化英雄数据
            config.HeroDataList = new List<HeroData>
            {
                new HeroData
                {
                    HeroId = "Warrior_01",
                    HeroName = "重装战士",
                    HeroType = HeroType.Warrior,
                    Description = "坦克型英雄",
                    BaseStats = new HeroStats
                    {
                        MaxHealth = 1000,
                        AttackDamage = 50,
                        AttackRange = 3,
                        AttackSpeed = 1,
                        MoveSpeed = 5,
                        Defense = 20,
                        HealthRegen = 5,
                        CritChance = 0.1f,
                        CritDamage = 1.5f,
                        PrimaryElement = ElementType.Fire
                    },
                    MaxLevel = 10,
                    CurrentLevel = 1,
                    HealthGrowth = 100,
                    AttackGrowth = 5,
                    DefenseGrowth = 2,
                    EquippedItems = new Dictionary<EquipmentSlot, EquipmentData>()
                },
                new HeroData
                {
                    HeroId = "Mage_01",
                    HeroName = "元素法师",
                    HeroType = HeroType.Mage,
                    Description = "群攻型英雄",
                    BaseStats = new HeroStats
                    {
                        MaxHealth = 600,
                        AttackDamage = 80,
                        AttackRange = 8,
                        AttackSpeed = 0.8f,
                        MoveSpeed = 4,
                        Defense = 10,
                        HealthRegen = 3,
                        CritChance = 0.15f,
                        CritDamage = 2f,
                        PrimaryElement = ElementType.Fire,
                        ElementalMastery = 50
                    },
                    MaxLevel = 10,
                    CurrentLevel = 1,
                    EquippedItems = new Dictionary<EquipmentSlot, EquipmentData>()
                }
            };

            config.LevelExpRequirements = new int[] { 100, 200, 400, 800, 1600 };

            heroSystem.Config = config;

            // 调用初始化
            var method = typeof(HeroSystem).GetMethod("Initialize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(heroSystem, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (heroSystem != null)
            {
                Object.DestroyImmediate(heroSystem.gameObject);
            }
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Test_SelectHero()
        {
            heroSystem.SelectHero("Warrior_01");

            Assert.NotNull(heroSystem.CurrentHero);
            Assert.AreEqual("重装战士", heroSystem.CurrentHero.HeroName);
            Assert.AreEqual(HeroType.Warrior, heroSystem.CurrentHero.HeroType);
        }

        [Test]
        public void Test_SelectHero_Invalid()
        {
            heroSystem.SelectHero("Invalid_ID");
            Assert.IsNull(heroSystem.CurrentHero);
        }

        [Test]
        public void Test_GetCurrentStats()
        {
            heroSystem.SelectHero("Warrior_01");
            var stats = heroSystem.GetCurrentStats();

            Assert.NotNull(stats);
            Assert.AreEqual(1000, stats.MaxHealth);
            Assert.AreEqual(50, stats.AttackDamage);
        }

        [Test]
        public void Test_EquipItem()
        {
            heroSystem.SelectHero("Warrior_01");

            var equipment = new EquipmentData
            {
                EquipmentId = "Sword_01",
                EquipmentName = "铁剑",
                Slot = EquipmentSlot.Weapon,
                Quality = 2,
                AttackBonus = 20
            };

            bool result = heroSystem.EquipItem(equipment);

            Assert.IsTrue(result);
            var equipped = heroSystem.GetEquippedItem(EquipmentSlot.Weapon);
            Assert.NotNull(equipped);
            Assert.AreEqual("铁剑", equipped.EquipmentName);
        }

        [Test]
        public void Test_EquipItem_NoHero()
        {
            var equipment = new EquipmentData
            {
                EquipmentId = "Sword_01",
                Slot = EquipmentSlot.Weapon
            };

            bool result = heroSystem.EquipItem(equipment);
            Assert.IsFalse(result);
        }

        [Test]
        public void Test_UnequipItem()
        {
            heroSystem.SelectHero("Warrior_01");

            var equipment = new EquipmentData
            {
                EquipmentId = "Sword_01",
                Slot = EquipmentSlot.Weapon
            };

            heroSystem.EquipItem(equipment);
            bool result = heroSystem.UnequipItem(EquipmentSlot.Weapon);

            Assert.IsTrue(result);
            var equipped = heroSystem.GetEquippedItem(EquipmentSlot.Weapon);
            Assert.IsNull(equipped);
        }

        [Test]
        public void Test_RecalculateStats_WithEquipment()
        {
            heroSystem.SelectHero("Warrior_01");

            var equipment = new EquipmentData
            {
                EquipmentId = "Sword_01",
                Slot = EquipmentSlot.Weapon,
                AttackBonus = 20,
                HealthBonus = 100
            };

            heroSystem.EquipItem(equipment);
            var stats = heroSystem.GetCurrentStats();

            Assert.AreEqual(70, stats.AttackDamage); // 50 + 20
            Assert.AreEqual(1100, stats.MaxHealth); // 1000 + 100
        }

        [Test]
        public void Test_TakeDamage()
        {
            heroSystem.SelectHero("Warrior_01");

            float initialHealth = heroSystem.GetCurrentHealth();
            heroSystem.TakeDamage(50);

            float expectedHealth = initialHealth - (50 - 20); // 伤害 - 防御
            Assert.AreEqual(expectedHealth, heroSystem.GetCurrentHealth());
        }

        [Test]
        public void Test_Heal()
        {
            heroSystem.SelectHero("Warrior_01");
            heroSystem.TakeDamage(100);

            float healthBefore = heroSystem.GetCurrentHealth();
            heroSystem.Heal(50);

            Assert.AreEqual(healthBefore + 50, heroSystem.GetCurrentHealth());
        }

        [Test]
        public void Test_Heal_OverMax()
        {
            heroSystem.SelectHero("Warrior_01");

            heroSystem.Heal(9999);
            Assert.AreEqual(1000, heroSystem.GetCurrentHealth());
        }

        [Test]
        public void Test_RestoreMana()
        {
            heroSystem.SelectHero("Warrior_01");
            heroSystem.UseSkill(0, Vector3.zero); // 消耗蓝量

            heroSystem.RestoreMana(50);
            Assert.Greater(heroSystem.GetCurrentMana(), 0);
        }

        [Test]
        public void Test_GetHealthPercent()
        {
            heroSystem.SelectHero("Warrior_01");
            heroSystem.TakeDamage(500);

            float percent = heroSystem.GetHealthPercent();
            Assert.AreEqual(0.5f, percent, 0.01f);
        }

        [Test]
        public void Test_GetManaPercent()
        {
            heroSystem.SelectHero("Warrior_01");

            float percent = heroSystem.GetManaPercent();
            Assert.AreEqual(1f, percent);
        }

        [Test]
        public void Test_GainExperience_LevelUp()
        {
            heroSystem.SelectHero("Warrior_01");

            int initialLevel = heroSystem.CurrentHero.CurrentLevel;
            heroSystem.GainExperience(150); // 超过100经验

            Assert.Greater(heroSystem.CurrentHero.CurrentLevel, initialLevel);
        }

        [Test]
        public void Test_GetExpToNextLevel()
        {
            heroSystem.SelectHero("Warrior_01");

            int expNeeded = heroSystem.GetExpToNextLevel();
            Assert.AreEqual(100, expNeeded);
        }

        [Test]
        public void Test_GetExpToNextLevel_MaxLevel()
        {
            heroSystem.SelectHero("Warrior_01");
            heroSystem.CurrentHero.CurrentLevel = 10;

            int expNeeded = heroSystem.GetExpToNextLevel();
            Assert.AreEqual(int.MaxValue, expNeeded);
        }

        [Test]
        public void Test_UseSkill_NoHero()
        {
            bool result = heroSystem.UseSkill(0, Vector3.zero);
            Assert.IsFalse(result);
        }

        [Test]
        public void Test_IsSkillOnCooldown()
        {
            heroSystem.SelectHero("Warrior_01");

            // 设置技能冷却
            var skill = new HeroSkillData
            {
                SkillId = "Skill_01",
                SkillName = "测试技能",
                Cooldown = 5f,
                ManaCost = 10
            };

            heroSystem.CurrentHero.Skills[0] = skill;
            heroSystem.UseSkill(0, Vector3.zero);

            bool onCooldown = heroSystem.IsSkillOnCooldown("Skill_01");
            Assert.IsTrue(onCooldown);
        }

        [Test]
        public void Test_GetSkillCooldown()
        {
            heroSystem.SelectHero("Warrior_01");

            var skill = new HeroSkillData
            {
                SkillId = "Skill_01",
                Cooldown = 5f,
                ManaCost = 10
            };

            heroSystem.CurrentHero.Skills[0] = skill;
            heroSystem.UseSkill(0, Vector3.zero);

            float cooldown = heroSystem.GetSkillCooldown("Skill_01");
            Assert.Greater(cooldown, 0);
        }

        [Test]
        public void Test_ReviveHero()
        {
            heroSystem.SelectHero("Warrior_01");
            heroSystem.TakeDamage(9999); // 杀死英雄

            heroSystem.ReviveHero(0.5f);

            Assert.AreEqual(500, heroSystem.GetCurrentHealth());
        }

        [Test]
        public void Test_GetHeroInfo()
        {
            heroSystem.SelectHero("Warrior_01");
            string info = heroSystem.GetHeroInfo();

            Assert.IsTrue(info.Contains("重装战士"));
            Assert.IsTrue(info.Contains("Lv.1"));
        }

        [Test]
        public void Test_GetAllEquippedItems()
        {
            heroSystem.SelectHero("Warrior_01");

            var weapon = new EquipmentData
            {
                EquipmentId = "Sword_01",
                Slot = EquipmentSlot.Weapon
            };
            var armor = new EquipmentData
            {
                EquipmentId = "Armor_01",
                Slot = EquipmentSlot.Armor
            };

            heroSystem.EquipItem(weapon);
            heroSystem.EquipItem(armor);

            var items = heroSystem.GetAllEquippedItems();
            Assert.AreEqual(2, items.Count);
        }

        [Test]
        public void Test_HeroType_EnumValues()
        {
            Assert.AreEqual(0, (int)HeroType.Warrior);
            Assert.AreEqual(1, (int)HeroType.Sniper);
            Assert.AreEqual(2, (int)HeroType.Engineer);
            Assert.AreEqual(3, (int)HeroType.Mage);
        }

        [Test]
        public void Test_EquipmentSlot_EnumValues()
        {
            Assert.AreEqual(0, (int)EquipmentSlot.Weapon);
            Assert.AreEqual(1, (int)EquipmentSlot.Armor);
            Assert.AreEqual(2, (int)EquipmentSlot.Helmet);
            Assert.AreEqual(3, (int)EquipmentSlot.Boots);
            Assert.AreEqual(4, (int)EquipmentSlot.Accessory1);
            Assert.AreEqual(5, (int)EquipmentSlot.Accessory2);
        }

        [Test]
        public void Test_Config_GetQualityColor()
        {
            config.QualityColors = new Color[]
            {
                Color.white,
                Color.green,
                Color.blue
            };

            Color color = config.GetQualityColor(1);
            Assert.AreEqual(Color.green, color);
        }

        [Test]
        public void Test_HeroStats_Clone()
        {
            var original = new HeroStats
            {
                MaxHealth = 1000,
                AttackDamage = 50,
                PrimaryElement = ElementType.Fire
            };

            var clone = original.Clone();
            clone.MaxHealth = 500;

            Assert.AreEqual(1000, original.MaxHealth);
            Assert.AreEqual(500, clone.MaxHealth);
        }
    }
}