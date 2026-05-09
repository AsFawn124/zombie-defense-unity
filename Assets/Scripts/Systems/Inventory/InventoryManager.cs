using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ZombieDefense.Upgrade.Data;

namespace ZombieDefense.Upgrade.Systems.Inventory
{
    /// <summary>
    /// 背包管理器 - 单例模式
    /// 管理背包网格、塔的存放、拖拽和合成
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("配置")]
        public InventoryConfig Config;

        [Header("当前状态")]
        public int GridWidth = 3;               // 网格宽度
        public int GridHeight = 3;              // 网格高度
        public int UnlockedCellCount = 9;       // 已解锁格子数

        // 运行时数据
        private GridCellData[,] gridCells;
        private Dictionary<string, InventoryTowerData> towerDictionary;
        private List<Vector2Int> selectedCells;

        // 事件
        public event Action<InventoryTowerData> OnTowerAdded;
        public event Action<InventoryTowerData> OnTowerRemoved;
        public event Action<InventoryTowerData, int, int> OnTowerMoved;
        public event Action<InventoryTowerData, int> OnTowerMerged;
        public event Action<int> OnCellUnlocked;
        public event Action OnInventoryChanged;

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

        /// <summary>
        /// 初始化背包
        /// </summary>
        private void Initialize()
        {
            if (Config == null)
            {
                Config = ScriptableObject.CreateInstance<InventoryConfig>();
            }

            gridCells = new GridCellData[GridWidth, GridHeight];
            towerDictionary = new Dictionary<string, InventoryTowerData>();
            selectedCells = new List<Vector2Int>();

            // 初始化格子
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    int index = y * GridWidth + x;
                    gridCells[x, y] = new GridCellData(x, y);
                    gridCells[x, y].State = index < UnlockedCellCount ? GridCellState.Empty : GridCellState.Locked;
                }
            }
        }

        #region 格子操作

        /// <summary>
        /// 获取格子
        /// </summary>
        public GridCellData GetCell(int x, int y)
        {
            if (IsValidPosition(x, y))
                return gridCells[x, y];
            return null;
        }

        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
        }

        /// <summary>
        /// 检查格子是否已解锁
        /// </summary>
        public bool IsCellUnlocked(int x, int y)
        {
            var cell = GetCell(x, y);
            return cell != null && cell.State != GridCellState.Locked;
        }

        /// <summary>
        /// 检查格子是否为空
        /// </summary>
        public bool IsCellEmpty(int x, int y)
        {
            var cell = GetCell(x, y);
            return cell != null && cell.State == GridCellState.Empty;
        }

        /// <summary>
        /// 解锁格子
        /// </summary>
        public bool UnlockCell(int count = 1)
        {
            int unlocked = 0;
            for (int x = 0; x < GridWidth && unlocked < count; x++)
            {
                for (int y = 0; y < GridHeight && unlocked < count; y++)
                {
                    if (gridCells[x, y].State == GridCellState.Locked)
                    {
                        gridCells[x, y].State = GridCellState.Empty;
                        unlocked++;
                        OnCellUnlocked?.Invoke(y * GridWidth + x);
                    }
                }
            }

            UnlockedCellCount += unlocked;
            OnInventoryChanged?.Invoke();
            return unlocked > 0;
        }

        /// <summary>
        /// 获取解锁费用
        /// </summary>
        public int GetUnlockCost(int count = 1)
        {
            int cost = 0;
            int unlockIndex = (UnlockedCellCount - Config.InitialUnlockedCells) / count;
            for (int i = 0; i < count && unlockIndex + i < Config.UnlockCosts.Length; i++)
            {
                cost += Config.UnlockCosts[unlockIndex + i];
            }
            return cost;
        }

        #endregion

        #region 塔的添加和移除

        /// <summary>
        /// 添加塔到背包
        /// </summary>
        public bool AddTower(InventoryTowerData tower, int x, int y)
        {
            if (!CanPlaceTower(tower, x, y))
                return false;

            // 设置位置
            tower.Position = new Vector2Int(x, y);
            tower.OccupiedPositions = GetOccupiedPositions(tower.SpaceType, x, y);

            // 占用格子
            foreach (var pos in tower.OccupiedPositions)
            {
                var cell = GetCell(pos.x, pos.y);
                cell.State = GridCellState.Occupied;
                cell.TowerId = tower.InstanceId;
                cell.OccupiedCells = tower.OccupiedPositions;
            }

            // 添加到字典
            towerDictionary[tower.InstanceId] = tower;

            OnTowerAdded?.Invoke(tower);
            OnInventoryChanged?.Invoke();

            Debug.Log($"[Inventory] 添加塔 {tower.TowerName} 到位置 ({x}, {y})");
            return true;
        }

        /// <summary>
        /// 从背包移除塔
        /// </summary>
        public bool RemoveTower(string towerId)
        {
            if (!towerDictionary.TryGetValue(towerId, out var tower))
                return false;

            // 释放格子
            foreach (var pos in tower.OccupiedPositions)
            {
                var cell = GetCell(pos.x, pos.y);
                if (cell != null)
                {
                    cell.State = GridCellState.Empty;
                    cell.TowerId = string.Empty;
                    cell.OccupiedCells = null;
                }
            }

            towerDictionary.Remove(towerId);

            OnTowerRemoved?.Invoke(tower);
            OnInventoryChanged?.Invoke();

            Debug.Log($"[Inventory] 移除塔 {tower.TowerName}");
            return true;
        }

        /// <summary>
        /// 移动塔
        /// </summary>
        public bool MoveTower(string towerId, int newX, int newY)
        {
            if (!towerDictionary.TryGetValue(towerId, out var tower))
                return false;

            // 检查新位置是否可以放置
            if (!CanPlaceTower(tower, newX, newY, towerId))
                return false;

            // 保存旧位置
            Vector2Int oldPos = tower.Position;

            // 释放旧格子
            foreach (var pos in tower.OccupiedPositions)
            {
                var cell = GetCell(pos.x, pos.y);
                if (cell != null)
                {
                    cell.State = GridCellState.Empty;
                    cell.TowerId = string.Empty;
                    cell.OccupiedCells = null;
                }
            }

            // 设置新位置
            tower.Position = new Vector2Int(newX, newY);
            tower.OccupiedPositions = GetOccupiedPositions(tower.SpaceType, newX, newY);

            // 占用新格子
            foreach (var pos in tower.OccupiedPositions)
            {
                var cell = GetCell(pos.x, pos.y);
                cell.State = GridCellState.Occupied;
                cell.TowerId = tower.InstanceId;
                cell.OccupiedCells = tower.OccupiedPositions;
            }

            OnTowerMoved?.Invoke(tower, oldPos.x, oldPos.y);
            OnInventoryChanged?.Invoke();

            Debug.Log($"[Inventory] 移动塔 {tower.TowerName} 从 ({oldPos.x}, {oldPos.y}) 到 ({newX}, {newY})");
            return true;
        }

        #endregion

        #region 放置检查

        /// <summary>
        /// 检查是否可以放置塔
        /// </summary>
        public bool CanPlaceTower(InventoryTowerData tower, int x, int y, string excludeTowerId = null)
        {
            var positions = GetOccupiedPositions(tower.SpaceType, x, y);

            foreach (var pos in positions)
            {
                if (!IsValidPosition(pos.x, pos.y))
                    return false;

                if (!IsCellUnlocked(pos.x, pos.y))
                    return false;

                var cell = GetCell(pos.x, pos.y);
                if (cell.State == GridCellState.Occupied)
                {
                    // 如果格子被其他塔占用，不能放置
                    if (excludeTowerId == null || cell.TowerId != excludeTowerId)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取塔占用的所有位置
        /// </summary>
        public Vector2Int[] GetOccupiedPositions(TowerSpaceType spaceType, int x, int y)
        {
            switch (spaceType)
            {
                case TowerSpaceType.Single:
                    return new Vector2Int[] { new Vector2Int(x, y) };

                case TowerSpaceType.Double:
                    // 横向双格
                    return new Vector2Int[] { new Vector2Int(x, y), new Vector2Int(x + 1, y) };

                case TowerSpaceType.Quad:
                    // 2x2四格
                    return new Vector2Int[]
                    {
                        new Vector2Int(x, y),
                        new Vector2Int(x + 1, y),
                        new Vector2Int(x, y + 1),
                        new Vector2Int(x + 1, y + 1)
                    };

                case TowerSpaceType.LShape:
                    // L型三格
                    return new Vector2Int[]
                    {
                        new Vector2Int(x, y),
                        new Vector2Int(x + 1, y),
                        new Vector2Int(x, y + 1)
                    };

                default:
                    return new Vector2Int[] { new Vector2Int(x, y) };
            }
        }

        #endregion

        #region 合成系统

        /// <summary>
        /// 检查是否可以合成
        /// </summary>
        public bool CanMerge(InventoryTowerData targetTower)
        {
            if (targetTower == null) return false;

            // 查找相邻的同类型同等级塔
            var adjacentTowers = GetAdjacentSameTowers(targetTower);
            return adjacentTowers.Count >= Config.MergeRequiredCount - 1;
        }

        /// <summary>
        /// 执行合成
        /// </summary>
        public bool MergeTower(InventoryTowerData targetTower)
        {
            if (!CanMerge(targetTower))
                return false;

            var mergeTowers = GetAdjacentSameTowers(targetTower);
            mergeTowers.Add(targetTower);

            if (mergeTowers.Count < Config.MergeRequiredCount)
                return false;

            // 只取需要的数量
            var towersToMerge = mergeTowers.Take(Config.MergeRequiredCount).ToList();

            // 移除用于合成的塔
            foreach (var tower in towersToMerge.Skip(1))
            {
                RemoveTower(tower.InstanceId);
            }

            // 升级目标塔
            targetTower.Level++;
            targetTower.Quantity = 1;

            OnTowerMerged?.Invoke(targetTower, targetTower.Level);
            OnInventoryChanged?.Invoke();

            Debug.Log($"[Inventory] 合成成功！{targetTower.TowerName} 升级到 Lv.{targetTower.Level}");
            return true;
        }

        /// <summary>
        /// 获取相邻的同类型同等级塔
        /// </summary>
        private List<InventoryTowerData> GetAdjacentSameTowers(InventoryTowerData targetTower)
        {
            var result = new List<InventoryTowerData>();

            foreach (var kvp in towerDictionary)
            {
                var tower = kvp.Value;
                if (tower.InstanceId == targetTower.InstanceId)
                    continue;

                if (tower.TowerTypeId == targetTower.TowerTypeId &&
                    tower.Level == targetTower.Level &&
                    IsAdjacent(targetTower, tower))
                {
                    result.Add(tower);
                }
            }

            return result;
        }

        /// <summary>
        /// 检查两个塔是否相邻
        /// </summary>
        private bool IsAdjacent(InventoryTowerData tower1, InventoryTowerData tower2)
        {
            foreach (var pos1 in tower1.OccupiedPositions)
            {
                foreach (var pos2 in tower2.OccupiedPositions)
                {
                    int dx = Mathf.Abs(pos1.x - pos2.x);
                    int dy = Mathf.Abs(pos1.y - pos2.y);

                    // 正交相邻
                    if ((dx == 1 && dy == 0) || (dx == 0 && dy == 1))
                        return true;
                }
            }
            return false;
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取所有塔
        /// </summary>
        public List<InventoryTowerData> GetAllTowers()
        {
            return towerDictionary.Values.ToList();
        }

        /// <summary>
        /// 获取特定类型的塔
        /// </summary>
        public List<InventoryTowerData> GetTowersByType(string towerTypeId)
        {
            return towerDictionary.Values.Where(t => t.TowerTypeId == towerTypeId).ToList();
        }

        /// <summary>
        /// 获取特定位置的塔
        /// </summary>
        public InventoryTowerData GetTowerAt(int x, int y)
        {
            var cell = GetCell(x, y);
            if (cell != null && !string.IsNullOrEmpty(cell.TowerId))
            {
                towerDictionary.TryGetValue(cell.TowerId, out var tower);
                return tower;
            }
            return null;
        }

        /// <summary>
        /// 获取塔通过ID
        /// </summary>
        public InventoryTowerData GetTowerById(string towerId)
        {
            towerDictionary.TryGetValue(towerId, out var tower);
            return tower;
        }

        /// <summary>
        /// 获取背包使用统计
        /// </summary>
        public (int used, int total, int unlocked) GetInventoryStats()
        {
            int used = towerDictionary.Count;
            int unlocked = UnlockedCellCount;
            int total = GridWidth * GridHeight;
            return (used, total, unlocked);
        }

        #endregion

        #region 保存和加载

        /// <summary>
        /// 获取保存数据
        /// </summary>
        public InventorySaveData GetSaveData()
        {
            var saveData = new InventorySaveData
            {
                UnlockedCellCount = UnlockedCellCount,
                LastSaveTime = DateTime.Now
            };

            // 保存格子状态
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    saveData.GridCells.Add(gridCells[x, y]);
                }
            }

            // 保存塔数据
            saveData.Towers = towerDictionary.Values.ToList();

            return saveData;
        }

        /// <summary>
        /// 加载保存数据
        /// </summary>
        public void LoadSaveData(InventorySaveData saveData)
        {
            if (saveData == null) return;

            // 清空当前数据
            Initialize();

            UnlockedCellCount = saveData.UnlockedCellCount;

            // 恢复格子状态
            foreach (var cell in saveData.GridCells)
            {
                if (IsValidPosition(cell.X, cell.Y))
                {
                    gridCells[cell.X, cell.Y] = cell;
                }
            }

            // 恢复塔
            foreach (var tower in saveData.Towers)
            {
                towerDictionary[tower.InstanceId] = tower;
            }

            OnInventoryChanged?.Invoke();
        }

        #endregion
    }
}