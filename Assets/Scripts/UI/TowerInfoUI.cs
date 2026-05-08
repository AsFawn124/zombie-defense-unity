using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 防御塔信息UI
/// </summary>
public class TowerInfoUI : MonoBehaviour
{
    [Header("信息面板")]
    public GameObject InfoPanel;
    public Text TowerNameText;
    public Text TowerLevelText;
    public Text DamageText;
    public Text RangeText;
    public Text FireRateText;
    
    [Header("按钮")]
    public Button UpgradeButton;
    public Text UpgradeCostText;
    public Button SellButton;
    public Text SellValueText;
    public Button CloseButton;
    
    [Header("技能显示")]
    public Transform SkillContainer;
    public GameObject SkillIconPrefab;
    
    private Tower currentTower;
    
    private void Start()
    {
        // 绑定按钮事件
        UpgradeButton?.onClick.AddListener(OnUpgradeClick);
        SellButton?.onClick.AddListener(OnSellClick);
        CloseButton?.onClick.AddListener(OnCloseClick);
        
        // 订阅塔选择事件
        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.OnTowerSelected += OnTowerSelected;
            TowerManager.Instance.OnTowerDeselected += OnTowerDeselected;
        }
        
        // 初始隐藏
        InfoPanel?.SetActive(false);
    }
    
    /// <summary>
    /// 选择塔
    /// </summary>
    private void OnTowerSelected(Tower tower)
    {
        currentTower = tower;
        UpdateUI();
        InfoPanel?.SetActive(true);
    }
    
    /// <summary>
    /// 取消选择
    /// </summary>
    private void OnTowerDeselected()
    {
        currentTower = null;
        InfoPanel?.SetActive(false);
    }
    
    /// <summary>
    /// 更新UI
    /// </summary>
    private void UpdateUI()
    {
        if (currentTower == null) return;
        
        // 更新基本信息
        if (TowerNameText != null)
            TowerNameText.text = currentTower.TowerName;
        
        if (TowerLevelText != null)
            TowerLevelText.text = $"等级: {currentTower.Level}/{currentTower.MaxLevel}";
        
        // 更新属性
        float damage = currentTower.AttackDamage * currentTower.DamageMultiplier * Mathf.Pow(currentTower.DamagePerLevel, currentTower.Level - 1);
        if (DamageText != null)
            DamageText.text = $"伤害: {damage:F1}";
        
        float range = currentTower.AttackRange * currentTower.RangeMultiplier;
        if (RangeText != null)
            RangeText.text = $"射程: {range:F1}";
        
        float fireRate = currentTower.FireRateMultiplier / currentTower.AttackInterval;
        if (FireRateText != null)
            FireRateText.text = $"攻速: {fireRate:F1}/s";
        
        // 更新升级按钮
        if (currentTower.Level >= currentTower.MaxLevel)
        {
            UpgradeButton?.gameObject.SetActive(false);
        }
        else
        {
            UpgradeButton?.gameObject.SetActive(true);
            int upgradeCost = currentTower.GetUpgradeCost();
            if (UpgradeCostText != null)
                UpgradeCostText.text = $"升级 ({upgradeCost}G)";
            
            // 检查金币是否足够
            bool canAfford = GameManager.Instance != null && GameManager.Instance.Gold >= upgradeCost;
            UpgradeButton.interactable = canAfford;
        }
        
        // 更新出售按钮
        int sellValue = currentTower.GetSellValue();
        if (SellValueText != null)
            SellValueText.text = $"出售 (+{sellValue}G)";
    }
    
    /// <summary>
    /// 升级按钮点击
    /// </summary>
    private void OnUpgradeClick()
    {
        if (currentTower != null)
        {
            if (currentTower.Upgrade())
            {
                AudioManager.Instance?.PlayButtonClick();
                UpdateUI();
            }
        }
    }
    
    /// <summary>
    /// 出售按钮点击
    /// </summary>
    private void OnSellClick()
    {
        if (currentTower != null)
        {
            AudioManager.Instance?.PlayButtonClick();
            currentTower.Sell();
        }
    }
    
    /// <summary>
    /// 关闭按钮点击
    /// </summary>
    private void OnCloseClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        TowerManager.Instance?.DeselectTower();
    }
    
    private void OnDestroy()
    {
        UpgradeButton?.onClick.RemoveListener(OnUpgradeClick);
        SellButton?.onClick.RemoveListener(OnSellClick);
        CloseButton?.onClick.RemoveListener(OnCloseClick);
        
        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.OnTowerSelected -= OnTowerSelected;
            TowerManager.Instance.OnTowerDeselected -= OnTowerDeselected;
        }
    }
}
