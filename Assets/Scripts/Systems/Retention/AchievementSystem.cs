using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 成就系统 - 长期留存活水
/// 提供多层级成就、进度追踪、称号系统、成就点数商店
/// </summary>
public class AchievementSystem : MonoBehaviour
{
    public static AchievementSystem Instance;

    [Header("配置")]
    public AchievementConfig Config;

    private Dictionary<string, AchievementProgress> _achievements = new Dictionary<string, AchievementProgress>();
    private List<string> _unlockedTitles = new List<string>();
    private string _activeTitle = "";

    // 事件
    public event Action<AchievementData> OnAchievementUnlocked;
    public event Action<AchievementData, int> OnAchievementProgressUpdated;
    public event Action<string> OnTitleUnlocked;
    public event Action<string> OnTitleChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadAchievementData();
        InitializeAchievements();
    }

    private void InitializeAchievements()
    {
        foreach (var ach in Config.Achievements)
        {
            if (!_achievements.ContainsKey(ach.AchievementId))
            {
                _achievements[ach.AchievementId] = new AchievementProgress
                {
                    AchievementId = ach.AchievementId,
                    CurrentProgress = 0,
                    CurrentTier = 0,
                    IsMaxed = false
                };
            }
        }
    }

    #region === 成就进度汇报 ===

    public void ReportAchievementProgress(AchievementCategory category, string subType = "", long amount = 1)
    {
        foreach (var achData in Config.Achievements)
        {
            if (achData.Category != category) continue;
            if (!string.IsNullOrEmpty(achData.SubType) && achData.SubType != subType) continue;

            if (!_achievements.ContainsKey(achData.AchievementId)) continue;
            var progress = _achievements[achData.AchievementId];
            if (progress.IsMaxed) continue;

            int tierIndex = progress.CurrentTier;
            if (tierIndex >= achData.Tiers.Count) continue;

            var currentTier = achData.Tiers[tierIndex];
            progress.CurrentProgress += amount;

            OnAchievementProgressUpdated?.Invoke(achData, (int)progress.CurrentProgress);

            // 检查是否达到当前阶段
            if (progress.CurrentProgress >= currentTier.RequiredValue)
            {
                // 发放奖励
                foreach (var reward in currentTier.Rewards)
                {
                    GrantReward(reward);
                }

                progress.CurrentTier++;
                OnAchievementUnlocked?.Invoke(achData);

                // 检查是否满级
                if (progress.CurrentTier >= achData.Tiers.Count)
                {
                    progress.IsMaxed = true;
                }

                // 检查是否获得称号
                if (!string.IsNullOrEmpty(achData.TitleReward) &&
                    !_unlockedTitles.Contains(achData.TitleReward))
                {
                    _unlockedTitles.Add(achData.TitleReward);
                    OnTitleUnlocked?.Invoke(achData.TitleReward);
                }
            }
        }
        SaveAchievementData();
    }

    private void GrantReward(AchievementReward reward)
    {
        switch (reward.Type)
        {
            case AchievementReward.RewardType.Gold:
                GameManager.Instance?.AddGold(reward.Amount);
                break;
            case AchievementReward.RewardType.Diamond:
                CommerceManager.Instance?.AddDiamond(reward.Amount);
                break;
            case AchievementReward.RewardType.AchievementPoints:
                _playerData.TotalAchievementPoints += reward.Amount;
                break;
            case AchievementReward.RewardType.Skin:
                // 解锁皮肤
                break;
            case AchievementReward.RewardType.Title:
                if (!_unlockedTitles.Contains(reward.ItemId))
                {
                    _unlockedTitles.Add(reward.ItemId);
                    OnTitleUnlocked?.Invoke(reward.ItemId);
                }
                break;
        }
    }

    #endregion

    #region === 便捷汇报接口 ===

    public void OnGameWon(int waveReached, float timeSpent)
    {
        ReportAchievementProgress(AchievementCategory.Victory);
        ReportAchievementProgress(AchievementCategory.WaveMaster, "", waveReached);
        if (waveReached >= 100) ReportAchievementProgress(AchievementCategory.Wave100);
        if (waveReached >= 50) ReportAchievementProgress(AchievementCategory.Wave50);
    }

    public void OnEnemyKilled(string enemyType, long totalKills)
    {
        ReportAchievementProgress(AchievementCategory.EnemySlayer, enemyType);
        ReportAchievementProgress(AchievementCategory.KillCount, "", totalKills);
        if (enemyType == "Boss") ReportAchievementProgress(AchievementCategory.BossSlayer);
    }

    public void OnTowerMerged(string towerType, int mergeLevel)
    {
        ReportAchievementProgress(AchievementCategory.TowerMaster, towerType);
        if (mergeLevel >= 5) ReportAchievementProgress(AchievementCategory.MaxLevelTower);
    }

    public void OnGoldEarned(long amount)
        => ReportAchievementProgress(AchievementCategory.Wealth, "", amount);

    public void OnDiamondSpent(int amount)
        => ReportAchievementProgress(AchievementCategory.Spender, "", amount);

    public void OnEquipmentCrafted(string rarity)
        => ReportAchievementProgress(AchievementCategory.Collector, rarity);

    public void OnArenaWin()
        => ReportAchievementProgress(AchievementCategory.ArenaChampion);

    public void OnDailyMissionsCompleted(int total)
        => ReportAchievementProgress(AchievementCategory.MissionMaster, "", total);

    public void OnConsecutiveLogin(int days)
        => ReportAchievementProgress(AchievementCategory.LoyalPlayer, "", days);

    public void OnHeroUnlocked(string heroId)
        => ReportAchievementProgress(AchievementCategory.HeroCollector, heroId);

    #endregion

    #region === 称号系统 ===

    public void SetActiveTitle(string titleId)
    {
        if (_unlockedTitles.Contains(titleId))
        {
            _activeTitle = titleId;
            OnTitleChanged?.Invoke(titleId);
            SaveAchievementData();
        }
    }

    public List<string> GetUnlockedTitles() => _unlockedTitles;
    public string GetActiveTitle() => _activeTitle;
    public int GetAchievementPoints() => _playerData.TotalAchievementPoints;

    #endregion

    #region === 成就点数商店 ===

    public List<AchievementShopItem> GetShopItems() => Config.ShopItems;

    public bool PurchaseShopItem(string itemId)
    {
        var item = Config.ShopItems.Find(i => i.ItemId == itemId);
        if (item == null) return false;
        if (_playerData.TotalAchievementPoints < item.Cost) return false;
        if (_playerData.PurchasedShopItems.Contains(itemId)) return false;

        _playerData.TotalAchievementPoints -= item.Cost;
        _playerData.PurchasedShopItems.Add(itemId);

        foreach (var reward in item.Rewards)
            GrantReward(reward);

        SaveAchievementData();
        return true;
    }

    #endregion

    #region === 数据持久化 ===

    private AchievementPlayerData _playerData = new AchievementPlayerData();

    private void LoadAchievementData()
    {
        string json = PlayerPrefs.GetString("achievement_data", "");
        _playerData = string.IsNullOrEmpty(json)
            ? new AchievementPlayerData()
            : JsonUtility.FromJson<AchievementPlayerData>(json);

        string achJson = PlayerPrefs.GetString("achievement_progress", "");
        if (!string.IsNullOrEmpty(achJson))
        {
            var list = JsonUtility.FromJson<AchievementProgressList>(achJson);
            _achievements = list.ToDictionary();
        }
    }

    private void SaveAchievementData()
    {
        PlayerPrefs.SetString("achievement_data", JsonUtility.ToJson(_playerData));
        PlayerPrefs.SetString("achievement_progress",
            JsonUtility.ToJson(new AchievementProgressList(_achievements)));
        PlayerPrefs.Save();
    }

    #endregion
}

