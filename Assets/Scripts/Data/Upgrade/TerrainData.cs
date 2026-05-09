using UnityEngine;
using System;
using System.Collections.Generic;

namespace ZombieDefense.Upgrade.Data
{
    /// <summary>
    /// 地形类型
    /// </summary>
    public enum TerrainType
    {
        Normal,     // 普通地形
        Lava,       // 熔岩（持续伤害）
        Ice,        // 冰冻（减速）
        HighGround, // 高地（射程加成）
        Obstacle,   // 障碍物（阻挡）
        Portal,     // 传送门（瞬移）
        PoisonSwamp,// 毒沼（持续中毒）
        Electric    // 电场（麻痹）
    }

    /// <summary>
    /// 地形效果数据
    /// </summary>
    [Serializable]
    public class TerrainEffectData
    {
        public TerrainType TerrainType;
        public string TerrainName;
        public string Description;

        [Header("对敌人效果")]
        public float DamagePerSecond;           // 每秒伤害
        public float MoveSpeedModifier;         // 移速修正（1.0为正常）
        public bool CanBlockEnemy;              // 是否阻挡敌人
        public ElementType ApplyElement;        // 施加的元素
        public float ElementApplyInterval;      // 元素施加间隔

        [Header("对防御塔效果")]
        public float RangeModifier;             // 射程修正
        public float DamageModifier;            // 伤害修正
        public float FireRateModifier;          // 攻速修正
        public bool CanBuildTower;              // 是否可以建塔

        [Header("特殊效果")]
        public bool IsPortal;                   // 是否为传送门
        public Vector2Int PortalExit;           // 传送出口（如果是传送门）
        public float PortalCooldown;            // 传送冷却

        [Header("视觉")]
        public Sprite TerrainSprite;
        public Color TintColor;
        public ParticleSystem AmbientEffect;
        public GameObject TransitionEffect;
    }

    /// <summary>
    /// 地形格子数据
    /// </summary>
    [Serializable]
    public class TerrainCellData
    {
        public Vector2Int Position;
        public TerrainType CurrentType;
        public TerrainType DefaultType;
        public float EffectIntensity;           // 效果强度（0-1）
        public float RemainingDuration;         // 剩余持续时间（-1为永久）
        public bool IsDynamic;                  // 是否动态变化
        public string TriggerCondition;         // 触发条件描述
    }

    /// <summary>
    /// 地形变化事件
    /// </summary>
    [Serializable]
    public class TerrainChangeEvent
    {
        public string EventId;
        public string EventName;
        public int TriggerWave;                 // 触发的波次
        public float TriggerTime;               // 触发时间（波次开始后）
        public Vector2Int[] AffectedCells;      // 影响的格子
        public TerrainType TargetType;          // 目标地形类型
        public float TransitionDuration;        // 变化动画时长
        public bool IsReversible;               // 是否可恢复
        public float ReverseDelay;              // 恢复延迟
    }

    /// <summary>
    /// 地形配置
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainConfig", menuName = "Game/Upgrade/Terrain Config")]
    public class TerrainConfig : ScriptableObject
    {
        [Header("地形效果配置")]
        public List<TerrainEffectData> TerrainEffects;

        [Header("地形变化事件")]
        public List<TerrainChangeEvent> ChangeEvents;

        [Header("动态地形配置")]
        public float MinChangeInterval = 10f;   // 最小变化间隔
        public float MaxChangeInterval = 30f;   // 最大变化间隔
        public int MaxDynamicTerrains = 5;      // 最大动态地形数

        [Header("视觉效果")]
        public float TransitionEffectDuration = 1f;
        public AnimationCurve TransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>
        /// 获取地形效果数据
        /// </summary>
        public TerrainEffectData GetTerrainEffect(TerrainType type)
        {
            return TerrainEffects.Find(t => t.TerrainType == type);
        }

        /// <summary>
        /// 获取波次触发的地形变化事件
        /// </summary>
        public List<TerrainChangeEvent> GetEventsByWave(int wave)
        {
            return ChangeEvents.FindAll(e => e.TriggerWave == wave);
        }
    }

    /// <summary>
    /// 地形保存数据
    /// </summary>
    [Serializable]
    public class TerrainSaveData
    {
        public List<TerrainCellData> CellDataList;
        public List<string> TriggeredEventIds;
        public DateTime LastSaveTime;

        public TerrainSaveData()
        {
            CellDataList = new List<TerrainCellData>();
            TriggeredEventIds = new List<string>();
        }
    }
}