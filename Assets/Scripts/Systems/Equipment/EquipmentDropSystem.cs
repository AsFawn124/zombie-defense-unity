using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

/// <summary>
/// 装备掉落系统
/// 管理关卡通关后的装备掉落逻辑
/// 对应 TASK-015
/// </summary>
public class EquipmentDropSystem : MonoBehaviour
{
    public static EquipmentDropSystem Instance { get; private set; }

    [Header("配置引用")]
    public EquipmentSystemConfig Config;

    [Header("掉落事件")]
    public event Action<List<EquipmentItem>> OnEquipmentDropped;   // 装备掉落事件
    public event Action<EquipmentItem> OnEquipmentDroppedSingle;   // 单件掉落

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 关卡通关时触发掉落
    /// </summary>
    public List<EquipmentItem> ProcessStageDrop(string stageId, int waveCount, int difficulty = 1)
    {
        var droppedItems = new List<EquipmentItem>();
        if (Config == null)
        {
            Debug.LogWarning("[EquipmentDrop] 配置为空");
            return droppedItems;
        }

        // 查找该关卡的掉落配置
        var dropConfig = Config.DropConfigs.Find(d => d.StageId == stageId);
        List<QualityWeight> weights;

        if (dropConfig != null && dropConfig.QualityWeights != null && dropConfig.QualityWeights.Count > 0)
        {
            weights = dropConfig.QualityWeights;
        }
        else
        {
            weights = Config.GetDefaultQualityWeights();
        }

        // 计算掉落数量
        int baseDropCount = dropConfig?.DropCount ?? 1;
        float globalRate = Config.GlobalDropRate;

        // 根据波次和难度调整
        float dropRateBonus = waveCount * 0.02f + (difficulty - 1) * 0.1f;
        float effectiveRate = Mathf.Min(globalRate + dropRateBonus, 1.0f);

        int dropCount = 0;
        for (int i = 0; i < baseDropCount + waveCount / 10; i++)
        {
            if (Random.value < effectiveRate)
                dropCount++;
        }

        // 限制每波最多掉落
        dropCount = Mathf.Min(dropCount, Config.MaxDropPerWave);

        // 生成掉落装备
        for (int i = 0; i < dropCount; i++)
        {
            EquipmentQuality quality = RollQuality(weights, difficulty);
            var equipment = EquipmentManager.Instance?.GenerateEquipment(quality, stageId);

            if (equipment != null)
            {
                droppedItems.Add(equipment);
                EquipmentManager.Instance?.AddEquipment(equipment);
                OnEquipmentDroppedSingle?.Invoke(equipment);
            }
        }

        if (droppedItems.Count > 0)
        {
            OnEquipmentDropped?.Invoke(droppedItems);
            Debug.Log($"[EquipmentDrop] 关卡 [{stageId}] 掉落 {droppedItems.Count} 件装备");
        }

        return droppedItems;
    }

    /// <summary>
    /// 权重随机品质
    /// </summary>
    public EquipmentQuality RollQuality(List<QualityWeight> weights, int difficulty = 1)
    {
        if (weights == null || weights.Count == 0)
            return EquipmentQuality.Common;

        // 根据难度调整：增加高品质权重
        var adjustedWeights = new List<QualityWeight>();
        foreach (var w in weights)
        {
            adjustedWeights.Add(new QualityWeight
            {
                Quality = w.Quality,
                Weight = w.Quality >= EquipmentQuality.Epic
                    ? w.Weight * (1f + (difficulty - 1) * 0.3f)
                    : w.Weight
            });
        }

        float totalWeight = adjustedWeights.Sum(w => w.Weight);
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var w in adjustedWeights)
        {
            cumulative += w.Weight;
            if (roll <= cumulative)
                return w.Quality;
        }

        return adjustedWeights[adjustedWeights.Count - 1].Quality;
    }

    /// <summary>
    /// Boss掉落（保底高品质）
    /// </summary>
    public EquipmentItem ProcessBossDrop(string stageId, int bossTier)
    {
        // Boss掉落：最低蓝色品质
        EquipmentQuality minQuality = bossTier switch
        {
            1 => EquipmentQuality.Rare,          // 小Boss：蓝+
            2 => EquipmentQuality.Epic,          // 中Boss：紫+
            3 => EquipmentQuality.Legendary,     // 大Boss：橙+
            _ => EquipmentQuality.Rare
        };

        // 在最低品质以上随机
        int maxQualityValue = Mathf.Min((int)minQuality + 2, (int)EquipmentQuality.Prismatic);
        int rolled = Random.Range((int)minQuality, maxQualityValue + 1);
        EquipmentQuality quality = (EquipmentQuality)rolled;

        var equipment = EquipmentManager.Instance?.GenerateEquipment(quality, stageId);
        if (equipment != null)
        {
            EquipmentManager.Instance?.AddEquipment(equipment);
            OnEquipmentDroppedSingle?.Invoke(equipment);
            Debug.Log($"[EquipmentDrop] Boss掉落: {equipment.EquipmentName} ({EquipmentItem.GetQualityName(quality)})");
        }

        return equipment;
    }

    /// <summary>
    /// 获取指定关卡的掉落信息（预览用）
    /// </summary>
    public EquipmentDropPreview GetDropPreview(string stageId)
    {
        var dropConfig = Config.DropConfigs.Find(d => d.StageId == stageId);
        var weights = dropConfig?.QualityWeights ?? Config.GetDefaultQualityWeights();

        return new EquipmentDropPreview
        {
            StageId = stageId,
            DropCount = dropConfig?.DropCount ?? 1,
            QualityWeights = weights
        };
    }
}

/// <summary>
/// 掉落预览
/// </summary>
[Serializable]
public class EquipmentDropPreview
{
    public string StageId;
    public int DropCount;
    public List<QualityWeight> QualityWeights;

    public string GetDropSummary()
    {
        if (QualityWeights == null || QualityWeights.Count == 0)
            return "无掉落信息";

        float totalWeight = QualityWeights.Sum(w => w.Weight);
        var lines = new List<string> { $"预计掉落: {DropCount} 件" };

        foreach (var w in QualityWeights)
        {
            float pct = w.Weight / totalWeight * 100f;
            lines.Add($"{EquipmentItem.GetQualityName(w.Quality)}: {pct:F1}%");
        }

        return string.Join("\n", lines);
    }
}
