using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 天赋树UI面板
/// 显示天赋树、节点激活、方案管理
/// 对应 TASK-021
/// </summary>
public class TalentUI : MonoBehaviour
{
    [Header("面板")]
    public GameObject MainPanel;                            // 主面板
    public GameObject TalentTreeView;                       // 天赋树视图
    public GameObject BuildManagerView;                     // 方案管理视图

    [Header("系别标签")]
    public Button AttackTabButton;                          // 攻击系标签
    public Button DefenseTabButton;                         // 防御系标签
    public Button EconomyTabButton;                         // 经济系标签
    public Button SpecialTabButton;                         // 特殊系标签

    [Header("天赋信息")]
    public Text AvailablePointsText;                        // 可用天赋点
    public Text TotalSpentText;                             // 已消耗天赋点
    public Text BranchNameText;                             // 当前系别名称

    [Header("天赋树容器")]
    public Transform TalentNodeContainer;                   // 节点容器
    public GameObject TalentNodePrefab;                     // 节点预制体

    [Header("节点详情")]
    public GameObject NodeDetailPanel;                      // 节点详情面板
    public Text NodeNameText;                               // 节点名称
    public Text NodeLevelText;                              // 节点等级
    public Text NodeDescriptionText;                        // 节点描述
    public Text NodeEffectText;                             // 节点效果
    public Button UpgradeButton;                            // 升级按钮
    public Button DowngradeButton;                          // 降级按钮

    [Header("重置")]
    public Button ResetButton;                              // 重置按钮
    public Text ResetInfoText;                              // 重置信息

    [Header("方案管理")]
    public Transform BuildListContainer;                    // 方案列表容器
    public GameObject BuildSlotPrefab;                      // 方案槽预制体
    public InputField NewBuildNameInput;                    // 新方案名称输入
    public Button SaveBuildButton;                          // 保存方案按钮
    public Button SwitchToBuildViewButton;                  // 切换到方案视图
    public Button SwitchToTreeViewButton;                   // 切换到天赋树视图

    // 运行时数据
    private TalentBranch currentBranch = TalentBranch.Attack;    // 当前显示的系别
    private TalentNodeDef selectedNode;                          // 当前选中的节点
    private Dictionary<string, TalentNodeUI> nodeUIs;           // 节点UI映射

    private void Start()
    {
        nodeUIs = new Dictionary<string, TalentNodeUI>();

        // 注册事件
        if (TalentTreeManager.Instance != null)
        {
            TalentTreeManager.Instance.OnTalentTreeChanged += RefreshAll;
            TalentTreeManager.Instance.OnTalentPointsChanged += OnPointsChanged;
            TalentTreeManager.Instance.OnNodeLevelChanged += OnNodeLevelChanged;
            TalentTreeManager.Instance.OnTalentReset += OnTalentReset;
            TalentTreeManager.Instance.OnBuildSaved += OnBuildChanged;
            TalentTreeManager.Instance.OnBuildDeleted += OnBuildChanged;
        }

        // 绑定按钮
        AttackTabButton?.onClick.AddListener(() => SwitchBranch(TalentBranch.Attack));
        DefenseTabButton?.onClick.AddListener(() => SwitchBranch(TalentBranch.Defense));
        EconomyTabButton?.onClick.AddListener(() => SwitchBranch(TalentBranch.Economy));
        SpecialTabButton?.onClick.AddListener(() => SwitchBranch(TalentBranch.Special));

        ResetButton?.onClick.AddListener(OnResetClicked);
        SaveBuildButton?.onClick.AddListener(OnSaveBuildClicked);

        SwitchToBuildViewButton?.onClick.AddListener(ShowBuildView);
        SwitchToTreeViewButton?.onClick.AddListener(ShowTreeView);

        SwitchBranch(currentBranch);
        ShowTreeView();
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (TalentTreeManager.Instance != null)
        {
            TalentTreeManager.Instance.OnTalentTreeChanged -= RefreshAll;
        }
    }

