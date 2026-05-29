using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// BOSS机制系统 - 多阶段Boss战、Boss技能、弱点系统
/// 超越简单的血量高→Boss有AI和多阶段机制
/// </summary>
public class BossMechanicSystem : MonoBehaviour
{
    public static BossMechanicSystem Instance;

    [Header("配置")]
    public BossMechanicConfig Config;

    private Dictionary<string, ActiveBoss> _activeBosses = new Dictionary<string, ActiveBoss>();

    // 事件
    public event Action<string, int> OnBossPhaseChanged;     // bossId, phase
    public event Action<string, BossAbility> OnBossAbilityUsed;
    public event Action<string, Vector3, float> OnBossWeakPointExposed;
    public event Action<string, bool> OnBossDefeated;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #region === Boss 注册/注销 ===

    public void RegisterBoss(GameObject bossObject, string bossTemplateId)
    {
        var template = Config.GetTemplate(bossTemplateId);
        if (template == null)
        {
            Debug.LogError($"[BossMechanic] Boss模板未找到: {bossTemplateId}");
            return;
        }

        var activeBoss = new ActiveBoss
        {
            BossId = Guid.NewGuid().ToString("N").Substring(0, 8),
            Template = template,
            GameObject = bossObject,
            CurrentPhase = 0,
            PhaseProgress = 0f,
            CurrentHp = template.PhaseConfigs[0].HpThreshold,
            MaxHp = template.PhaseConfigs[0].HpThreshold,
            ActiveBuffs = new List<BossBuff>(),
            AbilityCooldowns = new Dictionary<string, float>(),
            IsStunned = false,
            IsVulnerable = false
        };

        foreach (var ability in template.Abilities)
            activeBoss.AbilityCooldowns[ability.AbilityId] = 0f;

        _activeBosses[activeBoss.BossId] = activeBoss;

        // 激活第一阶段
        ActivatePhase(activeBoss, 0);
    }

    public void UnregisterBoss(string bossId)
    {
        _activeBosses.Remove(bossId);
    }

    #endregion

    #region === Boss 阶段机制 ===

    private void ActivatePhase(ActiveBoss boss, int phaseIndex)
    {
        if (phaseIndex >= boss.Template.PhaseConfigs.Count) return;

        var phase = boss.Template.PhaseConfigs[phaseIndex];
        boss.CurrentPhase = phaseIndex;
        boss.PhaseConfig = phase;
        boss.MaxHp = phase.HpThreshold;

        OnBossPhaseChanged?.Invoke(boss.BossId, phaseIndex);

        // 应用阶段特性
        if (phase.AddsEnrage)
            ApplyBuff(boss, new BossBuff { BuffType = BossBuffType.Enrage, Value = 0.5f, Duration = -1 });

        if (phase.SpawnsMinions)
            StartCoroutine(SpawnMinionsRoutine(boss));

        if (phase.ShieldAmount > 0)
            boss.ActiveShield = phase.ShieldAmount;

        // 暴露弱点
        if (phase.WeakPoints > 0)
            ExposeWeakPoints(boss, phase.WeakPoints);
    }

    /// <summary>
    /// Boss受到伤害 - 检查阶段切换
    /// </summary>
    public float BossTakeDamage(string bossId, float rawDamage, DamageType damageType = DamageType.Physical)
    {
        if (!_activeBosses.TryGetValue(bossId, out var boss)) return 0f;

        // 无敌状态检查
        if (boss.IsInvincible) return 0f;

        // 护盾先吸收
        float damage = rawDamage;
        if (boss.ActiveShield > 0)
        {
            float shieldAbsorb = Mathf.Min(boss.ActiveShield, damage);
            boss.ActiveShield -= shieldAbsorb;
            damage -= shieldAbsorb;
        }

        // 脆弱状态双倍伤害
        if (boss.IsVulnerable)
            damage *= 2f;

        // 弱点属性加成
        if (boss.PhaseConfig.WeaknessType != DamageType.None &&
            damageType == boss.PhaseConfig.WeaknessType)
        {
            damage *= boss.PhaseConfig.WeaknessMultiplier;
        }

        boss.CurrentHp -= damage;

        // 检查阶段切换
        float hpPercent = boss.CurrentHp / boss.MaxHp;
        CheckPhaseTransition(boss, hpPercent);

        // 检查死亡
        if (boss.CurrentHp <= 0)
        {
            OnBossDefeated?.Invoke(bossId, true);
            _activeBosses.Remove(bossId);
        }

        return damage;
    }

