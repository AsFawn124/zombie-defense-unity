using UnityEngine;

/// <summary>
/// 子弹 - 炮弹/子弹逻辑
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("视觉效果")]
    public TrailRenderer Trail;
    public ParticleSystem HitEffect;
    public SpriteRenderer SpriteRenderer;
    public Light BulletLight;
    
    // 子弹数据
    private float damage;
    private float speed;
    private Enemy target;
    private Vector2 moveDirection;
    
    // 特殊效果
    private bool canPierce;
    private int pierceCount;
    private int currentPierce;
    private bool canSplash;
    private float splashRadius;
    private bool isCritical;
    private bool canSlow;
    private float slowFactor;
    private float slowDuration;
    
    // 状态
    private bool isInitialized = false;
    private float lifetime = 5f;
    private float spawnTime;
    private LayerMask enemyLayer;
    
    /// <summary>
    /// 初始化子弹
    /// </summary>
    public void Initialize(
        float dmg, 
        float spd, 
        Enemy tgt, 
        bool pierce, 
        int pierceCnt, 
        bool splash, 
        float splashR,
        bool crit = false,
        bool slow = false,
        float slowF = 0.5f,
        float slowD = 2f
    )
    {
        damage = dmg;
        speed = spd;
        target = tgt;
        canPierce = pierce;
        pierceCount = pierceCnt;
        canSplash = splash;
        splashRadius = splashR;
        isCritical = crit;
        canSlow = slow;
        slowFactor = slowF;
        slowDuration = slowD;
        
        currentPierce = 0;
        spawnTime = Time.time;
        isInitialized = true;
        enemyLayer = LayerMask.GetMask("Enemy");
        
        // 计算初始方向
        if (target != null && target.IsAlive)
        {
            moveDirection = (target.transform.position - transform.position).normalized;
        }
        else
        {
            moveDirection = transform.right;
        }
        
        // 设置旋转
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // 暴击效果
        if (isCritical && SpriteRenderer != null)
        {
            SpriteRenderer.color = Color.yellow;
            if (BulletLight != null)
            {
                BulletLight.color = Color.yellow;
                BulletLight.intensity *= 1.5f;
            }
        }
    }
    
    private void Update()
    {
        if (!isInitialized)
            return;
        
        // 检查生命周期
        if (Time.time - spawnTime > lifetime)
        {
            ReturnToPool();
            return;
        }
        
        // 追踪目标
        if (target != null && target.IsAlive)
        {
            Vector2 targetDirection = (target.transform.position - transform.position).normalized;
            moveDirection = Vector2.Lerp(moveDirection, targetDirection, 0.1f);
        }
        
        // 移动
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
        
        // 更新旋转
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized)
            return;
        
        // 检查是否击中敌人
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null && enemy.IsAlive)
        {
            HitEnemy(enemy);
        }
    }
    
    /// <summary>
    /// 击中敌人
    /// </summary>
    private void HitEnemy(Enemy enemy)
    {
        // 计算伤害类型
        DamageType damageType = isCritical ? DamageType.Critical : DamageType.Normal;
        
        // 造成伤害
        enemy.TakeDamage(damage, damageType);
        
        // 减速效果
        if (canSlow)
        {
            enemy.ApplySlow(slowFactor, slowDuration);
        }
        
        // 播放击中特效
        PlayHitEffect();
        
        // 溅射伤害
        if (canSplash && splashRadius > 0)
        {
            ApplySplashDamage(enemy.transform.position);
        }
        
        // 穿透逻辑
        if (canPierce && currentPierce < pierceCount)
        {
            currentPierce++;
            // 继续飞行
        }
        else
        {
            ReturnToPool();
        }
    }
    
    /// <summary>
    /// 播放击中特效
    /// </summary>
    private void PlayHitEffect()
    {
        if (HitEffect != null)
        {
            ParticleSystem effect = Instantiate(HitEffect, transform.position, Quaternion.identity);
            
            // 暴击特效
            if (isCritical)
            {
                var main = effect.main;
                main.startSizeMultiplier = 1.5f;
            }
            
            effect.Play();
            Destroy(effect.gameObject, 1f);
        }
    }
    
    /// <summary>
    /// 应用溅射伤害
    /// </summary>
    private void ApplySplashDamage(Vector2 center)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, splashRadius, enemyLayer);
        
        foreach (var collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && enemy.IsAlive)
            {
                float splashDamage = damage * 0.5f;
                enemy.TakeDamage(splashDamage, DamageType.Normal);
            }
        }
        
        // 播放溅射特效
        // TODO: 实例化溅射特效
    }
    
    /// <summary>
    /// 归还到对象池
    /// </summary>
    private void ReturnToPool()
    {
        // 如果有对象池，归还对象池
        // ObjectPool.Instance?.ReturnToPool("Bullet", gameObject);
        
        // 否则直接销毁
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (canSplash && splashRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, splashRadius);
        }
    }
}
