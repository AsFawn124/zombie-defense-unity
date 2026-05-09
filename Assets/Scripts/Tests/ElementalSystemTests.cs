using UnityEngine;
using NUnit.Framework;
using ZombieDefense.Upgrade.Systems.Elemental;
using ZombieDefense.Upgrade.Data;
using System.Collections.Generic;

namespace ZombieDefense.Upgrade.Tests
{
    /// <summary>
    /// 元素系统单元测试
    /// </summary>
    public class ElementalSystemTests
    {
        private ElementalSystem elementalSystem;
        private ElementalTowerConfig config;

        [SetUp]
        public void Setup()
        {
            GameObject go = new GameObject("ElementalSystem");
            elementalSystem = go.AddComponent<ElementalSystem>();
            config = ScriptableObject.CreateInstance<ElementalTowerConfig>();

            // 初始化反应数据
            config.ReactionDataList = new List<ElementalReactionData>
            {
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.Vaporize,
                    PrimaryElement = ElementType.Fire,
                    SecondaryElement = ElementType.Ice,
                    DamageMultiplier = 2f,
                    Description = "蒸发反应"
                },
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.Overload,
                    PrimaryElement = ElementType.Fire,
                    SecondaryElement = ElementType.Electric,
                    DamageMultiplier = 1.5f,
                    AreaRadius = 3f,
                    Description = "超载反应"
                },
                new ElementalReactionData
                {
                    ReactionType = ElementalReactionType.Superconduct,
                    PrimaryElement = ElementType.Electric,
                    SecondaryElement = ElementType.Ice,
                    DamageMultiplier = 1f,
                    Duration = 5f,
                    Description = "超导反应"
                }
            };

            config.ColorConfigs = new ElementColorConfig[]
            {
                new ElementColorConfig { ElementType = ElementType.Fire, Color = Color.red },
                new ElementColorConfig { ElementType = ElementType.Ice, Color = Color.cyan },
                new ElementColorConfig { ElementType = ElementType.Electric, Color = Color.yellow }
            };

            elementalSystem.Config = config;
            elementalSystem.ShowDebugLogs = false;