    private void CheckPhaseTransition(ActiveBoss boss, float hpPercent)
    {
        var phases = boss.Template.PhaseConfigs;
        for (int i = phases.Count - 1; i > boss.CurrentPhase; i--)
        {
            if (hpPercent <= phases[i].HpRatioToTransition)
            {
                ActivatePhase(boss, i);
                break;
            }
        }
    }

    #endregion

    #region === Boss 技能系统 ===

    void Update()
    {
        foreach (var kvp in _activeBosses)
        {
            var boss = kvp.Value;
            if (boss.IsStunned) continue;

            // 更新冷却
            var cooldownKeys = new List<string>(boss.AbilityCooldowns.Keys);
            foreach (var key in cooldownKeys)
                if (boss.AbilityCooldowns[key] > 0)
                    boss.AbilityCooldowns[key] -= Time.deltaTime;

            // 尝试使用技能
            TryUseAbility(boss);
        }
    }

    private void TryUseAbility(ActiveBoss boss)
    {
        foreach (var ability in boss.Template.Abilities)
        {
            if (boss.AbilityCooldowns[ability.AbilityId] > 0) continue;
            if (!ability.AvailableInPhases.Contains(boss.CurrentPhase)) continue;

            // 随机判断是否使用
            if (UnityEngine.Random.value > ability.UseChancePerSecond * Time.deltaTime)
                continue;

            UseBossAbility(boss, ability);
        }
    }

    private void UseBossAbility(ActiveBoss boss, BossAbilityData ability)
    {
        Debug.Log($"[BossMechanic] {boss.Template.BossName} 使用技能: {ability.AbilityName}");

        switch (ability.Type)
        {
            case BossAbilityType.AOEDamage:
                ExecuteAOEDamage(boss, ability);
                break;
            case BossAbilityType.SummonMinions:
                ExecuteSummonMinions(boss, ability);
                break;
            case BossAbilityType.Charge:
                ExecuteCharge(boss, ability);
                break;
            case BossAbilityType.Shield:
                ExecuteShield(boss, ability);
                break;
            case BossAbilityType.Heal:
                ExecuteHeal(boss, ability);
                break;
            case BossAbilityType.Enrage:
                ExecuteEnrage(boss, ability);
                break;
            case BossAbilityType.Teleport:
                ExecuteTeleport(boss, ability);
                break;
            case BossAbilityType.RageMode:
                ExecuteRageMode(boss, ability);
                break;
            case BossAbilityType.SpawnTraps:
                ExecuteSpawnTraps(boss, ability);
                break;
        }

        boss.AbilityCooldowns[ability.AbilityId] = ability.Cooldown;
        OnBossAbilityUsed?.Invoke(boss.BossId, ability);
    }

    #endregion

    #region === 各技能执行 ===

    private void ExecuteAOEDamage(ActiveBoss boss, BossAbilityData ability)
    {
        float radius = ability.GetParam("radius", 5f);
        float damage = ability.GetParam("damage", 50f);

        // 对范围内所有防御塔和基地造成伤害
        var towers = FindObjectsOfType<Tower>();
        foreach (var tower in towers)
        {
            if (Vector3.Distance(boss.GameObject.transform.position, tower.transform.position) <= radius)
                tower.TakeDamage(damage * 0.5f); // 塔受到一半伤害
        }

        // 基地伤害
        if (BaseManager.Instance != null)
        {
            float dist = Vector3.Distance(boss.GameObject.transform.position,
                BaseManager.Instance.transform.position);
            if (dist <= radius)
                BaseManager.Instance.TakeDamage((int)(damage));
        }

        // 特效
        EffectManager.Instance?.PlayAOEEffect(boss.GameObject.transform.position, radius, ability.EffectId);
    }

    private void ExecuteSummonMinions(ActiveBoss boss, BossAbilityData ability)
    {
        int count = ability.GetParam("count", 3);
        string minionId = ability.GetParam("minion_type", "zombie_fast");

        WaveManager.Instance?.SpawnMinions(minionId, count, boss.GameObject.transform.position);
    }

