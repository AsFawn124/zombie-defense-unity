using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 异步竞技场系统 - TASK-028~030
/// 防守阵容保存、挑战匹配、战斗回放、积分排名、赛季重置
/// </summary>
public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance;

    [Header("竞技场配置")]
    public ArenaConfig Config;

    // 玩家竞技数据
    private ArenaPlayerData _playerData;
    private List<ArenaOpponent> _opponents = new List<ArenaOpponent>();
    private Dictionary<string, ArenaBattleRecord> _battleRecords = new Dictionary<string, ArenaBattleRecord>();

    // 事件
    public event Action<ArenaBattleResult> OnBattleComplete;
    public event Action<ArenaRank> OnRankChanged;
    public event Action<int> OnELOChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadPlayerData();
        RefreshOpponents();
        CheckSeasonReset();
    }

    #region === 防守阵容管理 (TASK-028) ===

    /// <summary>
    /// 保存防守阵容
    /// </summary>
    public void SaveDefenseFormation(ArenaDefenseFormation formation)
    {
        _playerData.DefenseFormation = formation;
        _playerData.LastDefenseUpdate = DateTime.UtcNow;
        PlayerPrefs.SetString("arena_defense", JsonUtility.ToJson(formation));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 加载防守阵容
    /// </summary>
    public ArenaDefenseFormation LoadDefenseFormation()
    {
        string json = PlayerPrefs.GetString("arena_defense", "");
        if (!string.IsNullOrEmpty(json))
        {
            return JsonUtility.FromJson<ArenaDefenseFormation>(json);
        }
        return CreateDefaultFormation();
    }

    private ArenaDefenseFormation CreateDefaultFormation()
    {
        return new ArenaDefenseFormation
        {
            TowerPositions = new List<Vector2>(),
            SelectedSkills = new List<int>(),
            HeroId = -1,
            TacticalItem = -1
        };
    }

    #endregion

    #region === 挑战匹配 (TASK-028) ===

    /// <summary>
    /// 刷新对手列表
    /// </summary>
    public void RefreshOpponents()
    {
        _opponents.Clear();

        // 基于ELO匹配相近实力的对手
        int playerELO = _playerData.ELO;

        for (int i = 0; i < Config.OpponentsPerRefresh; i++)
        {
            // 对手ELO在±200范围内随机
            int opponentELO = playerELO + UnityEngine.Random.Range(-200, 200);
            opponentELO = Mathf.Max(Config.StartingELO, opponentELO);

            var opponent = GenerateOpponent(i, opponentELO);
            _opponents.Add(opponent);
        }

        // 按ELO排序
        _opponents.Sort((a, b) => b.ELO.CompareTo(a.ELO));
    }

    private ArenaOpponent GenerateOpponent(int index, int elo)
    {
        return new ArenaOpponent
        {
            OpponentId = $"opp_{DateTime.UtcNow.Ticks}_{index}",
            PlayerName = $"挑战者{index + 1}",
            ELO = elo,
            Rank = GetRankFromELO(elo),
            DefenseFormation = GenerateRandomFormation(),
            Power = CalculatePower(elo)
        };
    }

    private ArenaRank GetRankFromELO(int elo)
    {
        if (elo >= Config.KingELO) return ArenaRank.King;
        if (elo >= Config.MasterELO) return ArenaRank.Master;
        if (elo >= Config.DiamondELO) return ArenaRank.Diamond;
        if (elo >= Config.PlatinumELO) return ArenaRank.Platinum;
        if (elo >= Config.GoldELO) return ArenaRank.Gold;
        if (elo >= Config.SilverELO) return ArenaRank.Silver;
        return ArenaRank.Bronze;
    }

    private ArenaDefenseFormation GenerateRandomFormation()
    {
        var formation = new ArenaDefenseFormation();
        int towerCount = UnityEngine.Random.Range(3, 7);
        for (int i = 0; i < towerCount; i++)
        {
            formation.TowerPositions.Add(new Vector2(
                UnityEngine.Random.Range(-4f, 4f),
                UnityEngine.Random.Range(-3f, 3f)
            ));
        }
        return formation;
    }

    private int CalculatePower(int elo)
    {
        return elo * 10 + UnityEngine.Random.Range(1000, 5000);
    }

    #endregion

    #region === 战斗回放 (TASK-028) ===

    /// <summary>
    /// 记录战斗过程
    /// </summary>
    public void RecordBattleEvent(ArenaBattleEvent battleEvent)
    {
        if (!_battleRecords.ContainsKey(battleEvent.BattleId))
        {
            _battleRecords[battleEvent.BattleId] = new ArenaBattleRecord
            {
                BattleId = battleEvent.BattleId,
                StartTime = DateTime.UtcNow
            };
        }
        _battleRecords[battleEvent.BattleId].Events.Add(battleEvent);
    }

    /// <summary>
    /// 获取战斗回放
    /// </summary>
    public ArenaBattleRecord GetBattleRecord(string battleId)
    {
        _battleRecords.TryGetValue(battleId, out var record);
        return record;
    }

    /// <summary>
    /// 回放战斗
    /// </summary>
    public System.Collections.IEnumerator ReplayBattle(string battleId, Action<ArenaBattleEvent> onEvent)
    {
        var record = GetBattleRecord(battleId);
        if (record == null) yield break;

        DateTime startTime = record.StartTime;
        foreach (var evt in record.Events)
        {
            float delay = (float)(evt.Timestamp - startTime).TotalSeconds;
            yield return new WaitForSeconds(delay);
            onEvent?.Invoke(evt);
        }
    }

    #endregion

    #region === 积分系统 (TASK-029) ===

    /// <summary>
    /// 计算ELO变化
    /// </summary>
    public int CalculateELOChange(int playerELO, int opponentELO, bool isWin)
    {
        float expectedScore = 1f / (1f + Mathf.Pow(10f, (opponentELO - playerELO) / 400f));
        float actualScore = isWin ? 1f : 0f;
        int eloChange = Mathf.RoundToInt(Config.KFactor * (actualScore - expectedScore));
        return Mathf.Clamp(eloChange, Config.MinELOChange, Config.MaxELOChange);
    }

    /// <summary>
    /// 结算战斗
    /// </summary>
    public ArenaBattleResult SettleBattle(string opponentId, bool isWin, int remainingHealth)
    {
        var opponent = _opponents.Find(o => o.OpponentId == opponentId);
        if (opponent == null) return null;

        int eloChange = CalculateELOChange(_playerData.ELO, opponent.ELO, isWin);

        var result = new ArenaBattleResult
        {
            Opponent = opponent,
            IsWin = isWin,
            ELOChange = eloChange,
            Rewards = CalculateRewards(isWin, opponent.Rank),
            NewELO = _playerData.ELO + eloChange,
            NewRank = GetRankFromELO(_playerData.ELO + eloChange)
        };

        // 更新数据
        ArenaRank oldRank = _playerData.Rank;
        _playerData.ELO += eloChange;
        _playerData.Rank = GetRankFromELO(_playerData.ELO);
        _playerData.BattlesFought++;
        if (isWin) _playerData.BattlesWon++;
        _playerData.LastBattleTime = DateTime.UtcNow;

        SavePlayerData();

        // 事件触发
        OnBattleComplete?.Invoke(result);
        OnELOChanged?.Invoke(eloChange);
        if (oldRank != _playerData.Rank)
        {
            OnRankChanged?.Invoke(_playerData.Rank);
        }

        return result;
    }

    private ArenaRewards CalculateRewards(bool isWin, ArenaRank opponentRank)
    {
        var rewards = new ArenaRewards
        {
            ArenaCoins = isWin ? Config.WinArenaCoins : Config.LossArenaCoins,
            Gold = isWin ? Config.WinGold : 0,
            TrophyPoints = isWin ? Config.WinTrophyPoints : 0
        };

        // 排名越高奖励越多
        int rankMultiplier = (int)opponentRank + 1;
        rewards.ArenaCoins *= rankMultiplier;
        rewards.Gold *= rankMultiplier;

        return rewards;
    }

    #endregion

    #region === 赛季系统 (TASK-029) ===

    /// <summary>
    /// 检查赛季重置
    /// </summary>
    private void CheckSeasonReset()
    {
        DateTime now = DateTime.UtcNow;
        if (_playerData.SeasonEndTime < now)
        {
            // 赛季结算奖励
            GrantSeasonRewards(_playerData.Rank);

            // 重置排名
            _playerData.ELO = Mathf.RoundToInt((_playerData.ELO + Config.StartingELO) / 2f);
            _playerData.Rank = GetRankFromELO(_playerData.ELO);
            _playerData.SeasonEndTime = now.AddDays(Config.SeasonDurationDays);
            _playerData.SeasonNumber++;

            SavePlayerData();
        }
    }

    private void GrantSeasonRewards(ArenaRank finalRank)
    {
        var rewards = Config.GetSeasonRewards(finalRank);
        // 发放奖励（钻石、限定皮肤、头像框等）
        GameManager.Instance?.AddGold(rewards.Gold);
        Debug.Log($"[Arena] 赛季{_playerData.SeasonNumber}结束，段位:{finalRank}，奖励发放完成");
    }

    #endregion

    #region === 数据持久化 ===

    private void LoadPlayerData()
    {
        string json = PlayerPrefs.GetString("arena_player_data", "");
        if (!string.IsNullOrEmpty(json))
        {
            _playerData = JsonUtility.FromJson<ArenaPlayerData>(json);
        }
        else
        {
            _playerData = new ArenaPlayerData
            {
                ELO = Config.StartingELO,
                Rank = ArenaRank.Bronze,
                SeasonEndTime = DateTime.UtcNow.AddDays(Config.SeasonDurationDays),
                SeasonNumber = 1
            };
        }
    }

    private void SavePlayerData()
    {
        PlayerPrefs.SetString("arena_player_data", JsonUtility.ToJson(_playerData));
        PlayerPrefs.Save();
    }

    #endregion

    #region === 公共属性 ===

    public ArenaPlayerData PlayerData => _playerData;
    public List<ArenaOpponent> Opponents => _opponents;
    public int OpponentCount => _opponents.Count;
    public ArenaOpponent GetOpponent(int index) => index < _opponents.Count ? _opponents[index] : null;

    #endregion
}

