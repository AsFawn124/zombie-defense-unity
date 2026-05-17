using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 赛季系统 - TASK-038~040
/// 赛季周期管理、主题切换、数据重置、通行证、限定内容
/// </summary>
public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance;

    [Header("赛季配置")]
    public SeasonConfig Config;

    // 赛季数据
    private SeasonPlayerData _seasonData;
    private BattlePassData _battlePassData;

    // 事件
    public event Action<Season> OnSeasonChanged;
    public event Action<int, BattlePassReward> OnPassLevelUp;
    public event Action<BattlePassReward> OnPassRewardClaimed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadSeasonData();
        CheckSeasonTransition();
    }

    #region === 赛季框架 (TASK-038) ===

    private void CheckSeasonTransition()
    {
        DateTime now = DateTime.UtcNow;
        Season currentSeason = Config.GetSeason(_seasonData.CurrentSeasonIndex);

        if (currentSeason == null || now > currentSeason.EndTime)
        {
            // 新赛季开始
            StartNewSeason();
        }
    }

    private void StartNewSeason()
    {
        int newIndex = _seasonData.CurrentSeasonIndex + 1;
        var newSeason = Config.GetSeason(newIndex);

        if (newSeason == null)
        {
            // 循环赛季
            newIndex = 0;
            newSeason = Config.GetSeason(0);
        }

        // 赛季结算
        GrantSeasonSettlementRewards(_battlePassData.PassLevel);

        // 重置数据
        _seasonData.CurrentSeasonIndex = newIndex;
        _battlePassData = new BattlePassData
        {
            PassLevel = 0,
            PassExp = 0,
            HasPremiumPass = false,
            HasDeluxePass = false,
            ClaimedRewards = new List<int>()
        };

        SaveAll();
        OnSeasonChanged?.Invoke(newSeason);

        Debug.Log($"[Season] 新赛季开始: {newSeason.SeasonName}!");
    }

    private void GrantSeasonSettlementRewards(int finalLevel)
    {
        int diamonds = finalLevel * Config.SettlementDiamondsPerLevel;
        GameManager.Instance?.AddGold(diamonds * 10); // 简化
        Debug.Log($"[Season] 赛季结算奖励: {diamonds}钻石 (等级{finalLevel})");
    }

    #endregion

    #region === 赛季通行证 (TASK-039) ===

    /// <summary>
    /// 获得通行证经验
    /// </summary>
    public void AddPassExp(int exp)
    {
        if (_battlePassData.HasDeluxePass)
        {
            exp = Mathf.RoundToInt(exp * Config.DeluxeExpMultiplier);
        }
        else if (_battlePassData.HasPremiumPass)
        {
            exp = Mathf.RoundToInt(exp * Config.PremiumExpMultiplier);
        }

        _battlePassData.PassExp += exp;

        int expPerLevel = Config.GetExpForLevel(_battlePassData.PassLevel);
        while (_battlePassData.PassExp >= expPerLevel && _battlePassData.PassLevel < Config.MaxPassLevel)
        {
            _battlePassData.PassExp -= expPerLevel;
            _battlePassData.PassLevel++;

            var reward = Config.GetFreeReward(_battlePassData.PassLevel);
            OnPassLevelUp?.Invoke(_battlePassData.PassLevel, reward);

            expPerLevel = Config.GetExpForLevel(_battlePassData.PassLevel);
        }

        SaveAll();
    }

    /// <summary>
    /// 购买通行证
    /// </summary>
    public bool PurchaseBattlePass(BattlePassType passType)
    {
        int cost = passType == BattlePassType.Premium
            ? Config.PremiumPassPrice
            : Config.DeluxePassPrice;

        // 检查支付
        if (passType == BattlePassType.Premium && _battlePassData.HasPremiumPass)
        {
            Debug.LogWarning("[Season] 已拥有高级通行证");
            return false;
        }

        if (passType == BattlePassType.Deluxe)
        {
            _battlePassData.HasDeluxePass = true;
            _battlePassData.HasPremiumPass = true; // 尊享包含高级

            // 立即提升等级
            _battlePassData.PassLevel += Config.DeluxeInstantLevels;
            _battlePassData.PassLevel = Mathf.Min(_battlePassData.PassLevel, Config.MaxPassLevel);
        }
        else
        {
            _battlePassData.HasPremiumPass = true;
        }

        SaveAll();
        return true;
    }

    /// <summary>
    /// 领取通行证奖励
    /// </summary>
    public BattlePassReward ClaimPassReward(int level)
    {
        if (_battlePassData.ClaimedRewards.Contains(level))
        {
            Debug.LogWarning($"[Season] 已领取{level}级奖励");
            return null;
        }

        var reward = GetPassReward(level);
        if (reward == null) return null;

        _battlePassData.ClaimedRewards.Add(level);

        // 发放奖励
        GameManager.Instance?.AddGold(reward.Gold);
        Debug.Log($"[Season] 领取通行证{level}级奖励: {reward.Gold}金币");

        OnPassRewardClaimed?.Invoke(reward);
        SaveAll();
        return reward;
    }

    private BattlePassReward GetPassReward(int level)
    {
        var freeReward = Config.GetFreeReward(level);
        var premiumReward = Config.GetPremiumReward(level);

        if (!_battlePassData.HasPremiumPass)
        {
            return freeReward;
        }

        // 合并免费和付费奖励
        return new BattlePassReward
        {
            Gold = freeReward.Gold + premiumReward.Gold,
            Diamonds = freeReward.Diamonds + premiumReward.Diamonds,
            Exp = freeReward.Exp,
            ItemReward = premiumReward.ItemReward,
            IsPremium = true
        };
    }

    #endregion

    #region === 赛季内容 (TASK-040) ===

    /// <summary>
    /// 获取当前赛季
    /// </summary>
    public Season GetCurrentSeason()
    {
        return Config.GetSeason(_seasonData.CurrentSeasonIndex);
    }

    /// <summary>
    /// 获取赛季限定皮肤
    /// </summary>
    public string[] GetSeasonSkins()
    {
        var season = GetCurrentSeason();
        return season?.LimitedSkins ?? new string[0];
    }

    /// <summary>
    /// 获取赛季专属敌人
    /// </summary>
    public string GetSeasonEnemy()
    {
        var season = GetCurrentSeason();
        return season?.SeasonEnemy ?? "";
    }

    /// <summary>
    /// 获取赛季机制
    /// </summary>
    public SeasonMechanic GetSeasonMechanic()
    {
        var season = GetCurrentSeason();
        return season?.Mechanic;
    }

    #endregion

    #region === 数据持久化 ===

    private void LoadSeasonData()
    {
        string json = PlayerPrefs.GetString("season_data", "");
        _seasonData = !string.IsNullOrEmpty(json)
            ? JsonUtility.FromJson<SeasonPlayerData>(json)
            : new SeasonPlayerData();

        json = PlayerPrefs.GetString("battlepass_data", "");
        _battlePassData = !string.IsNullOrEmpty(json)
            ? JsonUtility.FromJson<BattlePassData>(json)
            : new BattlePassData();
    }

    private void SaveAll()
    {
        PlayerPrefs.SetString("season_data", JsonUtility.ToJson(_seasonData));
        PlayerPrefs.SetString("battlepass_data", JsonUtility.ToJson(_battlePassData));
        PlayerPrefs.Save();
    }

    #endregion

    #region === 公共属性 ===

    public int PassLevel => _battlePassData.PassLevel;
    public int PassExp => _battlePassData.PassExp;
    public bool HasPremium => _battlePassData.HasPremiumPass;
    public bool HasDeluxe => _battlePassData.HasDeluxePass;

    #endregion
}

