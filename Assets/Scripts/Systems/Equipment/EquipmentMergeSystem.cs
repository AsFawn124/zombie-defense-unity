using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

/// <summary>
/// 装备合成系统
/// 5合1合成：5件同品质同部位装备合成1件更高品质
/// 对应 TASK-015
/// </summary>
public class EquipmentMergeSystem : MonoBehaviour
{
    public static EquipmentMergeSystem Instance { get; private set; }

    [Header("配置引用")]
    public EquipmentSystemConfig Config;

    [Header("合成事件")]
    public event Action<EquipmentItem, List<EquipmentItem>> OnMergeStarted;        // 开始合成
    public event Action<EquipmentItem> OnMergeSuccess;                             // 合成成功
    public event Action<string> OnMergeFailed;                                     // 合成失败

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
    /// 5合1合成
    /// </summary>
    public EquipmentItem Merge5To1(List<EquipmentItem> materials)
    {
        if (Config == null)
        {
            OnMergeFailed?.Invoke("配置为空");
            return null;
        }

        // 验证：恰好5件
        if (materials.Count != Config.MergeRequiredCount)
        {
            OnMergeFailed?.Invoke($"合成需要 {Config.MergeRequiredCount} 件相同品质装备");
            return null;
        }

        // 验证：全部同一品质
        EquipmentQuality quality = materials[0].Quality;
        if (!materials.All(m => m.Quality == quality))
        {
            OnMergeFailed?.Invoke("所有材料品质必须相同");
            return null;
        }

        // 验证：全部同一部位
        EquipmentSlotType slotType = materials[0].SlotType;
        if (!materials.All(m => m.SlotType == slotType))
        {
            OnMergeFailed?.Invoke("所有材料部位必须相同");
            return null;
        }

        // 验证：最高品质不能再合
        if (quality >= EquipmentQuality.Prismatic)
        {
            OnMergeFailed?.Invoke("已达到最高品质，无法继续合成");
            return null;
        }

        // 验证：不能使用已装备的装备
        if (materials.Any(m => EquipmentManager.Instance.IsEquipped(m.InstanceId)))
        {
            OnMergeFailed?.Invoke("不能使用已装备的装备作为合成材料");
            return null;
        }

        OnMergeStarted?.Invoke(null, materials);

        // 检查金币
        int goldCost = GetMergeGoldCost(quality);
        // 金币检查由GameManager处理，这里只计算消耗

        // 计算成功率
        float successRate = CalculateSuccessRate(quality);

        if (Random.value <= successRate)
        {
            // 合成成功：消费材料
            foreach (var mat in materials)
            {
                EquipmentManager.Instance.SellEquipment(mat.InstanceId);
            }

            // 生成新装备（品质提升一级）
            EquipmentQuality targetQuality = quality + 1;
            var result = EquipmentManager.Instance?.GenerateEquipment(targetQuality);
            if (result != null)
            {
                EquipmentManager.Instance?.AddEquipment(result);

                OnMergeSuccess?.Invoke(result);
                Debug.Log($"[EquipmentMerge] 合成成功! {EquipmentItem.GetQualityName(quality)} → {EquipmentItem.GetQualityName(targetQuality)} {result.EquipmentName}");
                return result;
            }
        }
        else
        {
            // 合成失败：消耗材料但不返还
            foreach (var mat in materials)
            {
                EquipmentManager.Instance.SellEquipment(mat.InstanceId);
            }

            OnMergeFailed?.Invoke("合成失败，材料已销毁");
            Debug.Log("[EquipmentMerge] 合成失败，材料已销毁");
        }

        return null;
    }

