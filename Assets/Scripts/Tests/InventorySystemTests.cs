using UnityEngine;
using NUnit.Framework;
using ZombieDefense.Upgrade.Systems.Inventory;
using ZombieDefense.Upgrade.Data;
using System.Collections.Generic;

namespace ZombieDefense.Upgrade.Tests
{
    /// <summary>
    /// 背包系统单元测试
    /// </summary>
    public class InventorySystemTests
    {
        private InventoryManager inventoryManager;
        private InventoryConfig config;

        [SetUp]
        public void Setup()
        {
            GameObject go = new GameObject("InventoryManager");
            inventoryManager = go.AddComponent<InventoryManager>();
            config = ScriptableObject.CreateInstance<InventoryConfig>();
            config.DefaultGridSize = 9;
            config.MaxGridSize = 16;
            config.InitialUnlockedCells = 9;
            config.MergeRequiredCount = 3;
            config.RequireAdjacent = true;

            inventoryManager.Config = config;
            inventoryManager.GridWidth = 3;
            inventoryManager.GridHeight = 3;
            inventoryManager.UnlockedCellCount = 9;

            // 通过反射调用私有初始化方法
            var method = typeof(InventoryManager).GetMethod("Initialize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(inventoryManager, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (inventoryManager != null)
            {
                Object.DestroyImmediate(inventoryManager.gameObject);
            }
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Test_Initialize_GridSize()
        {
            Assert.AreEqual(3, inventoryManager.GridWidth);
            Assert.AreEqual(3, inventoryManager.GridHeight);
            Assert.AreEqual(9, inventoryManager.UnlockedCellCount);
        }

        [Test]
        public void Test_GridCell_EmptyState()
        {
            var cell = inventoryManager.GetCell(0, 0);
            Assert.NotNull(cell);
            Assert.AreEqual(GridCellState.Empty, cell.State);
        }

        [Test]
        public void Test_GridCell_LockedState()
        {
            // 扩大网格但保持解锁格子数
            inventoryManager.GridWidth = 4;
            inventoryManager.GridHeight = 4;

            var method = typeof(InventoryManager).GetMethod("Initialize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(inventoryManager, null);

            var cell = inventoryManager.GetCell(3, 3);
            Assert.NotNull(cell);
            Assert.AreEqual(GridCellState.Locked, cell.State);
        }

        [Test]
        public void Test_AddTower_SingleSpace()
        {
            var tower = new InventoryTowerData
            {
                TowerTypeId = "BasicTower",
                TowerName = "基础塔",
                SpaceType = TowerSpaceType.Single,
                Level = 1
            };

            bool result = inventoryManager.AddTower(tower, 0, 0);

            Assert.IsTrue(result);
            Assert.AreEqual(1, inventoryManager.GetAllTowers().Count);

            var placedTower = inventoryManager.GetTowerAt(0, 0);
            Assert.NotNull(placedTower);
            Assert.AreEqual("基础塔", placedTower.TowerName);
        }

        [Test]
        public void Test_AddTower_DoubleSpace()
        {
            var tower = new InventoryTowerData
            {
                TowerTypeId = "AdvancedTower",
                TowerName = "高级塔",
                SpaceType = TowerSpaceType.Double,
                Level = 1
            };

            bool result = inventoryManager.AddTower(tower, 0, 0);

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector2Int(0, 0), tower.Position);
            Assert.AreEqual(2, tower.OccupiedPositions.Length);
        }

        [Test]
        public void Test_AddTower_InvalidPosition()
        {
            var tower = new InventoryTowerData
            {
                TowerTypeId = "BasicTower",
                TowerName = "基础塔",
                SpaceType = TowerSpaceType.Single
            };

            bool result = inventoryManager.AddTower(tower, 5, 5); // 超出范围

            Assert.IsFalse(result);
        }

        [Test]
        public void Test_AddTower_Overlap()
        {
            var tower1 = new InventoryTowerData
            {
                TowerTypeId = "BasicTower",
                TowerName = "塔1",
                SpaceType = TowerSpaceType.Single
            };
            var tower2 = new InventoryTowerData
            {
                TowerTypeId = "BasicTower",
                TowerName = "塔2",
                SpaceType = TowerSpaceType.Single
            };

            inventoryManager.AddTower(tower1, 0, 0);
            bool result = inventoryManager.AddTower(tower2, 0, 0);

            Assert.IsFalse(result);
        }

        [Test]
        public void Test_RemoveTower()
        {
            var tower = new InventoryTowerData
            {
                TowerTypeId = "BasicTower",
                TowerName = "基础塔",
                SpaceType = TowerSpaceType.Single
            };

            inventoryManager.AddTower(tower, 0, 0);
            bool result = inventoryManager.RemoveTower(tower.InstanceId);

            Assert.IsTrue(result);
            Assert.AreEqual(0, inventoryManager.GetAllTowers().Count);

            var cell = inventoryManager.GetCell(0, 0);
            Assert.AreEqual(GridCellState.Empty, cell.State);
        }

        [Test]
        public void Test_MoveTower()
        {
            var tower = new InventoryTowerData
            {
                TowerTypeId = "BasicTower",
                TowerName = "基础塔",
                SpaceType = TowerSpaceType.Single
            };

            inventoryManager.AddTower(tower, 0, 0);
            bool result = inventoryManager.MoveTower(tower.InstanceId, 1, 1);

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector2Int(1, 1), tower.Position);

            var oldCell = inventoryManager.GetCell(0, 0);
            var newCell = inventoryManager.GetCell(1, 1);
            Assert.AreEqual(GridCellState.Empty, oldCell.State);
            Assert.AreEqual(GridCellState.Occupied, newCell.State);
        }

        [Test]
        public void Test_CanPlaceTower()
        {
            var tower = new InventoryTowerData
            {
                TowerTypeId = "QuadTower",
                TowerName = "四格塔",
                SpaceType = TowerSpaceType.Quad
            };

            // 可以放置（2x2在3x3网格内）
            bool canPlace1 = inventoryManager.CanPlaceTower(tower, 0, 0);
            Assert.IsTrue(canPlace1);

            // 不能放置（超出边界）
            bool canPlace2 = inventoryManager.CanPlaceTower(tower, 2, 2);
            Assert.IsFalse(canPlace2);
        }

        [Test]
        public void Test_GetOccupiedPositions()
        {
            // 单格
            var single = inventoryManager.GetOccupiedPositions(TowerSpaceType.Single, 1, 1);
            Assert.AreEqual(1, single.Length);
            Assert.AreEqual(new Vector2Int(1, 1), single[0]);

            // 双格
            var dual = inventoryManager.GetOccupiedPositions(TowerSpaceType.Double, 0, 0);
            Assert.AreEqual(2, dual.Length);

            // 四格
            var quad = inventoryManager.GetOccupiedPositions(TowerSpaceType.Quad, 0, 0);
            Assert.AreEqual(4, quad.Length);

            // L型
            var lShape = inventoryManager.GetOccupiedPositions(TowerSpaceType.LShape, 0, 0);
            Assert.AreEqual(3, lShape.Length);
        }

        [Test]
        public void Test_UnlockCell()
        {
            // 先扩大网格
            inventoryManager.GridWidth = 4;
            inventoryManager.GridHeight = 4;
            inventoryManager.UnlockedCellCount = 9;

            var method = typeof(InventoryManager).GetMethod("Initialize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(inventoryManager, null);

            bool result = inventoryManager.UnlockCell(1);

            Assert.IsTrue(result);
            Assert.AreEqual(10, inventoryManager.UnlockedCellCount);
        }

        [Test]
        public void Test_GetInventoryStats()
        {
            var tower = new InventoryTowerData
            {
                TowerTypeId = "BasicTower",
                TowerName = "基础塔",
                SpaceType = TowerSpaceType.Single
            };

            inventoryManager.AddTower(tower, 0, 0);
            var stats = inventoryManager.GetInventoryStats();

            Assert.AreEqual(1, stats.used);
            Assert.AreEqual(9, stats.total);
            Assert.AreEqual(9, stats.unlocked);
        }

        [Test]
        public void Test_SaveAndLoad()
        {
            var tower = new InventoryTowerData
            {
                TowerTypeId = "BasicTower",
                TowerName = "基础塔",
                SpaceType = TowerSpaceType.Single,
                Level = 2
            };

            inventoryManager.AddTower(tower, 1, 1);

            var saveData = inventoryManager.GetSaveData();
            Assert.NotNull(saveData);
            Assert.AreEqual(1, saveData.Towers.Count);
            Assert.AreEqual(9, saveData.UnlockedCellCount);

            // 加载
            inventoryManager.LoadSaveData(saveData);
            var loadedTower = inventoryManager.GetTowerAt(1, 1);
            Assert.NotNull(loadedTower);
            Assert.AreEqual("基础塔", loadedTower.TowerName);
            Assert.AreEqual(2, loadedTower.Level);
        }

        [Test]
        public void Test_CanMerge_NotEnoughTowers()
        {
            var tower = new InventoryTowerData
            {
                TowerTypeId = "BasicTower",
                TowerName = "基础塔",
                SpaceType = TowerSpaceType.Single,
                Level = 1
            };

            inventoryManager.AddTower(tower, 0, 0);

            bool canMerge = inventoryManager.CanMerge(tower);
            Assert.IsFalse(canMerge);
        }

        [Test]
        public void Test_GetTowerById()
        {
            var tower = new InventoryTowerData
            {
                TowerTypeId = "BasicTower",
                TowerName = "基础塔",
                SpaceType = TowerSpaceType.Single
            };

            inventoryManager.AddTower(tower, 0, 0);
            var found = inventoryManager.GetTowerById(tower.InstanceId);

            Assert.NotNull(found);
            Assert.AreEqual(tower.InstanceId, found.InstanceId);
        }

        [Test]
        public void Test_GetTowersByType()
        {
            var tower1 = new InventoryTowerData
            {
                TowerTypeId = "TypeA",
                TowerName = "塔A1",
                SpaceType = TowerSpaceType.Single
            };
            var tower2 = new InventoryTowerData
            {
                TowerTypeId = "TypeA",
                TowerName = "塔A2",
                SpaceType = TowerSpaceType.Single
            };
            var tower3 = new InventoryTowerData
            {
                TowerTypeId = "TypeB",
                TowerName = "塔B",
                SpaceType = TowerSpaceType.Single
            };

            inventoryManager.AddTower(tower1, 0, 0);
            inventoryManager.AddTower(tower2, 1, 0);
            inventoryManager.AddTower(tower3, 2, 0);

            var typeATowers = inventoryManager.GetTowersByType("TypeA");
            Assert.AreEqual(2, typeATowers.Count);
        }
    }
}