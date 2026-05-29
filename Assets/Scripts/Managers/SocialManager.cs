/**
 * SocialManager - 社交裂变管理器
 * 分享、邀请、社区、好友对战、排行榜社交
 * 对标商业游戏社交体系
 */
using System;
using System.Collections.Generic;
using UnityEngine;

public class SocialManager : MonoBehaviour
{
    public static SocialManager Instance;

    // ==================== 分享类型 ====================
    public enum ShareType
    {
        VictoryShare,       // 通关分享
        HighScore,          // 高分炫耀
        NewTower,           // 获得新塔
        NewSkin,            // 获得稀有皮肤
        BossDefeat,         // Boss击杀
        LevelUp,            // 等级提升
        InviteFriend,       // 邀请好友
        DailyChallenge,     // 每日挑战
        SeasonRank,         // 赛季排名
        ReferralCode        // 推荐码
    }

    // ==================== 分享卡片 ====================
    [System.Serializable]
    public class ShareCard
    {
        public ShareType type;
        public string title;
        public string message;
        public string imageUrl;
        public string deepLink;
        public Dictionary<string, object> params_;
        public int coinReward;      // 分享获得金币
        public int gemReward;       // 分享获得钻石
    }

    // ==================== 邀请系统 ====================
    [System.Serializable]
    public class ReferralSystem
    {
        public string referralCode;
        public int invitedCount;
        public Dictionary<string, ReferralRecord> referrals;
        public List<ReferralMilestone> milestones;
    }

    [System.Serializable]
    public class ReferralRecord
    {
        public string inviteeId;
        public string inviteeName;
        public DateTime inviterAt;
        public bool hasCompletedTutorial;
        public bool hasReachedLevel5;
        public bool hasMadePurchase;
        public int rewardsEarned;
    }

    [System.Serializable]
    public class ReferralMilestone
    {
        public int inviteCount;
        public string rewardDescription;
        public bool claimed;
    }

    // ==================== 好友系统 ====================
    [System.Serializable]
    public class FriendData
    {
        public string id;
        public string nickname;
        public string avatar;
        public int level;
        public int highestStage;
        public int pvpRating;
        public long lastOnline;
        public bool isOnline;
        public bool canSendGift;
    }

    [System.Serializable]
    public class FriendGift
    {
        public string type;
        public int amount;
    }

    // ==================== 社区挑战 ====================
    [System.Serializable]
    public class CommunityChallenge
    {
        public string id;
        public string name;
        public string description;
        public ChallengeType type;
        public long targetValue;
        public long currentValue;
        public DateTime startTime;
        public DateTime endTime;
        public List<ChallengeMilestone> milestones;
        public bool completed;
    }

    public enum ChallengeType
    {
        GlobalKills,        // 全球击杀数
        TotalBosses,        // Boss击败数
        TotalWaves,         // 总波数
        DailyActive,        // 日活目标
        ReferralGoal        // 邀请目标
    }

    [System.Serializable]
    public class ChallengeMilestone
    {
        public long threshold;
        public bool reached;
        public RewardItem[] rewards;
    }

    // ==================== 每日助力 ====================
    [System.Serializable]
    public class DailyAssist
    {
        public int livesSent;
        public int livesReceived;
        public int maxPerDay = 5;
        public List<string> sentTo;
        public Dictionary<string, DateTime> receivedFrom;
    }

    // ==================== 数据 ====================
    private ReferralSystem referralSystem;
    private List<FriendData> friends;
    private List<CommunityChallenge> communityChallenges;
    private DailyAssist dailyAssist;
    private int shareCountToday;
    private int shareCountTotal;
    private List<ShareCard> pendingShareCards;

    // 回调
    public event Action<ShareType> OnShared;
    public event Action<string> OnFriendAdded;

    void Awake() { Instance = this; }

    void Start()
    {
        InitializeReferralSystem();
        InitializeCommunityChallenges();
        InitializeDailyAssist();
        LoadData();
    }

    // ==================== 分享系统 ====================