    #region 视图切换

    private void ShowTreeView()
    {
        TalentTreeView.SetActive(true);
        BuildManagerView.SetActive(false);
    }

    private void ShowBuildView()
    {
        TalentTreeView.SetActive(false);
        BuildManagerView.SetActive(true);
        RefreshBuildList();
    }

    public void SwitchBranch(TalentBranch branch)
    {
        currentBranch = branch;
        UpdateBranchTabColors();
        RefreshTalentTree();
    }

    private void UpdateBranchTabColors()
    {
        var config = TalentTreeManager.Instance?.Config;
        if (config == null) return;

        Color activeColor = config.GetBranchColor(currentBranch);
        Color inactiveColor = Color.gray;

        SetButtonColor(AttackTabButton, currentBranch == TalentBranch.Attack ? activeColor : inactiveColor);
        SetButtonColor(DefenseTabButton, currentBranch == TalentBranch.Defense ? activeColor : inactiveColor);
        SetButtonColor(EconomyTabButton, currentBranch == TalentBranch.Economy ? activeColor : inactiveColor);
        SetButtonColor(SpecialTabButton, currentBranch == TalentBranch.Special ? activeColor : inactiveColor);

        if (BranchNameText != null)
            BranchNameText.text = TalentTreeConfig.GetBranchName(currentBranch);
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            var colors = button.colors;
            colors.normalColor = color;
            button.colors = colors;
        }
    }

    #endregion

    #region 天赋树显示

    public void RefreshTalentTree()
    {
        if (TalentTreeManager.Instance?.Config == null) return;

        // 清空容器
        foreach (Transform child in TalentNodeContainer)
            Destroy(child.gameObject);

        nodeUIs.Clear();

        var nodes = TalentTreeManager.Instance.Config.GetNodesByBranch(currentBranch);

        // 按层级分组
        var tiers = nodes.GroupBy(n => n.Tier).OrderBy(g => g.Key);

        foreach (var tier in tiers)
        {
            CreateTierRow(tier.Key, tier.OrderBy(n => n.PositionIndex).ToList());
        }

        UpdatePointDisplay();
        UpdateResetInfo();
    }

    private void CreateTierRow(int tier, List<TalentNodeDef> nodes)
    {
        // 创建层级标签
        var tierLabel = new GameObject($"Tier_{tier}_Label");
        tierLabel.transform.SetParent(TalentNodeContainer);
        var labelText = tierLabel.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 16;
        labelText.color = Color.white;
        labelText.text = $"第{tier}层";
        labelText.alignment = TextAnchor.MiddleCenter;

        // 创建节点行
        var rowObj = new GameObject($"Tier_{tier}_Row");
        rowObj.transform.SetParent(TalentNodeContainer);
        var rowLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 15;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;

        foreach (var nodeDef in nodes)
        {
            CreateTalentNode(nodeDef, rowObj.transform);
        }
    }

    private void CreateTalentNode(TalentNodeDef nodeDef, Transform parent)
    {
        if (TalentNodePrefab == null)
        {
            // 动态创建节点UI
            var go = new GameObject($"Node_{nodeDef.NodeId}");
            go.transform.SetParent(parent);
            var nodeUI = go.AddComponent<TalentNodeUI>();
            nodeUI.Initialize(nodeDef, OnNodeClicked);
            nodeUIs[nodeDef.NodeId] = nodeUI;
        }
        else
        {
            var go = Instantiate(TalentNodePrefab, parent);
            var nodeUI = go.GetComponent<TalentNodeUI>();
            if (nodeUI == null) nodeUI = go.AddComponent<TalentNodeUI>();
            nodeUI.Initialize(nodeDef, OnNodeClicked);
            nodeUIs[nodeDef.NodeId] = nodeUI;
        }
    }

    private void OnNodeClicked(TalentNodeDef nodeDef)
    {
        selectedNode = nodeDef;
        ShowNodeDetail(nodeDef);
    }

    #endregion

    #region 节点详情

    public void ShowNodeDetail(TalentNodeDef nodeDef)
    {
        if (nodeDef == null)
        {
            NodeDetailPanel.SetActive(false);
            return;
        }

        NodeDetailPanel.SetActive(true);

        if (NodeNameText != null)
            NodeNameText.text = nodeDef.NodeName;

        int currentLevel = TalentTreeManager.Instance.GetNodeLevel(nodeDef.NodeId);

        if (NodeLevelText != null)
            NodeLevelText.text = $"Lv.{currentLevel} / Lv.{nodeDef.MaxLevel}";

        if (NodeDescriptionText != null)
            NodeDescriptionText.text = nodeDef.Description;

        if (NodeEffectText != null)
        {
            float currentValue = nodeDef.GetValueAtLevel(Mathf.Max(1, currentLevel));
            float nextValue = nodeDef.GetValueAtLevel(Mathf.Min(currentLevel + 1, nodeDef.MaxLevel));
            string valueStr = nodeDef.ValueType == AffixValueType.Percentage
                ? $"{currentValue * 100:F1}%"
                : $"{currentValue:F1}";

            if (currentLevel < nodeDef.MaxLevel)
            {
                string nextStr = nodeDef.ValueType == AffixValueType.Percentage
                    ? $"{nextValue * 100:F1}%"
                    : $"{nextValue:F1}";
                NodeEffectText.text = $"当前: {valueStr} → 下一级: {nextStr}";
            }
            else
            {
                NodeEffectText.text = $"当前: {valueStr}（已满级）";
            }
        }

        // 前置条件显示
        if (nodeDef.Prerequisites != null && nodeDef.Prerequisites.Count > 0)
        {
            var prereqText = string.Join(", ", nodeDef.Prerequisites.Select(id =>
            {
                var def = TalentTreeManager.Instance.Config.GetNodeDef(id);
                bool met = TalentTreeManager.Instance.IsNodeActive(id);
                string name = def?.NodeName ?? id;
                return met ? $"<color=green>{name}✓</color>" : $"<color=red>{name}✗</color>";
            }));
        }

        // 按钮状态
        if (UpgradeButton != null)
        {
            bool canUpgrade = TalentTreeManager.Instance.CanUpgradeNode(nodeDef.NodeId);
            UpgradeButton.interactable = canUpgrade;
            UpgradeButton.onClick.RemoveAllListeners();
            UpgradeButton.onClick.AddListener(() =>
            {
                TalentTreeManager.Instance.UpgradeNode(nodeDef.NodeId);
                ShowNodeDetail(selectedNode);
                RefreshTalentTree();
            });
        }

        if (DowngradeButton != null)
        {
            bool canDowngrade = currentLevel > 0;
            DowngradeButton.interactable = canDowngrade;
            DowngradeButton.onClick.RemoveAllListeners();
            DowngradeButton.onClick.AddListener(() =>
            {
                TalentTreeManager.Instance.DowngradeNode(nodeDef.NodeId);
                ShowNodeDetail(selectedNode);
                RefreshTalentTree();
            });
        }
    }

    #endregion

    #region 重置

    private void OnResetClicked()
    {
        if (TalentTreeManager.Instance == null) return;

        var preview = TalentTreeManager.Instance.GetResetPreview();

        if (preview.IsFree)
        {
            TalentTreeManager.Instance.ResetTalentTree(true);
        }
        else if (preview.Success)
        {
            // 确认弹窗（简化版：直接重置）
            TalentTreeManager.Instance.ResetTalentTree(false);
        }
        else
        {
            Debug.Log($"[TalentUI] {preview.Message}");
        }
    }

    private void UpdateResetInfo()
    {
        if (TalentTreeManager.Instance == null) return;

        var preview = TalentTreeManager.Instance.GetResetPreview();
        if (ResetInfoText != null)
            ResetInfoText.text = preview.Message;
    }

    private void OnTalentReset()
    {
        RefreshAll();
        NodeDetailPanel.SetActive(false);
    }

    #endregion

    #region 方案管理

    private void OnSaveBuildClicked()
    {
        if (TalentTreeManager.Instance == null) return;

        string name = NewBuildNameInput?.text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            name = $"方案 {DateTime.Now:MM-dd HH:mm}";
        }

        TalentTreeManager.Instance.SaveCurrentBuild(name);

        if (NewBuildNameInput != null)
            NewBuildNameInput.text = "";

        RefreshBuildList();
    }

    public void RefreshBuildList()
    {
        if (BuildListContainer == null) return;

        foreach (Transform child in BuildListContainer)
            Destroy(child.gameObject);

        var builds = TalentTreeManager.Instance?.GetAllBuilds();
        if (builds == null) return;

        foreach (var build in builds)
        {
            CreateBuildSlot(build);
        }
    }

    private void CreateBuildSlot(TalentBuild build)
    {
        var go = new GameObject($"Build_{build.BuildName}");
        go.transform.SetParent(BuildListContainer);

        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childForceExpandWidth = false;

        // 激活标识
        var activeObj = new GameObject("ActiveIndicator");
        activeObj.transform.SetParent(go.transform);
        var activeText = activeObj.AddComponent<Text>();
        activeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        activeText.fontSize = 14;
        activeText.text = build.IsActive ? "★" : "  ";
        activeText.color = build.IsActive ? Color.yellow : Color.gray;

        // 名称
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(go.transform);
        var nameText = nameObj.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 14;
        nameText.text = $"{build.BuildName} ({build.TotalPointsSpent}点)";
        nameText.color = Color.white;

        // 加载按钮
        var loadObj = new GameObject("LoadBtn");
        loadObj.transform.SetParent(go.transform);
        var loadBtn = loadObj.AddComponent<Button>();
        var loadText = loadObj.AddComponent<Text>();
        loadText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        loadText.fontSize = 12;
        loadText.text = "加载";
        loadText.color = Color.white;
        loadBtn.onClick.AddListener(() =>
        {
            TalentTreeManager.Instance?.LoadBuild(build.BuildId);
            ShowTreeView();
            RefreshBuildList();
        });

        // 删除按钮
        var delObj = new GameObject("DelBtn");
        delObj.transform.SetParent(go.transform);
        var delBtn = delObj.AddComponent<Button>();
        var delText = delObj.AddComponent<Text>();
        delText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        delText.fontSize = 12;
        delText.text = "删除";
        delText.color = Color.red;
        delBtn.onClick.AddListener(() =>
        {
            TalentTreeManager.Instance?.DeleteBuild(build.BuildId);
            RefreshBuildList();
        });
    }

    #endregion

    #region 刷新

    public void RefreshAll()
    {
        RefreshTalentTree();
        RefreshBuildList();
        UpdatePointDisplay();
        UpdateResetInfo();
    }

    private void UpdatePointDisplay()
    {
        if (TalentTreeManager.Instance == null) return;

        if (AvailablePointsText != null)
            AvailablePointsText.text = $"可用: {TalentTreeManager.Instance.GetAvailablePoints()}";

        if (TotalSpentText != null)
            TotalSpentText.text = $"已消耗: {TalentTreeManager.Instance.GetTotalPointsSpent()}";
    }

    private void OnPointsChanged(int before, int after)
    {
        UpdatePointDisplay();
    }

    private void OnNodeLevelChanged(string nodeId, int oldLevel, int newLevel)
    {
        RefreshTalentTree();
        if (selectedNode != null && selectedNode.NodeId == nodeId)
            ShowNodeDetail(selectedNode);
    }

    private void OnBuildChanged(TalentBuild build)
    {
        RefreshBuildList();
    }

    private void OnBuildChanged(string buildId)
    {
        RefreshBuildList();
    }

    #endregion
}

