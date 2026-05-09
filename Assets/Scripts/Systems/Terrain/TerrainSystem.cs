using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ZombieDefense.Upgrade.Data;

namespace ZombieDefense.Upgrade.Systems.Terrain
{
    /// <summary>
    /// 地形系统 - 管理动态地形和地形效果
    /// </summary>
    public class TerrainSystem : MonoBehaviour
    {
        public static TerrainSystem Instance { get; private set; }

        [Header("配置")]
        public TerrainConfig Config;

        [Header("地图设置")]
        public int MapWidth = 10;
        public int MapHeight = 10;
        public float CellSize = 1f;

        [Header("当前状态")]
        public int CurrentWave = 0;
        public float WaveTime = 0f;

        // 地形数据
        private TerrainCellData[,] terrainGrid;
        private List<TerrainChangeEvent> activeEvents;
        private Dictionary<string, bool> triggeredEvents;

        // 事件
        public event Action<Vector2Int, TerrainType, TerrainType> OnTerrainChanged;
        public event Action<TerrainChangeEvent> OnTerrainEventTriggered;
        public event Action<TerrainCellData> OnTerrainEffectApplied;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            terrainGrid = new TerrainCellData[MapWidth, MapHeight];
            activeEvents = new List<TerrainChangeEvent>();
            triggeredEvents = new Dictionary<string, bool>();

            if (Config == null)
            {
                Config = ScriptableObject.CreateInstance<TerrainConfig>();
            }

            InitializeTerrain();
        }

        private void Update()
        {
            if (CurrentWave > 0)
            {
                WaveTime += Time.deltaTime;
                CheckTerrainEvents();
            }

            UpdateTerrainEffects();
        }

        #region 地形初始化