#region === 数据结构 ===

[System.Serializable]
public class ArenaConfig
{
    [Header("匹配")]
    public int OpponentsPerRefresh = 5;
    public int RefreshCooldownMinutes = 30;

    [Header("ELO")]
    public int StartingELO = 1000;
    public float KFactor = 32f;
    public int MinELOChange = 5;
    public int MaxELOChange = 50;

    [Header("段位阈值")]
    public int SilverELO = 1100;
    public int GoldELO = 1300;
    public int PlatinumELO = 1600;
    public int DiamondELO = 2000;
    public int MasterELO = 2500;
    public int KingELO = 3000;

    [Header("奖励")]
    public int WinArenaCoins = 50;
    public int LossArenaCoins = 10;
    public int WinGold = 200;
    public int WinTrophyPoints = 10;

    [Header("赛季")]
    public int SeasonDurationDays = 56; // 8周

    public ArenaRewards GetSeasonRewards(ArenaRank rank)
    {
        return rank switch
        {
            ArenaRank.King => new ArenaRewards { Gold = 10000, Diamonds = 500, TitleReward = "王者" },
            ArenaRank.Master => new ArenaRewards { Gold = 5000, Diamonds = 300, TitleReward = "大师" },
            ArenaRank.Diamond => new ArenaRewards { Gold = 3000, Diamonds = 200, TitleReward = "钻石" },
            ArenaRank.Platinum => new ArenaRewards { Gold = 1500, Diamonds = 100, TitleReward = "铂金" },
            ArenaRank.Gold => new ArenaRewards { Gold = 800, Diamonds = 50, TitleReward = "黄金" },
            ArenaRank.Silver => new ArenaRewards { Gold = 400, Diamonds = 20, TitleReward = "白银" },
            _ => new ArenaRewards { Gold = 200, Diamonds = 10, TitleReward = "青铜" }
        };
    }
}

