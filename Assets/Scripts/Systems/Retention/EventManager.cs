using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 限时活动系统 - 运营活水引擎
/// 支持: 限时活动、节日活动、排行榜活动、收集活动、挑战活动
/// </summary>
public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    [Header("配置")]
    public EventConfig Config;

    private List<ActiveEvent> _activeEvents = new List<ActiveEvent>();
    private Dictionary<string, EventPlayerData> _eventData = new Dictionary<string, EventPlayerData>();

    // 事件
    public event Action<ActiveEvent> OnEventStarted;
    public event Action<ActiveEvent> OnEventEnded;
    public event Action<ActiveEvent, int> OnEventProgressUpdated;
    public event Action<ActiveEvent, int> OnEventMilestoneReached;
    public event Action<ActiveEvent, List<RewardData>> OnEventRewardClaimed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadEventData();
        CheckActiveEvents();
    }

    private void Update()
    {
        // 每分钟检查一次事件状态
        if (Time.frameCount % 3600 == 0)
            CheckActiveEvents();
    }

    #region === 事件生命周期 ===

    private void CheckActiveEvents()
    {
        DateTime now = DateTime.UtcNow;

        foreach (var template in Config.EventTemplates)
        {
            // 检查是否应该激活
            if (ShouldActivateEvent(template, now))
            {
                ActivateEvent(template);
            }

            // 检查是否应该结束
            var active = _activeEvents.Find(e => e.EventId == template.EventId);
            if (active != null)
            {
                if (active.EndTime <= now)
                {
                    EndEvent(template.EventId);
                }
                else
                {
                    // 更新剩余时间
                    active.RemainingSeconds = (float)(active.EndTime - now).TotalSeconds;
                }
            }
        }
    }

    private bool ShouldActivateEvent(EventTemplate template, DateTime now)
    {
        // 手动激活事件 - 运营配置时间
        if (template.ActivationType == EventActivationType.Scheduled)
        {
            return now >= template.StartTime && now < template.EndTime;
        }

        // 循环事件 (如每周挑战)
        if (template.ActivationType == EventActivationType.Recurring)
        {
            return IsInRecurringWindow(template, now);
        }

        // 触发式事件 (如累计登录N天触发)
        if (template.ActivationType == EventActivationType.Triggered)
        {
            return CheckTriggerCondition(template);
        }

        // 永久事件 (新手引导等)
        if (template.ActivationType == EventActivationType.Permanent)
        {
            return !IsEventCompleted(template.EventId);
        }

        return false;
    }

    private bool IsInRecurringWindow(EventTemplate template, DateTime now)
    {
        switch (template.RecurringType)
        {
            case RecurringType.Daily:
                return now.TimeOfDay >= template.RecurringStartTime &&
                       now.TimeOfDay < template.RecurringEndTime;
            case RecurringType.Weekly:
                return now.DayOfWeek == template.RecurringDayOfWeek;
            case RecurringType.Weekend:
                return now.DayOfWeek == DayOfWeek.Saturday ||
                       now.DayOfWeek == DayOfWeek.Sunday;
            case RecurringType.Monthly:
                return now.Day == template.RecurringDayOfMonth;
        }
        return false;
    }

    private bool CheckTriggerCondition(EventTemplate template)
    {
        switch (template.TriggerType)
        {
            case EventTriggerType.ConsecutiveLogin:
                return DailyMissionManager.Instance != null &&
                       DailyMissionManager.Instance.GetConsecutiveLoginDays() >= template.TriggerValue;
            case EventTriggerType.TotalWins:
                return GetTotalWins() >= template.TriggerValue;
            case EventTriggerType.LevelReached:
                var waveMgr = WaveManager.Instance;
                return waveMgr != null && waveMgr.CurrentWave >= template.TriggerValue;
        }
        return false;
    }

    private int GetTotalWins()
    {
        // 从存档读取
        return PlayerPrefs.GetInt("total_wins", 0);
    }

    #endregion

    #region === 事件激活/结束 ===

    public ActiveEvent ActivateEvent(EventTemplate template)
    {
        // 防止重复激活
        if (_activeEvents.Exists(e => e.EventId == template.EventId))
            return _activeEvents.Find(e => e.EventId == template.EventId);

        var activeEvent = new ActiveEvent
        {
            EventId = template.EventId,
            EventName = template.EventName,
            Description = template.Description,
            EventType = template.EventType,
            StartTime = DateTime.UtcNow,
            EndTime = template.ActivationType == EventActivationType.Scheduled
                ? template.EndTime
                : DateTime.UtcNow.AddSeconds(template.DefaultDurationSeconds),
            RemainingSeconds = template.DefaultDurationSeconds,
            Milestones = template.Milestones,
            EventMultiplier = template.DefaultMultiplier,
            BannerName = template.BannerName,
            IsNew = true
        };

        _activeEvents.Add(activeEvent);

        if (!_eventData.ContainsKey(template.EventId))
        {
            _eventData[template.EventId] = new EventPlayerData
            {
                EventId = template.EventId,
                CurrentProgress = 0,
                ClaimedMilestones = new List<int>(),
                EventScore = 0
            };
        }

        OnEventStarted?.Invoke(activeEvent);
        Debug.Log($"[EventManager] 活动激活: {template.EventName} ({template.EventId})");
        SaveEventData();
        return activeEvent;
    }

    public void EndEvent(string eventId)
    {
        var activeEvent = _activeEvents.Find(e => e.EventId == eventId);
        if (activeEvent == null) return;

        // 发放未领取的里程碑奖励
        var data = GetEventData(eventId);
        if (data != null)
        {
            var template = Config.GetTemplate(eventId);
            if (template != null)
            {
                foreach (var milestone in template.Milestones)
                {
                    if (data.CurrentProgress >= milestone.RequiredProgress &&
                        !data.ClaimedMilestones.Contains(milestone.MilestoneId))
                    {
                        // 自动发放邮件到邮箱
                        SendToMailbox(milestone.Rewards);
                    }
                }
            }

            // 排行榜结算
            if (activeEvent.EventType == EventType.Leaderboard)
            {
                SettleLeaderboard(eventId, data.EventScore);
            }
        }

        OnEventEnded?.Invoke(activeEvent);
        _activeEvents.Remove(activeEvent);
        Debug.Log($"[EventManager] 活动结束: {activeEvent.EventName} ({eventId})");
    }

    #endregion

    #region === 活动进度追踪 ===

    public void ReportEventProgress(string eventId, int amount = 1)
    {
        var activeEvent = _activeEvents.Find(e => e.EventId == eventId);
        if (activeEvent == null) return;

        var data = GetEventData(eventId);
        if (data == null) return;

        int multiplier = activeEvent.EventType == EventType.DoubleReward ? 2 : 1;
        data.CurrentProgress += amount * multiplier;
        data.EventScore += amount * activeEvent.EventMultiplier;

        OnEventProgressUpdated?.Invoke(activeEvent, data.CurrentProgress);

        // 检查里程碑
        foreach (var milestone in activeEvent.Milestones)
        {
            if (data.CurrentProgress >= milestone.RequiredProgress &&
                !data.ClaimedMilestones.Contains(milestone.MilestoneId))
            {
                data.ClaimedMilestones.Add(milestone.MilestoneId);
                OnEventMilestoneReached?.Invoke(activeEvent, milestone.MilestoneId);
            }
        }

        SaveEventData();
    }

    public List<RewardData> ClaimMilestoneRewards(string eventId, int milestoneId)
    {
        var data = GetEventData(eventId);
        if (data == null || !data.ClaimedMilestones.Contains(milestoneId))
            return null;

        var activeEvent = _activeEvents.Find(e => e.EventId == eventId);
        if (activeEvent == null) return null;

        var milestone = activeEvent.Milestones.Find(m => m.MilestoneId == milestoneId);
        if (milestone == null) return null;

        // 标记已领取
        data.ClaimedMilestones.Remove(milestoneId);
        if (!data.CollectedMilestones.Contains(milestoneId))
            data.CollectedMilestones.Add(milestoneId);

        OnEventRewardClaimed?.Invoke(activeEvent, milestone.Rewards);
        SaveEventData();
        return milestone.Rewards;
    }

    #endregion

    #region === 排行榜活动 ===

    /// <summary>
    /// 提交排行榜分数 (异步竞技场用)
    /// </summary>
    public void SubmitLeaderboardScore(string eventId, int score)
    {
        var data = GetEventData(eventId);
        if (data == null) return;

        if (score > data.EventScore)
            data.EventScore = score;

        SaveEventData();
    }

    private void SettleLeaderboard(string eventId, int finalScore)
    {
        // 根据最终分数确定排名奖励
        // 实际实现需要服务端支持
        var template = Config.GetTemplate(eventId);
        if (template == null) return;

        int rank = CalculateRank(finalScore);
        var tierReward = GetLeaderboardReward(template, rank);
        if (tierReward != null)
            SendToMailbox(tierReward);
    }

    private int CalculateRank(int score)
    {
        // 客户端模拟 - 实际需要服务端排行榜
        if (score >= 100000) return 1;
        if (score >= 50000) return 10;
        if (score >= 20000) return 50;
        if (score >= 10000) return 100;
        return 500;
    }

    private List<RewardData> GetLeaderboardReward(EventTemplate template, int rank)
    {
        foreach (var tier in template.LeaderboardRewardTiers)
        {
            if (rank <= tier.TopRank)
                return tier.Rewards;
        }
        return template.ParticipationReward;
    }

    #endregion

    #region === 邮箱系统(简化) ===

    private void SendToMailbox(List<RewardData> rewards)
    {
        string json = JsonUtility.ToJson(new RewardListWrapper(rewards));
        PlayerPrefs.SetString($"mailbox_{DateTime.UtcNow.Ticks}", json);
        PlayerPrefs.Save();
    }

    [Serializable]
    private class RewardListWrapper
    {
        public List<RewardData> Rewards;
        public RewardListWrapper(List<RewardData> rewards) { Rewards = rewards; }
    }

    #endregion

    #region === 查询接口 ===

    public List<ActiveEvent> GetActiveEvents() => _activeEvents;
    public ActiveEvent GetActiveEvent(string eventId) => _activeEvents.Find(e => e.EventId == eventId);
    public EventPlayerData GetEventData(string eventId)
    {
        _eventData.TryGetValue(eventId, out var data);
        return data;
    }
    public bool IsEventCompleted(string eventId)
    {
        var data = GetEventData(eventId);
        return data != null && data.IsCompleted;
    }

    #endregion

    #region === 数据持久化 ===

    private void LoadEventData()
    {
        string json = PlayerPrefs.GetString("event_player_data", "");
        if (!string.IsNullOrEmpty(json))
        {
            var wrapper = JsonUtility.FromJson<EventDataWrapper>(json);
            _eventData = wrapper.ToDictionary();
        }
    }

    private void SaveEventData()
    {
        var wrapper = EventDataWrapper.FromDictionary(_eventData);
        PlayerPrefs.SetString("event_player_data", JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    #endregion
}

#region === 数据结构 ===

[Serializable]
public class ActiveEvent
{
    public string EventId;
    public string EventName;
    public string Description;
    public EventType EventType;
    public DateTime StartTime;
    public DateTime EndTime;
    public float RemainingSeconds;
    public List<EventMilestone> Milestones;
    public int EventMultiplier;
    public string BannerName; // 活动Banner图片名
    public bool IsNew;
}

[Serializable]
public class EventPlayerData
{
    public string EventId;
    public int CurrentProgress;
    public int EventScore;
    public List<int> ClaimedMilestones = new List<int>();
    public List<int> CollectedMilestones = new List<int>();
    public bool IsCompleted;
}

public enum EventType
{
    ScoreCollection,    // 积分收集 (击杀/波次等)
    ItemCollection,     // 道具收集 (特殊掉落)
    StageChallenge,     // 关卡挑战 (特定关卡)
    Leaderboard,        // 排行榜竞赛
    DoubleReward,       // 双倍奖励
    LimitedGacha,       // 限定卡池
    BossRush,           // BOSS连续挑战
    EndlessMode         // 无尽模式挑战
}

public enum EventActivationType
{
    Scheduled,   // 定时 (运营配置)
    Recurring,   // 循环 (每日/每周)
    Triggered,   // 触发式 (登录N天等)
    Permanent    // 永久 (新手任务等)
}

public enum RecurringType
{
    Daily, Weekly, Weekend, Monthly
}

public enum EventTriggerType
{
    ConsecutiveLogin,
    TotalWins,
    LevelReached
}

[Serializable]
public class EventTemplate
{
    public string EventId;
    public string EventName;
    public string Description;
    public EventType EventType;
    public EventActivationType ActivationType;

    // 定时激活
    public DateTime StartTime;
    public DateTime EndTime;

    // 循环激活
    public RecurringType RecurringType;
    public TimeSpan RecurringStartTime;
    public TimeSpan RecurringEndTime;
    public DayOfWeek RecurringDayOfWeek;
    public int RecurringDayOfMonth;

    // 触发式
    public EventTriggerType TriggerType;
    public int TriggerValue;

    // 通用
    public float DefaultDurationSeconds = 86400 * 7; // 默认7天
    public int DefaultMultiplier = 1;
    public string BannerName;
    public List<EventMilestone> Milestones = new List<EventMilestone>();
    public List<LeaderboardRewardTier> LeaderboardRewardTiers = new List<LeaderboardRewardTier>();
    public List<RewardData> ParticipationReward = new List<RewardData>();
}

[Serializable]
public class EventMilestone
{
    public int MilestoneId;
    public int RequiredProgress;
    public string MilestoneName;
    public List<RewardData> Rewards = new List<RewardData>();
}

[Serializable]
public class LeaderboardRewardTier
{
    public int TopRank; // 如1, 10, 50, 100
    public string TierName;
    public List<RewardData> Rewards = new List<RewardData>();
}

[Serializable]
public class EventConfig : ScriptableObject
{
    public List<EventTemplate> EventTemplates = new List<EventTemplate>();

    public EventTemplate GetTemplate(string eventId)
        => EventTemplates.Find(t => t.EventId == eventId);
}

[Serializable]
public class EventDataWrapper
{
    public List<string> Keys = new List<string>();
    public List<string> Values = new List<string>();

    public static EventDataWrapper FromDictionary(Dictionary<string, EventPlayerData> dict)
    {
        var w = new EventDataWrapper();
        foreach (var kvp in dict)
        {
            w.Keys.Add(kvp.Key);
            w.Values.Add(JsonUtility.ToJson(kvp.Value));
        }
        return w;
    }

    public Dictionary<string, EventPlayerData> ToDictionary()
    {
        var d = new Dictionary<string, EventPlayerData>();
        for (int i = 0; i < Mathf.Min(Keys.Count, Values.Count); i++)
            d[Keys[i]] = JsonUtility.FromJson<EventPlayerData>(Values[i]);
        return d;
    }
}

#endregion