    private void ExecuteCharge(ActiveBoss boss, BossAbilityData ability)
    {
        float speed = ability.GetParam("speed", 10f);
        float damage = ability.GetParam("damage", 100f);

        // Boss向基地冲刺
        boss.IsCharging = true;
        Vector3 direction = (BaseManager.Instance.transform.position -
            boss.GameObject.transform.position).normalized;

        StartCoroutine(ChargeRoutine(boss, direction, speed, damage, ability.Duration));
    }

    private System.Collections.IEnumerator ChargeRoutine(ActiveBoss boss, Vector3 dir, float speed, float damage, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && boss.GameObject != null)
        {
            boss.GameObject.transform.position += dir * speed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        boss.IsCharging = false;
    }

    private void ExecuteShield(ActiveBoss boss, BossAbilityData ability)
    {
        float shieldAmount = ability.GetParam("shield_amount", 500f);
        boss.ActiveShield += shieldAmount;
    }

    private void ExecuteHeal(ActiveBoss boss, BossAbilityData ability)
    {
        float healPercent = ability.GetParam("heal_percent", 0.15f);
        boss.CurrentHp = Mathf.Min(boss.CurrentHp + boss.MaxHp * healPercent, boss.MaxHp);
    }

    private void ExecuteEnrage(ActiveBoss boss, BossAbilityData ability)
    {
        float attackBoost = ability.GetParam("attack_boost", 0.5f);
        ApplyBuff(boss, new BossBuff
        {
            BuffType = BossBuffType.Enrage,
            Value = attackBoost,
            Duration = ability.Duration
        });
    }

    private void ExecuteTeleport(ActiveBoss boss, BossAbilityData ability)
    {
        // 传送到随机防御塔附近
        var towers = FindObjectsOfType<Tower>();
        if (towers.Length > 0)
        {
            var target = towers[UnityEngine.Random.Range(0, towers.Length)];
            boss.GameObject.transform.position =
                target.transform.position + UnityEngine.Random.insideUnitSphere * 3f;
        }
    }

    private void ExecuteRageMode(ActiveBoss boss, BossAbilityData ability)
    {
        // 全属性大幅提升
        ApplyBuff(boss, new BossBuff
        {
            BuffType = BossBuffType.RageMode,
            Value = ability.GetParam("boost", 2.0f),
            Duration = ability.Duration
        });
        boss.IsInvincible = true; // 变身期间无敌
    }

    private void ExecuteSpawnTraps(ActiveBoss boss, BossAbilityData ability)
    {
        int count = ability.GetParam("count", 5);
        float radius = ability.GetParam("spread_radius", 8f);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = boss.GameObject.transform.position +
                UnityEngine.Random.insideUnitSphere * radius;
            pos.y = 0;
            TerrainSystem.Instance?.PlaceTrap(pos, ability.GetParam("trap_duration", 10f));
        }
    }

    #endregion

    #region === 弱点系统 ===

    private void ExposeWeakPoints(ActiveBoss boss, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 localPos = UnityEngine.Random.onUnitSphere * 2f;
            OnBossWeakPointExposed?.Invoke(boss.BossId,
                boss.GameObject.transform.position + localPos,
                boss.PhaseConfig.WeakPointDuration);
        }
    }

    /// <summary>
    /// 击中弱点
    /// </summary>
    public float HitWeakPoint(string bossId, float damage)
    {
        if (!_activeBosses.TryGetValue(bossId, out var boss)) return 0f;

        // 弱点击中造成3倍伤害 + 短暂眩晕
        boss.IsStunned = true;
        boss.IsVulnerable = true;

        float totalDamage = damage * 3f;

        // 1秒后恢复
        StartCoroutine(ResetAfterDelay(boss, 1f));

        return totalDamage;
    }

    private System.Collections.IEnumerator ResetAfterDelay(ActiveBoss boss, float delay)
    {
        yield return new WaitForSeconds(delay);
        boss.IsStunned = false;
        boss.IsVulnerable = false;
    }

    #endregion

    #region === Buff系统 ===

    private void ApplyBuff(ActiveBoss boss, BossBuff buff)
    {
        boss.ActiveBuffs.Add(buff);
        if (buff.Duration > 0)
            StartCoroutine(RemoveBuffAfterDelay(boss, buff));
    }

    private System.Collections.IEnumerator RemoveBuffAfterDelay(ActiveBoss boss, BossBuff buff)
    {
        yield return new WaitForSeconds(buff.Duration);
        boss.ActiveBuffs.Remove(buff);
    }

    public float GetBossAttackMultiplier(string bossId)
    {
        if (!_activeBosses.TryGetValue(bossId, out var boss)) return 1f;
        float multiplier = 1f;
        foreach (var buff in boss.ActiveBuffs)
            if (buff.BuffType == BossBuffType.Enrage || buff.BuffType == BossBuffType.RageMode)
                multiplier += buff.Value;
        return multiplier;
    }

    #endregion

    #region === 小怪召唤协程 ===

    private System.Collections.IEnumerator SpawnMinionsRoutine(ActiveBoss boss)
    {
        while (boss != null && boss.GameObject != null && boss.CurrentHp > 0)
        {
            WaveManager.Instance?.SpawnMinions("zombie_fast", 2, boss.GameObject.transform.position);
            yield return new WaitForSeconds(boss.PhaseConfig.MinionSpawnInterval);
        }
    }

    #endregion
}

