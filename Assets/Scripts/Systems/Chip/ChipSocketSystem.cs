using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 芯片镶嵌系统
/// 管理芯片的镶嵌、拆卸、槽位管理、套装激活
/// 对应 TASK-019
/// </summary>
public class ChipSocketSystem : MonoBehaviour
{
    public static ChipSocketSystem Instance { get; private set; }

    [Header("配置")]
    public ChipSystemConfig Config;

    // 事件
    public event Action<ChipInstance, EquipmentItem> OnChipEmbedded;    // 镶嵌成功
    public event Action<ChipInstance, EquipmentItem> OnChipRemoved;     // 拆卸成功
    public event Action<List<ActiveSetBonus>> OnSetBonusChanged;        // 套装效果变更

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

    #region 镶嵌与拆卸

    /// <summary>
    /// 镶嵌芯片到装备
    /// </summary>
    public bool EmbedChip(string chipInstanceId, string equipmentInstanceId, int slotIndex = -1)
    {
        var chip = ChipManager.Instance?.GetChipById(chipInstanceId);
        var equipment = EquipmentManager.Instance?.GetEquipmentById(equipmentInstanceId);

        if (chip == null || equipment == null)
        {
            Debug.LogWarning("[ChipSocket] 芯片或装备无效");
            return false;
        }

        // 检查芯片是否已镶嵌
        if (chip.IsEmbedded)
        {
            Debug.LogWarning("[ChipSocket] 芯片已镶嵌在其他装备上");
            return false;
        }

        // 检查装备品质是否支持芯片槽
        if (equipment.ChipSlotCount <= 0)
        {
            Debug.LogWarning("[ChipSocket] 该装备不支持镶嵌芯片");
            return false;
        }

        // 检查槽位
        var embeddedMap = ChipManager.Instance.GetEmbeddedMap();
        if (!embeddedMap.ContainsKey(equipment.InstanceId))
            embeddedMap[equipment.InstanceId] = new List<string>();

        var embeddedList = embeddedMap[equipment.InstanceId];

        if (embeddedList.Count >= equipment.ChipSlotCount)
        {
            Debug.LogWarning($"[ChipSocket] 芯片槽已满 ({embeddedList.Count}/{equipment.ChipSlotCount})");
            return false;
        }

        // 指定槽位
        if (slotIndex >= 0 && slotIndex < equipment.ChipSlotCount)
        {
            if (slotIndex < embeddedList.Count && !string.IsNullOrEmpty(embeddedList[slotIndex]))
            {
                Debug.LogWarning($"[ChipSocket] 槽位 {slotIndex} 已被占用");
                return false;
            }
        }
        else
        {
            slotIndex = embeddedList.Count;
        }

        // 执行镶嵌
        if (slotIndex < embeddedList.Count)
            embeddedList[slotIndex] = chip.InstanceId;
        else
            embeddedList.Add(chip.InstanceId);

        chip.IsEmbedded = true;
        chip.EmbeddedEquipmentId = equipment.InstanceId;
        chip.EmbeddedSlotIndex = slotIndex;

        ChipManager.Instance.SetEmbeddedMap(embeddedMap);

        // 金币消耗
        int cost = Config?.EmbedCost ?? 200;
        // 金币扣除由GameManager处理

        OnChipEmbedded?.Invoke(chip, equipment);
        CheckSetBonuses();

        Debug.Log($"[ChipSocket] 镶嵌 {chip.ChipName} → {equipment.EquipmentName} 槽位{slotIndex}");
        return true;
    }

    /// <summary>
    /// 拆卸芯片
    /// </summary>
    public bool RemoveChip(string equipmentInstanceId, int slotIndex)
    {
        var equipment = EquipmentManager.Instance?.GetEquipmentById(equipmentInstanceId);
        if (equipment == null)
        {
            Debug.LogWarning("[ChipSocket] 装备无效");
            return false;
        }

        var embeddedMap = ChipManager.Instance.GetEmbeddedMap();
        if (!embeddedMap.TryGetValue(equipment.InstanceId, out var embeddedList) ||
            slotIndex >= embeddedList.Count)
        {
            Debug.LogWarning("[ChipSocket] 槽位为空");
            return false;
        }

        string chipId = embeddedList[slotIndex];
        if (string.IsNullOrEmpty(chipId))
        {
            Debug.LogWarning("[ChipSocket] 槽位为空");
            return false;
        }

        var chip = ChipManager.Instance.GetChipById(chipId);
        if (chip == null)
        {
            // 清理无效引用
            embeddedList[slotIndex] = null;
            return false;
        }

        // 拆卸成本
        int cost = Config?.RemoveCost ?? 100;

        // 执行拆卸
        chip.IsEmbedded = false;
        chip.EmbeddedEquipmentId = string.Empty;
        chip.EmbeddedSlotIndex = -1;

        embeddedList[slotIndex] = null;

        // 清理空槽
        embeddedList.RemoveAll(string.IsNullOrEmpty);

        ChipManager.Instance.SetEmbeddedMap(embeddedMap);

        // 拆卸后是否销毁芯片
        if (Config?.DestroyOnRemove ?? false)
        {
            ChipManager.Instance.RemoveChip(chip.InstanceId);
            Debug.Log($"[ChipSocket] 拆卸并销毁 {chip.ChipName}");
        }

        OnChipRemoved?.Invoke(chip, equipment);
        CheckSetBonuses();

        Debug.Log($"[ChipSocket] 拆卸 {chip.ChipName} ← {equipment.EquipmentName}");
        return true;
    }

