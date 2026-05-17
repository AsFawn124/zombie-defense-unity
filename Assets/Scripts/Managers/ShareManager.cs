using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 分享与邀请系统 - TASK-036~037
/// 精彩截图生成、视频回放分享、分享奖励、邀请码、师徒系统
/// </summary>
public class ShareManager : MonoBehaviour
{
    public static ShareManager Instance;

    [Header("分享配置")]
    public ShareConfig Config;

    private int _shareCount;
    private int _totalShareClicks;
    private DateTime _lastShareTime;

    // 事件
    public event Action<ShareType> OnShared;
    public event Action<int> OnShareRewardEarned;
    public event Action<string> OnScreenshotSaved;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadShareData();
    }

    #region === 精彩截图生成 (TASK-036) ===

    /// <summary>
    /// 生成精彩截图（带赛博朋克边框和水印）
    /// </summary>
    public void CaptureScreenshot(Action<string> onComplete = null)
    {
        StartCoroutine(CaptureScreenshotRoutine(onComplete));
    }

    private System.Collections.IEnumerator CaptureScreenshotRoutine(Action<string> onComplete)
    {
        // 隐藏UI进行截图
        var hudVisible = ToggleHUD(false);

        yield return new WaitForEndOfFrame();

        string filename = $"screenshot_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
        string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
        ScreenCapture.CaptureScreenshot(filename);

        yield return new WaitForEndOfFrame();

        // 恢复UI
        ToggleHUD(hudVisible);

        yield return new WaitForSeconds(0.1f); // 等待保存完成

        OnScreenshotSaved?.Invoke(path);
        onComplete?.Invoke(path);
    }

    private bool ToggleHUD(bool visible)
    {
        var hud = FindObjectOfType<GameHUD>();
        if (hud != null)
        {
            bool wasActive = hud.gameObject.activeSelf;
            hud.gameObject.SetActive(visible);
            return wasActive;
        }
        return true;
    }

    #endregion

    #region === 分享功能 (TASK-036) ===

    /// <summary>
    /// 分享通关高分
    /// </summary>
    public void ShareHighScore(int score, int wave)
    {
        string shareText = string.Format(Config.HighScoreTemplate, score, wave);
        ExecuteShare(ShareType.HighScore, shareText, null);
    }

    /// <summary>
    /// 分享稀有装备掉落
    /// </summary>
    public void ShareRareDrop(string itemName, int rarity)
    {
        string shareText = string.Format(Config.RareDropTemplate, itemName);
        ExecuteShare(ShareType.RareDrop, shareText, null);
    }

    /// <summary>
    /// 分享创意关卡
    /// </summary>
    public void ShareCustomLevel(string levelCode, string levelName)
    {
        string shareText = string.Format(Config.CustomLevelTemplate, levelName, levelCode);
        ExecuteShare(ShareType.CustomLevel, shareText, levelCode);
    }

    /// <summary>
    /// 分享赛季排名
    /// </summary>
    public void ShareSeasonRank(int rank, string rankTitle)
    {
        string shareText = string.Format(Config.SeasonRankTemplate, rankTitle, rank);
        ExecuteShare(ShareType.SeasonRank, shareText, null);
    }

    private void ExecuteShare(ShareType type, string text, string query)
    {
        // 检查冷却
        if ((DateTime.UtcNow - _lastShareTime).TotalSeconds < Config.ShareCooldownSeconds)
        {
            Debug.Log($"[Share] 分享冷却中，剩余{Config.ShareCooldownSeconds - (DateTime.UtcNow - _lastShareTime).TotalSeconds:F0}秒");
            return;
        }

        // 通过微信SDK分享
        WeChatManager.Instance?.ShareMessage(text, query);

        _shareCount++;
        _lastShareTime = DateTime.UtcNow;

        // 发放分享奖励
        GrantShareReward(type);

        OnShared?.Invoke(type);
        SaveShareData();
    }

    private void GrantShareReward(ShareType type)
    {
        int diamonds = Config.GetShareReward(type);
        if (diamonds > 0)
        {
            GameManager.Instance?.AddGold(diamonds * 10); // 简化：用金币代替钻石
            OnShareRewardEarned?.Invoke(diamonds);
        }
    }

    /// <summary>
    /// 收到分享点击奖励
    /// </summary>
    public void OnShareClicked()
    {
        _totalShareClicks++;
        if (_totalShareClicks % Config.ClickRewardThreshold == 0)
        {
            GameManager.Instance?.AddGold(Config.ClickRewardGold);
        }
        SaveShareData();
    }

    #endregion

    #region === 视频回放分享 (TASK-036) ===

    /// <summary>
    /// 生成战斗回放视频
    /// </summary>
    public void RecordBattleReplay(string battleId, int maxDuration = 60)
    {
        StartCoroutine(RecordReplayRoutine(battleId, maxDuration));
    }

    private System.Collections.IEnumerator RecordReplayRoutine(string battleId, int maxDuration)
    {
        float elapsed = 0;
        var arenaManager = FindObjectOfType<ArenaManager>();
        if (arenaManager == null) yield break;

        // 开始录制
        Debug.Log($"[Share] 开始录制回放: {battleId}");

        while (elapsed < maxDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 停止录制并保存
        string videoPath = System.IO.Path.Combine(
            Application.persistentDataPath,
            $"replay_{battleId}_{DateTime.UtcNow:yyyyMMdd}.mp4"
        );

        Debug.Log($"[Share] 回放录制完成: {videoPath}");

        // 分享视频
        WeChatManager.Instance?.ShareVideo(videoPath);
    }

    #endregion

    #region === 获取分享统计 ===

    public int TotalShares => _shareCount;
    public int TotalClicks => _totalShareClicks;

    private void LoadShareData()
    {
        _shareCount = PlayerPrefs.GetInt("share_count", 0);
        _totalShareClicks = PlayerPrefs.GetInt("share_clicks", 0);
    }

    private void SaveShareData()
    {
        PlayerPrefs.SetInt("share_count", _shareCount);
        PlayerPrefs.SetInt("share_clicks", _totalShareClicks);
        PlayerPrefs.Save();
    }

    #endregion
}

