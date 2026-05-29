using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 每日任务与签到系统 - 留存活水核心
/// 包含: 每日任务、每周任务、每日签到、累计签到、活跃度宝箱
/// </summary>
public class DailyMissionManager : MonoBehaviour
{
    public static DailyMissionManager Instance;

    [Header("配置")]
    public DailyMissionConfig Config;

    // 运行时数据
    private DailyPlayerData _playerData;
    private Dictionary<string, MissionProgress> _activeMissions = new Dictionary<string, MissionProgress>();
    private Dictionary<int, bool> _signInRewardsClaimed = new Dictionary<int, bool>();

    // 事件
    public event Action<MissionData, int> OnMissionProgressUpdated;
    public event Action<MissionData> OnMissionCompleted;
    public event Action<MissionData> OnMissionRewardClaimed;
    public event Action<int, SignInReward> OnSignInRewardClaimed;
    public event Action<int> OnActivityChestOpened; // 活跃度宝箱

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadPlayerData();
        CheckDayRollover();
        GenerateDailyMissions();
        GenerateWeeklyMissions();
    }

    #region === 日期管理 ===

    private void CheckDayRollover()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (_playerData.LastLoginDate != today)
        {
            RolloverNewDay(today);
        }

        // 检查周刷新 (周一)
        if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Monday &&
            _playerData.LastWeekReset != GetWeekKey())
        {
            RolloverNewWeek();
        }
    }

    private string GetWeekKey() => $"{DateTime.UtcNow.Year}-W{GetIso8601WeekOfYear(DateTime.UtcNow)}";

    private int GetIso8601WeekOfYear(DateTime time)
    {
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            time = time.AddDays(3);
        return System.Globalization.CultureInfo.InvariantCulture.Calendar
            .GetWeekOfYear(time, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private void RolloverNewDay(string today)
    {
        _playerData.LastLoginDate = today;
        _playerData.ConsecutiveLoginDays++;
        _playerData.TotalLoginDays++;
        _activeMissions.Clear();
        _signInRewardsClaimed.Clear();
        SavePlayerData();
    }

    private void RolloverNewWeek()
    {
        _playerData.LastWeekReset = GetWeekKey();
        _playerData.WeeklyActivity = 0;
        SavePlayerData();
    }

    #endregion

    #region === 每日任务生成 ===

    public void GenerateDailyMissions()
    {
        _activeMissions.Clear();

        // 固定每日任务
        var dailyTemplates = Config.DailyMissionTemplates;
        foreach (var template in dailyTemplates)
        {
            var mission = new MissionProgress
            {
                MissionId = template.MissionId,
                CurrentProgress = 0,
                TargetProgress = template.TargetValue,
                IsCompleted = false,
                IsRewardClaimed = false,
                GeneratedTime = DateTime.UtcNow
            };
            _activeMissions[template.MissionId] = mission;
        }

        // 随机任务 (从池中抽取2个)
        var randomPool = Config.RandomMissionPool;
        var selected = new List<MissionData>();
        while (selected.Count < 2 && selected.Count < randomPool.Count)
        {
            var candidate = randomPool[UnityEngine.Random.Range(0, randomPool.Count)];
            if (!selected.Contains(candidate))
                selected.Add(candidate);
        }

        foreach (var template in selected)
        {
            var mission = new MissionProgress
            {
                MissionId = template.MissionId,
                CurrentProgress = 0,
                TargetProgress = template.TargetValue,
                IsCompleted = false,
                IsRewardClaimed = false,
                GeneratedTime = DateTime.UtcNow
            };
            _activeMissions[template.MissionId] = mission;
        }
    }

    public void GenerateWeeklyMissions()
    {
        foreach (var template in Config.WeeklyMissionTemplates)
        {
            if (!_activeMissions.ContainsKey(template.MissionId))
            {
                _activeMissions[template.MissionId] = new MissionProgress
                {
                    MissionId = template.MissionId,
                    CurrentProgress = 0,
                    TargetProgress = template.TargetValue,
                    IsCompleted = false,
                    IsRewardClaimed = false,
                    GeneratedTime = DateTime.UtcNow
                };
            }
        }
    }

    #endregion

    #region === 任务进度追踪 ===

    /// <summary>
    /// 汇报任务进度 - 由各系统调用
    /// </summary>
    public void ReportProgress(MissionType type, string subType = "", int amount = 1)
    {
        foreach (var kvp in _activeMissions)
        {
            var mission = kvp.Value;
            if (mission.IsCompleted || mission.IsRewardClaimed) continue;

            var template = GetMissionTemplate(mission.MissionId);
            if (template == null) continue;
            if (template.Type != type) continue;
            if (!string.IsNullOrEmpty(template.SubType) && template.SubType != subType) continue;

            mission.CurrentProgress = Mathf.Min(mission.CurrentProgress + amount, mission.TargetProgress);

            OnMissionProgressUpdated?.Invoke(template, mission.CurrentProgress);

            if (mission.CurrentProgress >= mission.TargetProgress)
            {
                mission.IsCompleted = true;
                _playerData.DailyActivity += template.ActivityPoints;
                _playerData.WeeklyActivity += template.ActivityPoints;
                OnMissionCompleted?.Invoke(template);
                CheckActivityChests();
            }
        }

        SavePlayerData();
    }

    private MissionData GetMissionTemplate(string missionId)
    {
        foreach (var t in Config.DailyMissionTemplates)
            if (t.MissionId == missionId) return t;
        foreach (var t in Config.WeeklyMissionTemplates)
            if (t.MissionId == missionId) return t;
        foreach (var t in Config.RandomMissionPool)
            if (t.MissionId == missionId) return t;
        return null;
    }

    #endregion

    #region === 活跃度宝箱 ===

    private void CheckActivityChests()
    {
        foreach (var chest in Config.ActivityChests)
        {
            if (_playerData.DailyActivity >= chest.RequiredActivity &&
                !_playerData.ClaimedDailyChests.Contains(chest.ChestId))
            {
                // 自动不领取，等待玩家手动开启
            }
        }
    }

    public bool CanOpenChest(ActivityChestData chest)
    {
        return _playerData.DailyActivity >= chest.RequiredActivity &&
               !_playerData.ClaimedDailyChests.Contains(chest.ChestId);
    }

    public RewardData OpenActivityChest(int chestId)
    {
        var chest = Config.ActivityChests.Find(c => c.ChestId == chestId);
        if (chest == null || !CanOpenChest(chest)) return null;

        _playerData.ClaimedDailyChests.Add(chestId);
        OnActivityChestOpened?.Invoke(chestId);

        // 随机抽取奖励
        var reward = chest.RewardPool[UnityEngine.Random.Range(0, chest.RewardPool.Count)];
        SavePlayerData();
        return reward;
    }

    #endregion

    #region === 签到系统 ===

    public bool CanSignInToday()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return _playerData.LastSignInDate != today;
    }

    public SignInReward SignIn()
    {
        if (!CanSignInToday()) return null;

        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        _playerData.LastSignInDate = today;

        int dayIndex = Mathf.Min(_playerData.ConsecutiveLoginDays - 1, Config.SignInRewards.Count - 1);
        var reward = Config.SignInRewards[dayIndex];

        _signInRewardsClaimed[dayIndex] = true;
        OnSignInRewardClaimed?.Invoke(dayIndex, reward);
        SavePlayerData();

        return reward;
    }

    /// <summary>
    /// 补签 (消耗钻石)
    /// </summary>
    public bool RetroactiveSignIn(int dayIndex, int diamondCost)
    {
        if (_signInRewardsClaimed.ContainsKey(dayIndex) && _signInRewardsClaimed[dayIndex])
            return false; // 已签过

        // 钻石扣除由CommerceManager处理
        _signInRewardsClaimed[dayIndex] = true;
        var reward = Config.SignInRewards[dayIndex];
        OnSignInRewardClaimed?.Invoke(dayIndex, reward);
        SavePlayerData();
        return true;
    }

    #endregion

    #region === 日常任务汇报接口 ===

    /// <summary>击败敌人</summary>
    public void OnEnemyKilled(string enemyType, int count = 1)
        => ReportProgress(MissionType.KillEnemy, enemyType, count);

    /// <summary>通过波次</summary>
    public void OnWaveCompleted(int wave)
        => ReportProgress(MissionType.CompleteWave);

    /// <summary>合成防御塔</summary>
    public void OnTowerMerged(string towerType)
        => ReportProgress(MissionType.MergeTower, towerType);

    /// <summary>使用技能</summary>
    public void OnSkillUsed(string skillId)
        => ReportProgress(MissionType.UseSkill, skillId);

    /// <summary>游玩局数</summary>
    public void OnGamePlayed(bool isWin)
    {
        ReportProgress(MissionType.PlayGame);
        if (isWin) ReportProgress(MissionType.WinGame);
    }

    /// <summary>观看广告</summary>
    public void OnAdWatched()
        => ReportProgress(MissionType.WatchAd);

    /// <summary>竞技场战斗</summary>
    public void OnArenaBattle()
        => ReportProgress(MissionType.ArenaBattle);

    /// <summary>装备强化</summary>
    public void OnEquipmentUpgraded()
        => ReportProgress(MissionType.UpgradeEquipment);

    /// <summary>消耗金币</summary>
    public void OnGoldSpent(int amount)
        => ReportProgress(MissionType.SpendGold, "", amount);

    /// <summary>英雄升级</summary>
    public void OnHeroLeveledUp()
        => ReportProgress(MissionType.LevelUpHero);

    #endregion

    #region === 数据持久化 ===

    private void LoadPlayerData()
    {
        string json = PlayerPrefs.GetString("daily_mission_data", "");
        _playerData = string.IsNullOrEmpty(json)
            ? new DailyPlayerData()
            : JsonUtility.FromJson<DailyPlayerData>(json);

        // 加载签到记录
        string signJson = PlayerPrefs.GetString("sign_in_data", "");
        _signInRewardsClaimed = string.IsNullOrEmpty(signJson)
            ? new Dictionary<int, bool>()
            : JsonUtility.FromJson<SerializableDict>(signJson).ToDictionary();
    }

    private void SavePlayerData()
    {
        PlayerPrefs.SetString("daily_mission_data", JsonUtility.ToJson(_playerData));
        PlayerPrefs.SetString("sign_in_data",
            JsonUtility.ToJson(new SerializableDict(_signInRewardsClaimed)));
        PlayerPrefs.Save();
    }

    #endregion

    #region === 查询接口 ===

    public int GetDailyActivity() => _playerData.DailyActivity;
    public int GetWeeklyActivity() => _playerData.WeeklyActivity;
    public int GetConsecutiveLoginDays() => _playerData.ConsecutiveLoginDays;
    public int GetTotalLoginDays() => _playerData.TotalLoginDays;
    public Dictionary<string, MissionProgress> GetActiveMissions() => _activeMissions;
    public List<ActivityChestData> GetActivityChests() => Config.ActivityChests;
    public List<SignInReward> GetSignInRewards() => Config.SignInRewards;

    #endregion
}

