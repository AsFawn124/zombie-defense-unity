using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 网络管理器 - 竞技场/公会/合作模式的网络通信层
/// 支持: REST API + WebSocket 实时通信
/// 包含: 断线重连、消息队列、心跳检测、连接池
/// </summary>
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    [Header("服务器配置")]
    public string ServerUrl = "https://api.zombie-defense.com";
    public string WebSocketUrl = "wss://ws.zombie-defense.com/game";
    public int ReconnectMaxAttempts = 5;
    public float ReconnectInterval = 3f;
    public float HeartbeatInterval = 30f;

    // 状态
    public NetworkState State { get; private set; }
    public bool IsConnected => State == NetworkState.Connected;

    private WebSocketSharp.WebSocket _ws;
    private Queue<NetworkMessage> _pendingMessages = new Queue<NetworkMessage>();
    private float _lastHeartbeatTime;
    private int _reconnectAttempts;
    private string _authToken;
    private string _playerId;

    // 事件
    public event Action<NetworkState, NetworkState> OnStateChanged;
    public event Action<string, string> OnMessageReceived; // type, json
    public event Action<int, string> OnError;
    public event Action OnReconnected;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        _playerId = PlayerPrefs.GetString("user_id", "");
        _authToken = PlayerPrefs.GetString("auth_token", "");
    }

    private void Update()
    {
        // 心跳
        if (IsConnected && Time.time - _lastHeartbeatTime > HeartbeatInterval)
        {
            SendWebSocketMessage("heartbeat", "{}");
            _lastHeartbeatTime = Time.time;
        }

        // 重连
        if (State == NetworkState.Disconnected && _reconnectAttempts < ReconnectMaxAttempts)
        {
            StartCoroutine(TryReconnect());
        }
    }

    #region === 连接管理 ===

    /// <summary>
    /// 登录并建立WebSocket连接
    /// </summary>
    public void Connect(string playerId, string token)
    {
        _playerId = playerId;
        _authToken = token;
        SetState(NetworkState.Connecting);

        try
        {
            _ws = new WebSocketSharp.WebSocket(WebSocketUrl);

            _ws.OnOpen += (sender, e) =>
            {
                SetState(NetworkState.Connected);
                _reconnectAttempts = 0;
                _lastHeartbeatTime = Time.time;

                // 发送认证
                SendWebSocketMessage("auth", JsonUtility.ToJson(new AuthRequest
                {
                    playerId = _playerId,
                    token = _authToken,
                    version = Application.version
                }));

                // 发送积压消息
                while (_pendingMessages.Count > 0)
                    SendMessage(_pendingMessages.Dequeue());
            };

            _ws.OnMessage += (sender, e) =>
            {
                try
                {
                    var envelope = JsonUtility.FromJson<MessageEnvelope>(e.Data);
                    OnMessageReceived?.Invoke(envelope.type, envelope.payload);
                    ProcessServerMessage(envelope);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Network] 消息解析失败: {ex.Message}");
                }
            };

            _ws.OnError += (sender, e) =>
            {
                Debug.LogError($"[Network] WebSocket错误: {e.Message}");
                OnError?.Invoke(500, e.Message);
            };

            _ws.OnClose += (sender, e) =>
            {
                Debug.LogWarning($"[Network] 连接关闭: {e.Reason}");
                SetState(NetworkState.Disconnected);
            };

            _ws.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Network] 连接失败: {e.Message}");
            SetState(NetworkState.Disconnected);
        }
    }

    public void Disconnect()
    {
        if (_ws != null && _ws.IsAlive)
            _ws.Close();
        SetState(NetworkState.Disconnected);
    }

    private IEnumerator TryReconnect()
    {
        _reconnectAttempts++;
        SetState(NetworkState.Reconnecting);
        yield return new WaitForSeconds(ReconnectInterval);

        if (!string.IsNullOrEmpty(_authToken))
        {
            Connect(_playerId, _authToken);
            if (IsConnected)
                OnReconnected?.Invoke();
        }
    }

    private void SetState(NetworkState newState)
    {
        var old = State;
        State = newState;
        if (old != newState)
            OnStateChanged?.Invoke(old, newState);
    }

    #endregion

    #region === 消息收发 ===

    public void SendWebSocketMessage(string type, string jsonPayload)
    {
        if (!IsConnected)
        {
            _pendingMessages.Enqueue(new NetworkMessage { Type = type, Payload = jsonPayload });
            return;
        }

        var envelope = new MessageEnvelope
        {
            type = type,
            payload = jsonPayload,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            sessionId = _playerId
        };

        string json = JsonUtility.ToJson(envelope);
        _ws.Send(json);
    }

    private void SendMessage(NetworkMessage msg)
    {
        SendWebSocketMessage(msg.Type, msg.Payload);
    }

    private void ProcessServerMessage(MessageEnvelope envelope)
    {
        switch (envelope.type)
        {
            case "auth_response":
                var authResp = JsonUtility.FromJson<AuthResponse>(envelope.payload);
                if (authResp.success)
                {
                    Debug.Log($"[Network] 认证成功: {authResp.playerName}");
                    _playerId = authResp.playerId;
                }
                else
                {
                    OnError?.Invoke(401, "认证失败");
                }
                break;

            case "match_found":
                var matchInfo = JsonUtility.FromJson<MatchFoundInfo>(envelope.payload);
                OnMatchFound?.Invoke(matchInfo);
                break;

            case "arena_battle_result":
                var result = JsonUtility.FromJson<ArenaBattleResult>(envelope.payload);
                OnArenaResult?.Invoke(result);
                break;

            case "guild_event":
                var guildEvent = JsonUtility.FromJson<GuildEvent>(envelope.payload);
                OnGuildEvent?.Invoke(guildEvent);
                break;

            case "coop_room_update":
                var roomUpdate = JsonUtility.FromJson<CoopRoomUpdate>(envelope.payload);
                OnCoopRoomUpdate?.Invoke(roomUpdate);
                break;

            case "season_announcement":
                var announcement = JsonUtility.FromJson<SeasonAnnouncement>(envelope.payload);
                OnSeasonAnnouncement?.Invoke(announcement);
                break;

            case "leaderboard_update":
                var lb = JsonUtility.FromJson<LeaderboardUpdate>(envelope.payload);
                OnLeaderboardUpdate?.Invoke(lb);
                break;

            case "error":
                var err = JsonUtility.FromJson<ErrorInfo>(envelope.payload);
                OnError?.Invoke(err.code, err.message);
                break;
        }
    }

    #endregion

    #region === REST API 封装 ===

    // --- 竞技场 API ---
    public void RequestArenaMatch()
    {
        SendWebSocketMessage("arena_match", JsonUtility.ToJson(new ArenaMatchRequest
        {
            playerId = _playerId,
            defenseFormation = ArenaManager.Instance?.LoadDefenseFormation()
        }));
    }

    public void SubmitArenaResult(string battleId, bool isWin, int unitsLost)
    {
        SendWebSocketMessage("arena_submit", JsonUtility.ToJson(new ArenaBattleSubmit
        {
            battleId = battleId,
            playerId = _playerId,
            isWin = isWin,
            unitsLost = unitsLost,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }));
    }

    // --- 合作模式 API ---
    public void CreateCoopRoom(string roomName, int maxPlayers, string password = "")
    {
        SendWebSocketMessage("coop_create", JsonUtility.ToJson(new CoopCreateRequest
        {
            playerId = _playerId,
            roomName = roomName,
            maxPlayers = maxPlayers,
            password = password
        }));
    }

    public void JoinCoopRoom(string roomId, string password = "")
    {
        SendWebSocketMessage("coop_join", JsonUtility.ToJson(new CoopJoinRequest
        {
            playerId = _playerId,
            roomId = roomId,
            password = password
        }));
    }

    public void LeaveCoopRoom(string roomId)
    {
        SendWebSocketMessage("coop_leave", JsonUtility.ToJson(new CoopLeaveRequest
        {
            playerId = _playerId,
            roomId = roomId
        }));
    }

    public void SendCoopAction(string roomId, string actionType, string actionData)
    {
        SendWebSocketMessage("coop_action", JsonUtility.ToJson(new CoopAction
        {
            playerId = _playerId,
            roomId = roomId,
            actionType = actionType,
            actionData = actionData
        }));
    }

    // --- 公会 API ---
    public void CreateGuild(string guildName, string tag, string description)
    {
        SendWebSocketMessage("guild_create", JsonUtility.ToJson(new GuildCreateRequest
        {
            playerId = _playerId,
            guildName = guildName,
            tag = tag,
            description = description
        }));
    }

    public void JoinGuild(string guildId)
    {
        SendWebSocketMessage("guild_join", JsonUtility.ToJson(new GuildJoinRequest
        {
            playerId = _playerId,
            guildId = guildId
        }));
    }

    public void StartGuildWar(string guildId, string targetGuildId)
    {
        SendWebSocketMessage("guild_war_start", JsonUtility.ToJson(new GuildWarRequest
        {
            playerId = _playerId,
            guildId = guildId,
            targetGuildId = targetGuildId
        }));
    }

    // --- 排行榜 API ---
    public void FetchLeaderboard(string boardType, int offset = 0, int limit = 50)
    {
        SendWebSocketMessage("leaderboard_fetch", JsonUtility.ToJson(new LeaderboardRequest
        {
            playerId = _playerId,
            boardType = boardType, // "arena", "guild", "season", "wave"
            offset = offset,
            limit = limit
        }));
    }

    // --- 赛季 API ---
    public void ClaimBattlePassReward(int passLevel)
    {
        SendWebSocketMessage("season_claim_pass", JsonUtility.ToJson(new BattlePassClaimRequest
        {
            playerId = _playerId,
            passLevel = passLevel
        }));
    }

    public void PurchaseBattlePass(int tier)
    {
        SendWebSocketMessage("season_purchase_pass", JsonUtility.ToJson(new BattlePassPurchaseRequest
        {
            playerId = _playerId,
            tier = tier, // 1=付费 2=高级
            price = tier == 1 ? 30 : 68
        }));
    }

    #endregion

    #region === 事件 ===

    public event Action<MatchFoundInfo> OnMatchFound;
    public event Action<ArenaBattleResult> OnArenaResult;
    public event Action<GuildEvent> OnGuildEvent;
    public event Action<CoopRoomUpdate> OnCoopRoomUpdate;
    public event Action<SeasonAnnouncement> OnSeasonAnnouncement;
    public event Action<LeaderboardUpdate> OnLeaderboardUpdate;

    #endregion
}

