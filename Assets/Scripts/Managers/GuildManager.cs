using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 公会系统 - TASK-033~035
/// 创建/加入公会、成员管理、公会聊天、公会任务、公会商店、公会科技、公会战
/// </summary>
public class GuildManager : MonoBehaviour
{
    public static GuildManager Instance;

    [Header("公会配置")]
    public GuildConfig Config;

    // 公会数据
    private Dictionary<string, Guild> _guilds = new Dictionary<string, Guild>();
    private Guild _myGuild;
    private GuildMemberData _myMemberData;

    // 聊天
    private List<GuildChatMessage> _chatMessages = new List<GuildChatMessage>();
    private const int MaxChatMessages = 200;

    // 事件
    public event Action<Guild> OnGuildCreated;
    public event Action<Guild> OnGuildJoined;
    public event Action OnGuildLeft;
    public event Action<GuildChatMessage> OnChatMessage;
    public event Action<GuildWarResult> OnGuildWarEnd;
    public event Action<GuildTask> OnTaskCompleted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadGuildData();
    }

    #region === 公会基础功能 (TASK-033) ===

    /// <summary>
    /// 创建公会
    /// </summary>
    public Guild CreateGuild(string name, string tag, string description, GuildType type)
    {
        if (_myGuild != null)
        {
            Debug.LogWarning("[Guild] 你已经在公会中");
            return null;
        }

        // 检查消耗
        if (!CanAffordCreateCost())
        {
            Debug.LogWarning("[Guild] 钻石不足，创建公会需要200钻石");
            return null;
        }

        DeductCreateCost();

        var guild = new Guild
        {
            GuildId = GenerateGuildId(),
            Name = name,
            Tag = tag,
            Description = description,
            Type = type,
            Level = 1,
            CreatedTime = DateTime.UtcNow,
            LeaderId = GetPlayerId()
        };

        // 添加创建者为会长
        var memberData = CreateMemberData(GuildRole.Leader);
        guild.Members.Add(memberData);

        // 初始化
        guild.Shop = new GuildShop();
        guild.Tech = new GuildTech();
        guild.WarStatus = GuildWarStatus.Peace;

        _guilds[guild.GuildId] = guild;
        _myGuild = guild;
        _myMemberData = memberData;

        SaveGuildData();
        OnGuildCreated?.Invoke(guild);

        return guild;
    }

    /// <summary>
    /// 加入公会
    /// </summary>
    public void JoinGuild(string guildId)
    {
        if (_myGuild != null)
        {
            Debug.LogWarning("[Guild] 你已经在公会中");
            return;
        }

        if (!_guilds.TryGetValue(guildId, out var guild))
        {
            Debug.LogError($"[Guild] 公会不存在: {guildId}");
            return;
        }

        if (guild.Members.Count >= GetMaxMembers(guild.Level))
        {
            Debug.LogWarning("[Guild] 公会人数已满");
            return;
        }

        var memberData = CreateMemberData(GuildRole.Member);
        guild.Members.Add(memberData);
        _myGuild = guild;
        _myMemberData = memberData;

        SaveGuildData();
        OnGuildJoined?.Invoke(guild);
    }

    /// <summary>
    /// 离开公会
    /// </summary>
    public void LeaveGuild()
    {
        if (_myGuild == null) return;

        // 会长需要转让或解散
        if (_myMemberData.Role == GuildRole.Leader)
        {
            if (_myGuild.Members.Count > 1)
            {
                Debug.LogWarning("[Guild] 会长请先转让会长再离开");
                return;
            }
        }

        _myGuild.Members.RemoveAll(m => m.PlayerId == GetPlayerId());
        _guilds.Remove(_myGuild.GuildId);

        var oldGuild = _myGuild;
        _myGuild = null;
        _myMemberData = null;

        SaveGuildData();
        OnGuildLeft?.Invoke();
    }

    /// <summary>
    /// 成员管理：晋升/降级/踢出
    /// </summary>
    public void ManageMember(string targetPlayerId, GuildManageAction action)
    {
        if (_myGuild == null || _myMemberData.Role != GuildRole.Leader) return;

        var target = _myGuild.Members.Find(m => m.PlayerId == targetPlayerId);
        if (target == null) return;

        switch (action)
        {
            case GuildManageAction.Promote:
                if (target.Role == GuildRole.Member)
                    target.Role = GuildRole.Elder;
                else if (target.Role == GuildRole.Elder)
                    target.Role = GuildRole.ViceLeader;
                break;

            case GuildManageAction.Demote:
                if (target.Role == GuildRole.ViceLeader)
                    target.Role = GuildRole.Elder;
                else if (target.Role == GuildRole.Elder)
                    target.Role = GuildRole.Member;
                break;

            case GuildManageAction.Kick:
                _myGuild.Members.Remove(target);
                break;

            case GuildManageAction.TransferLeadership:
                // 转让会长
                var oldLeader = _myGuild.Members.Find(m => m.PlayerId == GetPlayerId());
                if (oldLeader != null) oldLeader.Role = GuildRole.Elder;
                target.Role = GuildRole.Leader;
                _myGuild.LeaderId = targetPlayerId;
                _myMemberData = target;
                break;
        }

        SaveGuildData();
    }

    /// <summary>
    /// 公会聊天
    /// </summary>
    public void SendChatMessage(string message)
    {
        if (_myGuild == null) return;

        var chatMsg = new GuildChatMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            PlayerId = GetPlayerId(),
            PlayerName = _myMemberData.PlayerName,
            Role = _myMemberData.Role,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        _chatMessages.Add(chatMsg);
        if (_chatMessages.Count > MaxChatMessages)
        {
            _chatMessages.RemoveAt(0);
        }

        OnChatMessage?.Invoke(chatMsg);
    }

    #endregion

    #region === 公会任务 (TASK-034) ===

    /// <summary>
    /// 获取今日公会任务
    /// </summary>
    public List<GuildTask> GetDailyTasks()
    {
        if (_myGuild == null) return new List<GuildTask>();

        // 检查是否刷新
        if (_myGuild.LastTaskRefresh.Date != DateTime.UtcNow.Date)
        {
            RefreshDailyTasks();
        }

        return _myGuild.DailyTasks;
    }

    private void RefreshDailyTasks()
    {
        _myGuild.DailyTasks.Clear();

        int taskCount = Config.BaseTaskCount + _myGuild.Level;
        var availableTasks = new List<GuildTask>(Config.TaskPool);

        for (int i = 0; i < Mathf.Min(taskCount, availableTasks.Count); i++)
        {
            int index = UnityEngine.Random.Range(0, availableTasks.Count);
            var task = availableTasks[index];
            task.TaskId = $"task_{_myGuild.GuildId}_{DateTime.UtcNow:yyyyMMdd}_{i}";
            task.Progress = 0;
            task.IsCompleted = false;
            task.IsClaimed = false;
            _myGuild.DailyTasks.Add(task);
            availableTasks.RemoveAt(index);
        }

        _myGuild.LastTaskRefresh = DateTime.UtcNow;
        SaveGuildData();
    }

    /// <summary>
    /// 更新任务进度
    /// </summary>
    public void UpdateTaskProgress(string taskId, int amount)
    {
        if (_myGuild == null) return;

        var task = _myGuild.DailyTasks.Find(t => t.TaskId == taskId);
        if (task == null || task.IsClaimed) return;

        task.Progress += amount;
        if (task.Progress >= task.TargetAmount)
        {
            task.Progress = task.TargetAmount;
            task.IsCompleted = true;
            OnTaskCompleted?.Invoke(task);
        }
    }

    /// <summary>
    /// 领取任务奖励
    /// </summary>
    public void ClaimTaskReward(string taskId)
    {
        var task = _myGuild.DailyTasks.Find(t => t.TaskId == taskId);
        if (task == null || !task.IsCompleted || task.IsClaimed) return;

        task.IsClaimed = true;
        _myGuild.ContributionPoints += task.ContributionReward;
        _myMemberData.WeeklyContribution += task.ContributionReward;

        SaveGuildData();
    }

    #endregion

    #region === 公会商店 (TASK-034) ===

    /// <summary>
    /// 公会商店购买
    /// </summary>
    public bool BuyGuildShopItem(string itemId)
    {
        if (_myGuild == null) return false;

        var item = _myGuild.Shop.Items.Find(i => i.ItemId == itemId);
        if (item == null) return false;

        if (item.PurchasedCount >= item.MaxPurchase)
        {
            Debug.LogWarning("[Guild] 该物品已购买上限");
            return false;
        }

        if (_myMemberData.GuildCoins < item.Price)
        {
            Debug.LogWarning("[Guild] 公会币不足");
            return false;
        }

        _myMemberData.GuildCoins -= item.Price;
        item.PurchasedCount++;

        // 发放物品
        GrantItem(item.RewardItemId, item.RewardAmount);

        SaveGuildData();
        return true;
    }

    private void GrantItem(string itemId, int amount)
    {
        // 根据物品类型发放
        GameManager.Instance?.AddGold(amount);
        Debug.Log($"[Guild] 获得物品: {itemId} x{amount}");
    }

    #endregion

    #region === 公会科技 (TASK-034) ===

    /// <summary>
    /// 升级公会科技
    /// </summary>
    public bool UpgradeGuildTech(GuildTechType techType)
    {
        if (_myGuild == null) return false;
        if (_myMemberData.Role != GuildRole.Leader && _myMemberData.Role != GuildRole.ViceLeader)
        {
            Debug.LogWarning("[Guild] 只有会长/副会长可以升级科技");
            return false;
        }

        var tech = _myGuild.Tech.GetTech(techType);
        if (tech == null || tech.Level >= tech.MaxLevel) return false;

        int cost = Config.GetTechUpgradeCost(tech.Level);
        if (_myGuild.ContributionPoints < cost)
        {
            Debug.LogWarning("[Guild] 公会贡献不足");
            return false;
        }

        _myGuild.ContributionPoints -= cost;
        tech.Level++;

        SaveGuildData();
        return true;
    }

    /// <summary>
    /// 获取公会科技加成值
    /// </summary>
    public float GetTechBonus(GuildTechType techType)
    {
        if (_myGuild == null) return 0f;

        var tech = _myGuild.Tech.GetTech(techType);
        if (tech == null) return 0f;

        return Config.GetTechBonusValue(techType) * tech.Level;
    }

    #endregion

    #region === 公会战 (TASK-035) ===

    /// <summary>
    /// 宣战
    /// </summary>
    public void DeclareWar(string targetGuildId, GuildWarType warType)
    {
        if (_myGuild == null || _myMemberData.Role != GuildRole.Leader) return;
        if (_myGuild.WarStatus != GuildWarStatus.Peace)
        {
            Debug.LogWarning("[Guild] 公会正在进行其他战争");
            return;
        }

        if (!_guilds.TryGetValue(targetGuildId, out var targetGuild)) return;

        _myGuild.WarStatus = GuildWarStatus.AtWar;
        _myGuild.WarOpponentId = targetGuildId;
        _myGuild.WarType = warType;
        _myGuild.WarStartTime = DateTime.UtcNow;
        _myGuild.WarScore = 0;

        targetGuild.WarStatus = GuildWarStatus.AtWar;
        targetGuild.WarOpponentId = _myGuild.GuildId;
        targetGuild.WarScore = 0;

        SaveGuildData();
    }

    /// <summary>
    /// 上报公会战得分
    /// </summary>
    public void ReportWarScore(int score)
    {
        if (_myGuild == null || _myGuild.WarStatus != GuildWarStatus.AtWar) return;

        _myGuild.WarScore += score;
        _myMemberData.WarScore += score;

        CheckWarEnd();
        SaveGuildData();
    }

    private void CheckWarEnd()
    {
        var elapsed = DateTime.UtcNow - _myGuild.WarStartTime;
        float warHours = _myGuild.WarType switch
        {
            GuildWarType.Siege => Config.SiegeWarHours,
            GuildWarType.Defense => Config.DefenseWarHours,
            GuildWarType.Resource => Config.ResourceWarHours,
            _ => 24f
        };

        if (elapsed.TotalHours >= warHours)
        {
            EndWar();
        }
    }

    private void EndWar()
    {
        if (!_guilds.TryGetValue(_myGuild.WarOpponentId, out var opponent)) return;

        bool isWin = _myGuild.WarScore > opponent.WarScore;
        bool isDraw = _myGuild.WarScore == opponent.WarScore;

        var result = new GuildWarResult
        {
            AttackerGuildId = _myGuild.GuildId,
            DefenderGuildId = opponent.GuildId,
            AttackerScore = _myGuild.WarScore,
            DefenderScore = opponent.WarScore,
            IsAttackerWin = isWin,
            IsDraw = isDraw,
            WarType = _myGuild.WarType,
            EndTime = DateTime.UtcNow
        };

        // 发放奖励
        if (isWin)
        {
            _myGuild.ContributionPoints += Config.WarWinContribution;
            foreach (var member in _myGuild.Members)
            {
                member.GuildCoins += Config.WarWinCoins;
            }
        }

        // 重置战争状态
        _myGuild.WarStatus = GuildWarStatus.Peace;
        _myGuild.WarOpponentId = null;
        opponent.WarStatus = GuildWarStatus.Peace;
        opponent.WarOpponentId = null;

        OnGuildWarEnd?.Invoke(result);
        SaveGuildData();
    }

    #endregion

    #region === 工具方法 ===

    private GuildMemberData CreateMemberData(GuildRole role)
    {
        return new GuildMemberData
        {
            PlayerId = GetPlayerId(),
            PlayerName = PlayerPrefs.GetString("player_name", "冒险者"),
            Role = role,
            JoinTime = DateTime.UtcNow,
            GuildCoins = 0,
            WeeklyContribution = 0
        };
    }

    private int GetMaxMembers(int guildLevel)
    {
        return Config.BaseMaxMembers + guildLevel * Config.MembersPerLevel;
    }

    private bool CanAffordCreateCost()
    {
        return true; // 简化：默认可创建
    }

    private void DeductCreateCost()
    {
        // 简化
    }

    private string GenerateGuildId()
    {
        return $"GUILD_{DateTime.UtcNow.Ticks:X8}";
    }

    private string GetPlayerId()
    {
        return PlayerPrefs.GetString("player_id", "local_player");
    }

    private void LoadGuildData()
    {
        // 从PlayerPrefs加载公会数据
        // 实际项目中从服务器加载
    }

    private void SaveGuildData()
    {
        // 保存到PlayerPrefs/服务器
    }

    #endregion

    #region === 公共属性 ===

    public Guild MyGuild => _myGuild;
    public GuildMemberData MyMemberData => _myMemberData;
    public bool IsInGuild => _myGuild != null;
    public List<GuildChatMessage> ChatMessages => _chatMessages;

    #endregion
}

