using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 装备UI面板
/// 包含装备面板、合成界面、洗练界面
/// 对应 TASK-017
/// </summary>
public class EquipmentUI : MonoBehaviour
{
    [Header("面板")]
    public GameObject MainPanel;                        // 主面板
    public GameObject EquipmentPanel;                   // 装备列表
    public GameObject MergePanel;                       // 合成界面
    public GameObject ReforgePanel;                     // 洗练界面

    [Header("装备列表")]
    public Transform EquipmentListContainer;            // 装备列表容器
    public GameObject EquipmentSlotPrefab;              // 装备槽预制体
    public Text EquipmentCountText;                     // 装备数量
    public Dropdown FilterSlotDropdown;                 // 部位筛选
    public Dropdown FilterQualityDropdown;              // 品质筛选

    [Header("装备详情")]
    public GameObject DetailPanel;                      // 详情面板
    public Text DetailNameText;                         // 装备名称
    public Text DetailQualityText;                      // 品质文字
    public Image DetailIconImage;                       // 装备图标
    public Text DetailSlotText;                         // 部位文字
    public Transform DetailStatsContainer;              // 属性容器
    public Text DetailAffixText;                        // 词条文字
    public Button EquipButton;                          // 装备/卸载按钮
    public Button SellButton;                           // 出售按钮

    [Header("已装备槽")]
    public GameObject[] EquippedSlotObjects;            // 已装备槽显示（3个部位）

    [Header("合成界面")]
    public Transform MergeSlotContainer;                // 合成材料槽容器
    public Text MergeSuccessRateText;                   // 成功率文字
    public Text MergeCostText;                          // 消耗文字
    public Text MergePreviewText;                       // 预览文字
    public Button MergeButton;                          // 合成按钮
    public Button MergeClearButton;                     // 清空按钮

    [Header("洗练界面")]
    public Transform ReforgeEquipmentSlot;              // 当前洗练装备槽
    public Text ReforgeEquipmentName;                   // 装备名称
    public Transform ReforgeAffixContainer;             // 词条容器
    public GameObject ReforgeAffixPrefab;               // 词条预制体
    public Text ReforgeCostText;                        // 洗练消耗
    public Button ReforgeButton;                        // 洗练按钮

    // 运行时数据
    private EquipmentItem selectedEquipment;            // 当前选中的装备
    private EquipmentItem reforgeEquipment;             // 正在洗练的装备
    private List<EquipmentItem> mergeMaterials;         // 合成材料列表
    private int currentFilterSlot = -1;                 // 部位筛选（-1=全部）
    private int currentFilterQuality = -1;              // 品质筛选（-1=全部）

    private void Start()
    {
        mergeMaterials = new List<EquipmentItem>();

        // 注册事件
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged += RefreshAll;
            EquipmentManager.Instance.OnEquipmentAcquired += OnAcquired;
        }

        if (EquipmentMergeSystem.Instance != null)
        {
            EquipmentMergeSystem.Instance.OnMergeSuccess += OnMergeSuccess;
            EquipmentMergeSystem.Instance.OnMergeFailed += OnMergeFailed;
        }

        if (EquipmentReforgeSystem.Instance != null)
        {
            EquipmentReforgeSystem.Instance.OnReforgeCompleted += OnReforgeCompleted;
            EquipmentReforgeSystem.Instance.OnAffixLocked += OnAffixLockStateChanged;
            EquipmentReforgeSystem.Instance.OnAffixUnlocked += OnAffixLockStateChanged;
        }