#region === 数据结构 ===

[Serializable]
public class ActiveBoss
{
    public string BossId;
    public BossTemplate Template;
    public GameObject GameObject;
    public int CurrentPhase;
    public float PhaseProgress;
    public float CurrentHp;
    public float MaxHp;
    public float ActiveShield;
    public bool IsInvincible;
    public bool IsStunned;
    public bool IsVulnerable;
    public bool IsCharging;
    public BossPhaseConfig PhaseConfig;
    public List<BossBuff> ActiveBuffs;
    public Dictionary<string, float> AbilityCooldowns;
}

[Serializable]
public class BossTemplate
{
    public string BossId;
    public string BossName;
    public string BossTitle; // "赛博暴君·泽拉图"
    public BossArchetype Archetype; // 坦克/刺客/法师/召唤师
    public List<BossPhaseConfig> PhaseConfigs = new List<BossPhaseConfig>();
    public List<BossAbilityData> Abilities = new List<BossAbilityData>();
    public float MoveSpeed = 2f;
    public float BaseAttack = 50f;
    public int RewardGold = 500;
    public int RewardDiamonds = 5;
    public string IntroEffectId;
    public string DeathEffectId;
}

public enum BossArchetype
{
    Tank,       // 高血量、护盾、冲锋
    Assassin,   // 高伤害、传送、突袭
    Mage,       // AOE、远程、召唤
    Summoner,   // 召唤小怪、陷阱
    Hybrid      // 混合型
}

[Serializable]
public class BossPhaseConfig
{
    public int PhaseIndex;
    public string PhaseName;          // "第一阶段: 机械装甲"
    public float HpRatioToTransition; // 血量比例触发 (0.7 = 70%血量触发此阶段)
    public float HpThreshold;         // 此阶段血量
    public DamageType WeaknessType;   // 弱点属性
    public float WeaknessMultiplier = 2f;
    public int WeakPoints;            // 弱点数量
    public float WeakPointDuration = 5f;
    public float ShieldAmount;
    public bool AddsEnrage;
    public bool SpawnsMinions;
    public float MinionSpawnInterval = 15f;
    public bool SpeedsUp;
    public float SpeedMultiplier = 1f;
}

public enum DamageType
{
    None, Physical, Fire, Ice, Lightning, Poison, Wind
}

[Serializable]
public class BossAbilityData
{
    public string AbilityId;
    public string AbilityName;
    public BossAbilityType Type;
    public float Cooldown = 15f;
    public float Duration = 3f;
    public float UseChancePerSecond = 0.1f; // 每秒使用概率
    public List<int> AvailableInPhases = new List<int> { 0 };
    public string EffectId;
    public Dictionary<string, float> Params = new Dictionary<string, float>();

    public float GetParam(string key, float defaultValue)
    {
        if (Params.TryGetValue(key, out var val)) return val;
        return defaultValue;
    }
}

public enum BossAbilityType
{
    AOEDamage,
    SummonMinions,
    Charge,
    Shield,
    Heal,
    Enrage,
    Teleport,
    RageMode,
    SpawnTraps,
}

public enum BossBuffType
{
    Enrage, RageMode, SpeedUp, DamageUp, Invincible
}

[Serializable]
public class BossBuff
{
    public BossBuffType BuffType;
    public float Value;
    public float Duration; // -1 = 永久
}

[Serializable]
public class BossMechanicConfig : ScriptableObject
{
    public List<BossTemplate> BossTemplates = new List<BossTemplate>();

    public BossTemplate GetTemplate(string bossId)
        => BossTemplates.Find(t => t.BossId == bossId);
}

#endregion