/// <summary>
/// UGC关卡编辑器系统 - TASK-041~043
/// 地形绘制、路径设计、波次配置、测试发布、关卡码、社区浏览、创作者收益
/// </summary>
public class UGCManager : MonoBehaviour
{
    public static UGCManager Instance;

    [Header("UGC配置")]
    public UGCConfig Config;

    private List<UGCLevel> _communityLevels = new List<UGCLevel>();
    private UGCLevel _editingLevel;
    private float _creatorEarnings;

    // 事件
    public event Action<UGCLevel> OnLevelPublished;
    public event Action<string> OnLevelCodeGenerated;
    public event Action<int> OnCreatorEarningsUpdated;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadCommunityLevels();
    }

    #region === 关卡编辑器 (TASK-041) ===

    /// <summary>
    /// 创建新关卡
    /// </summary>
    public UGCLevel CreateNewLevel(string name, string description)
    {
        _editingLevel = new UGCLevel
        {
            LevelId = Guid.NewGuid().ToString(),
            LevelName = name,
            Description = description,
            AuthorId = PlayerPrefs.GetString("player_id", "local"),
            AuthorName = PlayerPrefs.GetString("player_name", "创作者"),
            CreatedTime = DateTime.UtcNow,
            Tiles = new List<UGCTile>(),
            PathPoints = new List<Vector2>(),
            WaveConfigs = new List<UGCWaveConfig>(),
            MapSize = Config.DefaultMapSize
        };

        return _editingLevel;
    }

    /// <summary>
    /// 绘制地形
    /// </summary>
    public void PaintTile(int x, int y, UGCTileType tileType)
    {
        if (_editingLevel == null) return;

        if (x < 0 || x >= _editingLevel.MapSize.x || y < 0 || y >= _editingLevel.MapSize.y)
        {
            Debug.LogWarning("[UGC] 超出地图范围");
            return;
        }

        int existingIndex = _editingLevel.Tiles.FindIndex(t => t.X == x && t.Y == y);
        if (existingIndex >= 0)
        {
            _editingLevel.Tiles[existingIndex] = new UGCTile { X = x, Y = y, Type = tileType };
        }
        else
        {
            _editingLevel.Tiles.Add(new UGCTile { X = x, Y = y, Type = tileType });
        }
    }

    /// <summary>
    /// 擦除地形
    /// </summary>
    public void EraseTile(int x, int y)
    {
        if (_editingLevel == null) return;
        _editingLevel.Tiles.RemoveAll(t => t.X == x && t.Y == y);
    }

    /// <summary>
    /// 添加路径点
    /// </summary>
    public void AddPathPoint(Vector2 point)
    {
        if (_editingLevel == null) return;
        _editingLevel.PathPoints.Add(point);
    }

    /// <summary>
    /// 配置波次
    /// </summary>
    public void AddWaveConfig(UGCWaveConfig waveConfig)
    {
        if (_editingLevel == null) return;
        _editingLevel.WaveConfigs.Add(waveConfig);
    }

    /// <summary>
    /// 测试关卡
    /// </summary>
    public void TestLevel()
    {
        if (_editingLevel == null)
        {
            Debug.LogWarning("[UGC] 没有正在编辑的关卡");
            return;
        }

        // 验证关卡完整性
        string validationError = ValidateLevel(_editingLevel);
        if (!string.IsNullOrEmpty(validationError))
        {
            Debug.LogError($"[UGC] 关卡验证失败: {validationError}");
            return;
        }

        // 加载测试场景
        Debug.Log($"[UGC] 开始测试关卡: {_editingLevel.LevelName}");
        LoadLevelForPlay(_editingLevel);
    }

    private string ValidateLevel(UGCLevel level)
    {
        if (level.PathPoints.Count < 2)
            return "至少需要2个路径点";
        if (level.WaveConfigs.Count == 0)
            return "至少需要配置1个波次";
        if (level.WaveConfigs.Count > Config.MaxWaves)
            return $"最多{Config.MaxWaves}个波次";
        return null;
    }

    #endregion

    #region === 发布与分享 (TASK-042) ===

    /// <summary>
    /// 发布关卡
    /// </summary>
    public string PublishLevel()
    {
        if (_editingLevel == null) return null;

        string error = ValidateLevel(_editingLevel);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"[UGC] 无法发布: {error}");
            return null;
        }

        // 生成关卡码
        string levelCode = GenerateLevelCode();
        _editingLevel.LevelCode = levelCode;
        _editingLevel.PublishTime = DateTime.UtcNow;
        _editingLevel.IsPublished = true;

        _communityLevels.Add(_editingLevel);

        OnLevelPublished?.Invoke(_editingLevel);
        OnLevelCodeGenerated?.Invoke(levelCode);

        SaveCommunityLevels();

        Debug.Log($"[UGC] 关卡发布成功! 关卡码: {levelCode}");
        return levelCode;
    }

    private string GenerateLevelCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        string code;
        do
        {
            char[] codeChars = new char[Config.LevelCodeLength];
            for (int i = 0; i < Config.LevelCodeLength; i++)
            {
                codeChars[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            code = new string(codeChars);
        } while (FindLevelByCode(code) != null);

        return code;
    }

    /// <summary>
    /// 通过关卡码查找关卡
    /// </summary>
    public UGCLevel FindLevelByCode(string code)
    {
        return _communityLevels.Find(l => l.LevelCode == code);
    }

    /// <summary>
    /// 通过关卡码导入关卡
    /// </summary>
    public UGCLevel ImportLevelByCode(string code)
    {
        var level = FindLevelByCode(code);
        if (level == null)
        {
            Debug.LogError($"[UGC] 未找到关卡码: {code}");
            return null;
        }

        level.PlayCount++;
        UpdateCreatorEarnings(level);
        return level;
    }

    #endregion

    #region === 社区功能 (TASK-042) ===

    /// <summary>
    /// 获取推荐关卡列表
    /// </summary>
    public List<UGCLevel> GetRecommendedLevels(int count = 10)
    {
        var sorted = new List<UGCLevel>(_communityLevels);
        sorted.Sort((a, b) => b.Likes.CompareTo(a.Likes));
        return sorted.GetRange(0, Mathf.Min(count, sorted.Count));
    }

    /// <summary>
    /// 点赞关卡
    /// </summary>
    public void LikeLevel(string levelCode)
    {
        var level = FindLevelByCode(levelCode);
        if (level == null) return;

        if (PlayerPrefs.GetInt($"liked_{levelCode}", 0) == 1)
        {
            Debug.LogWarning("[UGC] 已经点过赞了");
            return;
        }

        level.Likes++;
        PlayerPrefs.SetInt($"liked_{levelCode}", 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 评论关卡
    /// </summary>
    public void CommentLevel(string levelCode, string comment)
    {
        var level = FindLevelByCode(levelCode);
        if (level == null) return;

        level.Comments.Add(new UGCComment
        {
            AuthorName = PlayerPrefs.GetString("player_name", "玩家"),
            Content = comment,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region === 创作者收益 (TASK-043) ===

    private void UpdateCreatorEarnings(UGCLevel level)
    {
        int earnings = Mathf.RoundToInt(level.PlayCount * Config.DiamondsPerPlay);
        level.CreatorEarnings += earnings;

        // 累计达到阈值给予额外奖励
        if (level.PlayCount >= Config.FeaturedPlayThreshold)
        {
            level.IsFeatured = true;
            level.CreatorEarnings += Config.FeaturedBonus;
        }

        OnCreatorEarningsUpdated?.Invoke(level.CreatorEarnings);
    }

    /// <summary>
    /// 获取创作者排行榜
    /// </summary>
    public List<CreatorRankEntry> GetCreatorLeaderboard(int count = 10)
    {
        var entries = new Dictionary<string, CreatorRankEntry>();

        foreach (var level in _communityLevels)
        {
            if (!entries.ContainsKey(level.AuthorId))
            {
                entries[level.AuthorId] = new CreatorRankEntry
                {
                    AuthorId = level.AuthorId,
                    AuthorName = level.AuthorName,
                    TotalPlays = 0,
                    TotalLikes = 0,
                    TotalEarnings = 0
                };
            }

            var entry = entries[level.AuthorId];
            entry.TotalPlays += level.PlayCount;
            entry.TotalLikes += level.Likes;
            entry.TotalEarnings += level.CreatorEarnings;
        }

        var leaderboard = new List<CreatorRankEntry>(entries.Values);
        leaderboard.Sort((a, b) => b.TotalEarnings.CompareTo(a.TotalEarnings));
        return leaderboard.GetRange(0, Mathf.Min(count, leaderboard.Count));
    }

    #endregion

    #region === 工具方法 ===

    private void LoadLevelForPlay(UGCLevel level)
    {
        // 在测试/实际游戏中加载关卡配置
        PathManager.Instance?.LoadCustomPath(level.PathPoints);
        WaveManager.Instance?.LoadCustomWaves(level.WaveConfigs);
    }

    private void LoadCommunityLevels()
    {
        string json = PlayerPrefs.GetString("community_levels", "");
        if (!string.IsNullOrEmpty(json))
        {
            var wrapper = JsonUtility.FromJson<UGCLevelWrapper>(json);
            _communityLevels = wrapper?.Levels ?? new List<UGCLevel>();
        }
    }

    private void SaveCommunityLevels()
    {
        var wrapper = new UGCLevelWrapper { Levels = _communityLevels };
        PlayerPrefs.SetString("community_levels", JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    #endregion

    #region === 公共属性 ===

    public UGCLevel EditingLevel => _editingLevel;
    public List<UGCLevel> CommunityLevels => _communityLevels;
    public float CreatorEarnings => _creatorEarnings;

    #endregion
}

#region === 赛季数据结构 ===

[System.Serializable]
public class SeasonConfig
{
    public int MaxPassLevel = 50;
    public int PremiumPassPrice = 30;
    public int DeluxePassPrice = 68;
    public int DeluxeInstantLevels = 20;
    public float PremiumExpMultiplier = 1.2f;
    public float DeluxeExpMultiplier = 1.2f;
    public int SettlementDiamondsPerLevel = 10;
    public int BaseExpPerLevel = 100;
    public int ExpIncreasePerLevel = 20;

    public Season[] Seasons;
    public BattlePassReward[] FreeRewards;
    public BattlePassReward[] PremiumRewards;

    public Season GetSeason(int index) => index < Seasons?.Length ? Seasons[index] : null;

    public int GetExpForLevel(int level) => BaseExpPerLevel + level * ExpIncreasePerLevel;

    public BattlePassReward GetFreeReward(int level)
    {
        return level < FreeRewards?.Length ? FreeRewards[level] : new BattlePassReward { Gold = 100, Exp = 0 };
    }

    public BattlePassReward GetPremiumReward(int level)
    {
        return level < PremiumRewards?.Length ? PremiumRewards[level] : new BattlePassReward { Gold = 300, Diamonds = 30 };
    }
}

[System.Serializable]
public class Season
{
    public int SeasonIndex;
    public string SeasonName;
    public string SeasonTheme;
    public string[] LimitedSkins;
    public string SeasonEnemy;
    public SeasonMechanic Mechanic;
    public DateTime StartTime;
    public DateTime EndTime;
}

[System.Serializable]
public class SeasonMechanic
{
    public string MechanicName;
    public string Description;
    public float EffectValue;
}

[System.Serializable]
public class BattlePassReward
{
    public int Gold;
    public int Diamonds;
    public int Exp;
    public string ItemReward;
    public bool IsPremium;
}

[System.Serializable]
public class SeasonPlayerData
{
    public int CurrentSeasonIndex;
}

[System.Serializable]
public class BattlePassData
{
    public int PassLevel;
    public int PassExp;
    public bool HasPremiumPass;
    public bool HasDeluxePass;
    public List<int> ClaimedRewards = new List<int>();
}

public enum BattlePassType
{
    Free,
    Premium,
    Deluxe
}

#endregion

#region === UGC数据结构 ===

[System.Serializable]
public class UGCConfig
{
    public Vector2Int DefaultMapSize = new Vector2Int(20, 15);
    public int MaxWaves = 30;
    public int LevelCodeLength = 8;
    public float DiamondsPerPlay = 0.5f;
    public int FeaturedPlayThreshold = 100;
    public int FeaturedBonus = 500;
}

[System.Serializable]
public class UGCLevel
{
    public string LevelId;
    public string LevelName;
    public string Description;
    public string LevelCode;
    public string AuthorId;
    public string AuthorName;
    public DateTime CreatedTime;
    public DateTime? PublishTime;
    public bool IsPublished;
    public bool IsFeatured;

    // 关卡数据
    public Vector2Int MapSize;
    public List<UGCTile> Tiles = new List<UGCTile>();
    public List<Vector2> PathPoints = new List<Vector2>();
    public List<UGCWaveConfig> WaveConfigs = new List<UGCWaveConfig>();

    // 社区数据
    public int PlayCount;
    public int Likes;
    public List<UGCComment> Comments = new List<UGCComment>();
    public int CreatorEarnings;
}

[System.Serializable]
public class UGCTile
{
    public int X;
    public int Y;
    public UGCTileType Type;
}

[System.Serializable]
public class UGCWaveConfig
{
    public int WaveNumber;
    public string EnemyType;
    public int EnemyCount;
    public float SpawnInterval;
    public float StartDelay;
    public Vector2 SpawnPoint;
}

[System.Serializable]
public class UGCComment
{
    public string AuthorName;
    public string Content;
    public DateTime Timestamp;
}

[System.Serializable]
public class CreatorRankEntry
{
    public string AuthorId;
    public string AuthorName;
    public int TotalPlays;
    public int TotalLikes;
    public int TotalEarnings;
}

[System.Serializable]
public class UGCLevelWrapper
{
    public List<UGCLevel> Levels;
}

public enum UGCTileType
{
    Empty,
    Path,
    Buildable,
    Obstacle,
    LavaZone,
    IceGround,
    HighGround,
    Portal
}

#endregion