#region === 数据结构 ===

public enum NetworkState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Error
}

[Serializable]
public class NetworkMessage
{
    public string Type;
    public string Payload;
}

[Serializable]
public class MessageEnvelope
{
    public string type;
    public string payload;
    public long timestamp;
    public string sessionId;
}

[Serializable]
public class AuthRequest
{
    public string playerId;
    public string token;
    public string version;
}

[Serializable]
public class AuthResponse
{
    public bool success;
    public string playerId;
    public string playerName;
    public int serverTime;
}

// === 竞技场 ===
[Serializable]
public class ArenaMatchRequest
{
    public string playerId;
    public object defenseFormation;
}

[Serializable]
public class MatchFoundInfo
{
    public string battleId;
    public string opponentId;
    public string opponentName;
    public int opponentRank;
    public int opponentElo;
    public long matchTime;
}

[Serializable]
public class ArenaBattleSubmit
{
    public string battleId;
    public string playerId;
    public bool isWin;
    public int unitsLost;
    public long timestamp;
}

[Serializable]
public class ArenaBattleResult
{
    public string battleId;
    public string winnerId;
    public int eloChange;
    public int newRank;
    public List<string> rewards;
}

// === 合作模式 ===
[Serializable]
public class CoopCreateRequest
{
    public string playerId;
    public string roomName;
    public int maxPlayers;
    public string password;
}