        // 初始化筛选项
        SetupFilters();
        ShowMainPanel();

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= RefreshAll;
        if (EquipmentMergeSystem.Instance != null)
        {
            EquipmentMergeSystem.Instance.OnMergeSuccess -= OnMergeSuccess;
            EquipmentMergeSystem.Instance.OnMergeFailed -= OnMergeFailed;
        }
        if (EquipmentReforgeSystem.Instance != null)
        {
            EquipmentReforgeSystem.Instance.OnReforgeCompleted -= OnReforgeCompleted;
            EquipmentReforgeSystem.Instance.OnAffixLocked -= OnAffixLockStateChanged;
            EquipmentReforgeSystem.Instance.OnAffixUnlocked -= OnAffixLockStateChanged;
        }
    }

    #region 面板切换

    public void ShowMainPanel()
    {
        EquipmentPanel.SetActive(true);
        MergePanel.SetActive(false);
        ReforgePanel.SetActive(false);
        selectedEquipment = null;
        HideDetailPanel();
    }

    public void ShowMergePanel()
    {
        EquipmentPanel.SetActive(false);
        MergePanel.SetActive(true);
        ReforgePanel.SetActive(false);
        mergeMaterials.Clear();
        RefreshMergePanel();
    }

    public void ShowReforgePanel()
    {
        EquipmentPanel.SetActive(false);
        MergePanel.SetActive(false);
        ReforgePanel.SetActive(true);
        RefreshReforgePanel();
    }

    #endregion

    #region 装备列表

    /// <summary>
    /// 设置筛选下拉框
    /// </summary>
    private void SetupFilters()
    {
        if (FilterSlotDropdown != null)
        {
            FilterSlotDropdown.ClearOptions();
            var slotOptions = new List<string> { "全部部位" };
            slotOptions.AddRange(EquipmentSystemConfig.Instance?.SlotTypeNames ?? new string[] { "武器", "护甲", "饰品" });
            FilterSlotDropdown.AddOptions(slotOptions);
        }

        if (FilterQualityDropdown != null)
        {
            FilterQualityDropdown.ClearOptions();
            var qualityOptions = new List<string> { "全部品质" };
            for (int i = 1; i <= 7; i++)
            {
                qualityOptions.Add(EquipmentItem.GetQualityName((EquipmentQuality)i));
            }
            FilterQualityDropdown.AddOptions(qualityOptions);
        }
    }

    public void OnSlotFilterChanged(int value)
    {
        currentFilterSlot = value - 1; // -1=全部, 0=武器, 1=护甲, 2=饰品
        RefreshEquipmentList();
    }

    public void OnQualityFilterChanged(int value)
    {
        currentFilterQuality = value - 1; // -1=全部, 0=白, 1=绿...
        RefreshEquipmentList();
    }

    /// <summary>
    /// 刷新所有UI
    /// </summary>
    public void RefreshAll()
    {
        RefreshEquipmentList();
        RefreshEquippedSlots();
        RefreshMergePanel();
        RefreshReforgePanel();
    }

    /// <summary>
    /// 刷新装备列表
    /// </summary>
    public void RefreshEquipmentList()
    {
        if (EquipmentManager.Instance == null) return;

        // 清空列表
        foreach (Transform child in EquipmentListContainer)
            Destroy(child.gameObject);

        var allEquipments = EquipmentManager.Instance.GetAllEquipments().ToList();

        // 筛选
        var filtered = allEquipments.AsEnumerable();
        if (currentFilterSlot >= 0 && currentFilterSlot <= 2)
            filtered = filtered.Where(e => e.SlotType == (EquipmentSlotType)currentFilterSlot);
        if (currentFilterQuality >= 0 && currentFilterQuality <= 6)
            filtered = filtered.Where(e => (int)e.Quality == currentFilterQuality + 1);

        // 排序：品质降序 > 部位
        var sorted = filtered.OrderByDescending(e => e.Quality)
                             .ThenBy(e => e.SlotType)
                             .ToList();

        // 创建装备槽
        foreach (var equipment in sorted)
        {
            CreateEquipmentSlot(equipment);
        }

        // 更新数量
        if (EquipmentCountText != null)
            EquipmentCountText.text = $"{sorted.Count} / {allEquipments.Count}";
    }

    /// <summary>
    /// 创建装备槽
    /// </summary>
    private void CreateEquipmentSlot(EquipmentItem equipment)
    {
        if (EquipmentSlotPrefab == null) return;

        var slot = Instantiate(EquipmentSlotPrefab, EquipmentListContainer);
        var slotScript = slot.GetComponent<EquipmentSlotUI>();
        if (slotScript == null) slotScript = slot.AddComponent<EquipmentSlotUI>();

        slotScript.Setup(equipment, OnEquipmentSlotClicked);
    }

    /// <summary>
    /// 装备槽点击
    /// </summary>
    private void OnEquipmentSlotClicked(EquipmentItem equipment)
    {
        selectedEquipment = equipment;
        ShowDetail(equipment);
    }

    #endregion

    #region 装备详情

    public void ShowDetail(EquipmentItem equipment)
    {
        if (equipment == null)
        {
            HideDetailPanel();
            return;
        }

        DetailPanel.SetActive(true);

        if (DetailNameText != null)
            DetailNameText.text = equipment.EquipmentName;

        if (DetailQualityText != null)
        {
            DetailQualityText.text = EquipmentItem.GetQualityStars(equipment.Quality);
            DetailQualityText.color = EquipmentItem.GetQualityColor(equipment.Quality);
        }

        if (DetailSlotText != null)
        {
            DetailSlotText.text = equipment.SlotType switch
            {
                EquipmentSlotType.Weapon => "武器",
                EquipmentSlotType.Armor => "护甲",
                EquipmentSlotType.Accessory => "饰品",
                _ => "未知"
            };
        }

        if (DetailIconImage != null && DetailIconImage.sprite == null)
        {
            DetailIconImage.color = EquipmentItem.GetQualityColor(equipment.Quality) * 0.5f + Color.white * 0.5f;
        }

        // 基础属性
        if (DetailStatsContainer != null)
        {
            foreach (Transform child in DetailStatsContainer)
                Destroy(child.gameObject);

            CreateStatText(DetailStatsContainer, "攻击", equipment.AttackBonus);
            CreateStatText(DetailStatsContainer, "防御", equipment.DefenseBonus);
            CreateStatText(DetailStatsContainer, "生命", equipment.HealthBonus);
            CreateStatText(DetailStatsContainer, "暴击率", equipment.CritChanceBonus, true);
            CreateStatText(DetailStatsContainer, "暴击伤害", equipment.CritDamageBonus, true);
            CreateStatText(DetailStatsContainer, "攻速", equipment.AttackSpeedBonus, true);
        }

        // 词条
        if (DetailAffixText != null)
        {
            if (equipment.Affixes != null && equipment.Affixes.Count > 0)
            {
                var affixLines = new List<string>();
                foreach (var affix in equipment.Affixes)
                {
                    string lockSymbol = affix.IsLocked ? "[🔒]" : "";
                    affixLines.Add($"{lockSymbol}{affix.GetDescription()}");
                }
                DetailAffixText.text = string.Join("\n", affixLines);
            }
            else
            {
                DetailAffixText.text = "无词条";
            }
        }

        // 装备/卸载按钮
        if (EquipButton != null)
        {
            bool isEquipped = EquipmentManager.Instance.IsEquipped(equipment.InstanceId);
            EquipButton.GetComponentInChildren<Text>().text = isEquipped ? "卸下" : "装备";
            EquipButton.interactable = true;
            EquipButton.onClick.RemoveAllListeners();
            EquipButton.onClick.AddListener(() =>
            {
                if (isEquipped)
                    EquipmentManager.Instance.UnequipSlot(equipment.SlotType);
                else
                    EquipmentManager.Instance.EquipItem(equipment.InstanceId);
                RefreshAll();
            });
        }

        // 出售按钮
        if (SellButton != null)
        {
            bool isEquipped = EquipmentManager.Instance.IsEquipped(equipment.InstanceId);
            int sellPrice = EquipmentManager.Instance.CalculateSellPrice(equipment);
            SellButton.GetComponentInChildren<Text>().text = isEquipped ? "已装备" : $"出售 ({sellPrice}G)";
            SellButton.interactable = !isEquipped;
            SellButton.onClick.RemoveAllListeners();
            SellButton.onClick.AddListener(() =>
            {
                EquipmentManager.Instance.SellEquipment(equipment.InstanceId);
                selectedEquipment = null;
                HideDetailPanel();
                RefreshAll();
            });
        }
    }

    public void HideDetailPanel()
    {
        DetailPanel.SetActive(false);
        selectedEquipment = null;
    }

    private void CreateStatText(Transform parent, string name, float value, bool isPercent = false)
    {
        var go = new GameObject($"Stat_{name}");
        go.transform.SetParent(parent);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.color = Color.black;
        text.text = isPercent ? $"{name}: +{value * 100:F1}%" : $"{name}: +{value:F1}";
    }

    #endregion

    #region 已装备槽

    public void RefreshEquippedSlots()
    {
        if (EquipmentManager.Instance == null || EquippedSlotObjects == null) return;

        var equippedItems = EquipmentManager.Instance.GetAllEquippedItems();
        var slots = new EquipmentSlotType[] { EquipmentSlotType.Weapon, EquipmentSlotType.Armor, EquipmentSlotType.Accessory };

        for (int i = 0; i < EquippedSlotObjects.Length && i < slots.Length; i++)
        {
            var slotObj = EquippedSlotObjects[i];
            if (slotObj == null) continue;

            var text = slotObj.GetComponentInChildren<Text>();
            var image = slotObj.GetComponentInChildren<Image>();

            if (equippedItems.TryGetValue(slots[i], out var equipment))
            {
                if (text != null)
                {
                    text.text = $"{equipment.EquipmentName}\n{EquipmentItem.GetQualityStars(equipment.Quality)}";
                    text.color = EquipmentItem.GetQualityColor(equipment.Quality);
                }
                if (image != null)
                    image.color = EquipmentItem.GetQualityColor(equipment.Quality);
            }
            else
            {
                if (text != null)
                {
                    text.text = slots[i] switch
                    {
                        EquipmentSlotType.Weapon => "武器 - 空",
                        EquipmentSlotType.Armor => "护甲 - 空",
                        EquipmentSlotType.Accessory => "饰品 - 空",
                        _ => "- 空"
                    };
                    text.color = Color.gray;
                }
                if (image != null)
                    image.color = Color.gray;
            }
        }
    }

    #endregion

    #region 合成界面

    public void AddMergeMaterial(EquipmentItem equipment)
    {
        if (equipment == null) return;
        if (EquipmentManager.Instance.IsEquipped(equipment.InstanceId))
        {
            Debug.Log("[EquipmentUI] 不能选择已装备的装备作为合成材料");
            return;
        }

        if (mergeMaterials.Count >= 5)
        {
            Debug.Log("[EquipmentUI] 合成材料已满");
            return;
        }

        // 检查一致性
        if (mergeMaterials.Count > 0)
        {
            if (mergeMaterials[0].SlotType != equipment.SlotType)
            {
                Debug.Log("[EquipmentUI] 合成材料部位必须相同");
                return;
            }
            if (mergeMaterials[0].Quality != equipment.Quality)
            {
                Debug.Log("[EquipmentUI] 合成材料品质必须相同");
                return;
            }
        }

        mergeMaterials.Add(equipment);
        RefreshMergePanel();
    }

    public void RemoveMergeMaterial(int index)
    {
        if (index >= 0 && index < mergeMaterials.Count)
        {
            mergeMaterials.RemoveAt(index);
            RefreshMergePanel();
        }
    }

    public void ClearMergeMaterials()
    {
        mergeMaterials.Clear();
        RefreshMergePanel();
    }

    public void ExecuteMerge()
    {
        if (EquipmentMergeSystem.Instance == null) return;

        if (mergeMaterials.Count < 5)
        {
            Debug.Log("[EquipmentUI] 材料不足");
            return;
        }

        var preview = EquipmentMergeSystem.Instance.GetMergePreview(mergeMaterials);
        if (!preview.Valid)
        {
            Debug.Log($"[EquipmentUI] {preview.Message}");
            return;
        }

        EquipmentMergeSystem.Instance.Merge5To1(new List<EquipmentItem>(mergeMaterials));
    }

    public void RefreshMergePanel()
    {
        if (!MergePanel.activeSelf) return;

        // 清空材料槽
        if (MergeSlotContainer != null)
        {
            foreach (Transform child in MergeSlotContainer)
            {
                var slotUI = child.GetComponent<MergeMaterialSlotUI>();
                if (slotUI != null) slotUI.Clear();
            }
        }

        // 填充材料
        if (MergeSlotContainer != null)
        {
            for (int i = 0; i < MergeSlotContainer.childCount && i < mergeMaterials.Count; i++)
            {
                var slotUI = MergeSlotContainer.GetChild(i).GetComponent<MergeMaterialSlotUI>();
                if (slotUI == null)
                    slotUI = MergeSlotContainer.GetChild(i).gameObject.AddComponent<MergeMaterialSlotUI>();

                int index = i;
                slotUI.Setup(mergeMaterials[i], () => RemoveMergeMaterial(index));
            }
        }

        // 更新预览
        var preview = EquipmentMergeSystem.Instance?.GetMergePreview(mergeMaterials) ?? new MergePreview { Valid = false, Message = "未选择材料" };

        if (MergeSuccessRateText != null)
            MergeSuccessRateText.text = preview.Valid ? $"成功率: {preview.SuccessRate:P0}" : "-";

        if (MergeCostText != null)
            MergeCostText.text = preview.Valid ? $"金币: {preview.GoldCost}" : "-";

        if (MergePreviewText != null)
            MergePreviewText.text = preview.Message;

        if (MergeButton != null)
            MergeButton.interactable = preview.Valid;
    }

    private void OnMergeSuccess(EquipmentItem result)
    {
        mergeMaterials.Clear();
        RefreshMergePanel();
        RefreshAll();
        Debug.Log($"[EquipmentUI] 合成成功！获得 {result.EquipmentName}");
    }

    private void OnMergeFailed(string message)
    {
        mergeMaterials.Clear();
        RefreshMergePanel();
        RefreshAll();
        Debug.Log($"[EquipmentUI] 合成失败: {message}");
    }

    #endregion

    #region 洗练界面

    public void SelectReforgeEquipment(EquipmentItem equipment)
    {
        reforgeEquipment = equipment;
        RefreshReforgePanel();
    }

    public void ExecuteReforge()
    {
        if (reforgeEquipment == null || EquipmentReforgeSystem.Instance == null) return;

        EquipmentReforgeSystem.Instance.Reforge(reforgeEquipment);
    }

    public void ToggleAffixLock(int affixIndex)
    {
        if (reforgeEquipment == null || EquipmentReforgeSystem.Instance == null) return;

        if (reforgeEquipment.Affixes[affixIndex].IsLocked)
            EquipmentReforgeSystem.Instance.UnlockAffix(reforgeEquipment, affixIndex);
        else
            EquipmentReforgeSystem.Instance.LockAffix(reforgeEquipment, affixIndex);

        RefreshReforgePanel();
    }

    public void RefreshReforgePanel()
    {
        if (!ReforgePanel.activeSelf) return;

        if (reforgeEquipment != null)
        {
            if (ReforgeEquipmentName != null)
                ReforgeEquipmentName.text = $"{reforgeEquipment.EquipmentName} [{EquipmentItem.GetQualityName(reforgeEquipment.Quality)}]";

            // 刷新词条显示
            if (ReforgeAffixContainer != null)
            {
                foreach (Transform child in ReforgeAffixContainer)
                    Destroy(child.gameObject);

                if (reforgeEquipment.Affixes != null)
                {
                    for (int i = 0; i < reforgeEquipment.Affixes.Count; i++)
                    {
                        CreateAffixRow(i, reforgeEquipment.Affixes[i]);
                    }
                }
            }

            var preview = EquipmentReforgeSystem.Instance?.GetReforgePreview(reforgeEquipment);
            if (ReforgeCostText != null)
                ReforgeCostText.text = preview?.Message ?? "选择装备进行洗练";

            if (ReforgeButton != null)
                ReforgeButton.interactable = preview?.CanReforge ?? false;
        }
        else
        {
            if (ReforgeEquipmentName != null)
                ReforgeEquipmentName.text = "请选择装备";

            if (ReforgeAffixContainer != null)
            {
                foreach (Transform child in ReforgeAffixContainer)
                    Destroy(child.gameObject);
            }

            if (ReforgeCostText != null)
                ReforgeCostText.text = "选择装备进行洗练";

            if (ReforgeButton != null)
                ReforgeButton.interactable = false;
        }
    }

    private void CreateAffixRow(int index, EquipmentAffixInstance affix)
    {
        var go = new GameObject($"Affix_{index}");
        go.transform.SetParent(ReforgeAffixContainer);

        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.childForceExpandWidth = false;
        layout.spacing = 10;

        // 锁定按钮
        var lockBtnObj = new GameObject("LockBtn");
        lockBtnObj.transform.SetParent(go.transform);
        var lockBtn = lockBtnObj.AddComponent<Button>();
        var lockText = lockBtnObj.AddComponent<Text>();
        lockText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lockText.fontSize = 14;
        lockText.text = affix.IsLocked ? "🔒" : "🔓";
        lockText.color = affix.IsLocked ? Color.green : Color.gray;
        int idx = index;
        lockBtn.onClick.AddListener(() => ToggleAffixLock(idx));

        // 词条文字
        var affixTextObj = new GameObject("AffixText");
        affixTextObj.transform.SetParent(go.transform);
        var affixText = affixTextObj.AddComponent<Text>();
        affixText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        affixText.fontSize = 14;
        affixText.color = Color.black;
        affixText.text = affix.GetDescription();

        // 稀有度标识
        var rarityTextObj = new GameObject("RarityText");
        rarityTextObj.transform.SetParent(go.transform);
        var rarityText = rarityTextObj.AddComponent<Text>();
        rarityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rarityText.fontSize = 12;
        rarityText.text = affix.AffixRarity.ToString();
        rarityText.color = affix.AffixRarity switch
        {
            AffixRarity.Normal => Color.gray,
            AffixRarity.Rare => Color.blue,
            AffixRarity.Epic => new Color(0.7f, 0.3f, 1.0f),
            _ => Color.gray
        };
    }

    private void OnReforgeCompleted(EquipmentItem equipment, ReforgeResult result)
    {
        RefreshReforgePanel();
        Debug.Log($"[EquipmentUI] 洗练完成: {result.Message}");
    }

    private void OnAffixLockStateChanged(EquipmentItem equipment, int index)
    {
        RefreshReforgePanel();
    }

    #endregion

    #region 回调

    private void OnAcquired(EquipmentItem equipment)
    {
        // 装备获得时，可以直接显示提示
        Debug.Log($"[EquipmentUI] 获得新装备: {equipment.EquipmentName}");
    }

    #endregion
}