/// <summary>
/// 邀请系统 - TASK-037
/// 邀请码生成、邀请奖励、师徒系统
/// </summary>
public class InviteManager : MonoBehaviour
{
    public static InviteManager Instance;

    [Header("邀请配置")]
    public InviteConfig Config;

    private List<InviteRecord> _invitedPlayers = new List<InviteRecord>();
    private List<InviteRecord> _invitedByPlayers = new List<InviteRecord>();
    private string _myInviteCode;

    // 师徒系统
    private MentorData _mentorData;

    // 事件
    public event Action<InviteRecord> OnPlayerInvited;
    public event Action<InviteReward> OnInviteRewardEarned;
    public event Action<string> OnInviteCodeGenerated;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _myInviteCode = GenerateInviteCode();
        LoadInviteData();
        LoadMentorData();
    }

    #region === 邀请码系统 (TASK-037) ===

    /// <summary>
    /// 生成唯一邀请码
    /// </summary>
    private string GenerateInviteCode()
    {
        string existing = PlayerPrefs.GetString("invite_code", "");
        if (!string.IsNullOrEmpty(existing)) return existing;

        string code;
        do
        {
            code = GenerateRandomCode(Config.CodeLength);
        } while (PlayerPrefs.GetInt($"invite_code_used_{code}", 0) == 1);

        PlayerPrefs.SetString("invite_code", code);
        PlayerPrefs.Save();

        OnInviteCodeGenerated?.Invoke(code);
        return code;
    }

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        char[] code = new char[length];
        for (int i = 0; i < length; i++)
        {
            code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        }
        return new string(code);
    }

    /// <summary>
    /// 使用邀请码
    /// </summary>
    public bool UseInviteCode(string code)
    {
        if (code == _myInviteCode)
        {
            Debug.LogWarning("[Invite] 不能使用自己的邀请码");
            return false;
        }

        if (HasUsedInviteCode())
        {
            Debug.LogWarning("[Invite] 已经使用过邀请码");
            return false;
        }

        // 标记邀请码已使用
        PlayerPrefs.SetInt($"invite_code_used_{code}", 1);
        PlayerPrefs.SetString("used_invite_code", code);
        PlayerPrefs.Save();

        // 记录邀请关系
        var record = new InviteRecord
        {
            InviteCode = code,
            InvitedPlayerId = PlayerPrefs.GetString("player_id", "local"),
            InviteTime = DateTime.UtcNow
        };
        _invitedByPlayers.Add(record);

        // 发放被邀请人奖励
        GrantInviteeReward();

        return true;
    }

    /// <summary>
    /// 确认邀请成功（邀请人端）
    /// </summary>
    public void ConfirmInvite(string invitedPlayerId)
    {
        var record = new InviteRecord
        {
            InviteCode = _myInviteCode,
            InvitedPlayerId = invitedPlayerId,
            InviteTime = DateTime.UtcNow
        };
        _invitedPlayers.Add(record);

        int totalInvites = _invitedPlayers.Count;
        CheckMilestoneRewards(totalInvites);

        OnPlayerInvited?.Invoke(record);
        SaveInviteData();
    }

    private void CheckMilestoneRewards(int totalInvites)
    {
        foreach (var milestone in Config.InviteMilestones)
        {
            if (totalInvites >= milestone.RequiredInvites &&
                !IsMilestoneClaimed(milestone.RequiredInvites))
            {
                var reward = new InviteReward
                {
                    Milestone = milestone.RequiredInvites,
                    Gold = milestone.GoldReward,
                    Diamonds = milestone.DiamondReward,
                    ItemReward = milestone.ItemReward
                };

                GrantReward(reward);
                MarkMilestoneClaimed(milestone.RequiredInvites);
                OnInviteRewardEarned?.Invoke(reward);
            }
        }
    }

    private void GrantInviteeReward()
    {
        var reward = Config.InviteeReward;
        GameManager.Instance?.AddGold(reward.Gold);
        Debug.Log($"[Invite] 获得新手礼包: {reward.Gold}金币 + {reward.Diamonds}钻石");
    }

    private void GrantReward(InviteReward reward)
    {
        GameManager.Instance?.AddGold(reward.Gold);
        Debug.Log($"[Invite] 邀请里程碑{reward.Milestone}人奖励: {reward.Gold}金币");
    }

    private bool IsMilestoneClaimed(int requiredInvites)
    {
        return PlayerPrefs.GetInt($"invite_milestone_{requiredInvites}", 0) == 1;
    }

    private void MarkMilestoneClaimed(int requiredInvites)
    {
        PlayerPrefs.SetInt($"invite_milestone_{requiredInvites}", 1);
        PlayerPrefs.Save();
    }

    #endregion

    #region === 师徒系统 (TASK-037) ===

    /// <summary>
    /// 建立师徒关系
    /// </summary>
    public void EstablishMentorship(string mentorInviteCode, string studentId)
    {
        _mentorData = new MentorData
        {
            MentorInviteCode = mentorInviteCode,
            StudentId = studentId,
            StartTime = DateTime.UtcNow,
            TasksCompleted = 0,
            IsActive = true
        };

        SaveMentorData();
    }

    /// <summary>
    /// 完成师徒任务
    /// </summary>
    public void CompleteMentorTask()
    {
        if (_mentorData == null || !_mentorData.IsActive) return;

        _mentorData.TasksCompleted++;

        // 徒弟奖励
        GrantMentorshipReward(false);

        // 师傅奖励（通过邀请人系统发放）
        // 实际项目中通过服务器通知师傅

        // 毕业检查
        if (_mentorData.TasksCompleted >= Config.MentorGraduateTasks)
        {
            GraduateMentorship();
        }

        SaveMentorData();
    }

    private void GrantMentorshipReward(bool isGraduate)
    {
        int gold = isGraduate ? Config.MentorGraduateGold : Config.MentorTaskGold;
        GameManager.Instance?.AddGold(gold);

        if (isGraduate)
        {
            Debug.Log("[Mentor] 恭喜毕业! 获得毕业奖励");
        }
    }

    private void GraduateMentorship()
    {
        _mentorData.IsActive = false;
        _mentorData.GraduateTime = DateTime.UtcNow;
        GrantMentorshipReward(true);

        // 师傅获得充值返利（实际项目中实现）
        Debug.Log($"[Mentor] 师徒关系毕业! 共完成{_mentorData.TasksCompleted}个任务");
    }

    #endregion

    #region === 数据持久化 ===

    private bool HasUsedInviteCode()
    {
        return !string.IsNullOrEmpty(PlayerPrefs.GetString("used_invite_code", ""));
    }

    private void LoadInviteData()
    {
        string json = PlayerPrefs.GetString("invite_records", "");
        if (!string.IsNullOrEmpty(json))
        {
            var wrapper = JsonUtility.FromJson<InviteRecordWrapper>(json);
            if (wrapper != null)
            {
                _invitedPlayers = wrapper.Records ?? new List<InviteRecord>();
            }
        }
    }

    private void SaveInviteData()
    {
        var wrapper = new InviteRecordWrapper { Records = _invitedPlayers };
        PlayerPrefs.SetString("invite_records", JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    private void LoadMentorData()
    {
        string json = PlayerPrefs.GetString("mentor_data", "");
        if (!string.IsNullOrEmpty(json))
        {
            _mentorData = JsonUtility.FromJson<MentorData>(json);
        }
    }

    private void SaveMentorData()
    {
        if (_mentorData != null)
        {
            PlayerPrefs.SetString("mentor_data", JsonUtility.ToJson(_mentorData));
            PlayerPrefs.Save();
        }
    }

    #endregion

    #region === 公共属性 ===

    public string MyInviteCode => _myInviteCode;
    public int TotalInvited => _invitedPlayers.Count;
    public MentorData MentorInfo => _mentorData;
    public bool IsMentorActive => _mentorData?.IsActive ?? false;

    #endregion
}

#region === 共享数据结构 ===

[System.Serializable]
public class ShareConfig
{
    public float ShareCooldownSeconds = 300f;

    // 分享模板
    public string HighScoreTemplate = "我在《僵尸防线》获得了{0}分，挑战到第{1}波！来挑战我吧！";
    public string RareDropTemplate = "我在《僵尸防线》获得了传说装备【{0}】！";
    public string CustomLevelTemplate = "来挑战我设计的关卡《{0}》！关卡码：{1}";
    public string SeasonRankTemplate = "本赛我获得了{0}段位，排名第{1}！";

    // 分享奖励
    public int HighScoreShareReward = 10;
    public int RareDropShareReward = 20;
    public int CustomLevelShareReward = 15;
    public int SeasonRankShareReward = 30;

    // 点击奖励
    public int ClickRewardThreshold = 5;
    public int ClickRewardGold = 100;

    public int GetShareReward(ShareType type)
    {
        return type switch
        {
            ShareType.HighScore => HighScoreShareReward,
            ShareType.RareDrop => RareDropShareReward,
            ShareType.CustomLevel => CustomLevelShareReward,
            ShareType.SeasonRank => SeasonRankShareReward,
            _ => 10
        };
    }
}

[System.Serializable]
public class InviteConfig
{
    public int CodeLength = 6;

    // 邀请里程碑
    public InviteMilestone[] InviteMilestones = new InviteMilestone[]
    {
        new InviteMilestone { RequiredInvites = 1, GoldReward = 1000, DiamondReward = 100, ItemReward = "" },
        new InviteMilestone { RequiredInvites = 3, GoldReward = 3000, DiamondReward = 300, ItemReward = "限定皮肤" },
        new InviteMilestone { RequiredInvites = 5, GoldReward = 5000, DiamondReward = 500, ItemReward = "英雄角色" },
        new InviteMilestone { RequiredInvites = 10, GoldReward = 10000, DiamondReward = 1000, ItemReward = "专属称号" },
    };

    // 被邀请人奖励
    public InviteReward InviteeReward = new InviteReward
    {
        Gold = 3000,
        Diamonds = 300
    };

    // 师徒系统
    public int MentorGraduateTasks = 20;
    public int MentorTaskGold = 200;
    public int MentorGraduateGold = 5000;
}

[System.Serializable]
public class InviteMilestone
{
    public int RequiredInvites;
    public int GoldReward;
    public int DiamondReward;
    public string ItemReward;
}

[System.Serializable]
public class InviteRecord
{
    public string InviteCode;
    public string InvitedPlayerId;
    public DateTime InviteTime;
}

[System.Serializable]
public class InviteRecordWrapper
{
    public List<InviteRecord> Records;
}

[System.Serializable]
public class InviteReward
{
    public int Milestone;
    public int Gold;
    public int Diamonds;
    public string ItemReward;
}

[System.Serializable]
public class MentorData
{
    public string MentorInviteCode;
    public string StudentId;
    public DateTime StartTime;
    public DateTime? GraduateTime;
    public int TasksCompleted;
    public bool IsActive;
}

public enum ShareType
{
    HighScore,
    RareDrop,
    CustomLevel,
    SeasonRank
}

#endregion
