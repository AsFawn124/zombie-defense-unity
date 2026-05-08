using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 防御塔 - 核心战斗单位
/// </summary>
public class Tower : MonoBehaviour
{
    [Header("基础属性")]
    public string TowerName = "基础炮台";
    public float AttackRange = 5f;
    public float AttackDamage = 10f;
    public float AttackInterval = 0.5f;
    public float BulletSpeed = 10f;
    public int MaxLevel = 5;
    public int UpgradeCost = 100;
    public int SellValue = 50;
    
    [Header("等级属性")]
    public int Level = 1;
    public float DamagePerLevel = 1.2f;
    public float RangePerLevel = 1.1f;
    public float SpeedPerLevel = 0.9f;
    
    [Header("组件引用")]
    public Transform FirePoint;
    public GameObject BulletPrefab;
    public LayerMask EnemyLayer;
    public SpriteRenderer TowerSprite;
    public Animator TowerAnimator;
    
    [Header("特效")]
    public ParticleSystem MuzzleFlash;
    public ParticleSystem UpgradeEffect;
    public GameObject RangeIndicator;
    
    [Header("资源路径")]
    private const string SPRITE_PATH = "Sprites/";
    private const string PREFAB_PATH = "Prefabs/";
    
    /// <summary>
    /// 从Resources加载精灵
    /// </summary>
    public void LoadSpriteFromResources(string spriteName)
    {
        if (TowerSprite != null)
        {
            Sprite sprite = Resources.Load<Sprite>(SPRITE_PATH + spriteName);
            if (sprite != null)
            {
                TowerSprite.sprite = sprite;
            }
        }
    }
    
    [Header("音频")]
    public AudioClip FireSound;
    public AudioClip UpgradeSound;
    public AudioClip SellSound;
    
    // 运行时数据
    private float lastAttackTime = 0f;
    private Enemy currentTarget;
    private List<Enemy> enemiesInRange = new List<Enemy>();
    private AudioSource audioSource;
    private bool isSelected = false;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;
    
    // 技能加成
    public float DamageMultiplier = 1f;
    public float RangeMultiplier = 1f;
    public float FireRateMultiplier = 1f;
    public bool CanPierce = false;
    public int PierceCount = 0;
    public bool CanSplash = false;
    public float SplashRadius = 0f;
    public bool CanCrit = false;
    public float CritChance = 0f;
    public float CritMultiplier = 2f;
    public int MultiShotCount = 1;
    public bool CanSlow = false;
    public float SlowFactor = 0.5f;
    public float SlowDuration = 2f;
    
    // 事件
    public System.Action<Tower> OnTowerSelected;
    public System.Action<Tower> OnTowerUpgraded;
    public System.Action<Tower> OnTowerSold;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (TowerSprite == null)
            TowerSprite = GetComponent<SpriteRenderer>();
        if (TowerAnimator == null)
            TowerAnimator = GetComponent<Animator>();
            
        mainCamera = Camera.main;
        