[Serializable]
public class CoopJoinRequest
{
    public string playerId;
    public string roomId;
    public string password;
}

[Serializable]
public class CoopLeaveRequest
{
    public string playerId;
    public string roomId;
}

[Serializable]
public class CoopAction
{
    public string playerId;
    public string roomId;
    public string actionType; // "tower_place", "skill_use", "hero_move", "emoji"
    public string actionData;
}

[Serializable]
public class CoopRoomUpdate
{
    public string roomId;
    public string roomName;
    public List<CoopPlayerInfo> players;
    public string roomState; // "waiting", "playing", "finished"
    public int currentWave;
    public int maxWave;
}

[Serializable]
public class CoopPlayerInfo
{
    public string playerId;
    public string playerName;
    public int towersBuilt;
    public int enemiesKilled;
    public bool isReady;
    public bool isHost;
}

// === 公会 ===
[Serializable]
public class GuildCreateRequest
{
    public string playerId;
    public string guildName;
    public string tag;
    public string description;
}

[Serializable]
public class GuildJoinRequest
{
    public string playerId;
    public string guildId;
}

[Serializable]
public class GuildWarRequest
{
    public string playerId;
    public string guildId;
    public string targetGuildId;
}

[Serializable]
public class GuildEvent
{
    public string eventType; // "member_join", "member_leave", "war_start", "war_end", "tech_upgrade"
    public string guildId;
    public string guildName;
    public string data;
    public long timestamp;
}

// === 排行榜 ===
[Serializable]
public class LeaderboardRequest
{
    public string playerId;
    public string boardType;
    public int offset;
    public int limit;
}

[Serializable]
public class LeaderboardUpdate
{
    public string boardType;
    public List<LeaderboardEntry> entries;
    public int playerRank;
    public int totalEntries;
}

[Serializable]
public class LeaderboardEntry
{
    public int rank;
    public string playerId;
    public string playerName;
    public int score;
    public int level;
}

// === 赛季 ===
[Serializable]
public class SeasonAnnouncement
{
    public int seasonNumber;
    public string seasonName;
    public string seasonTheme;
    public long startTime;
    public long endTime;
    public string keyFeatures;
}

[Serializable]
public class BattlePassClaimRequest
{
    public string playerId;
    public int passLevel;
}

[Serializable]
public class BattlePassPurchaseRequest
{
    public string playerId;
    public int tier;
    public int price;
}

// === 通用 ===
[Serializable]
public class ErrorInfo
{
    public int code;
    public string message;
}

#endregion