    /// <summary>
    /// 计算合成成功率
    /// </summary>
    public float CalculateSuccessRate(EquipmentQuality quality)
    {
        float baseRate = quality switch
        {
            EquipmentQuality.Common => 0.95f,       // 白→绿：95%
            EquipmentQuality.Uncommon => 0.85f,      // 绿→蓝：85%
            EquipmentQuality.Rare => 0.75f,          // 蓝→紫：75%
            EquipmentQuality.Epic => 0.60f,          // 紫→橙：60%
            EquipmentQuality.Legendary => 0.40f,     // 橙→红：40%
            EquipmentQuality.Mythic => 0.20f,        // 红→彩：20%
            _ => 1.0f
        };

        // 品质加成
        if (Config != null)
            baseRate += Config.QualityUpChance;

        return Mathf.Clamp01(baseRate);
    }

    /// <summary>
    /// 计算合成金币消耗
    /// </summary>
    public int GetMergeGoldCost(EquipmentQuality quality)
    {
        if (Config == null) return 500;

        return quality switch
        {
            EquipmentQuality.Common => Config.BaseMergeGoldCost,            // 1000
            EquipmentQuality.Uncommon => Config.BaseMergeGoldCost * 2,     // 2000
            EquipmentQuality.Rare => Config.BaseMergeGoldCost * 4,         // 4000
            EquipmentQuality.Epic => Config.BaseMergeGoldCost * 8,         // 8000
            EquipmentQuality.Legendary => Config.BaseMergeGoldCost * 15,   // 15000
            EquipmentQuality.Mythic => Config.BaseMergeGoldCost * 30,      // 30000
            _ => Config.BaseMergeGoldCost
        };
    }

    /// <summary>
    /// 获取合成预览
    /// </summary>
    public MergePreview GetMergePreview(List<EquipmentItem> materials)
    {
        if (materials == null || materials.Count == 0)
            return new MergePreview { Valid = false, Message = "请选择材料" };

        var preview = new MergePreview();

        EquipmentQuality quality = materials[0].Quality;
        EquipmentSlotType slotType = materials[0].SlotType;

        // 检查一致性
        bool sameQuality = materials.All(m => m.Quality == quality);
        bool sameSlot = materials.All(m => m.SlotType == slotType);
        bool notEquipped = materials.All(m => !EquipmentManager.Instance.IsEquipped(m.InstanceId));
        bool enoughMaterials = materials.Count == Config.MergeRequiredCount;
        bool canMergeUp = quality < EquipmentQuality.Prismatic;

        preview.Valid = sameQuality && sameSlot && notEquipped && enoughMaterials && canMergeUp;
        preview.CurrentQuality = quality;
        preview.TargetQuality = canMergeUp ? quality + 1 : quality;
        preview.SlotType = slotType;
        preview.SuccessRate = canMergeUp ? CalculateSuccessRate(quality) : 0;
        preview.GoldCost = canMergeUp ? GetMergeGoldCost(quality) : 0;
        preview.MaterialCount = materials.Count;
        preview.RequiredCount = Config?.MergeRequiredCount ?? 5;

        // 构建消息
        if (!enoughMaterials)
            preview.Message = $"材料不足 ({materials.Count}/{preview.RequiredCount})";
        else if (!sameQuality)
            preview.Message = "所有材料品质必须相同";
        else if (!sameSlot)
            preview.Message = "所有材料部位必须相同";
        else if (!notEquipped)
            preview.Message = "包含已装备的装备";
        else if (!canMergeUp)
            preview.Message = "已达最高品质";
        else
            preview.Message = $"预览: {EquipmentItem.GetQualityName(quality)} → {EquipmentItem.GetQualityName(preview.TargetQuality)} (成功率{preview.SuccessRate:P0})";

        return preview;
    }
}

/// <summary>
/// 合成预览信息
/// </summary>
[Serializable]
public class MergePreview
{
    public bool Valid;
    public EquipmentQuality CurrentQuality;
    public EquipmentQuality TargetQuality;
    public EquipmentSlotType SlotType;
    public float SuccessRate;
    public int GoldCost;
    public int MaterialCount;
    public int RequiredCount;
    public string Message;
}