            // 调用初始化
            var method = typeof(ElementalSystem).GetMethod("Initialize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(elementalSystem, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (elementalSystem != null)
            {
                Object.DestroyImmediate(elementalSystem.gameObject);
            }
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Test_ApplyElement()
        {
            int enemyId = 1;
            elementalSystem.ApplyElement(enemyId, ElementType.Fire, 5f);

            bool hasFire = elementalSystem.HasElement(enemyId, ElementType.Fire);
            Assert.IsTrue(hasFire);
        }

        [Test]
        public void Test_HasElement_False()
        {
            int enemyId = 1;
            bool hasIce = elementalSystem.HasElement(enemyId, ElementType.Ice);
            Assert.IsFalse(hasIce);
        }

        [Test]
        public void Test_GetEnemyElements()
        {
            int enemyId = 1;
            elementalSystem.ApplyElement(enemyId, ElementType.Fire, 5f);
            elementalSystem.ApplyElement(enemyId, ElementType.Ice, 3f);

            var elements = elementalSystem.GetEnemyElements(enemyId);
            Assert.AreEqual(2, elements.Count);
            Assert.IsTrue(elements.ContainsKey(ElementType.Fire));
            Assert.IsTrue(elements.ContainsKey(ElementType.Ice));
        }

        [Test]
        public void Test_ClearElements()
        {
            int enemyId = 1;
            elementalSystem.ApplyElement(enemyId, ElementType.Fire, 5f);
            elementalSystem.ClearElements(enemyId);

            bool hasFire = elementalSystem.HasElement(enemyId, ElementType.Fire);
            Assert.IsFalse(hasFire);
        }

        [Test]
        public void Test_TriggerReaction_Vaporize()
        {
            int enemyId = 1;
            elementalSystem.ApplyElement(enemyId, ElementType.Fire, 5f);

            var result = elementalSystem.TriggerReaction(enemyId, ElementType.Fire, ElementType.Ice, 100f, Vector3.zero);

            Assert.NotNull(result);
            Assert.AreEqual(ElementalReactionType.Vaporize, result.ReactionType);
            Assert.AreEqual(200f, result.FinalDamage); // 100 * 2
            Assert.IsTrue(result.IsCritical);
        }

        [Test]
        public void Test_TriggerReaction_Overload()
        {
            int enemyId = 1;
            elementalSystem.ApplyElement(enemyId, ElementType.Fire, 5f);

            var result = elementalSystem.TriggerReaction(enemyId, ElementType.Fire, ElementType.Electric, 100f, Vector3.zero);

            Assert.NotNull(result);
            Assert.AreEqual(ElementalReactionType.Overload, result.ReactionType);
            Assert.AreEqual(150f, result.FinalDamage); // 100 * 1.5
            Assert.AreEqual(3f, result.AreaRadius);
        }

        [Test]
        public void Test_TriggerReaction_NoReaction()
        {
            int enemyId = 1;

            var result = elementalSystem.TriggerReaction(enemyId, ElementType.Poison, ElementType.Wind, 100f, Vector3.zero);

            Assert.IsNull(result);
        }

        [Test]
        public void Test_GetElementColor()
        {
            Color fireColor = elementalSystem.GetElementColor(ElementType.Fire);
            Assert.AreEqual(Color.red, fireColor);

            Color iceColor = elementalSystem.GetElementColor(ElementType.Ice);
            Assert.AreEqual(Color.cyan, iceColor);
        }

        [Test]
        public void Test_GetElementName()
        {
            Assert.AreEqual("火", elementalSystem.GetElementName(ElementType.Fire));
            Assert.AreEqual("冰", elementalSystem.GetElementName(ElementType.Ice));
            Assert.AreEqual("电", elementalSystem.GetElementName(ElementType.Electric));
            Assert.AreEqual("毒", elementalSystem.GetElementName(ElementType.Poison));
            Assert.AreEqual("风", elementalSystem.GetElementName(ElementType.Wind));
            Assert.AreEqual("无", elementalSystem.GetElementName(ElementType.None));
        }

        [Test]
        public void Test_ApplyStatusEffect()
        {
            int enemyId = 1;
            var effect = new ElementalStatusEffect
            {
                ElementType = ElementType.Fire,
                Duration = 5f,
                DamagePerTick = 10f,
                TickInterval = 1f
            };

            elementalSystem.ApplyStatusEffect(enemyId, effect);

            var retrieved = elementalSystem.GetStatusEffect(enemyId);
            Assert.NotNull(retrieved);
            Assert.AreEqual(ElementType.Fire, retrieved.ElementType);
        }

        [Test]
        public void Test_RemoveStatusEffect()
        {
            int enemyId = 1;
            var effect = new ElementalStatusEffect
            {
                ElementType = ElementType.Fire,
                Duration = 5f
            };

            elementalSystem.ApplyStatusEffect(enemyId, effect);
            elementalSystem.RemoveStatusEffect(enemyId);

            var retrieved = elementalSystem.GetStatusEffect(enemyId);
            Assert.IsNull(retrieved);
        }

        [Test]
        public void Test_Config_GetReactionData()
        {
            var reaction = config.GetReactionData(ElementType.Fire, ElementType.Ice);
            Assert.NotNull(reaction);
            Assert.AreEqual(ElementalReactionType.Vaporize, reaction.ReactionType);
        }

        [Test]
        public void Test_Config_GetReactionData_NotFound()
        {
            var reaction = config.GetReactionData(ElementType.Poison, ElementType.Wind);
            Assert.IsNull(reaction);
        }

        [Test]
        public void Test_Config_GetElementColor()
        {
            Color color = config.GetElementColor(ElementType.Fire);
            Assert.AreEqual(Color.red, color);
        }

        [Test]
        public void Test_ClearAll()
        {
            int enemyId1 = 1;
            int enemyId2 = 2;

            elementalSystem.ApplyElement(enemyId1, ElementType.Fire, 5f);
            elementalSystem.ApplyElement(enemyId2, ElementType.Ice, 5f);

            elementalSystem.ClearAll();

            Assert.IsFalse(elementalSystem.HasElement(enemyId1, ElementType.Fire));
            Assert.IsFalse(elementalSystem.HasElement(enemyId2, ElementType.Ice));
        }

        [Test]
        public void Test_ElementType_EnumValues()
        {
            // 验证所有元素类型值
            Assert.AreEqual(0, (int)ElementType.None);
            Assert.AreEqual(1, (int)ElementType.Fire);
            Assert.AreEqual(2, (int)ElementType.Ice);
            Assert.AreEqual(3, (int)ElementType.Electric);
            Assert.AreEqual(4, (int)ElementType.Poison);
            Assert.AreEqual(5, (int)ElementType.Wind);
        }

        [Test]
        public void Test_ReactionType_EnumValues()
        {
            // 验证反应类型
            Assert.AreEqual(0, (int)ElementalReactionType.None);
            Assert.AreEqual(1, (int)ElementalReactionType.Vaporize);
            Assert.AreEqual(2, (int)ElementalReactionType.Overload);
            Assert.AreEqual(3, (int)ElementalReactionType.Melt);
            Assert.AreEqual(4, (int)ElementalReactionType.ElectroCharge);
            Assert.AreEqual(5, (int)ElementalReactionType.Superconduct);
            Assert.AreEqual(6, (int)ElementalReactionType.Swirl);
            Assert.AreEqual(7, (int)ElementalReactionType.Crystallize);
            Assert.AreEqual(8, (int)ElementalReactionType.Burning);
            Assert.AreEqual(9, (int)ElementalReactionType.Frozen);
            Assert.AreEqual(10, (int)ElementalReactionType.PoisonCloud);
        }
    }
}