#region === 数据结构 ===

[System.Serializable]
public class GuildConfig
{
    public int BaseMaxMembers = 20;
    public int MembersPerLevel = 5;
    public int BaseTaskCount = 3;
    public GuildTask[] TaskPool;

    public float SiegeWarHours = 24f;
    public float DefenseWarHours = 48f;
    public float ResourceWarHours = 12f;

    public int WarWinContribution = 5000;
    public int WarWinCoins = 200;

    public int[] TechUpgradeCosts = { 100, 300, 600, 1000, 1500, 2200, 3000, 4000, 5000, 6000 };

    public int GetTechUpgradeCost(int currentLevel)
    {
        return currentLevel < TechUpgradeCosts.Length ? TechUpgradeCosts[currentLevel] : 10000;
    }

    public float GetTechBonusValue(GuildTechType type)
    {
        return type switch
        {
            GuildTechType.Damage => 0.05f,
            GuildTechType.Defense => 0.05f,
            GuildTechType.Gold => 0.08f,
            GuildTechType.Experience => 0.08f,
            GuildTechType.Health => 0.03f,
            _ => 0.05f
        };
    }
}

[System.Serializable]
public class Guild
{
    public string GuildId;
    public string Name;
    public string Tag;
    public string Description;
    public GuildType Type;
    public int Level;
    public DateTime CreatedTime;
    public string LeaderId;
    public List<GuildMemberData> Members = new List<GuildMemberData>();
    public int ContributionPoints;