/// <summary>
/// 装备槽UI组件
/// </summary>
public class EquipmentSlotUI : MonoBehaviour
{
    public Text NameText;
    public Text QualityText;
    public Text SlotText;
    public Image Background;
    public Button ClickButton;

    private EquipmentItem currentEquipment;
    private Action<EquipmentItem> onClickCallback;

    public void Setup(EquipmentItem equipment, Action<EquipmentItem> onClick)
    {
        currentEquipment = equipment;
        onClickCallback = onClick;

        if (NameText != null)
            NameText.text = equipment.EquipmentName;

        if (QualityText != null)
        {
            QualityText.text = EquipmentItem.GetQualityStars(equipment.Quality);
            QualityText.color = EquipmentItem.GetQualityColor(equipment.Quality);
        }

        if (SlotText != null)
        {
            SlotText.text = equipment.SlotType switch
            {
                EquipmentSlotType.Weapon => "武",
                EquipmentSlotType.Armor => "防",
                EquipmentSlotType.Accessory => "饰",
                _ => "?"
            };
        }

        if (Background != null)
            Background.color = EquipmentItem.GetQualityColor(equipment.Quality) * 0.2f + Color.white * 0.8f;

        if (ClickButton != null)
        {
            ClickButton.onClick.RemoveAllListeners();
            ClickButton.onClick.AddListener(() => onClickCallback?.Invoke(equipment));
        }
    }
}

/// <summary>
/// 合成材料槽UI
/// </summary>
public class MergeMaterialSlotUI : MonoBehaviour
{
    public Text NameText;
    public Image IconImage;
    public Button RemoveButton;

    public void Setup(EquipmentItem equipment, Action onRemove)
    {
        if (NameText != null)
        {
            NameText.text = $"{equipment.EquipmentName}\n[{EquipmentItem.GetQualityName(equipment.Quality)}]";
            NameText.color = EquipmentItem.GetQualityColor(equipment.Quality);
        }

        if (IconImage != null)
            IconImage.color = EquipmentItem.GetQualityColor(equipment.Quality) * 0.5f + Color.white * 0.5f;

        if (RemoveButton != null)
        {
            RemoveButton.onClick.RemoveAllListeners();
            RemoveButton.onClick.AddListener(() => onRemove?.Invoke());
        }
    }

    public void Clear()
    {
        if (NameText != null)
        {
            NameText.text = "空";
            NameText.color = Color.gray;
        }
        if (IconImage != null)
            IconImage.color = Color.gray;
    }
}
