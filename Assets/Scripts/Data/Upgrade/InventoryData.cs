using UnityEngine;
using System;
using System.Collections.Generic;

namespace ZombieDefense.Upgrade.Data
{
    /// <summary>
    /// 背包格子状态
    /// </summary>
    public enum GridCellState
    {
        Empty,      // 空
        Occupied,   // 被占用
        Locked      // 锁定（未解锁）
    }

    /// <summary>
    /// 背包格子数据
    /// </summary>
    [Serializable]
    public class GridCellData
    {
        public int X;                           // 格子X坐标
        public int Y;                           // 格子Y坐标
        public GridCellState State;             // 格子状态
        public string TowerId;                  // 占据的塔ID（如果有）
        public Vector2Int[] OccupiedCells;      // 该塔占用的所有格子（用于多格塔）

        public GridCellData(int x, int y)
        {
            X = x;
            Y = y;
            State = GridCellState.Empty;
            TowerId = string.Empty;
            OccupiedCells = null;
        }
    }

    /// <summary>
    /// 防御塔占用空间类型
    /// </summary>
    public enum TowerSpaceType
    {
        Single,     // 1格
        Double,     // 2格（横向或纵向）
        Quad,       // 4格（2x2）
        LShape      // L型（3格）
    }

    /// <summary>
    /// 背包中的防御塔数据
    /// </summary>
    [Serializable]
    public class InventoryTowerData
    {
        public string InstanceId;               // 实例ID（唯一）
        public string TowerTypeId;              // 塔类型ID
        public string TowerName;                // 塔名称
        public int Level;                       // 等级
        public TowerSpaceType SpaceType;        // 占用空间类型
        public Vector2Int Position;             // 左上角位置
        public Vector2Int[] OccupiedPositions;  // 占用的所有格子位置
        public ElementType ElementType;         // 元素类型
        public int Quantity;                    // 数量（用于合成）

        public InventoryTowerData()
        {
            InstanceId = Guid.NewGuid().ToString();
            Level = 1;
            Quantity = 1;
        }
    }

    /// <summary>
    /// 背包配置数据
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryConfig", menuName = "Game/Upgrade/Inventory Config")]
    public class InventoryConfig : ScriptableObject
    {
        [Header("背包配置")]
        public int DefaultGridSize = 9;         // 默认9宫格
        public int MaxGridSize = 16;            // 最大16宫格
        public int InitialUnlockedCells = 9;    // 初始解锁格子数

        [Header("解锁配置")]
        public int[] UnlockCosts = new int[] { 100, 200, 300, 500 };  // 解锁费用

        [Header("合成配置")]
        public int MergeRequiredCount = 3;      // 合成所需数量
        public bool RequireAdjacent = true;     // 是否需要相邻

        [Header("塔空间配置")]
        public TowerSpaceConfig[] TowerSpaceConfigs;
    }

    [Serializable]
    public class TowerSpaceConfig
    {
        public string TowerTypeId;
        public TowerSpaceType SpaceType;
        public Vector2Int[] RelativePositions;  // 相对位置（从左上角开始）
    }

    /// <summary>
    /// 背包数据（可序列化保存）
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        public int UnlockedCellCount;                       // 已解锁格子数
        public List<GridCellData> GridCells;                // 格子数据
        public List<InventoryTowerData> Towers;             // 塔列表
        public DateTime LastSaveTime;                       // 最后保存时间

        public InventorySaveData()
        {
            GridCells = new List<GridCellData>();
            Towers = new List<InventoryTowerData>();
        }
    }
}