    /// <summary>
    /// 一键拆卸所有芯片
    /// </summary>
    public int RemoveAllChips(string equipmentInstanceId)
    {
        var equipment = EquipmentManager.Instance?.GetEquipmentById(equipmentInstanceId);
        if (equipment == null) return 0;

        var embeddedMap = ChipManager.Instance.GetEmbeddedMap();
        if (!embeddedMap.TryGetValue(equipment.InstanceId, out var embeddedList))
            return 0;

        int removedCount = 0;
        for (int i = embeddedList.Count - 1; i >= 0; i--)
        {
            if (RemoveChip(equipmentInstanceId, i))
                removedCount++;
        }

        return removedCount;
    }

    /// <summary>
    /// 检查是否可以镶嵌
    /// </summary>
    public bool CanEmbed(string equipmentInstanceId)
    {
        var equipment = EquipmentManager.Instance?.GetEquipmentById(equipmentInstanceId);
        if (equipment == null || equipment.ChipSlotCount <= 0) return false;

        var embeddedMap = ChipManager.Instance.GetEmbeddedMap();
        if (!embeddedMap.TryGetValue(equipment.InstanceId, out var embeddedList))
            return true;

        int filledCount = embeddedList.Count(s => !string.IsNullOrEmpty(s));
        return filledCount < equipment.ChipSlotCount;
    }

    /// <summary>
    /// 获取装备芯片槽状态
    /// </summary>
    public List<ChipSocketState> GetSocketStates(string equipmentInstanceId)
    {
        var states = new List<ChipSocketState>();
        var equipment = EquipmentManager.Instance?.GetEquipmentById(equipmentInstanceId);
        if (equipment == null) return states;

        var embeddedMap = ChipManager.Instance.GetEmbeddedMap();
        embeddedMap.TryGetValue(equipment.InstanceId, out var embeddedList);

        for (int i = 0; i < equipment.ChipSlotCount; i++)
        {
            var state = new ChipSocketState
            {
                SlotIndex = i,
                IsOccupied = embeddedList != null && i < embeddedList.Count && !string.IsNullOrEmpty(embeddedList[i])
            };

            if (state.IsOccupied)
            {
                state.EmbeddedChip = ChipManager.Instance.GetChipById(embeddedList[i]);
            }

            states.Add(state);
        }

        return states;
    }

    #endregion

    #region 套装效果

    /// <summary>
    /// 检查并触发套装效果变更
    /// </summary>
    public void CheckSetBonuses()
    {
        var activeSets = ChipManager.Instance?.GetActiveSetBonuses();
        OnSetBonusChanged?.Invoke(activeSets);

        if (activeSets != null && activeSets.Count > 0)
        {
            foreach (var set in activeSets)
            {
                Debug.Log($"[ChipSocket] 套装效果激活: {set.GetDescription()}");
            }
        }
    }

    /// <summary>
    /// 获取当前激活的所有套装效果加成
    /// </summary>
    public EquipmentStats CalculateSetBonusStats()
    {
        var stats = new EquipmentStats();
        var activeSets = ChipManager.Instance?.GetActiveSetBonuses();

        if (activeSets == null) return stats;

        foreach (var set in activeSets)
        {
            if (set.Effect == null) continue;

            float value = set.Effect.Value;
            switch (set.Effect.ValueType)
            {
                case AffixValueType.Fixed:
                    ApplyEffectToStats(ref stats, set.Effect.EffectType, value);
                    break;
                case AffixValueType.Percentage:
                    ApplyEffectToStats(ref stats, set.Effect.EffectType, value * 100f);
                    break;
            }
        }

        return stats;
    }

    private void ApplyEffectToStats(ref EquipmentStats stats, AffixType type, float value)
    {
        switch (type)
        {
            case AffixType.Attack: stats.AttackBonus += value; break;
            case AffixType.Defense: stats.DefenseBonus += value; break;
            case AffixType.Health: stats.HealthBonus += value; break;
            case AffixType.CritChance: stats.CritChanceBonus += value; break;
            case AffixType.CritDamage: stats.CritDamageBonus += value; break;
            case AffixType.Speed: stats.AttackSpeedBonus += value; break;
            case AffixType.CooldownReduction: stats.CooldownReduction += value; break;
            case AffixType.ElementalMastery: stats.ElementalMastery += value; break;
            case AffixType.ElementalResistance: stats.ElementalResistance += value; break;
            case AffixType.LifeSteal: stats.LifeSteal += value; break;
            case AffixType.DamageReduction: stats.DamageReduction += value; break;
        }
    }

    #endregion
}

/// <summary>
/// 芯片槽状态
/// </summary>
[Serializable]
public class ChipSocketState
{
    public int SlotIndex;
    public bool IsOccupied;
    public ChipInstance EmbeddedChip;

    public string GetDisplayText()
    {
        if (!IsOccupied || EmbeddedChip == null)
            return "空槽";
        return $"{EmbeddedChip.ChipName} Lv.{EmbeddedChip.Level}";
    }
}
