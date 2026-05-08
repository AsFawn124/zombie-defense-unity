using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 技能选择UI - Roguelike三选一界面
/// </summary>
public class SkillSelectionUI : MonoBehaviour
{
    [Header("UI元素")]
    public GameObject SelectionPanel;
    public Transform SkillContainer;
    public GameObject SkillCardPrefab;
    
    [Header("动画")]
    public Animator PanelAnimator;
    public float CardAppearDelay = 0.2f;
    
    private List<SkillCard> skillCards = new List<SkillCard>();
    private bool hasSelected = false;
    
    private void Start()
    {
        // 初始隐藏
        if (SelectionPanel != null)
        {
            SelectionPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 显示技能选择界面
    /// </summary>
    public void Show(List<SkillData> skills)
    {
        if (SelectionPanel == null)
            return;
        
        hasSelected = false;
        SelectionPanel.SetActive(true);
        
        // 清除旧卡片
        ClearCards();
        
        // 创建新卡片
        for (int i = 0; i < skills.Count; i++)
        {
            CreateSkillCard(skills[i], i);
        }
        
        // 播放动画
        if (PanelAnimator != null)
        {
            PanelAnimator.SetTrigger("Show");
        }
        
        // 暂停游戏
        Time.timeScale = 0f;
    }
    
    /// <summary>
    /// 创建技能卡片
    /// </summary>
    private void CreateSkillCard(SkillData skill, int index)
    {
        if (SkillCardPrefab == null || SkillContainer == null)
            return;
        
        GameObject cardObj = Instantiate(SkillCardPrefab, SkillContainer);
        SkillCard card = cardObj.GetComponent<SkillCard>();
        
        if (card != null)
        {
            card.Setup(skill, OnSkillSelected);
            skillCards.Add(card);
            
            // 延迟显示动画
            cardObj.SetActive(false);
            Invoke(nameof(ShowCards), CardAppearDelay * index);
        }
    }
    
    /// <summary>
    /// 显示卡片
    /// </summary>
    private void ShowCards()
    {
        foreach (var card in skillCards)
        {
            if (card != null)
            {
                card.gameObject.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// 技能选择回调
    /// </summary>
    private void OnSkillSelected(SkillData skill)
    {
        if (hasSelected)
            return;
        
        hasSelected = true;
        
        // 应用技能
        SkillManager.Instance.SelectSkill(skill);
        
        // 隐藏界面
        Hide();
    }
    
    /// <summary>
    /// 隐藏界面
    /// </summary>
    public void Hide()
    {
        if (PanelAnimator != null)
        {
            PanelAnimator.SetTrigger("Hide");
        }
        
        Invoke(nameof(DisablePanel), 0.5f);
    }
    
    /// <summary>
    /// 禁用面板
    /// </summary>
    private void DisablePanel()
    {
        if (SelectionPanel != null)
        {
            SelectionPanel.SetActive(false);
        }
        ClearCards();
    }
    
    /// <summary>
    /// 清除卡片
    /// </summary>
    private void ClearCards()
    {
        foreach (var card in skillCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        skillCards.Clear();
    }
}

/// <summary>
/// 技能卡片组件
/// </summary>
public class SkillCard : MonoBehaviour
{
    [Header("UI元素")]
    public Image IconImage;
    public Text NameText;
    public Text DescriptionText;
    public Image RarityImage;
    public Button SelectButton;
    
    [Header("稀有度颜色")]
    public Color CommonColor = Color.white;
    public Color RareColor = Color.blue;
    public Color EpicColor = Color.magenta;
    public Color LegendaryColor = Color.yellow;
    
    private SkillData skillData;
    private System.Action<SkillData> onSelectCallback;
    
    /// <summary>
    /// 设置卡片数据
    /// </summary>
    public void Setup(SkillData skill, System.Action<SkillData> callback)
    {
        skillData = skill;
        onSelectCallback = callback;
        
        // 设置UI
        if (NameText != null)
        {
            NameText.text = skill.SkillName;
        }
        
        if (DescriptionText != null)
        {
            DescriptionText.text = skill.Description;
        }
        
        if (IconImage != null && skill.Icon != null)
        {
            IconImage.sprite = skill.Icon;
        }
        
        // 设置稀有度颜色
        if (RarityImage != null)
        {
            RarityImage.color = GetRarityColor(skill.Rarity);
        }
        
        // 绑定按钮事件
        if (SelectButton != null)
        {
            SelectButton.onClick.AddListener(OnClick);
        }
    }
    
    /// <summary>
    /// 获取稀有度颜色
    /// </summary>
    private Color GetRarityColor(SkillRarity rarity)
    {
        switch (rarity)
        {
            case SkillRarity.Common:
                return CommonColor;
            case SkillRarity.Rare:
                return RareColor;
            case SkillRarity.Epic:
                return EpicColor;
            case SkillRarity.Legendary:
                return LegendaryColor;
            default:
                return CommonColor;
        }
    }
    
    /// <summary>
    /// 点击选择
    /// </summary>
    private void OnClick()
    {
        onSelectCallback?.Invoke(skillData);
    }
    
    private void OnDestroy()
    {
        if (SelectButton != null)
        {
            SelectButton.onClick.RemoveListener(OnClick);
        }
    }
}
