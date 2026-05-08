using UnityEngine;
using System.Collections.Generic;

// ============================================
// 快速僵尸
// ============================================
public class FastZombie : Enemy
{
    protected override void Awake()
    {
        EnemyName = "快速僵尸";
        MaxHealth = 30f;
        MoveSpeed = 4f;
        GoldReward = 15;
        ScoreReward = 120;
        base.Awake();
    }
}

// ============================================
// 坦克僵尸
// ============================================
public class TankZombie : Enemy
{
    [Header("坦克特性")]
    public float DamageReduction = 0.3f;
    
    protected override void Awake()
    {
        EnemyName = "坦克僵尸";
        MaxHealth = 200f;
        MoveSpeed = 1f;
        GoldReward = 30;
        ScoreReward = 200;
        base.Awake();
    }
    
    protected override float CalculateDamage(float baseDamage, DamageType damageType)
    {
        // 30%伤害减免
        return baseDamage * (1 - DamageReduction);
    }
}

// ============================================
// 自爆僵尸
// ============================================
public class BomberZombie : Enemy
{
    [Header("自爆设置")]
    public float ExplosionRadius = 2f;
    public float ExplosionDamage = 30f;
    public GameObject ExplosionEffect;
    public LayerMask EnemyLayer;
    
    private bool hasExploded = false;
    
    protected override void Awake()
    {
        EnemyName = "自爆僵尸";
        MaxHealth = 60f;
        MoveSpeed = 2.5f;
        GoldReward = 20;
        ScoreReward = 150;
        base.Awake();
    }
    
    protected override void Die()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        Explode();
        base.Die();
    }
    
    private void Explode()
    {
        // 播放爆炸特效
        if (ExplosionEffect != null)
        {
            GameObject effect = Instantiate(ExplosionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // 对周围敌人造成伤害
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius, EnemyLayer);
        foreach (var collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && enemy != this && enemy.IsAlive)
            {
                enemy.TakeDamage(ExplosionDamage);
            }
        }
        
        // 对基地造成伤害（如果靠近）
        if (BaseManager.Instance != null)
        {
            float distanceToBase = Vector2.Distance(transform.position, BaseManager.Instance.transform.position);
            if (distanceToBase <= ExplosionRadius)
            {
                BaseManager.Instance.TakeDamage(Mathf.RoundToInt(ExplosionDamage / 10));
            }
        }
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // 绘制爆炸范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ExplosionRadius);
    }
}

// ============================================
// 治疗僵尸
// ============================================
public class HealerZombie : Enemy
{
    [Header("治疗设置")]
    public float HealRadius = 3f;
    public float HealAmount = 5f;
    public float HealInterval = 2f;
    public GameObject HealEffect;
    public LayerMask AllyLayer;
    
    private float lastHealTime;
    
    protected override void Awake()
    {
        EnemyName = "治疗僵尸";
        MaxHealth = 80f;
        MoveSpeed = 1.5f;
        GoldReward = 25;
        ScoreReward = 180;
        base.Awake();
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 治疗周围友军
        if (Time.time - lastHealTime >= HealInterval)
        {
            HealNearbyEnemies();
            lastHealTime = Time.time;
        }
    }
    
    private void HealNearbyEnemies()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, HealRadius, AllyLayer);
        bool hasHealed = false;
        
        foreach (var collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && enemy != this && enemy.IsAlive)
            {
                enemy.Heal(HealAmount);
                hasHealed = true;
            }
        }
        
        if (hasHealed && HealEffect != null)
        {
            GameObject effect = Instantiate(HealEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // 绘制治疗范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, HealRadius);
    }
}

// ============================================
// 分裂僵尸
// ============================================
public class SplitZombie : Enemy
{
    [Header("分裂设置")]
    public int SplitCount = 2;
    public GameObject MiniZombiePrefab;
    
    protected override void Awake()
    {
        EnemyName = "分裂僵尸";
        MaxHealth = 100f;
        MoveSpeed = 2f;
        GoldReward = 25;
        ScoreReward = 160;
        base.Awake();
    }
    
    protected override void Die()
    {
        Split();
        base.Die();
    }
    
    private void Split()
    {
        if (MiniZombiePrefab == null) return;
        
        for (int i = 0; i < SplitCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.5f;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);
            
            GameObject miniZombie = Instantiate(MiniZombiePrefab, spawnPos, Quaternion.identity);
            Enemy miniEnemy = miniZombie.GetComponent<Enemy>();
            if (miniEnemy != null)
            {
                // 小僵尸属性
                miniEnemy.Initialize(0.3f, 1.2f, GameManager.Instance?.CurrentWave ?? 1);
            }
        }
    }
}

