using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// AI导演系统 - 动态难度调整 (Director AI)
/// 根据玩家表现实时调整难度、掉落和敌人配置
/// 参考: Left 4 Dead AI Director, 明日方舟危机合约
/// </summary>
public class DynamicDifficultyManager : MonoBehaviour
{
    public static DynamicDifficultyManager Instance;

    [Header("配置")]
    public DirectorConfig Config;

    // 玩家状态追踪
    private DirectorPlayerState _playerState;
    private DirectorSessionState _sessionState;
    private float _sessionStartTime;
    private int _sessionGamesPlayed;

    // 动态参数
    private float _currentIntensity;        // 0~1 当前压力指数
    private float _peakIntensity;           // 本次会话峰值
    private float _timeSinceLastPeak;       // 距离上次峰值的时间
    private float _relaxTimer;              // 放松倒计时

    // 事件
    public event Action<DirectorEvent> OnDirectorEvent;
    public event Action<float> OnIntensityChanged;
    public event Action<DifficultyModifier> OnDifficultyAdjusted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadPlayerState();
        _sessionStartTime = Time.time;
        _currentIntensity = 0.3f;
        _relaxTimer = Config.BaseRelaxTime;
    }

    #region === 核心循环: 评估 → 调整 ===

    void Update()
    {
        if (_sessionState == null) return;

        // 每个评估间隔检查一次
        if (Time.frameCount % (int)(Config.EvaluateIntervalSec * 60) != 0) return;

        EvaluatePlayerPerformance();
        AdjustDifficulty();
    }

    /// <summary>
    /// 评估玩家表现
    /// </summary>
    private void EvaluatePlayerPerformance()
    {
        var metrics = _sessionState;

        // 1. 计算基础压力指数
        float pressure = 0f;

        // HP压力 (基地血量越低 压力越大)
        if (metrics.MaxBaseHp > 0)
        {
            float hpRatio = (float)metrics.CurrentBaseHp / metrics.MaxBaseHp;
            pressure += (1f - hpRatio) * Config.HpPressureWeight;
        }

        // 击杀速度 (击杀越快 压力越大→意味着需要更多挑战)
        float killRate = metrics.EnemiesKilledPerMinute;
        float normalizedKillRate = Mathf.Clamp01(killRate / Config.ExpectedKillRate);
        pressure += normalizedKillRate * Config.KillRateWeight;

        // 波次进度
        float waveProgress = (float)metrics.CurrentWave / metrics.TargetWave;
        pressure += waveProgress * Config.WaveProgressWeight;

        // 金币储备 (金币越多 压力越小)
        float goldRatio = Mathf.Clamp01(metrics.CurrentGold / Config.ComfortableGold);
        pressure -= goldRatio * Config.GoldComfortWeight;

        // 防御塔数量/等级
        float towerPower = CalculateTowerPower();
        pressure -= Mathf.Clamp01(towerPower / Config.ExpectedTowerPower) * Config.TowerWeight;

        // 2. 平滑处理
        _currentIntensity = Mathf.Lerp(_currentIntensity,
            Mathf.Clamp(pressure, Config.MinIntensity, Config.MaxIntensity),
            Config.IntensitySmoothing * Time.deltaTime);

        // 3. 追踪峰值
        if (_currentIntensity > _peakIntensity)
        {
            _peakIntensity = _currentIntensity;
            _timeSinceLastPeak = 0f;
        }
        _timeSinceLastPeak += Config.EvaluateIntervalSec;

        // 4. 放松机制
        if (_timeSinceLastPeak > Config.PeakMemorySeconds)
        {
            _relaxTimer -= Config.EvaluateIntervalSec;
            if (_relaxTimer <= 0)
            {
                _currentIntensity = Mathf.Max(Config.MinIntensity,
                    _currentIntensity - Config.RelaxStep);
                _relaxTimer = Config.BaseRelaxTime;
            }
        }

        OnIntensityChanged?.Invoke(_currentIntensity);
    }

    private float CalculateTowerPower()
    {
        float power = 0f;
        var towers = FindObjectsOfType<Tower>();
        foreach (var tower in towers)
        {
            power += tower.Damage * tower.FireRate * (1f + tower.Range * 0.1f);
        }
        return power;
    }

    #endregion

    #region === 动态调整 ===

    private void AdjustDifficulty()
    {
        var modifier = new DifficultyModifier();

        // === 敌人调整 ===
        modifier.EnemyHpMultiplier = Mathf.Lerp(0.7f, 2.0f, _currentIntensity);
        modifier.EnemySpeedMultiplier = Mathf.Lerp(0.9f, 1.5f, _currentIntensity);
        modifier.EnemySpawnRateMultiplier = Mathf.Lerp(0.8f, 1.8f, _currentIntensity);

        // === 经济调整 ===
        modifier.GoldMultiplier = Mathf.Lerp(1.5f, 0.7f, _currentIntensity);
        modifier.DropRateMultiplier = Mathf.Lerp(1.3f, 0.8f, _currentIntensity);

        // === Boss调整 ===
        modifier.BossExtraAbility = _currentIntensity > 0.7f;
        modifier.BossEnrageTimer = Mathf.Lerp(60f, 30f, _currentIntensity);

        // === 特殊调整 ===
        modifier.EnableSpecialEnemy = _currentIntensity > 0.5f;
        modifier.EnableEliteWave = _currentIntensity > 0.6f;

        // 新手上手保护 (前5局)
        if (_playerState.TotalGamesPlayed < Config.NewbieProtectionGames)
        {
            modifier.EnemyHpMultiplier *= Config.NewbieHpMultiplier;
            modifier.EnemySpeedMultiplier *= Config.NewbieSpeedMultiplier;
            modifier.GoldMultiplier *= Config.NewbieGoldMultiplier;
        }

        // 连败补偿
        if (_sessionState.ConsecutiveLosses >= Config.LossCompensationThreshold)
        {
            int bonus = Mathf.Min(_sessionState.ConsecutiveLosses - Config.LossCompensationThreshold + 1, 5);
            modifier.EnemyHpMultiplier *= (1f - bonus * 0.1f);
            modifier.GoldMultiplier *= (1f + bonus * 0.1f);
        }

        _sessionState.ActiveModifier = modifier;
        OnDifficultyAdjusted?.Invoke(modifier);
    }

    public DifficultyModifier GetCurrentModifier() => _sessionState?.ActiveModifier;

    #endregion

    #region === 导演事件系统 ===

    /// <summary>
    /// 触发导演事件 - 制造戏剧性时刻
    /// </summary>
    public DirectorEvent TriggerDirectorEvent()
    {
        // 评估是否触发特殊事件
        float eventChance = CalculateEventChance();
        if (UnityEngine.Random.value > eventChance) return null;

        var availableEvents = GetAvailableEvents();
        if (availableEvents.Count == 0) return null;

        var selected = availableEvents[UnityEngine.Random.Range(0, availableEvents.Count)];
        selected.Timestamp = Time.time;

        OnDirectorEvent?.Invoke(selected);

        switch (selected.Type)
        {
            case DirectorEventType.SupplyDrop:
                ExecuteSupplyDrop();
                break;
            case DirectorEventType.EliteSpawn:
                ExecuteEliteSpawn();
                break;
            case DirectorEventType.ExtraReward:
                ExecuteExtraReward();
                break;
            case DirectorEventType.SuddenAttack:
                ExecuteSuddenAttack();
                break;
            case DirectorEventType.PowerUp:
                ExecutePowerUp();
                break;
        }

        return selected;
    }

    private float CalculateEventChance()
    {
        // 压力适中时触发事件最有意义
        float optimalIntensity = 0.5f;
        float distanceFromOptimal = Mathf.Abs(_currentIntensity - optimalIntensity);
        return Config.BaseEventChance * (1f - distanceFromOptimal * 2f);
    }

    private List<DirectorEvent> GetAvailableEvents()
    {
        var events = new List<DirectorEvent>(Config.DirectorEvents);

        // 过滤冷却中的事件
        events.RemoveAll(e => Time.time - e.LastTriggerTime < e.MinInterval);

        // 根据强度过滤
        if (_currentIntensity > 0.7f)
        {
            // 高压力时只提供帮助类事件
            events.RemoveAll(e => e.Type == DirectorEventType.SuddenAttack ||
                                   e.Type == DirectorEventType.EliteSpawn);
        }
        else if (_currentIntensity < 0.3f)
        {
            // 低压力时提供挑战类事件
            events.RemoveAll(e => e.Type == DirectorEventType.SupplyDrop ||
                                   e.Type == DirectorEventType.PowerUp);
        }

        return events;
    }

    private void ExecuteSupplyDrop()
    {
        int gold = UnityEngine.Random.Range(200, 500);
        GameManager.Instance?.AddGold(gold);
        EffectManager.Instance?.PlaySupplyDropEffect();
    }

    private void ExecuteEliteSpawn()
    {
        var eliteTypes = new[] { "dasher", "regenerator", "shielder" };
        string type = eliteTypes[UnityEngine.Random.Range(0, eliteTypes.Length)];
        WaveManager.Instance?.SpawnMinions(type, 3, GetRandomSpawnPoint());
    }

    private void ExecuteExtraReward()
    {
        _sessionState.ActiveModifier.GoldMultiplier *= 2f;
        _sessionState.ActiveModifier.BuffDuration = 30f; // 持续30秒
    }

    private void ExecuteSuddenAttack()
    {
        int count = Mathf.RoundToInt(5 + _currentIntensity * 10);
        WaveManager.Instance?.SpawnMinions("fast", count, GetRandomSpawnPoint());
    }

    private void ExecutePowerUp()
    {
        // 随机Buff: 全塔攻速+50%持续20秒
        _sessionState.ActiveModifier.TemporaryAttackBoost = 1.5f;
        _sessionState.ActiveModifier.BuffDuration = 20f;
    }

    private Vector3 GetRandomSpawnPoint()
    {
        return PathManager.Instance?.GetRandomSpawnPoint() ?? Vector3.zero;
    }

    #endregion

    #region === 会话管理 ===

    public void StartNewGame(int targetWave)
    {
        _sessionState = new DirectorSessionState
        {
            TargetWave = targetWave,
            MaxBaseHp = BaseManager.Instance?.MaxHp ?? 100,
            StartTime = Time.time
        };
        _sessionGamesPlayed++;
    }

    public void OnGameEnd(bool isWin, int waveReached, int enemiesKilled, float durationMinutes)
    {
        if (_sessionState == null) return;

        _sessionState.IsWin = isWin;
        _sessionState.WaveReached = waveReached;
        _sessionState.EnemiesKilled = enemiesKilled;
        _sessionState.DurationMinutes = durationMinutes;

        if (isWin)
        {
            _playerState.TotalWins++;
            _sessionState.ConsecutiveLosses = 0;
        }
        else
        {
            _playerState.TotalLosses++;
            _sessionState.ConsecutiveLosses++;
        }

        _playerState.TotalGamesPlayed++;
        _playerState.TotalEnemiesKilled += enemiesKilled;
        _playerState.HighestWave = Mathf.Max(_playerState.HighestWave, waveReached);

        SavePlayerState();
        _sessionState = null;
    }

    public void UpdateSessionMetrics(int baseHp, int gold, int wave, int enemiesKilled, float sessionTimeMinutes)
    {
        if (_sessionState == null) return;

        _sessionState.CurrentBaseHp = baseHp;
        _sessionState.CurrentGold = gold;
        _sessionState.CurrentWave = wave;
        _sessionState.EnemiesKilledPerMinute = enemiesKilled / Mathf.Max(sessionTimeMinutes, 1f);
    }

    #endregion

    #region === 持久化 ===

    private void LoadPlayerState()
    {
        string json = PlayerPrefs.GetString("director_player_state", "");
        _playerState = string.IsNullOrEmpty(json)
            ? new DirectorPlayerState()
            : JsonUtility.FromJson<DirectorPlayerState>(json);
    }

    private void SavePlayerState()
    {
        PlayerPrefs.SetString("director_player_state", JsonUtility.ToJson(_playerState));
        PlayerPrefs.Save();
    }

    #endregion

    #region === 查询 ===

    public float GetIntensity() => _currentIntensity;
    public float GetPeakIntensity() => _peakIntensity;
    public int GetGamesPlayed() => _playerState.TotalGamesPlayed;
    public int GetHighestWave() => _playerState.HighestWave;
    public bool IsInNewbieProtection() => _playerState.TotalGamesPlayed < Config.NewbieProtectionGames;

    #endregion
}