        /// <summary>
        /// 初始化地形
        /// </summary>
        private void InitializeTerrain()
        {
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    terrainGrid[x, y] = new TerrainCellData
                    {
                        Position = new Vector2Int(x, y),
                        CurrentType = TerrainType.Normal,
                        DefaultType = TerrainType.Normal,
                        EffectIntensity = 1f,
                        RemainingDuration = -1,
                        IsDynamic = false
                    };
                }
            }
        }

        /// <summary>
        /// 从配置加载地形
        /// </summary>
        public void LoadTerrainFromConfig(TerrainSaveData saveData)
        {
            if (saveData?.CellDataList != null)
            {
                foreach (var cellData in saveData.CellDataList)
                {
                    if (IsValidPosition(cellData.Position))
                    {
                        terrainGrid[cellData.Position.x, cellData.Position.y] = cellData;
                    }
                }
            }

            if (saveData?.TriggeredEventIds != null)
            {
                foreach (var eventId in saveData.TriggeredEventIds)
                {
                    triggeredEvents[eventId] = true;
                }
            }
        }

        #endregion

        #region 地形查询

        /// <summary>
        /// 获取地形格子
        /// </summary>
        public TerrainCellData GetCell(Vector2Int position)
        {
            return GetCell(position.x, position.y);
        }

        /// <summary>
        /// 获取地形格子
        /// </summary>
        public TerrainCellData GetCell(int x, int y)
        {
            if (IsValidPosition(x, y))
                return terrainGrid[x, y];
            return null;
        }

        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        public bool IsValidPosition(Vector2Int position)
        {
            return IsValidPosition(position.x, position.y);
        }

        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < MapWidth && y >= 0 && y < MapHeight;
        }

        /// <summary>
        /// 获取地形类型
        /// </summary>
        public TerrainType GetTerrainType(Vector2Int position)
        {
            var cell = GetCell(position);
            return cell?.CurrentType ?? TerrainType.Normal;
        }

        /// <summary>
        /// 获取地形效果
        /// </summary>
        public TerrainEffectData GetTerrainEffect(Vector2Int position)
        {
            var terrainType = GetTerrainType(position);
            return Config?.GetTerrainEffect(terrainType);
        }

        #endregion

        #region 地形修改

        /// <summary>
        /// 改变地形
        /// </summary>
        public bool ChangeTerrain(Vector2Int position, TerrainType newType, float duration = -1, bool playEffect = true)
        {
            if (!IsValidPosition(position))
                return false;

            var cell = terrainGrid[position.x, position.y];
            TerrainType oldType = cell.CurrentType;

            if (oldType == newType)
                return false;

            cell.CurrentType = newType;
            cell.RemainingDuration = duration;
            cell.IsDynamic = duration > 0;

            OnTerrainChanged?.Invoke(position, oldType, newType);

            if (playEffect)
            {
                PlayTransitionEffect(position, oldType, newType);
            }

            return true;
        }

        /// <summary>
        /// 批量改变地形
        /// </summary>
        public void ChangeTerrainBatch(Vector2Int[] positions, TerrainType newType, float duration = -1)
        {
            foreach (var pos in positions)
            {
                ChangeTerrain(pos, newType, duration);
            }
        }

        /// <summary>
        /// 恢复地形到默认状态
        /// </summary>
        public void RestoreTerrain(Vector2Int position)
        {
            var cell = GetCell(position);
            if (cell != null)
            {
                ChangeTerrain(position, cell.DefaultType);
            }
        }

        /// <summary>
        /// 播放地形变化特效
        /// </summary>
        private void PlayTransitionEffect(Vector2Int position, TerrainType fromType, TerrainType toType)
        {
            var effectData = Config?.GetTerrainEffect(toType);
            if (effectData?.TransitionEffect != null)
            {
                Vector3 worldPos = GridToWorldPosition(position);
                Instantiate(effectData.TransitionEffect, worldPos, Quaternion.identity);
            }
        }

        #endregion

        #region 地形事件

        /// <summary>
        /// 开始新波次
        /// </summary>
        public void StartWave(int waveNumber)
        {
            CurrentWave = waveNumber;
            WaveTime = 0f;

            // 检查该波次的地形事件
            var events = Config?.GetEventsByWave(waveNumber);
            if (events != null)
            {
                foreach (var evt in events)
                {
                    if (!triggeredEvents.ContainsKey(evt.EventId))
                    {
                        activeEvents.Add(evt);
                    }
                }
            }
        }

        /// <summary>
        /// 检查地形事件
        /// </summary>
        private void CheckTerrainEvents()
        {
            var triggered = new List<TerrainChangeEvent>();

            foreach (var evt in activeEvents)
            {
                if (triggeredEvents.ContainsKey(evt.EventId))
                    continue;

                // 检查触发条件
                bool shouldTrigger = false;

                if (evt.TriggerWave == CurrentWave && evt.TriggerTime <= 0)
                {
                    // 波次开始时触发
                    shouldTrigger = true;
                }
                else if (evt.TriggerWave == CurrentWave && WaveTime >= evt.TriggerTime)
                {
                    // 波次中定时触发
                    shouldTrigger = true;
                }

                if (shouldTrigger)
                {
                    TriggerTerrainEvent(evt);
                    triggered.Add(evt);
                }
            }

            foreach (var evt in triggered)
            {
                activeEvents.Remove(evt);
            }
        }

        /// <summary>
        /// 触发地形事件
        /// </summary>
        private void TriggerTerrainEvent(TerrainChangeEvent evt)
        {
            triggeredEvents[evt.EventId] = true;

            // 应用地形变化
            if (evt.AffectedCells != null)
            {
                float duration = evt.IsReversible ? evt.ReverseDelay : -1;
                ChangeTerrainBatch(evt.AffectedCells, evt.TargetType, duration);
            }

            OnTerrainEventTriggered?.Invoke(evt);

            Debug.Log($"[Terrain] 触发地形事件: {evt.EventName}");
        }

        /// <summary>
        /// 手动触发地形事件
        /// </summary>
        public void TriggerEventManually(string eventId)
        {
            var evt = Config?.ChangeEvents?.Find(e => e.EventId == eventId);
            if (evt != null && !triggeredEvents.ContainsKey(eventId))
            {
                TriggerTerrainEvent(evt);
            }
        }

        #endregion

        #region 地形效果更新

        /// <summary>
        /// 更新地形效果
        /// </summary>
        private void UpdateTerrainEffects()
        {
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    var cell = terrainGrid[x, y];

                    if (cell.IsDynamic && cell.RemainingDuration > 0)
                    {
                        cell.RemainingDuration -= Time.deltaTime;

                        if (cell.RemainingDuration <= 0)
                        {
                            // 恢复默认地形
                            RestoreTerrain(cell.Position);
                        }
                    }
                }
            }
        }

        #endregion

        #region 效果应用

        /// <summary>
        /// 获取对敌人的效果
        /// </summary>
        public (float damage, float speedMod, bool blocked) GetEnemyEffect(Vector2Int position)
        {
            var effect = GetTerrainEffect(position);
            if (effect == null)
                return (0, 1f, false);

            return (effect.DamagePerSecond, effect.MoveSpeedModifier, effect.CanBlockEnemy);
        }

        /// <summary>
        /// 获取对防御塔的效果
        /// </summary>
        public (float rangeMod, float damageMod, float fireRateMod, bool canBuild) GetTowerEffect(Vector2Int position)
        {
            var effect = GetTerrainEffect(position);
            if (effect == null)
                return (1f, 1f, 1f, true);

            return (effect.RangeModifier, effect.DamageModifier, effect.FireRateModifier, effect.CanBuildTower);
        }

        /// <summary>
        /// 检查是否可以建造防御塔
        /// </summary>
        public bool CanBuildTower(Vector2Int position)
        {
            var effect = GetTerrainEffect(position);
            return effect?.CanBuildTower ?? true;
        }

        /// <summary>
        /// 检查是否阻挡敌人
        /// </summary>
        public bool BlocksEnemy(Vector2Int position)
        {
            var effect = GetTerrainEffect(position);
            return effect?.CanBlockEnemy ?? false;
        }

        #endregion

        #region 传送门

        /// <summary>
        /// 获取传送门出口
        /// </summary>
        public Vector2Int? GetPortalExit(Vector2Int entrance)
        {
            var cell = GetCell(entrance);
            if (cell?.CurrentType == TerrainType.Portal)
            {
                var effect = Config?.GetTerrainEffect(TerrainType.Portal);
                if (effect != null && IsValidPosition(effect.PortalExit))
                {
                    return effect.PortalExit;
                }
            }
            return null;
        }

        /// <summary>
        /// 设置传送门
        /// </summary>
        public void SetPortal(Vector2Int entrance, Vector2Int exit, float cooldown = 5f)
        {
            ChangeTerrain(entrance, TerrainType.Portal);
            ChangeTerrain(exit, TerrainType.Portal);

            // TODO: 配置传送门出口和冷却
        }

        #endregion

        #region 坐标转换

        /// <summary>
        /// 网格坐标转世界坐标
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * CellSize, gridPos.y * CellSize, 0);
        }

        /// <summary>
        /// 世界坐标转网格坐标
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / CellSize),
                Mathf.FloorToInt(worldPos.y / CellSize)
            );
        }

        #endregion

        #region 保存和加载

        /// <summary>
        /// 获取保存数据
        /// </summary>
        public TerrainSaveData GetSaveData()
        {
            var saveData = new TerrainSaveData();

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    saveData.CellDataList.Add(terrainGrid[x, y]);
                }
            }

            saveData.TriggeredEventIds = triggeredEvents.Keys.ToList();
            saveData.LastSaveTime = DateTime.Now;

            return saveData;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取所有特定类型的地形格子
        /// </summary>
        public List<TerrainCellData> GetCellsByType(TerrainType type)
        {
            var result = new List<TerrainCellData>();
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    if (terrainGrid[x, y].CurrentType == type)
                    {
                        result.Add(terrainGrid[x, y]);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 获取地形统计
        /// </summary>
        public Dictionary<TerrainType, int> GetTerrainStatistics()
        {
            var stats = new Dictionary<TerrainType, int>();
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    var type = terrainGrid[x, y].CurrentType;
                    if (!stats.ContainsKey(type))
                        stats[type] = 0;
                    stats[type]++;
                }
            }
            return stats;
        }

        /// <summary>
        /// 重置地形
        /// </summary>
        public void ResetTerrain()
        {
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    RestoreTerrain(new Vector2Int(x, y));
                }
            }

            activeEvents.Clear();
            triggeredEvents.Clear();
            CurrentWave = 0;
            WaveTime = 0f;
        }

        #endregion
    }
}