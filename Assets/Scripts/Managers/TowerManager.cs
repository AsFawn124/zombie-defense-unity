using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 防御塔管理器 - 管理多个防御塔
/// </summary>
public class TowerManager : MonoBehaviour
{
    public static TowerManager Instance { get; private set; }
    
    [Header("塔配置")]
    public GameObject[] TowerPrefabs;
    public Transform TowerContainer;
    
    [Header("放置设置")]
    public LayerMask PlacementLayer;
    public LayerMask ObstacleLayer;
    public float MinPlacementDistance = 1f;
    
    // 运行时数据
    private List<Tower> activeTowers = new List<Tower>();
    private Tower selectedTower;
    private GameObject placementPreview;
    private bool isPlacing = false;
    private int selectedTowerIndex = -1;
    
    // 事件
    public System.Action<Tower> OnTowerSelected;
    public System.Action OnTowerDeselected;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        // 放置预览
        if (isPlacing && placementPreview != null)
        {
            UpdatePlacementPreview();
        }
        
        // 取消选择
        if (Input.GetMouseButtonDown(1) && selectedTower != null)
        {
            DeselectTower();
        }
    }
    
    /// <summary>
    /// 开始放置塔
    /// </summary>
    public void StartPlacement(int towerIndex)
    {
        if (towerIndex < 0 || towerIndex >= TowerPrefabs.Length)
            return;
        
        selectedTowerIndex = towerIndex;
        isPlacing = true;
        
        // 创建预览
        if (placementPreview != null)
            Destroy(placementPreview);
        
        placementPreview = Instantiate(TowerPrefabs[towerIndex]);
        
        // 禁用碰撞器和脚本
        Collider2D col = placementPreview.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        Tower tower = placementPreview.GetComponent<Tower>();
        if (tower != null) tower.enabled = false;
        
        // 设置半透明
        SpriteRenderer sr = placementPreview.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.5f;
            sr.color = c;
        }
    }
    
    /// <summary>
    /// 更新放置预览
    /// </summary>
    private void UpdatePlacementPreview()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        placementPreview.transform.position = mousePos;
        
        // 检查是否可以放置
        bool canPlace = CanPlaceTower(mousePos);
        
        // 更新预览颜色
        SpriteRenderer sr = placementPreview.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = canPlace ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        }
        
        // 点击放置
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            PlaceTower(mousePos);
        }
        
        // 右键取消
        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }
    
    /// <summary>
    /// 检查是否可以放置
    /// </summary>
    private bool CanPlaceTower(Vector3 position)
    {
        // 检查与其他塔的距离
        foreach (var tower in activeTowers)
        {
            if (Vector3.Distance(position, tower.transform.position) < MinPlacementDistance)
            {
                return false;
            }
        }
        
        // 检查障碍物
        if (Physics2D.OverlapCircle(position, 0.3f, ObstacleLayer))
        {
            return false;
        }
        
        // 检查放置层
        if (!Physics2D.OverlapCircle(position, 0.3f, PlacementLayer))
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 放置塔
    /// </summary>
    private void PlaceTower(Vector3 position)
    {
        if (selectedTowerIndex < 0 || selectedTowerIndex >= TowerPrefabs.Length)
            return;
        
        // 创建塔
        GameObject towerObj = Instantiate(TowerPrefabs[selectedTowerIndex], position, Quaternion.identity, TowerContainer);
        Tower tower = towerObj.GetComponent<Tower>();
        
        if (tower != null)
        {
            activeTowers.Add(tower);
            tower.OnTowerSelected += OnTowerSelectedHandler;
            tower.OnTowerSold += OnTowerSoldHandler;
        }
        
        // 清理预览
        CancelPlacement();
        
        // 播放音效
        AudioManager.Instance?.PlaySFX(AudioManager.Instance?.TowerShoot);
    }
    
    /// <summary>
    /// 取消放置
    /// </summary>
    public void CancelPlacement()
    {
        isPlacing = false;
        selectedTowerIndex = -1;
        
        if (placementPreview != null)
        {
            Destroy(placementPreview);
            placementPreview = null;
        }
    }
    
    /// <summary>
    /// 选择塔
    /// </summary>
    private void OnTowerSelectedHandler(Tower tower)
    {
        // 取消之前的选择
        if (selectedTower != null && selectedTower != tower)
        {
            selectedTower.HideRangeIndicator?.Invoke();
        }
        
        selectedTower = tower;
        OnTowerSelected?.Invoke(tower);
    }
    
    /// <summary>
    /// 取消选择
    /// </summary>
    public void DeselectTower()
    {
        if (selectedTower != null)
        {
            selectedTower.HideRangeIndicator?.Invoke();
            selectedTower = null;
        }
        
        OnTowerDeselected?.Invoke();
    }
    
    /// <summary>
    /// 塔被出售
    /// </summary>
    private void OnTowerSoldHandler(Tower tower)
    {
        activeTowers.Remove(tower);
        
        if (selectedTower == tower)
        {
            selectedTower = null;
            OnTowerDeselected?.Invoke();
        }
    }
    
    /// <summary>
    /// 升级选中的塔
    /// </summary>
    public bool UpgradeSelectedTower()
    {
        if (selectedTower != null)
        {
            return selectedTower.Upgrade();
        }
        return false;
    }
    
    /// <summary>
    /// 出售选中的塔
    /// </summary>
    public void SellSelectedTower()
    {
        if (selectedTower != null)
        {
            selectedTower.Sell();
        }
    }
    
    /// <summary>
    /// 获取选中的塔
    /// </summary>
    public Tower GetSelectedTower()
    {
        return selectedTower;
    }
    
    /// <summary>
    /// 获取所有塔
    /// </summary>
    public List<Tower> GetAllTowers()
    {
        return new List<Tower>(activeTowers);
    }
    
    /// <summary>
    /// 应用技能到所有塔
    /// </summary>
    public void ApplySkillToAllTowers(SkillData skill)
    {
        foreach (var tower in activeTowers)
        {
            tower.ApplySkill(skill);
        }
    }
    
    /// <summary>
    /// 清除所有塔
    /// </summary>
    public void ClearAllTowers()
    {
        foreach (var tower in activeTowers)
        {
            if (tower != null)
            {
                Destroy(tower.gameObject);
            }
        }
        activeTowers.Clear();
        selectedTower = null;
    }
}
