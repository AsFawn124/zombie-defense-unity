using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 数据分析管理器 - 全量埋点+漏斗分析
/// 支持: 玩家行为追踪、关卡漏斗、商业化漏斗、留存分析、实时数据上报
/// </summary>
public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance;

    [Header("配置")]
    public string AnalyticsEndpoint = "https://analytics.game.com/events";
    public int BatchSize = 20;          // 批量上报数量
    public float BatchInterval = 30f;    // 批量上报间隔(秒)
    public bool EnableDebugLog = true;

    private Queue<AnalyticsEvent> _eventQueue = new Queue<AnalyticsEvent>();
    private float _lastBatchTime;
    private int _sessionEventCount;
    private string _sessionId;
    private DateTime _sessionStartTime;

    // 会话级别的漏斗跟踪
    private Dictionary<string, FunnelTracker> _activeFunnels = new Dictionary<string, FunnelTracker>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _sessionStartTime = DateTime.UtcNow;
        TrackEvent("session_start", new Dictionary<string, object>
        {
            ["session_id"] = _sessionId,
            ["version"] = Application.version,
            ["platform"] = "wechat_minigame"
        });
    }

    private void Update()
    {
        if (Time.time - _lastBatchTime > BatchInterval && _eventQueue.Count > 0)
        {
            FlushEvents();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            TrackEvent("session_end", new Dictionary<string, object>
            {
                ["session_id"] = _sessionId,
                ["duration_seconds"] = (DateTime.UtcNow - _sessionStartTime).TotalSeconds,
                ["events_count"] = _sessionEventCount
            });
            FlushEvents(immediate: true);
        }
    }

    private void OnDestroy()
    {
        FlushEvents(immediate: true);
    }

    #region === 核心追踪 ===

    public void TrackEvent(string eventName, Dictionary<string, object> properties = null)
    {
        var evt = new AnalyticsEvent
        {
            EventName = eventName,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            SessionId = _sessionId,
            Properties = properties ?? new Dictionary<string, object>(),
            UserId = GetUserId(),
            Level = GetCurrentLevel()
        };

        // 添加公共属性
        evt.Properties["screen"] = GetCurrentScreen();
        evt.Properties["session_event_index"] = _sessionEventCount++;

        _eventQueue.Enqueue(evt);

        if (EnableDebugLog)
            Debug.Log($"[Analytics] {eventName} | {string.Join(", ", evt.Properties)}");

        if (_eventQueue.Count >= BatchSize)
            FlushEvents();
    }

    private void FlushEvents(bool immediate = false)
    {
        if (_eventQueue.Count == 0) return;

        _lastBatchTime = Time.time;

        var batch = new List<AnalyticsEvent>();
        while (_eventQueue.Count > 0 && batch.Count < BatchSize)
            batch.Add(_eventQueue.Dequeue());

        if (batch.Count == 0) return;

        string json = JsonUtility.ToJson(new AnalyticsBatch { Events = batch });

        if (EnableDebugLog)
            Debug.Log($"[Analytics] Flushing {batch.Count} events");

        // 实际上报 - 接入真实服务端后替换
        // StartCoroutine(PostEvents(json));
    }

    #endregion

    #region === 漏斗追踪 ===

    /// <summary>
    /// 开始追踪一个转化漏斗
    /// </summary>
    public void StartFunnel(string funnelName, string[] steps)
    {
        _activeFunnels[funnelName] = new FunnelTracker
        {
            FunnelName = funnelName,
            Steps = new List<string>(steps),
            StepTimestamps = new Dictionary<int, long>(),
            StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        RecordFunnelStep(funnelName, 0);
    }

    /// <summary>
    /// 记录漏斗步骤达成
    /// </summary>
    public void RecordFunnelStep(string funnelName, int stepIndex)
    {
        if (!_activeFunnels.TryGetValue(funnelName, out var funnel)) return;
        if (stepIndex >= funnel.Steps.Count) return;

        funnel.CurrentStep = stepIndex;
        funnel.StepTimestamps[stepIndex] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        TrackEvent("funnel_step", new Dictionary<string, object>
        {
            ["funnel_name"] = funnelName,
            ["step_index"] = stepIndex,
            ["step_name"] = funnel.Steps[stepIndex],
        });
    }

    /// <summary>
    /// 完成漏斗
    /// </summary>
    public void CompleteFunnel(string funnelName)
    {
        if (!_activeFunnels.TryGetValue(funnelName, out var funnel)) return;

        funnel.CompletedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        funnel.IsCompleted = true;

        TrackEvent("funnel_complete", new Dictionary<string, object>
        {
            ["funnel_name"] = funnelName,
            ["total_steps"] = funnel.Steps.Count,
            ["total_time_ms"] = funnel.CompletedAt - funnel.StartTime,
        });

        _activeFunnels.Remove(funnelName);
    }

    /// <summary>
    /// 放弃漏斗
    /// </summary>
    public void AbandonFunnel(string funnelName, string reason = "")
    {
        if (!_activeFunnels.TryGetValue(funnelName, out var funnel)) return;

        TrackEvent("funnel_abandon", new Dictionary<string, object>
        {
            ["funnel_name"] = funnelName,
            ["last_step"] = funnel.CurrentStep,
            ["last_step_name"] = funnel.Steps[funnel.CurrentStep],
            ["reason"] = reason,
            ["time_spent_ms"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - funnel.StartTime,
        });

        _activeFunnels.Remove(funnelName);
    }

    #endregion

    #region === 专用埋点方法 ===

    // --- 战斗相关 ---
    public void TrackGameStart(int waveLevel, string heroId, string[] towerIds)
    {
        StartFunnel($"game_{_sessionEventCount}", new[] { "start", "wave5", "wave10", "wave20", "win", "lose" });

        TrackEvent("game_start", new Dictionary<string, object>
        {
            ["wave_level"] = waveLevel,
            ["hero_id"] = heroId,
            ["tower_count"] = towerIds.Length,
            ["tower_ids"] = string.Join(",", towerIds),
        });
    }

    public void TrackWaveCompleted(int wave, float clearTime, int enemiesKilled, long goldEarned)
    {
        if (wave == 5) RecordFunnelStep($"game_{_sessionEventCount - 1}", 1);
        else if (wave == 10) RecordFunnelStep($"game_{_sessionEventCount - 1}", 2);
        else if (wave == 20) RecordFunnelStep($"game_{_sessionEventCount - 1}", 3);

        TrackEvent("wave_completed", new Dictionary<string, object>
        {
            ["wave"] = wave,
            ["clear_time_sec"] = clearTime,
            ["enemies_killed"] = enemiesKilled,
            ["gold_earned"] = goldEarned,
        });
    }

    public void TrackGameEnd(bool isWin, int waveReached, float totalTime, Dictionary<string, int> damageByTower)
    {
        RecordFunnelStep($"game_{_sessionEventCount - 1}", isWin ? 4 : 5);
        CompleteFunnel($"game_{_sessionEventCount - 1}");

        TrackEvent("game_end", new Dictionary<string, object>
        {
            ["result"] = isWin ? "win" : "lose",
            ["wave_reached"] = waveReached,
            ["total_time_sec"] = totalTime,
            ["damage_by_tower"] = JsonUtility.ToJson(new SerializableDictStrInt(damageByTower)),
        });
    }

    // --- 商业化相关 ---
    public void TrackPurchase(string itemId, string itemType, float price, string currency)
    {
        TrackEvent("purchase", new Dictionary<string, object>
        {
            ["item_id"] = itemId,
            ["item_type"] = itemType,
            ["price"] = price,
            ["currency"] = currency,
        });
    }

    public void TrackAdWatch(string adPlacement, bool completed)
    {
        TrackEvent("ad_watch", new Dictionary<string, object>
        {
            ["placement"] = adPlacement,
            ["completed"] = completed,
        });
    }

    public void TrackBattlePassPurchase(int tier, float price)
    {
        TrackEvent("battle_pass_purchase", new Dictionary<string, object>
        {
            ["tier"] = tier, // 免费/付费/高级
            ["price"] = price,
        });
    }

    public void TrackGacha(int gachaType, int pullCount, string rarityObtained)
    {
        TrackEvent("gacha_pull", new Dictionary<string, object>
        {
            ["gacha_type"] = gachaType, // 新手/常驻/限定
            ["pull_count"] = pullCount,
            ["highest_rarity"] = rarityObtained,
        });
    }

    // --- 养成相关 ---
    public void TrackEquipmentMerge(int fromRarity, int toRarity, string equipmentType)
    {
        TrackEvent("equipment_merge", new Dictionary<string, object>
        {
            ["from_rarity"] = fromRarity,
            ["to_rarity"] = toRarity,
            ["equipment_type"] = equipmentType,
        });
    }

    public void TrackTalentActivated(int talentId, int talentLevel, int totalPoints)
    {
        TrackEvent("talent_activated", new Dictionary<string, object>
        {
            ["talent_id"] = talentId,
            ["talent_level"] = talentLevel,
            ["total_points"] = totalPoints,
        });
    }

    public void TrackHeroUpgrade(string heroId, int newLevel, int cost)
    {
        TrackEvent("hero_upgrade", new Dictionary<string, object>
        {
            ["hero_id"] = heroId,
            ["new_level"] = newLevel,
            ["cost"] = cost,
        });
    }

    // --- 社交相关 ---
    public void TrackArenaBattle(string opponentId, int playerRank, bool isWin, int eloChange)
    {
        TrackEvent("arena_battle", new Dictionary<string, object>
        {
            ["opponent_id"] = opponentId,
            ["player_rank"] = playerRank,
            ["result"] = isWin ? "win" : "lose",
            ["elo_change"] = eloChange,
        });
    }

    public void TrackGuildAction(string action, string guildId)
    {
        TrackEvent("guild_action", new Dictionary<string, object>
        {
            ["action"] = action,
            ["guild_id"] = guildId,
        });
    }

    // --- 留存相关 ---
    public void TrackDailyLogin(int consecutiveDays, int totalDays)
    {
        TrackEvent("daily_login", new Dictionary<string, object>
        {
            ["consecutive_days"] = consecutiveDays,
            ["total_days"] = totalDays,
        });
    }

    public void TrackMissionCompleted(string missionId, MissionType missionType)
    {
        TrackEvent("mission_completed", new Dictionary<string, object>
        {
            ["mission_id"] = missionId,
            ["mission_type"] = missionType.ToString(),
        });
    }

    public void TrackAchievementUnlocked(string achievementId, int tier)
    {
        TrackEvent("achievement_unlocked", new Dictionary<string, object>
        {
            ["achievement_id"] = achievementId,
            ["tier"] = tier,
        });
    }

    // --- 关卡/内容相关 ---
    public void TrackLevelStart(int levelId, string difficulty)
    {
        StartFunnel($"level_{levelId}", new[] { "start", "mid", "boss", "clear" });

        TrackEvent("level_start", new Dictionary<string, object>
        {
            ["level_id"] = levelId,
            ["difficulty"] = difficulty,
        });
    }

    public void TrackBossEncounter(string bossId, int wave)
    {
        RecordFunnelStep($"level_{GetCurrentLevel()}", 2);

        TrackEvent("boss_encounter", new Dictionary<string, object>
        {
            ["boss_id"] = bossId,
            ["wave"] = wave,
        });
    }

    #endregion

    #region === 工具方法 ===

    private string GetUserId()
    {
        return PlayerPrefs.GetString("user_id", "unknown");
    }

    private int GetCurrentLevel()
    {
        return PlayerPrefs.GetInt("current_level", 1);
    }

    private string GetCurrentScreen()
    {
        return "game"; // 由UI管理器更新
    }

    public int GetSessionEventCount() => _sessionEventCount;

    /// <summary>
    /// 获取用户属性 (用于用户画像)
    /// </summary>
    public Dictionary<string, object> GetUserProfile()
    {
        return new Dictionary<string, object>
        {
            ["user_id"] = GetUserId(),
            ["total_play_time"] = PlayerPrefs.GetFloat("total_play_time", 0),
            ["total_games"] = PlayerPrefs.GetInt("total_games", 0),
            ["total_wins"] = PlayerPrefs.GetInt("total_wins", 0),
            ["highest_wave"] = PlayerPrefs.GetInt("highest_wave", 0),
            ["total_spend"] = PlayerPrefs.GetFloat("total_spend", 0),
            ["days_since_install"] = PlayerPrefs.GetInt("days_since_install", 0),
        };
    }

    #endregion
}

#region === 数据结构 ===

[Serializable]
public class AnalyticsEvent
{
    public string EventName;
    public long Timestamp;
    public string SessionId;
    public string UserId;
    public int Level;
    public Dictionary<string, object> Properties;
}

[Serializable]
public class AnalyticsBatch
{
    public List<AnalyticsEvent> Events;
}

[Serializable]
public class FunnelTracker
{
    public string FunnelName;
    public List<string> Steps;
    public int CurrentStep;
    public Dictionary<int, long> StepTimestamps;
    public long StartTime;
    public long CompletedAt;
    public bool IsCompleted;
}

[Serializable]
public class SerializableDictStrInt
{
    public List<string> Keys = new List<string>();
    public List<int> Values = new List<int>();

    public SerializableDictStrInt(Dictionary<string, int> dict)
    {
        if (dict == null) return;
        foreach (var kvp in dict)
        {
            Keys.Add(kvp.Key);
            Values.Add(kvp.Value);
        }
    }
}

#endregion