#region === 数据结构 ===

[Serializable]
public class DirectorConfig
{
    [Header("评估配置")]
    public float EvaluateIntervalSec = 5f;
    public float IntensitySmoothing = 0.1f;
    public float MinIntensity = 0.1f;
    public float MaxIntensity = 0.95f;

    [Header("压力权重")]
    public float HpPressureWeight = 0.4f;
    public float KillRateWeight = 0.2f;
    public float WaveProgressWeight = 0.2f;
    public float GoldComfortWeight = 0.15f;
    public float TowerWeight = 0.15f;

    [Header("基准线")]
    public float ExpectedKillRate = 10f;
    public float ComfortableGold = 500f;
    public float ExpectedTowerPower = 200f;

    [Header("放松机制")]
    public float PeakMemorySeconds = 30f;
    public float BaseRelaxTime = 15f;
    public float RelaxStep = 0.05f;

    [Header("新手保护")]
    public int NewbieProtectionGames = 5;
    public float NewbieHpMultiplier = 0.7f;
    public float NewbieSpeedMultiplier = 0.8f;
    public float NewbieGoldMultiplier = 1.5f;

    [Header("连败补偿")]
    public int LossCompensationThreshold = 3;

    [Header("导演事件")]
    public float BaseEventChance = 0.3f;
    public List<DirectorEvent> DirectorEvents = new List<DirectorEvent>();
}