#region === 数据结构 ===

[Serializable]
public class AchievementPlayerData
{
    public int TotalAchievementPoints;
    public List<string> PurchasedShopItems = new List<string>();
}

[Serializable]
public class AchievementProgress
{
    public string AchievementId;
    public long CurrentProgress;
    public int CurrentTier; // 当前达到的阶数 (0=未解锁第一阶)
    public bool IsMaxed;
}

[Serializable]
public class AchievementProgressList
{
    public List<string> Ids = new List<string>();
    public List<long> Progress = new List<long>();
    public List<int> Tiers = new List<int>();
    public List<bool> Maxed = new List<bool>();

    public AchievementProgressList() { }
    public AchievementProgressList(Dictionary<string, AchievementProgress> dict)
    {
        foreach (var kvp in dict)
        {
            Ids.Add(kvp.Key);
            Progress.Add(kvp.Value.CurrentProgress);
            Tiers.Add(kvp.Value.CurrentTier);
            Maxed.Add(kvp.Value.IsMaxed);
        }
    }

    public Dictionary<string, AchievementProgress> ToDictionary()
    {
        var dict = new Dictionary<string, AchievementProgress>();
        for (int i = 0; i < Ids.Count; i++)
        {
            dict[Ids[i]] = new AchievementProgress
            {
                AchievementId = Ids[i],
                CurrentProgress = Progress[i],
                CurrentTier = Tiers[i],
                IsMaxed = Maxed[i]
            };
        }
        return dict;
    }
}

[Serializable]
public class AchievementConfig : ScriptableObject
{
    public List<AchievementData> Achievements = new List<AchievementData>();
    public List<AchievementShopItem> ShopItems = new List<AchievementShopItem>();
}

public enum AchievementCategory
{
    Victory,        // 胜利相关
    WaveMaster,     // 波次大师
    Wave50,         // 50波
    Wave100,        // 100波
    EnemySlayer,    // 击杀
    KillCount,      // 累计击杀
    BossSlayer,     // BOSS击杀
    TowerMaster,    // 防御塔大师
    MaxLevelTower,  // 满级防御塔
    Wealth,         // 财富积累
    Spender,        // 挥金如土
    Collector,      // 装备收藏家
    ArenaChampion,  // 竞技场冠军
    MissionMaster,  // 任务大师
    LoyalPlayer,    // 忠实玩家
    HeroCollector   // 英雄收藏家
}

[Serializable]
public class AchievementData
{
    public string AchievementId;
    public string AchievementName;
    public string IconName;
    public AchievementCategory Category;
    public string SubType = "";
    public string TitleReward; // 达成后获得的称号
    public List<AchievementTier> Tiers = new List<AchievementTier>();
}

[Serializable]
public class AchievementTier
{
    public int TierLevel;       // 1=铜 2=银 3=金 4=钻石 5=传说
    public long RequiredValue;
    public string TierName;
    public List<AchievementReward> Rewards = new List<AchievementReward>();
}

[Serializable]
public class AchievementReward
{
    public enum RewardType { Gold, Diamond, AchievementPoints, Skin, Title, Equipment, Chip }

    public RewardType Type;
    public string ItemId;
    public int Amount;
}

[Serializable]
public class AchievementShopItem
{
    public string ItemId;
    public string ItemName;
    public int Cost;
    public List<AchievementReward> Rewards = new List<AchievementReward>();
}

#endregion