// ============================================
// 精英僵尸
// ============================================
public class EliteZombie : Enemy
{
    [Header("精英特性")]
    public float DamageReduction = 0.3f;
    public float CritChance = 0.2f;
    
    protected override void Awake()
    {
        EnemyName = "精英僵尸";
        MaxHealth = 150f;
        MoveSpeed = 2f;
        GoldReward = 40;
        ScoreReward = 250;
        AttackDamage = 2f;
        base.Awake();
        
        // 增大体型
        transform.localScale = new Vector3(1.3f, 1.3f, 1f);
    }
    
    protected override float CalculateDamage(float baseDamage, DamageType damageType)
    {
        return baseDamage * (1 - DamageReduction);
    }
    
    protected override void ReachEnd()
    {
        // 精英僵尸对基地造成更多伤害
        if (CanAttackBase && BaseManager.Instance != null)
        {
            // 20%概率暴击
            float damage = AttackDamage;
            if (Random.value < CritChance)
            {
                damage *= 2;
            }
            BaseManager.Instance.TakeDamage(Mathf.RoundToInt(damage));
        }
        
        IsAlive = false;
        OnReachEnd?.Invoke(this);
        Destroy(gameObject);
    }
}

// ============================================
// BOSS僵尸
// ============================================
public class BossZombie : Enemy
{
    [Header("BOSS特性")]
    public float EnrageThreshold = 0.3f;
    public float EnrageSpeedMultiplier = 1.5f;
    public float EnrageDamageMultiplier = 1.5f;
    public GameObject EnrageEffect;
    public float SkillCooldown = 5f;
    public GameObject SkillEffect;
    
    private bool isEnraged = false;
    private float lastSkillTime;
    
    protected override void Awake()
    {
        EnemyName = "僵尸BOSS";
        MaxHealth = 1000f;
        MoveSpeed = 1.2f;
        GoldReward = 200;
        ScoreReward = 1000;
        AttackDamage = 5f;
        base.Awake();
        
        // BOSS体型更大
        transform.localScale = new Vector3(2f, 2f, 1f);
    }
    
    protected override void Start()
    {
        base.Start();
        
        // BOSS出现时切换BGM
        AudioManager.Instance?.PlayBossBGM();
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 检查狂暴状态
        if (!isEnraged && CurrentHealth / MaxHealth <= EnrageThreshold)
        {
            Enrage();
        }
        
        // 使用技能
        if (Time.time - lastSkillTime >= SkillCooldown)
        {
            UseSkill();
            lastSkillTime = Time.time;
        }
    }
    
    private void Enrage()
    {
        isEnraged = true;
        MoveSpeed *= EnrageSpeedMultiplier;
        AttackDamage *= EnrageDamageMultiplier;
        
        // 播放狂暴特效
        if (EnrageEffect != null)
        {
            GameObject effect = Instantiate(EnrageEffect, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform);
        }
        
        // 变红
        if (SpriteRenderer != null)
        {
            SpriteRenderer.color = Color.red;
        }
        
        // 屏幕震动
        CameraShake.Instance?.Shake(0.5f, 0.5f);
        
        Debug.Log($"{EnemyName} 进入狂暴状态！");
    }
    
    private void UseSkill()
    {
        // BOSS技能：召唤小怪
        int spawnCount = isEnraged ? 4 : 2;
        
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 2f;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);
            
            // 召唤普通僵尸
            // TODO: 从对象池获取
        }
        
        if (SkillEffect != null)
        {
            GameObject effect = Instantiate(SkillEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        Debug.Log($"{EnemyName} 使用技能：召唤小弟！");
    }
    
    protected override void Die()
    {
        // BOSS死亡特殊奖励
        GameManager.Instance?.AddGold(GoldReward * 2);
        GameManager.Instance?.AddScore(ScoreReward * 2);
        
        // 恢复普通BGM
        AudioManager.Instance?.PlayBattleBGM();
        
        base.Die();
    }
}

// ============================================
// 小僵尸（分裂僵尸的子体）
// ============================================
public class MiniZombie : Enemy
{
    protected override void Awake()
    {
        EnemyName = "小僵尸";
        MaxHealth = 20f;
        MoveSpeed = 3f;
        GoldReward = 5;
        ScoreReward = 50;
        base.Awake();
        
        // 小体型
        transform.localScale = new Vector3(0.6f, 0.6f, 1f);
    }
}