#region === 数据结构 ===

[Serializable]
public class DailyPlayerData
{
    public string LastLoginDate = "";
    public string LastWeekReset = "";
    public int ConsecutiveLoginDays;
    public int TotalLoginDays;
    public int DailyActivity;
    public int WeeklyActivity;
    public string LastSignInDate = "";
    public List<int> ClaimedDailyChests = new List<int>();
}

[Serializable]
public class MissionProgress
{
    public string MissionId;
    public int CurrentProgress;
    public int TargetProgress;
    public bool IsCompleted;
    public bool IsRewardClaimed;
    public DateTime GeneratedTime;
}

[Serializable]
public class DailyMissionConfig : ScriptableObject
{
    public List<MissionData> DailyMissionTemplates = new List<MissionData>();
    public List<MissionData> WeeklyMissionTemplates = new List<MissionData>();
    public List<MissionData> RandomMissionPool = new List<MissionData>();
    public List<ActivityChestData> ActivityChests = new List<ActivityChestData>();
    public List<SignInReward> SignInRewards = new List<SignInReward>();
}

public enum MissionType
{
    KillEnemy,      // 击杀敌人
    CompleteWave,   // 通关波次
    MergeTower,     // 合成塔
    UseSkill,       // 使用技能
    PlayGame,       // 游玩局数
    WinGame,        // 胜利局数
    WatchAd,        // 观看广告
    ArenaBattle,    // 竞技场
    UpgradeEquipment, // 强化装备
    SpendGold,      // 消耗金币
    LevelUpHero     // 英雄升级
}