    // 功能
    public List<GuildTask> DailyTasks = new List<GuildTask>();
    public DateTime LastTaskRefresh;
    public GuildShop Shop = new GuildShop();
    public GuildTech Tech = new GuildTech();

    // 战争
    public GuildWarStatus WarStatus;
    public string WarOpponentId;
    public GuildWarType WarType;
    public DateTime WarStartTime;
    public int WarScore;
}

[System.Serializable]
public class GuildMemberData
{
    public string PlayerId;
    public string PlayerName;
    public GuildRole Role;
    public DateTime JoinTime;
    public int GuildCoins;
    public int WeeklyContribution;
    public int WarScore;
}

[System.Serializable]
public class GuildTask
{
    public string TaskId;
    public string TaskName;
    public string Description;
    public GuildTaskType TaskType;
    public int TargetAmount;
    public int Progress;
    public int ContributionReward;
    public int GuildCoinReward;
    public bool IsCompleted;
    public bool IsClaimed;
}

[System.Serializable]
public class GuildShop
{
    public List<GuildShopItem> Items = new List<GuildShopItem>();
}

[System.Serializable]
public class GuildShopItem
{
    public string ItemId;
    public string ItemName;
    public int Price;
    public int MaxPurchase;
    public int PurchasedCount;
    public string RewardItemId;
    public int RewardAmount;
}