    public ShareCard GenerateShareCard(ShareType type, Dictionary<string, object> params_ = null)
    {
        ShareCard card = null;

        switch (type)
        {
            case ShareType.VictoryShare:
                card = new ShareCard
                {
                    type = ShareType.VictoryShare,
                    title = "我在僵尸防线的第X关坚守成功!",
                    message = $"守住了 {(params_?.ContainsKey("wave") == true ? params_["wave"] : "?")} 波攻击! 击杀 {(params_?.ContainsKey("kills") == true ? params_["kills"] : "?")} 只僵尸! 来挑战我吧!",
                    coinReward = 50,
                    gemReward = 0
                };
                break;

            case ShareType.HighScore:
                card = new ShareCard
                {
                    type = ShareType.HighScore,
                    title = $"获得 {params_?["score"]} 高分!",
                    message = "我在僵尸防线的无尽模式刷新了记录! 敢来破我的记录吗?",
                    coinReward = 100,
                    gemReward = 5
                };
                break;

            case ShareType.NewSkin:
                card = new ShareCard
                {
                    type = ShareType.NewSkin,
                    title = $"获得限定皮肤: {params_?["skinName"]}!",
                    message = $"运气爆棚! 刚抽到了{params_?["skinName"]}皮肤! 来试试你的运气?",
                    coinReward = 200,
                    gemReward = 10
                };
                break;

            case ShareType.BossDefeat:
                card = new ShareCard
                {
                    type = ShareType.BossDefeat,
                    title = $"击败 {params_?["bossName"]}!",
                    message = $"我打败了{params_?["bossName"]}! {params_?["difficulty"]}难度哦~",
                    coinReward = 150,
                    gemReward = 5
                };
                break;

            case ShareType.InviteFriend:
                card = new ShareCard
                {
                    type = ShareType.InviteFriend,
                    title = "一起来打僵尸吧!",
                    message = $"用我的邀请码 {referralSystem.referralCode} 加入，领取新手大礼包!",
                    coinReward = 0,
                    gemReward = 50,
                    deepLink = $"zombie_defense://invite?code={referralSystem.referralCode}"
                };
                break;

            case ShareType.DailyChallenge:
                card = new ShareCard
                {
                    type = ShareType.DailyChallenge,
                    title = "每日挑战已完成!",
                    message = $"今天我拿到了{(params_?["score"])}分, 排名第{(params_?["rank"])}名!",
                    coinReward = 75,
                    gemReward = 5
                };
                break;

            case ShareType.SeasonRank:
                card = new ShareCard
                {
                    type = ShareType.SeasonRank,
                    title = $"赛季排名: TOP {params_?["rank"]}!",
                    message = $"本赛季我冲到了第{params_?["rank"]}名! 来看看你能排第几?",
                    coinReward = 200,
                    gemReward = 20
                };
                break;
        }

        if (card != null)
        {
            card.params_ = params_;
            pendingShareCards?.Add(card);
        }

        return card;
    }

    public void CompleteShare(ShareType type)
    {
        var card = pendingShareCards?.Find(s => s.type == type);
        if (card == null) return;

        shareCountToday++;
        shareCountTotal++;

        // 发放分享奖励
        if (card.coinReward > 0)
        {
            MonetizationManager.Instance?.AddCurrency(MonetizationManager.CurrencyType.Gold, card.coinReward, "分享奖励");
        }
        if (card.gemReward > 0)
        {
            MonetizationManager.Instance?.AddCurrency(MonetizationManager.CurrencyType.Gems, card.gemReward, "分享奖励");
        }

        pendingShareCards?.Remove(card);
        OnShared?.Invoke(type);
        Debug.Log($"[Social] 📤 分享完成: {type} (+{card.coinReward}金币, +{card.gemReward}钻石)");
    }

    // ==================== 邀请系统 ====================

