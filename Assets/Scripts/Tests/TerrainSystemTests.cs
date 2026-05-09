using UnityEngine;
using NUnit.Framework;
using ZombieDefense.Upgrade.Systems.Terrain;
using ZombieDefense.Upgrade.Data;
using System.Collections.Generic;

namespace ZombieDefense.Upgrade.Tests
{
    /// <summary>
    /// 地形系统单元测试
    /// </summary>
    public class TerrainSystemTests
    {
        private TerrainSystem terrainSystem;
        private TerrainConfig config;

        [SetUp]
        public void Setup()
        {
            GameObject go = new GameObject("TerrainSystem");
            terrainSystem = go.AddComponent<TerrainSystem>();
            config = ScriptableObject.CreateInstance<TerrainConfig>();

            // 初始化地形效果
            config.TerrainEffects = new List<TerrainEffectData>
            {
                new TerrainEffectData
                {
                    TerrainType = TerrainType.Normal,
                    TerrainName = "普通地形",
                    CanBuildTower = true
                },
                new TerrainEffectData
                {
                    TerrainType = TerrainType.Lava,
                    TerrainName = "熔岩",
                    DamagePerSecond = 10f,
                    MoveSpeedModifier = 1f,
                    CanBuildTower = false
                },
                new TerrainEffectData
                {
                    TerrainType = TerrainType.Ice,
                    TerrainName = "冰冻地面",
                    MoveSpeedModifier = 0.5f,
                    CanBuildTower = true
                },
                new TerrainEffectData
                {
                    TerrainType = TerrainType.HighGround,
                    TerrainName = "高地",
                    RangeModifier = 1.5f,
                    DamageModifier = 1.2f,
                    CanBuildTower = true
                },
                new TerrainEffectData
                {
                    TerrainType = TerrainType.Obstacle,
                    TerrainName = "障碍物",
                    CanBlockEnemy = true,
                    CanBuildTower = false
                }
            };

            terrainSystem.Config = config;
            terrainSystem.MapWidth = 10;
            terrainSystem.MapHeight = 10;

            // 调用初始化
            var method = typeof(TerrainSystem).GetMethod("Initialize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(terrainSystem, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (terrainSystem != null)
            {
                Object.DestroyImmediate(terrainSystem.gameObject);
            }
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Test_Initialize_MapSize()
        {
            Assert.AreEqual(10, terrainSystem.MapWidth);
            Assert.AreEqual(10, terrainSystem.MapHeight);
        }

        [Test]
        public void Test_GetCell_ValidPosition()
        {
            var cell = terrainSystem.GetCell(5, 5);
            Assert.NotNull(cell);
            Assert.AreEqual(new Vector2Int(5, 5), cell.Position);
        }

        [Test]
        public void Test_GetCell_InvalidPosition()
        {
            var cell = terrainSystem.GetCell(15, 15);
            Assert.IsNull(cell);
        }

        [Test]
        public void Test_IsValidPosition()
        {
            Assert.IsTrue(terrainSystem.IsValidPosition(0, 0));
            Assert.IsTrue(terrainSystem.IsValidPosition(9, 9));
            Assert.IsFalse(terrainSystem.IsValidPosition(-1, 0));
            Assert.IsFalse(terrainSystem.IsValidPosition(10, 10));
        }

        [Test]
        public void Test_ChangeTerrain()
        {
            var pos = new Vector2Int(3, 3);
            bool result = terrainSystem.ChangeTerrain(pos, TerrainType.Lava);

            Assert.IsTrue(result);
            Assert.AreEqual(TerrainType.Lava, terrainSystem.GetTerrainType(pos));
        }

        [Test]
        public void Test_ChangeTerrain_InvalidPosition()
        {
            var pos = new Vector2Int(15, 15);
            bool result = terrainSystem.ChangeTerrain(pos, TerrainType.Lava);

            Assert.IsFalse(result);
        }

        [Test]
        public void Test_ChangeTerrain_SameType()
        {
            var pos = new Vector2Int(3, 3);
            terrainSystem.ChangeTerrain(pos, TerrainType.Lava);
            bool result = terrainSystem.ChangeTerrain(pos, TerrainType.Lava);

            Assert.IsFalse(result);
        }

        [Test]
        public void Test_GetTerrainEffect()
        {
            var pos = new Vector2Int(3, 3);
            terrainSystem.ChangeTerrain(pos, TerrainType.Lava);

            var effect = terrainSystem.GetTerrainEffect(pos);
            Assert.NotNull(effect);
            Assert.AreEqual("熔岩", effect.TerrainName);
            Assert.AreEqual(10f, effect.DamagePerSecond);
        }

        [Test]
        public void Test_GetEnemyEffect_Lava()
        {
            var pos = new Vector2Int(3, 3);
            terrainSystem.ChangeTerrain(pos, TerrainType.Lava);

            var (damage, speedMod, blocked) = terrainSystem.GetEnemyEffect(pos);
            Assert.AreEqual(10f, damage);
            Assert.AreEqual(1f, speedMod);
            Assert.IsFalse(blocked);
        }

        [Test]
        public void Test_GetEnemyEffect_Ice()
        {
            var pos = new Vector2Int(3, 3);
            terrainSystem.ChangeTerrain(pos, TerrainType.Ice);

            var (damage, speedMod, blocked) = terrainSystem.GetEnemyEffect(pos);
            Assert.AreEqual(0f, damage);
            Assert.AreEqual(0.5f, speedMod);
            Assert.IsFalse(blocked);
        }

        [Test]
        public void Test_GetEnemyEffect_Obstacle()
        {
            var pos = new Vector2Int(3, 3);
            terrainSystem.ChangeTerrain(pos, TerrainType.Obstacle);

            var (damage, speedMod, blocked) = terrainSystem.GetEnemyEffect(pos);
            Assert.IsTrue(blocked);
        }

        [Test]
        public void Test_GetTowerEffect_HighGround()
        {
            var pos = new Vector2Int(3, 3);
            terrainSystem.ChangeTerrain(pos, TerrainType.HighGround);

            var (rangeMod, damageMod, fireRateMod, canBuild) = terrainSystem.GetTowerEffect(pos);
            Assert.AreEqual(1.5f, rangeMod);
            Assert.AreEqual(1.2f, damageMod);
            Assert.AreEqual(1f, fireRateMod);
            Assert.IsTrue(canBuild);
        }

        [Test]
        public void Test_CanBuildTower_Lava()
        {
            var pos = new Vector2Int(3, 3);
            terrainSystem.ChangeTerrain(pos, TerrainType.Lava);

            Assert.IsFalse(terrainSystem.CanBuildTower(pos));
        }

        [Test]
        public void Test_CanBuildTower_Normal()
        {
            var pos = new Vector2Int(3, 3);
            Assert.IsTrue(terrainSystem.CanBuildTower(pos));
        }

        [Test]
        public void Test_BlocksEnemy_Obstacle()
        {
            var pos = new Vector2Int(3, 3);
            terrainSystem.ChangeTerrain(pos, TerrainType.Obstacle);

            Assert.IsTrue(terrainSystem.BlocksEnemy(pos));
        }

        [Test]
        public void Test_BlocksEnemy_Normal()
        {
            var pos = new Vector2Int(3, 3);
            Assert.IsFalse(terrainSystem.BlocksEnemy(pos));
        }

        [Test]
        public void Test_ChangeTerrainBatch()
        {
            var positions = new Vector2Int[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0)
            };

            terrainSystem.ChangeTerrainBatch(positions, TerrainType.Ice);

            foreach (var pos in positions)
            {
                Assert.AreEqual(TerrainType.Ice, terrainSystem.GetTerrainType(pos));
            }
        }

        [Test]
        public void Test_RestoreTerrain()
        {
            var pos = new Vector2Int(3, 3);
            terrainSystem.ChangeTerrain(pos, TerrainType.Lava);
            terrainSystem.RestoreTerrain(pos);

            Assert.AreEqual(TerrainType.Normal, terrainSystem.GetTerrainType(pos));
        }

        [Test]
        public void Test_GetCellsByType()
        {
            terrainSystem.ChangeTerrain(new Vector2Int(0, 0), TerrainType.Lava);
            terrainSystem.ChangeTerrain(new Vector2Int(1, 1), TerrainType.Lava);
            terrainSystem.ChangeTerrain(new Vector2Int(2, 2), TerrainType.Ice);

            var lavaCells = terrainSystem.GetCellsByType(TerrainType.Lava);
            Assert.AreEqual(2, lavaCells.Count);
        }

        [Test]
        public void Test_GetTerrainStatistics()
        {
            terrainSystem.ChangeTerrain(new Vector2Int(0, 0), TerrainType.Lava);
            terrainSystem.ChangeTerrain(new Vector2Int(1, 1), TerrainType.Lava);
            terrainSystem.ChangeTerrain(new Vector2Int(2, 2), TerrainType.Ice);

            var stats = terrainSystem.GetTerrainStatistics();
            Assert.AreEqual(2, stats[TerrainType.Lava]);
            Assert.AreEqual(1, stats[TerrainType.Ice]);
            Assert.AreEqual(97, stats[TerrainType.Normal]); // 100 - 2 - 1
        }

        [Test]
        public void Test_ResetTerrain()
        {
            terrainSystem.ChangeTerrain(new Vector2Int(0, 0), TerrainType.Lava);
            terrainSystem.ChangeTerrain(new Vector2Int(1, 1), TerrainType.Ice);

            terrainSystem.ResetTerrain();

            Assert.AreEqual(TerrainType.Normal, terrainSystem.GetTerrainType(new Vector2Int(0, 0)));
            Assert.AreEqual(TerrainType.Normal, terrainSystem.GetTerrainType(new Vector2Int(1, 1)));
            Assert.AreEqual(0, terrainSystem.CurrentWave);
        }

        [Test]
        public void Test_GridToWorldPosition()
        {
            terrainSystem.CellSize = 2f;
            var gridPos = new Vector2Int(3, 4);
            var worldPos = terrainSystem.GridToWorldPosition(gridPos);

            Assert.AreEqual(new Vector3(6f, 8f, 0), worldPos);
        }

        [Test]
        public void Test_WorldToGridPosition()
        {
            terrainSystem.CellSize = 2f;
            var worldPos = new Vector3(6f, 8f, 0);
            var gridPos = terrainSystem.WorldToGridPosition(worldPos);

            Assert.AreEqual(new Vector2Int(3, 4), gridPos);
        }

        [Test]
        public void Test_Config_GetTerrainEffect()
        {
            var effect = config.GetTerrainEffect(TerrainType.Lava);
            Assert.NotNull(effect);
            Assert.AreEqual("熔岩", effect.TerrainName);
        }

        [Test]
        public void Test_TerrainType_EnumValues()
        {
            Assert.AreEqual(0, (int)TerrainType.Normal);
            Assert.AreEqual(1, (int)TerrainType.Lava);
            Assert.AreEqual(2, (int)TerrainType.Ice);
            Assert.AreEqual(3, (int)TerrainType.HighGround);
            Assert.AreEqual(4, (int)TerrainType.Obstacle);
            Assert.AreEqual(5, (int)TerrainType.Portal);
            Assert.AreEqual(6, (int)TerrainType.PoisonSwamp);
            Assert.AreEqual(7, (int)TerrainType.Electric);
        }

        [Test]
        public void Test_ChangeTerrain_WithDuration()
        {
            var pos = new Vector2Int(3, 3);
            bool result = terrainSystem.ChangeTerrain(pos, TerrainType.Lava, 5f);

            Assert.IsTrue(result);
            var cell = terrainSystem.GetCell(pos);
            Assert.IsTrue(cell.IsDynamic);
            Assert.AreEqual(5f, cell.RemainingDuration);
        }

        [Test]
        public void Test_GetSaveData()
        {
            terrainSystem.ChangeTerrain(new Vector2Int(0, 0), TerrainType.Lava);
            terrainSystem.ChangeTerrain(new Vector2Int(1, 1), TerrainType.Ice);

            var saveData = terrainSystem.GetSaveData();
            Assert.NotNull(saveData);
            Assert.AreEqual(100, saveData.CellDataList.Count);
        }
    }
}