[Serializable]
public class DirectorPlayerState
{
    public int TotalGamesPlayed;
    public int TotalWins;
    public int TotalLosses;
    public int TotalEnemiesKilled;
    public int HighestWave;
}

[Serializable]
public class DirectorSessionState
{
    public int TargetWave;
    public int CurrentWave;
    public int MaxBaseHp;
    public int CurrentBaseHp;
    public int CurrentGold;
    public float EnemiesKilledPerMinute;
    public int EnemiesKilled;
    public int WaveReached;
    public float DurationMinutes;
    public bool IsWin;
    public int ConsecutiveLosses;
    public float StartTime;
    public DifficultyModifier ActiveModifier;
}

[Serializable]
public class DifficultyModifier
{
    public float EnemyHpMultiplier = 1f;
    public float EnemySpeedMultiplier = 1f;
    public float EnemySpawnRateMultiplier = 1f;
    public float GoldMultiplier = 1f;
    public float DropRateMultiplier = 1f;
    public bool BossExtraAbility;
    public float BossEnrageTimer = 60f;
    public bool EnableSpecialEnemy;
    public bool EnableEliteWave;
    public float TemporaryAttackBoost = 1f;
    public float BuffDuration; // 临时Buff持续时间
}

public enum DirectorEventType
{
    SupplyDrop,     // 补给空投 (金币)
    EliteSpawn,     // 精英敌人
    ExtraReward,    // 额外奖励
    SuddenAttack,   // 突袭
    PowerUp         // 强化Buff
}

[Serializable]
public class DirectorEvent
{
    public DirectorEventType Type;
    public string EventName;
    public string EventDescription;
    public float MinInterval = 120f; // 最小触发间隔
    public float LastTriggerTime;
    public float Timestamp;
}

#endregion