[System.Serializable]
public class GuildTech
{
    public List<GuildTechNode> TechNodes = new List<GuildTechNode>
    {
        new GuildTechNode { Type = GuildTechType.Damage, Level = 0, MaxLevel = 10 },
        new GuildTechNode { Type = GuildTechType.Defense, Level = 0, MaxLevel = 10 },
        new GuildTechNode { Type = GuildTechType.Gold, Level = 0, MaxLevel = 10 },
        new GuildTechNode { Type = GuildTechType.Experience, Level = 0, MaxLevel = 10 },
        new GuildTechNode { Type = GuildTechType.Health, Level = 0, MaxLevel = 10 }
    };

    public GuildTechNode GetTech(GuildTechType type)
    {
        return TechNodes.Find(t => t.Type == type);
    }
}

[System.Serializable]
public class GuildTechNode
{
    public GuildTechType Type;
    public int Level;
    public int MaxLevel;
}

[System.Serializable]
public class GuildChatMessage
{
    public string MessageId;
    public string PlayerId;
    public string PlayerName;
    public GuildRole Role;
    public string Message;
    public DateTime Timestamp;
}

[System.Serializable]
public class GuildWarResult
{
    public string AttackerGuildId;
    public string DefenderGuildId;
    public int AttackerScore;
    public int DefenderScore;
    public bool IsAttackerWin;
    public bool IsDraw;
    public GuildWarType WarType;
    public DateTime EndTime;
}

public enum GuildType
{
    Open,       // 公开
    Approval,   // 需要审批
    Closed      // 私有
}

public enum GuildRole
{
    Member,
    Elder,
    ViceLeader,
    Leader
}

public enum GuildManageAction
{
    Promote,
    Demote,
    Kick,
    TransferLeadership
}

public enum GuildTaskType
{
    KillEnemies,
    WinBattles,
    SpendGold,
    UpgradeTowers,
    UseSkills,
    DonateGold
}

public enum GuildTechType
{
    Damage,
    Defense,
    Gold,
    Experience,
    Health
}

public enum GuildWarStatus
{
    Peace,
    AtWar,
    Cooldown
}

public enum GuildWarType
{
    Siege,      // 攻城战
    Defense,    // 防守战
    Resource    // 资源战
}

#endregion