        // 隐藏范围指示器
        if (RangeIndicator != null)
            RangeIndicator.SetActive(false);
    }
    
    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;
        
        // 拖拽逻辑
        HandleDrag();
        
        // 寻找目标
        FindTarget();
        
        // 攻击逻辑
        if (currentTarget != null && CanAttack())
        {
            Attack();
        }
        
        // 朝向目标
        if (currentTarget != null)
        {
            RotateTowardsTarget();
        }
    }
    
    private void OnMouseDown()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;
        
        isSelected = true;
        OnTowerSelected?.Invoke(this);
        
        // 显示范围指示器
        ShowRangeIndicator();
        
        // 开始拖拽
        isDragging = true;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = mainCamera.WorldToScreenPoint(transform.position).z;
        dragOffset = transform.position - mainCamera.ScreenToWorldPoint(mousePos);
    }
    
    private void OnMouseUp()
    {
        isDragging = false;
    }
    
    private void OnMouseExit()
    {
        if (!isDragging)
        {
            isSelected = false;
            HideRangeIndicator();
        }
    }
    
    /// <summary>
    /// 处理拖拽
    /// </summary>
    private void HandleDrag()
    {
        if (!isDragging) return;
        
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = mainCamera.WorldToScreenPoint(transform.position).z;
            Vector3 newPos = mainCamera.ScreenToWorldPoint(mousePos) + dragOffset;
            newPos.z = 0;
            transform.position = newPos;
        }
        else
        {
            isDragging = false;
        }
    }
    
    /// <summary>
    /// 显示范围指示器
    /// </summary>
    private void ShowRangeIndicator()
    {
        if (RangeIndicator != null)
        {
            RangeIndicator.SetActive(true);
            float range = AttackRange * RangeMultiplier;
            RangeIndicator.transform.localScale = new Vector3(range * 2, range * 2, 1);
        }
    }
    
    /// <summary>
    /// 隐藏范围指示器
    /// </summary>
    private void HideRangeIndicator()
    {
        if (RangeIndicator != null)
            RangeIndicator.SetActive(false);
    }
    
    /// <summary>
    /// 寻找攻击目标
    /// </summary>
    private void FindTarget()
    {
        enemiesInRange.RemoveAll(e => e == null || !e.IsAlive);
        
        float actualRange = AttackRange * RangeMultiplier;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, actualRange, EnemyLayer);
        
        enemiesInRange.Clear();
        foreach (var collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && enemy.IsAlive)
            {
                enemiesInRange.Add(enemy);
            }
        }
        
        currentTarget = GetClosestEnemy();
    }
    
    /// <summary>
    /// 获取最近的敌人
    /// </summary>
    private Enemy GetClosestEnemy()
    {
        Enemy closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (var enemy in enemiesInRange)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// 是否可以攻击
    /// </summary>
    private bool CanAttack()
    {
        float interval = AttackInterval / FireRateMultiplier;
        return Time.time - lastAttackTime >= interval;
    }
    
    /// <summary>
    /// 执行攻击
    /// </summary>
    private void Attack()
    {
        lastAttackTime = Time.time;
        
        // 播放特效
        if (MuzzleFlash != null)
        {
            MuzzleFlash.Play();
        }
        
        // 播放音效
        if (FireSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(FireSound, 0.5f);
        }
        
        // 播放动画
        if (TowerAnimator != null)
        {
            TowerAnimator.SetTrigger("Fire");
        }
        
        // 发射多发子弹
        for (int i = 0; i < MultiShotCount; i++)
        {
            float angleOffset = 0;
            if (MultiShotCount > 1)
            {
                angleOffset = (i - (MultiShotCount - 1) / 2f) * 15f;
            }
            FireBullet(angleOffset);
        }
    }
    
    /// <summary>
    /// 发射子弹
    /// </summary>
    private void FireBullet(float angleOffset = 0)
    {
        if (BulletPrefab == null || FirePoint == null)
            return;
        
        GameObject bulletObj = Instantiate(BulletPrefab, FirePoint.position, FirePoint.rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        
        if (bullet != null)
        {
            // 计算伤害
            float damage = AttackDamage * DamageMultiplier * Mathf.Pow(DamagePerLevel, Level - 1);
            
            // 暴击判定
            bool isCrit = CanCrit && Random.value < CritChance;
            if (isCrit)
            {
                damage *= CritMultiplier;
            }
            
            // 应用角度偏移
            if (angleOffset != 0)
            {
                bulletObj.transform.Rotate(0, 0, angleOffset);
            }
            
            bullet.Initialize(
                damage, 
                BulletSpeed, 
                currentTarget, 
                CanPierce, 
                PierceCount, 
                CanSplash, 
                SplashRadius,
                isCrit,
                CanSlow,
                SlowFactor,
                SlowDuration
            );
        }
    }
    
    /// <summary>
    /// 朝向目标旋转
    /// </summary>
    private void RotateTowardsTarget()
    {
        Vector2 direction = currentTarget.transform.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    /// <summary>
    /// 升级塔
    /// </summary>
    public bool Upgrade()
    {
        if (Level >= MaxLevel)
            return false;
        
        int cost = GetUpgradeCost();
        if (GameManager.Instance != null && GameManager.Instance.SpendGold(cost))
        {
            Level++;
            
            // 播放特效
            if (UpgradeEffect != null)
            {
                UpgradeEffect.Play();
            }
            
            // 播放音效
            if (UpgradeSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(UpgradeSound);
            }
            
            // 播放动画
            if (TowerAnimator != null)
            {
                TowerAnimator.SetTrigger("Upgrade");
            }
            
            OnTowerUpgraded?.Invoke(this);
            
            Debug.Log($"{TowerName} 升级到 Lv.{Level}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取升级费用
    /// </summary>
    public int GetUpgradeCost()
    {
        return Mathf.RoundToInt(UpgradeCost * Mathf.Pow(1.5f, Level - 1));
    }
    
    /// <summary>
    /// 出售塔
    /// </summary>
    public void Sell()
    {
        int sellValue = GetSellValue();
        GameManager.Instance?.AddGold(sellValue);
        
        // 播放音效
        if (SellSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(SellSound);
        }
        
        OnTowerSold?.Invoke(this);
        
        // 销毁
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 获取出售价值
    /// </summary>
    public int GetSellValue()
    {
        float totalValue = SellValue;
        for (int i = 1; i < Level; i++)
        {
            totalValue += UpgradeCost * Mathf.Pow(1.5f, i - 1) * 0.7f;
        }
        return Mathf.RoundToInt(totalValue * 0.5f);
    }
    
    /// <summary>
    /// 应用技能效果
    /// </summary>
    public void ApplySkill(SkillData skill)
    {
        switch (skill.SkillType)
        {
            case SkillType.DamageUp:
                DamageMultiplier += skill.Value;
                break;
            case SkillType.RangeUp:
                RangeMultiplier += skill.Value;
                break;
            case SkillType.FireRateUp:
                FireRateMultiplier += skill.Value;
                break;
            case SkillType.Pierce:
                CanPierce = true;
                PierceCount += (int)skill.Value;
                break;
            case SkillType.Splash:
                CanSplash = true;
                SplashRadius += skill.Value;
                break;
            case SkillType.CritRateUp:
                CanCrit = true;
                CritChance += skill.Value;
                break;
            case SkillType.MultiShot:
                MultiShotCount = Mathf.Max(MultiShotCount, (int)skill.Value);
                break;
            case SkillType.SlowEffect:
                CanSlow = true;
                SlowFactor = skill.Value;
                break;
        }
        
        Debug.Log($"应用技能: {skill.SkillName}");
    }
    
    /// <summary>
    /// 重置塔状态
    /// </summary>
    public void ResetTower()
    {
        Level = 1;
        DamageMultiplier = 1f;
        RangeMultiplier = 1f;
        FireRateMultiplier = 1f;
        CanPierce = false;
        PierceCount = 0;
        CanSplash = false;
        SplashRadius = 0f;
        CanCrit = false;
        CritChance = 0f;
        MultiShotCount = 1;
        CanSlow = false;
    }
    
    /// <summary>
    /// 获取塔信息
    /// </summary>
    public string GetTowerInfo()
    {
        float damage = AttackDamage * DamageMultiplier * Mathf.Pow(DamagePerLevel, Level - 1);
        float range = AttackRange * RangeMultiplier;
        float fireRate = FireRateMultiplier / AttackInterval;
        
        return $"{TowerName} Lv.{Level}\n" +
               $"伤害: {damage:F1}\n" +
               $"射程: {range:F1}\n" +
               $"攻速: {fireRate:F1}/s";
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
}
