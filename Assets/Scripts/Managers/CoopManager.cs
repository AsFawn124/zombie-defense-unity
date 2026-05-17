using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 合作模式管理器 - TASK-031~032
/// 双人合作、多人团战(4人)、同步机制、组合技系统
/// </summary>
public class CoopManager : MonoBehaviour
{
    public static CoopManager Instance;

    [Header("合作配置")]
    public CoopConfig Config;

    // 房间管理
    private Dictionary<string, CoopRoom> _rooms = new Dictionary<string, CoopRoom>();
    private CoopRoom _currentRoom;
    private int _localPlayerIndex;

    // 同步状态
    private float _syncTimer;
    private List<CoopSyncMessage> _pendingSyncs = new List<CoopSyncMessage>();

    // 事件
    public event Action<CoopRoom> OnRoomCreated;
    public event Action<CoopRoom> OnRoomJoined;
    public event Action<string> OnRoomLeft;
    public event Action<CoopSyncMessage> OnSyncReceived;
    public event Action<ComboSkill> OnComboTriggered;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (_currentRoom == null) return;

        // 定期同步
        _syncTimer += Time.deltaTime;
        if (_syncTimer >= Config.SyncInterval)
        {
            _syncTimer = 0;
            ProcessPendingSyncs();
        }
    }

    #region === 房间管理 (TASK-031) ===

    /// <summary>
    /// 创建合作房间
    /// </summary>
    public CoopRoom CreateRoom(string roomName, CoopMode mode, int maxPlayers = 2)
    {
        string roomId = GenerateRoomId();
        var room = new CoopRoom
        {
            RoomId = roomId,
            RoomName = roomName,
            Mode = mode,
            MaxPlayers = maxPlayers,
            HostPlayerId = GetLocalPlayerId(),
            CreatedTime = DateTime.UtcNow,
            State = CoopRoomState.Waiting
        };

        room.Players.Add(GetLocalPlayerId());
        _rooms[roomId] = room;
        _currentRoom = room;
        _localPlayerIndex = 0;

        OnRoomCreated?.Invoke(room);

        // 微信分享房间
        WeChatManager.Instance?.ShareCoopRoom(roomId, roomName, mode);

        return room;
    }

    /// <summary>
    /// 加入合作房间
    /// </summary>
    public void JoinRoom(string roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            Debug.LogError($"[Coop] 房间不存在: {roomId}");
            return;
        }

        if (room.Players.Count >= room.MaxPlayers)
        {
            Debug.LogError("[Coop] 房间已满");
            return;
        }

        string playerId = GetLocalPlayerId();
        if (!room.Players.Contains(playerId))
        {
            room.Players.Add(playerId);
            _localPlayerIndex = room.Players.Count - 1;
        }

        _currentRoom = room;

        if (room.Players.Count == room.MaxPlayers)
        {
            room.State = CoopRoomState.Ready;
        }

        OnRoomJoined?.Invoke(room);
    }

    /// <summary>
    /// 离开房间
    /// </summary>
    public void LeaveRoom()
    {
        if (_currentRoom == null) return;

        string roomId = _currentRoom.RoomId;
        _currentRoom.Players.Remove(GetLocalPlayerId());

        if (_currentRoom.Players.Count == 0)
        {
            _rooms.Remove(roomId);
        }

        OnRoomLeft?.Invoke(roomId);
        _currentRoom = null;
    }

    private string GenerateRoomId()
    {
        return $"COOP_{DateTime.UtcNow.Ticks:X8}_{UnityEngine.Random.Range(1000, 9999)}";
    }

    private string GetLocalPlayerId()
    {
        return PlayerPrefs.GetString("player_id", "local_player");
    }

    #endregion

    #region === 同步机制 (TASK-031) ===

    /// <summary>
    /// 发送同步消息
    /// </summary>
    public void SendSyncMessage(CoopSyncType type, string data)
    {
        var message = new CoopSyncMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            PlayerId = GetLocalPlayerId(),
            PlayerIndex = _localPlayerIndex,
            SyncType = type,
            Data = data,
            Timestamp = DateTime.UtcNow
        };

        _pendingSyncs.Add(message);

        // 立即同步关键消息
        if (type == CoopSyncType.EnemyKilled ||
            type == CoopSyncType.BaseDamaged ||
            type == CoopSyncType.SkillUsed)
        {
            ProcessSyncMessage(message);
        }
    }

    private void ProcessPendingSyncs()
    {
        foreach (var msg in _pendingSyncs)
        {
            ProcessSyncMessage(msg);
        }
        _pendingSyncs.Clear();
    }

    private void ProcessSyncMessage(CoopSyncMessage message)
    {
        // 在实际网络环境中，这里会通过WebSocket/Photon发送
        // 本地模式下直接触发事件
        OnSyncReceived?.Invoke(message);
    }

    #endregion

    #region === 组合技系统 (TASK-031) ===

    /// <summary>
    /// 检查组合技触发条件
    /// </summary>
    public void CheckComboTrigger(int playerIndex, int skillId, Vector2 position)
    {
        if (_currentRoom == null) return;

        // 查找可触发的组合技
        foreach (var combo in Config.ComboSkills)
        {
            if (combo.RequiredSkills.Contains(skillId))
            {
                // 检查是否有其他玩家使用了所需技能
                if (CanTriggerCombo(combo, playerIndex, skillId))
                {
                    TriggerComboSkill(combo, position);
                    return;
                }
            }
        }
    }

    private bool CanTriggerCombo(ComboSkill combo, int playerIndex, int usedSkillId)
    {
        int comboCount = 0;
        foreach (var skill in combo.RequiredSkills)
        {
            if (skill == usedSkillId) comboCount++;
            // 在实际实现中，检查其他玩家的技能使用记录
        }
        return comboCount >= combo.RequiredSkills.Count;
    }

    private void TriggerComboSkill(ComboSkill combo, Vector2 position)
    {
        Debug.Log($"[Coop] 触发组合技: {combo.ComboName}!");

        // 播放组合技特效
        EffectManager.Instance?.PlayComboEffect(combo.EffectId, position);

        // 应用组合技效果
        ApplyComboEffect(combo, position);

        OnComboTriggered?.Invoke(combo);
    }

    private void ApplyComboEffect(ComboSkill combo, Vector2 position)
    {
        switch (combo.EffectType)
        {
            case ComboEffectType.AreaDamage:
                // 范围伤害
                var enemies = WaveManager.Instance?.GetEnemiesInRange(position, combo.EffectRadius);
                if (enemies != null)
                {
                    foreach (var enemy in enemies)
                    {
                        enemy?.TakeDamage(combo.DamageMultiplier);
                    }
                }
                break;

            case ComboEffectType.TeamBuff:
                // 团队增益
                // 所有塔临时增加伤害
                TowerManager.Instance?.ApplyTeamBuff(combo.BuffDuration, combo.DamageMultiplier);
                break;

            case ComboEffectType.Healing:
                // 团队治疗
                BaseManager.Instance?.Heal(Mathf.RoundToInt(BaseManager.Instance.MaxHealth * 0.2f));
                break;
        }
    }

    #endregion

    #region === 多人团战 (TASK-032) ===

    /// <summary>
    /// 初始化4人团战地图
    /// </summary>
    public void InitializeRaidMap()
    {
        if (_currentRoom == null || _currentRoom.Mode != CoopMode.Raid4Player) return;

        // 四方地图布局
        // 玩家0: 北方 (上)
        // 玩家1: 东方 (右)
        // 玩家2: 南方 (下)
        // 玩家3: 西方 (左)

        float angle = _localPlayerIndex * 90f;
        float rad = angle * Mathf.Deg2Rad;

        // 设置本地玩家的防御区域
        Vector2 spawnPoint = new Vector2(
            Mathf.Sin(rad) * Config.RaidMapRadius,
            Mathf.Cos(rad) * Config.RaidMapRadius
        );

        // 中央BOSS会在所有玩家准备好后出现
        Debug.Log($"[Coop] 团战地图初始化，玩家{_localPlayerIndex}防守区域: {spawnPoint}");
    }

    /// <summary>
    /// 召唤中央BOSS
    /// </summary>
    public void SpawnRaidBoss()
    {
        if (_currentRoom == null || _currentRoom.Mode != CoopMode.Raid4Player) return;

        // 在所有玩家中间生成BOSS
        Vector2 center = Vector2.zero;
        WaveManager.Instance?.SpawnRaidBoss(center, Config.RaidBossHealth, Config.RaidBossDamage);

        Debug.Log("[Coop] 团战BOSS已出现!");
    }

    /// <summary>
    /// 团队BUFF共享
    /// </summary>
    public void ShareTeamBuff(TeamBuff buff)
    {
        SendSyncMessage(CoopSyncType.TeamBuff, JsonUtility.ToJson(buff));
        ApplyTeamBuff(buff);
    }

    private void ApplyTeamBuff(TeamBuff buff)
    {
        switch (buff.BuffType)
        {
            case TeamBuffType.DamageBoost:
                TowerManager.Instance?.ApplyTeamBuff(buff.Duration, buff.Value);
                break;
            case TeamBuffType.SpeedBoost:
                // 全局加速
                Time.timeScale = Mathf.Min(Time.timeScale + buff.Value, Config.MaxGameSpeed);
                StartCoroutine(ResetSpeedAfterDelay(buff.Duration));
                break;
            case TeamBuffType.GoldBonus:
                GameManager.Instance?.AddGold(Mathf.RoundToInt(buff.Value));
                break;
        }
    }

    private System.Collections.IEnumerator ResetSpeedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 1f;
    }

    #endregion

    #region === 公共属性 ===

    public CoopRoom CurrentRoom => _currentRoom;
    public bool IsInRoom => _currentRoom != null;
    public int PlayerCount => _currentRoom?.Players.Count ?? 0;

    #endregion
}