[System.Serializable]
public class ArenaPlayerData
{
    public int ELO;
    public ArenaRank Rank;
    public int BattlesFought;
    public int BattlesWon;
    public DateTime LastBattleTime;
    public DateTime SeasonEndTime;
    public int SeasonNumber;
    public ArenaDefenseFormation DefenseFormation;
    public DateTime LastDefenseUpdate;
}

[System.Serializable]
public class ArenaDefenseFormation
{
    public List<Vector2> TowerPositions;
    public List<int> SelectedSkills;
    public int HeroId;
    public int TacticalItem;
}

[System.Serializable]
public class ArenaOpponent
{
    public string OpponentId;
    public string PlayerName;
    public int ELO;
    public ArenaRank Rank;
    public ArenaDefenseFormation DefenseFormation;
    public int Power;
}

[System.Serializable]
public class ArenaBattleRecord
{
    public string BattleId;
    public DateTime StartTime;
    public List<ArenaBattleEvent> Events = new List<ArenaBattleEvent>();
}

[System.Serializable]
public class ArenaBattleEvent
{
    public string BattleId;
    public DateTime Timestamp;
    public ArenaEventType EventType;
    public string EventData; // JSON data
}

[System.Serializable]
public class ArenaBattleResult
{
    public ArenaOpponent Opponent;
    public bool IsWin;
    public int ELOChange;
    public int NewELO;
    public ArenaRank NewRank;
    public ArenaRewards Rewards;
}

[System.Serializable]
public class ArenaRewards
{
    public int ArenaCoins;
    public int Gold;
    public int Diamonds;
    public int TrophyPoints;
    public string TitleReward;
}

public enum ArenaRank
{
    Bronze = 0,
    Silver = 1,
    Gold = 2,
    Platinum = 3,
    Diamond = 4,
    Master = 5,
    King = 6
}

public enum ArenaEventType
{
    TowerPlaced,
    TowerUpgraded,
    TowerSold,
    SkillUsed,
    EnemyKilled,
    BaseDamaged,
    WaveCleared,
    BattleWon,
    BattleLost
}

#endregion
