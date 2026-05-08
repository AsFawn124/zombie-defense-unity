using UnityEngine;
using System;

/// <summary>
/// 敌人 - 僵尸类敌人基类
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("基础属性")]
    public string EnemyName = "普通僵尸";
    public float MaxHealth = 50f;
    public float MoveSpeed = 2f;
    public int GoldReward = 10;
    public int ScoreReward = 100;
    public float AttackDamage = 1f;         // 对基地的伤害
    public bool CanAttackBase = true;       // 是否能攻击基地
    
    [Header("组件")]
    public SpriteRenderer SpriteRenderer;
    public Animator Animator;
    public ParticleSystem DeathEffect;
    public ParticleSystem HitEffect;
    
    [Header("血条")]
    public GameObject HealthBarPrefab;
    private HealthBar healthBar;
    
    [Header("伤害数字")]
    public GameObject DamageTextPrefab;
    
    [Header("资源路径")]
    private const string SPRITE_PATH = "Sprites/";
    private const string PREFAB_PATH = "Prefabs/";
    private const string EFFECT_PATH = "Effects/";
    
    /// <summary>
    /// 从Resources加载精灵
    /// </summary>
    public void LoadSpriteFromResources(string spriteName)
    {
        if (SpriteRenderer != null)
        {
            Sprite sprite = Resources.Load<Sprite>(SPRITE_PATH + spriteName);
            if (sprite != null)
            {
                SpriteRenderer.sprite = sprite;
            }
        }
    }
    
    /// <summary>
    /// 从Resources加载特效
    /// </summary>
    public void LoadEffectFromResources(string effectName)
    {
        GameObject effectPrefab = Resources.Load<GameObject>(EFFECT_PATH + effectName);
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    // 运行时数据
    public float CurrentHealth { get; protected set; }
    public bool IsAlive { get; protected set; } = true;
    protected Transform targetPoint;
    protected Vector3[] pathPoints;           // 路径点
    protected int currentPathIndex = 0;
    
    // 事件
    public Action<Enemy> OnDeath;
    public Action<Enemy> OnReachEnd;
    
    // 状态效果
    protected float slowFactor = 1f;
    protected float slowDuration = 0f;
    protected bool isPoisoned = false;
    protected float poisonDamage = 0f;
    protected float poisonInterval = 0f;
    protected float lastPoisonTime = 0f;
    protected bool isStunned = false;
    protected float stunDuration = 0f;
    
    // 原始属性（用于重置）
    protected float originalMaxHealth;
    protected float originalMoveSpeed;
    
    protected virtual void Awake()
    {
        originalMaxHealth = MaxHealth;
        originalMoveSpeed = MoveSpeed;
        CurrentHealth = MaxHealth;
        
        // 创建血条
        if (HealthBarPrefab != null)
        {
            GameObject hbObj = Instantiate(HealthBarPrefab, transform);
            hbObj.transform.localPosition = new Vector3(0, 0.8f, 0);
            healthBar = hbObj.GetComponent<HealthBar>();
        }
        
        // 获取组件
        if (SpriteRenderer == null)
            SpriteRenderer = GetComponent<SpriteRenderer>();
        if (Animator == null)
            Animator = GetComponent<Animator>();
    }
    
    protected virtual void Start()
    {
        // 寻找目标点（基地）
        FindTargetPoint();
        
        // 获取路径
        GetPath();
    }
    
    protected virtual void Update()
    {
        if (!IsAlive)
            return;
        
        // 处理眩晕
        if (isStunned)
        {
            stunDuration -= Time.deltaTime;
            if (stunDuration <= 0)
            {
                isStunned = false;
            }
            return;
        }
        
        // 处理减速效果
        if (slowDuration > 0)
        {
            slowDuration -= Time.deltaTime;
            if (slowDuration <= 0)
            {
                slowFactor = 1f;
            }
        }
        
        // 处理中毒效果
        if (isPoisoned && Time.time - lastPoisonTime >= poisonInterval)
        {
            TakeDamage(poisonDamage, DamageType.Poison);
            lastPoisonTime = Time.time;
        }
        
        // 移动
        Move();
    }
    
    /// <summary>
    /// 寻找目标点
    /// </summary>
    protected virtual void FindTargetPoint()
    {
        GameObject baseObj = GameObject.FindWithTag("Base");
        if (baseObj != null)
        {
            targetPoint = baseObj.transform;
        }
    }
    
    /// <summary>
    /// 获取移动路径
    /// </summary>
    protected virtual void GetPath()
    {
        // 从 PathManager 获取路径
        if (PathManager.Instance != null)
        {
            pathPoints = PathManager.Instance.GetPath();
            currentPathIndex = 0;
        }
    }
    
    /// <summary>
    /// 移动逻辑
    /// </summary>
    protected virtual void Move()
    {
        if (isStunned) return;
        
        Vector3 targetPos;
        
        // 使用路径点移动
        if (pathPoints != null && pathPoints.Length > 0 && currentPathIndex < pathPoints.Length)
        {
            targetPos = pathPoints[currentPathIndex];
            
            // 检查是否到达当前路径点
            float distanceToPoint = Vector2.Distance(transform.position, targetPos);
            if (distanceToPoint < 0.3f)
            {
                currentPathIndex++;
                if (currentPathIndex >= pathPoints.Length)
                {
                    // 到达终点
                    ReachEnd();
                    return;
                }
            }
        }
        else if (targetPoint != null)
        {
            targetPos = targetPoint.position;
        }
        else
        {
            return;
        }
        
        // 移动
        float currentSpeed = MoveSpeed * slowFactor;
        Vector2 direction = (targetPos - transform.position).normalized;
        transform.position += (Vector3)(direction * currentSpeed * Time.deltaTime);
        
        // 朝向目标
        UpdateFacing(direction);
    }
    
    /// <summary>
    /// 更新朝向
    /// </summary>
    protected virtual void UpdateFacing(Vector2 direction)
    {
        if (SpriteRenderer == null) return;
        
        if (direction.x > 0.1f)
            SpriteRenderer.flipX = false;
        else if (direction.x < -0.1f)
            SpriteRenderer.flipX = true;
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    public virtual void TakeDamage(float damage, DamageType damageType = DamageType.Normal)
    {
        if (!IsAlive) return;
        
        // 计算实际伤害（可被覆盖）
        float actualDamage = CalculateDamage(damage, damageType);
        
        CurrentHealth -= actualDamage;
        
        // 显示伤害数字
        ShowDamageText(actualDamage, damageType);
        
        // 更新血条
        if (healthBar != null)
        {
            healthBar.UpdateHealth(CurrentHealth / MaxHealth);
        }
        
        // 播放受击特效
        if (HitEffect != null)
        {
            HitEffect.Play();
        }
        
        // 受伤动画
        if (Animator != null)
        {
            Animator.SetTrigger("Hit");
        }
        
        // 受伤闪烁
        StartCoroutine(DamageFlash());
        
        // 检查死亡
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 计算伤害（可被覆盖）
    /// </summary>
    protected virtual float CalculateDamage(float baseDamage, DamageType damageType)
    {
        return baseDamage;
    }
    
    /// <summary>
    /// 显示伤害数字
    /// </summary>
    protected virtual void ShowDamageText(float damage, DamageType damageType)
    {
        if (DamageTextPrefab == null) return;
        
        GameObject textObj = Instantiate(DamageTextPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        DamageText damageText = textObj.GetComponent<DamageText>();
        if (damageText != null)
        {
            damageText.Show(damage, damageType);
        }
    }
    
    /// <summary>
    /// 受伤闪烁
    /// </summary>
    protected System.Collections.IEnumerator DamageFlash()
    {
        if (SpriteRenderer == null) yield break;
        
        Color originalColor = SpriteRenderer.color;
        SpriteRenderer.color = Color.red;
        
        yield return new WaitForSeconds(0.1f);
        
        SpriteRenderer.color = originalColor;
    }
    
    /// <summary>
    /// 恢复生命
    /// </summary>
    public virtual void Heal(float amount)
    {
        if (!IsAlive) return;
        
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        
        if (healthBar != null)
        {
            healthBar.UpdateHealth(CurrentHealth / MaxHealth);
        }
        
        // 显示治疗数字
        ShowHealText(amount);
    }
    
    /// <summary>
    /// 显示治疗数字
    /// </summary>
    protected virtual void ShowHealText(float amount)
    {
        if (DamageTextPrefab == null) return;
        
        GameObject textObj = Instantiate(DamageTextPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        DamageText damageText = textObj.GetComponent<DamageText>();
        if (damageText != null)
        {
            damageText.ShowHeal(amount);
        }
    }
    
    /// <summary>
    /// 死亡
    /// </summary>
    protected virtual void Die()
    {
        IsAlive = false;
        
        // 播放死亡特效
        if (DeathEffect != null)
        {
            ParticleSystem effect = Instantiate(DeathEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, 2f);
        }
        
        // 播放死亡音效
        AudioManager.Instance?.PlayEnemyDeath();
        
        // 奖励
        GameManager.Instance?.AddGold(GoldReward);
        GameManager.Instance?.AddScore(ScoreReward);
        GameManager.Instance?.AddKill();
        
        // 事件
        OnDeath?.Invoke(this);
        
        // 延迟销毁
        if (SpriteRenderer != null)
            SpriteRenderer.enabled = false;
        
        if (healthBar != null)
            healthBar.gameObject.SetActive(false);
        
        // 禁用碰撞器
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
        
        Invoke(nameof(DestroySelf), 1f);
    }
    
    /// <summary>
    /// 到达终点
    /// </summary>
    protected virtual void ReachEnd()
    {
        IsAlive = false;
        OnReachEnd?.Invoke(this);
        
        // 对基地造成伤害
        if (CanAttackBase && BaseManager.Instance != null)
        {
            BaseManager.Instance.TakeDamage(Mathf.RoundToInt(AttackDamage));
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 销毁自身
    /// </summary>
    protected virtual void DestroySelf()
    {
        Destroy(gameObject);
    }
    
    #region 状态效果
    
    /// <summary>
    /// 应用减速效果
    /// </summary>
    public virtual void ApplySlow(float factor, float duration)
    {
        slowFactor = factor;
        slowDuration = duration;
    }
    
    /// <summary>
    /// 应用中毒效果
    /// </summary>
    public virtual void ApplyPoison(float damage, float interval, float duration)
    {
        isPoisoned = true;
        poisonDamage = damage;
        poisonInterval = interval;
        lastPoisonTime = Time.time;
        
        // 延迟结束中毒
        Invoke(nameof(EndPoison), duration);
    }
    
    /// <summary>
    /// 结束中毒
    /// </summary>
    protected virtual void EndPoison()
    {
        isPoisoned = false;
    }
    
    /// <summary>
    /// 应用眩晕效果
    /// </summary>
    public virtual void ApplyStun(float duration)
    {
        isStunned = true;
        stunDuration = duration;
        
        if (Animator != null)
        {
            Animator.SetBool("Stunned", true);
        }
    }
    
    /// <summary>
    /// 结束眩晕
    /// </summary>
    protected virtual void EndStun()
    {
        isStunned = false;
        
        if (Animator != null)
        {
            Animator.SetBool("Stunned", false);
        }
    }
    
    #endregion
    
    /// <summary>
    /// 初始化敌人
    /// </summary>
    public virtual void Initialize(float healthMultiplier, float speedMultiplier, int waveNumber)
    {
        MaxHealth = originalMaxHealth * healthMultiplier;
        CurrentHealth = MaxHealth;
        MoveSpeed = originalMoveSpeed * speedMultiplier;
        
        // 随波次增加奖励
        GoldReward += waveNumber * 2;
        ScoreReward += waveNumber * 10;
        
        // 更新血条
        if (healthBar != null)
        {
            healthBar.UpdateHealth(1f);
        }
    }
    
    /// <summary>
    /// 重置敌人
    /// </summary>
    public virtual void ResetEnemy()
    {
        MaxHealth = originalMaxHealth;
        CurrentHealth = MaxHealth;
        MoveSpeed = originalMoveSpeed;
        IsAlive = true;
        currentPathIndex = 0;
        
        if (SpriteRenderer != null)
            SpriteRenderer.enabled = true;
        
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(true);
            healthBar.UpdateHealth(1f);
        }
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        // 绘制路径
        if (pathPoints != null && pathPoints.Length > 0)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
            }
        }
    }
}

/// <summary>
/// 伤害类型
/// </summary>
public enum DamageType
{
    Normal,     // 普通
    Critical,   // 暴击
    Poison,     // 中毒
    Burn,       // 燃烧
    Freeze,     // 冰冻
    Electric    // 电击
}