    private void InitializeReferralSystem()
    {
        referralSystem = new ReferralSystem
        {
            referralCode = GenerateReferralCode(),
            invitedCount = 0,
            referrals = new Dictionary<string, ReferralRecord>(),
            milestones = new List<ReferralMilestone>
            {
                new ReferralMilestone { inviteCount = 1, rewardDescription = "100钻石", claimed = false },
                new ReferralMilestone { inviteCount = 3, rewardDescription = "SSR随机塔", claimed = false },
                new ReferralMilestone { inviteCount = 5, rewardDescription = "限定皮肤", claimed = false },
                new ReferralMilestone { inviteCount = 10, rewardDescription = "UR神话塔", claimed = false },
                new ReferralMilestone { inviteCount = 20, rewardDescription = "传说皮肤+5000钻石", claimed = false }
            }
        };
    }

    private string GenerateReferralCode()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var code = new char[8];
        var rng = new System.Random();
        for (int i = 0; i < 8; i++)
            code[i] = chars[rng.Next(chars.Length)];
        return $"ZF{new string(code)}";
    }

    public void ProcessReferral(string inviteeId, string inviteeName)
    {
        if (referralSystem.referrals.ContainsKey(inviteeId))
            return;

        var record = new ReferralRecord
        {
            inviteeId = inviteeId,
            inviteeName = inviteeName,
            inviterAt = DateTime.Now
        };

        referralSystem.referrals[inviteeId] = record;
        referralSystem.invitedCount++;
        CheckReferralMilestones();

        Debug.Log($"[Social] 👥 新邀请: {inviteeName}");
        SaveData();
    }

    public void OnInviteeCompleteTutorial(string inviteeId)
    {
        if (!referralSystem.referrals.TryGetValue(inviteeId, out var record))
            return;

        record.hasCompletedTutorial = true;
        record.rewardsEarned += 50;
        MonetizationManager.Instance?.AddCurrency(MonetizationManager.CurrencyType.Gems, 50, "邀请奖励");
        Debug.Log($"[Social] 🎓 {record.inviteeName} 完成新手教程 (+50钻石)");
    }

    public void OnInviteeReachLevel(string inviteeId, int level)
    {
        if (level < 5) return;
        if (!referralSystem.referrals.TryGetValue(inviteeId, out var record))
            return;

        if (!record.hasReachedLevel5)
        {
            record.hasReachedLevel5 = true;
            record.rewardsEarned += 100;
            MonetizationManager.Instance?.AddCurrency(MonetizationManager.CurrencyType.Gems, 100, "邀请奖励");
            Debug.Log($"[Social] 🆙 {record.inviteeName} 达到5级 (+100钻石)");
        }
    }

    public void OnInviteeFirstPurchase(string inviteeId, int amount)
    {
        if (!referralSystem.referrals.TryGetValue(inviteeId, out var record))
            return;

        if (!record.hasMadePurchase)
        {
            record.hasMadePurchase = true;
            int reward = Mathf.FloorToInt(amount * 0.1f); // 10%返利
            record.rewardsEarned += reward;
            MonetizationManager.Instance?.AddCurrency(
                MonetizationManager.CurrencyType.Gems, reward, "邀请充值返利");
            Debug.Log($"[Social] 💰 {record.inviteeName} 首充返利 (+{reward}钻石)");
        }
    }

    private void CheckReferralMilestones()
    {
        foreach (var milestone in referralSystem.milestones)
        {
            if (!milestone.claimed && referralSystem.invitedCount >= milestone.inviteCount)
            {
                milestone.claimed = true;
                Debug.Log($"[Social] 🏆 邀请里程碑达成! {milestone.inviteCount}人 → {milestone.rewardDescription}");
            }
        }
    }

    // ==================== 好友系统 ====================

    public void AddFriend(string friendId, string nickname, string avatar)
    {
        if (friends.Exists(f => f.id == friendId))
            return;

        friends.Add(new FriendData
        {
            id = friendId,
            nickname = nickname,
            avatar = avatar,
            canSendGift = true
        });

        OnFriendAdded?.Invoke(friendId);
        Debug.Log($"[Social] 👋 添加好友: {nickname}");
        SaveData();
    }

    public void SendGift(string friendId)
    {
        var friend = friends.Find(f => f.id == friendId);
        if (friend == null || !friend.canSendGift) return;

        friend.canSendGift = false;
        Debug.Log($"[Social] 🎁 送给{friend.nickname}一份礼物!");
    }

    public List<FriendData> GetFriends() => friends;
    public List<FriendData> GetOnlineFriends() =>
        friends.FindAll(f => f.isOnline);

    // ==================== 社区挑战 ====================

    private void InitializeCommunityChallenges()
    {
        var now = DateTime.Now;
        var endOfWeek = now.AddDays(7 - (int)now.DayOfWeek).Date;

        communityChallenges = new List<CommunityChallenge>
        {
            new CommunityChallenge
            {
                id = "global_kills_week",
                name = "本周全球击杀挑战",
                description = "全服玩家共同努力，击杀僵尸达到目标!",
                type = ChallengeType.GlobalKills,
                targetValue = 100_000_000,
                currentValue = 0,
                startTime = now,
                endTime = endOfWeek.AddDays(1).AddSeconds(-1),
                milestones = new List<ChallengeMilestone>
                {
                    new ChallengeMilestone { threshold = 10_000_000, rewards = new[] { new RewardItem { type = "gold", amount = 5000 } } },
                    new ChallengeMilestone { threshold = 50_000_000, rewards = new[] { new RewardItem { type = "gems", amount = 100 } } },
                    new ChallengeMilestone { threshold = 100_000_000, rewards = new[] { new RewardItem { type = "gems", amount = 500 }, new RewardItem { type = "skin_token", amount = 50 } } }
                }
            }
        };
    }

    public void ContributeToChallenge(ChallengeType type, long value)
    {
        var challenge = communityChallenges.Find(c => c.type == type);
        if (challenge == null || challenge.completed) return;

        challenge.currentValue += value;

        // 检查里程碑
        foreach (var ms in challenge.milestones)
        {
            if (!ms.reached && challenge.currentValue >= ms.threshold)
            {
                ms.reached = true;
                foreach (var reward in ms.rewards)
                {
                    Debug.Log($"[Social] 🎯 社区挑战里程碑! {reward.amount} {reward.type}");
                }
            }
        }

        if (challenge.currentValue >= challenge.targetValue)
        {
            challenge.completed = true;
            Debug.Log($"[Social] 🎉 社区挑战完成! {challenge.name}");
        }
    }

    // ==================== 每日助力 ====================

    private void InitializeDailyAssist()
    {
        dailyAssist = new DailyAssist
        {
            livesSent = 0,
            livesReceived = 0,
            sentTo = new List<string>(),
            receivedFrom = new Dictionary<string, DateTime>()
        };
    }

    // ==================== 每日重置 ====================

    public void DailyReset()
    {
        shareCountToday = 0;
        dailyAssist.livesSent = 0;

        // 重置好友送礼
        foreach (var friend in friends)
            friend.canSendGift = true;

        SaveData();
    }

    // ==================== 持久化 ====================

    private void LoadData()
    {
        try
        {
            var json = PlayerPrefs.GetString("social_data", "");
            if (string.IsNullOrEmpty(json)) return;
            var data = JsonUtility.FromJson<SocialSaveData>(json);
            friends = data.friends ?? new List<FriendData>();
            shareCountTotal = data.shareCountTotal;
            referralSystem = data.referral ?? referralSystem;
        }
        catch { }
    }

    private void SaveData()
    {
        var data = new SocialSaveData
        {
            friends = friends,
            shareCountTotal = shareCountTotal,
            referral = referralSystem
        };
        PlayerPrefs.SetString("social_data", JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    [System.Serializable]
    private class SocialSaveData
    {
        public List<FriendData> friends;
        public int shareCountTotal;
        public ReferralSystem referral;
    }

    // ==================== Getter ====================
    public ReferralSystem GetReferralSystem() => referralSystem;
    public int GetShareCountToday() => shareCountToday;
    public int GetShareCountTotal() => shareCountTotal;
    public List<CommunityChallenge> GetChallenges() => communityChallenges;
    public string GetReferralCode() => referralSystem?.referralCode ?? "";
}