#region === 数据结构 ===

[System.Serializable]
public class CoopConfig
{
    public float SyncInterval = 0.1f;
    public float RaidMapRadius = 10f;
    public float RaidBossHealth = 50000f;
    public float RaidBossDamage = 100f;
    public float MaxGameSpeed = 2f;
    public ComboSkill[] ComboSkills;
}

[System.Serializable]
public class CoopRoom
{
    public string RoomId;
    public string RoomName;
    public CoopMode Mode;
    public int MaxPlayers;
    public string HostPlayerId;
    public List<string> Players = new List<string>();
    public DateTime CreatedTime;
    public CoopRoomState State;
}

[System.Serializable]
public class CoopSyncMessage
{
    public string MessageId;
    public string PlayerId;
    public int PlayerIndex;
    public CoopSyncType SyncType;
    public string Data;
    public DateTime Timestamp;
}

[System.Serializable]
public class ComboSkill
{
    public string ComboName;
    public List<int> RequiredSkills;
    public ComboEffectType EffectType;
    public float EffectRadius;
    public float DamageMultiplier;
    public float BuffDuration;
    public string EffectId;
}

[System.Serializable]
public class TeamBuff
{
    public TeamBuffType BuffType;
    public float Value;
    public float Duration;
}

public enum CoopMode
{
    Duo,            // 双人
    Raid4Player     // 4人团战
}

public enum CoopRoomState
{
    Waiting,
    Ready,
    InGame,
    Finished
}

public enum CoopSyncType
{
    TowerPlaced,
    TowerUpgraded,
    SkillUsed,
    EnemyKilled,
    BaseDamaged,
    GoldChanged,
    TeamBuff,
    ComboTriggered
}

public enum ComboEffectType
{
    AreaDamage,
    TeamBuff,
    Healing
}

public enum TeamBuffType
{
    DamageBoost,
    SpeedBoost,
    GoldBonus
}

#endregion