/// <summary>
/// 天赋节点UI组件（动态创建使用）
/// </summary>
public class TalentNodeUI : MonoBehaviour
{
    public Text NodeNameText;
    public Text NodeLevelText;
    public Image NodeIcon;
    public Image Background;
    public Button ClickButton;
    public GameObject LockedOverlay;
    public GameObject ActivatedIndicator;

    private TalentNodeDef nodeDef;
    private Action<TalentNodeDef> onClickCallback;

    public void Initialize(TalentNodeDef def, Action<TalentNodeDef> onClick)
    {
        nodeDef = def;
        onClickCallback = onClick;

        // 创建基础UI元素（运行时使用）
        EnsureComponents();

        Refresh();
    }

    private void EnsureComponents()
    {
        if (ClickButton == null)
        {
            ClickButton = gameObject.AddComponent<Button>();
            ClickButton.onClick.AddListener(() => onClickCallback?.Invoke(nodeDef));

            var imgComp = gameObject.AddComponent<Image>();
            imgComp.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }

        if (NodeNameText == null)
        {
            var go = new GameObject("Name");
            go.transform.SetParent(transform);
            NodeNameText = go.AddComponent<Text>();
            NodeNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            NodeNameText.fontSize = 10;
            NodeNameText.alignment = TextAnchor.MiddleCenter;
            NodeNameText.rectTransform.sizeDelta = new Vector2(80, 20);
        }

        if (NodeLevelText == null)
        {
            var go = new GameObject("Level");
            go.transform.SetParent(transform);
            NodeLevelText = go.AddComponent<Text>();
            NodeLevelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            NodeLevelText.fontSize = 8;
            NodeLevelText.alignment = TextAnchor.MiddleCenter;
            NodeLevelText.rectTransform.anchorMin = new Vector2(1, 1);
            NodeLevelText.rectTransform.anchorMax = new Vector2(1, 1);
            NodeLevelText.rectTransform.sizeDelta = new Vector2(30, 15);
            NodeLevelText.rectTransform.anchoredPosition = new Vector2(-5, -5);
        }

        // 设置大小
        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(80, 80);
        }
    }

    public void Refresh()
    {
        if (nodeDef == null || TalentTreeManager.Instance == null) return;

        int currentLevel = TalentTreeManager.Instance.GetNodeLevel(nodeDef.NodeId);
        bool isActive = currentLevel > 0;
        bool canUpgrade = TalentTreeManager.Instance.CanUpgradeNode(nodeDef.NodeId);
        bool prerequisitesMet = TalentTreeManager.Instance.Config?.ArePrerequisitesMet(nodeDef, TalentTreeManager.Instance.Config != null ? new Dictionary<string, int>() : null) ?? true;

        // 名称
        if (NodeNameText != null)
        {
            NodeNameText.text = nodeDef.NodeName;
            NodeNameText.color = isActive ? Color.white : Color.gray;
        }

        // 等级
        if (NodeLevelText != null)
        {
            NodeLevelText.text = $"{currentLevel}/{nodeDef.MaxLevel}";
            NodeLevelText.color = isActive ? Color.yellow : Color.gray;
        }

        // 图标
        if (NodeIcon != null)
        {
            NodeIcon.color = isActive ? Color.white : Color.gray;
        }

        // 背景色
        var bg = GetComponent<Image>();
        if (bg != null)
        {
            Color branchColor = TalentTreeManager.Instance.Config?.GetBranchColor(nodeDef.Branch) ?? Color.white;

            if (isActive && currentLevel >= nodeDef.MaxLevel)
                bg.color = new Color(1f, 0.8f, 0f, 0.8f); // 金色-满级
            else if (isActive)
                bg.color = branchColor * 0.8f;
            else if (canUpgrade)
                bg.color = branchColor * 0.4f; // 可升级高亮
            else
                bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // 灰色-不可用
        }

        // 锁定覆盖层
        if (LockedOverlay != null)
            LockedOverlay.SetActive(!prerequisitesMet && !isActive);

        // 已激活标识
        if (ActivatedIndicator != null)
            ActivatedIndicator.SetActive(isActive);
    }
}