[Serializable]
public class MissionData
{
    public string MissionId;
    public string MissionName;
    public string Description;
    public MissionType Type;
    public string SubType = "";
    public int TargetValue = 1;
    public int ActivityPoints = 10;
    public List<RewardData> Rewards = new List<RewardData>();
}

[Serializable]
public class ActivityChestData
{
    public int ChestId;
    public string ChestName;
    public int RequiredActivity;
    public List<RewardData> RewardPool = new List<RewardData>();
}

[Serializable]
public class SignInReward
{
    public int DayIndex;
    public string IconName;
    public List<RewardData> Rewards = new List<RewardData>();
    public bool IsSpecial; // 特殊奖励(第7天/第15天/第30天)
}

[Serializable]
public class RewardData
{
    public enum RewardType { Gold, Diamond, Equipment, Chip, HeroShard, Stamina, Skin, BattlePassExp }

    public RewardType Type;
    public string ItemId;
    public int Amount;
    public int Rarity; // 品质: 1白 2绿 3蓝 4紫 5橙
}

[Serializable]
public class SerializableDict
{
    public List<string> Keys = new List<string>();
    public List<bool> Values = new List<bool>();

    public SerializableDict() { }
    public SerializableDict(Dictionary<int, bool> dict)
    {
        foreach (var kvp in dict)
        {
            Keys.Add(kvp.Key.ToString());
            Values.Add(kvp.Value);
        }
    }

    public Dictionary<int, bool> ToDictionary()
    {
        var dict = new Dictionary<int, bool>();
        for (int i = 0; i < Mathf.Min(Keys.Count, Values.Count); i++)
        {
            if (int.TryParse(Keys[i], out int key))
                dict[key] = Values[i];
        }
        return dict;
    }
}

#endregion
