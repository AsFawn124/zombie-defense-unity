using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// BattleReplayManager - 战斗回放系统
/// 记录整局战斗指令，支持回放、快进、暂停
/// </summary>
public class BattleReplayManager : MonoBehaviour
{
    public static BattleReplayManager Instance { get; private set; }

    // 回放状态
    public enum ReplayState
    {
        Idle,
        Recording,
        Playing,
        Paused
    }

    // 游戏指令类型
    [System.Serializable]
    public enum CommandType
    {
        PlaceTower,
        UpgradeTower,
        SellTower,
        UseSkill,
        ActivateHero,
        SpawnWave,
        EnemyMove,
        EnemyDeath,
        TowerAttack,
        DamageDealt,
        GoldChange,
        HpChange,
        GameStart,
        GameEnd,
        StageComplete,
        BossSpawn,
        PowerUp,
        RerollCard
    }

    // 回放指令
    [System.Serializable]
    public class ReplayCommand
    {
        public float timestamp;
        public CommandType type;
        public string towerId;
        public string enemyId;
        public int targetId;
        public float x, y, z;
        public float value;
        public string extraData;
    }

    // 回放元数据
    [System.Serializable]
    public class ReplayMeta
    {
        public string replayId;
        public string playerName;
        public int stageReached;
        public int totalWaves;
        public int totalKills;
        public int totalGoldEarned;
        public float duration;
        public int score;
        public string mapName;
        public string difficulty;
        public DateTime date;
        public List<string> towersUsed;
        public List<string> modifiers;
        public bool isRoguelike;
        public int roguelikeStage;
    }

    // 回放文件
    [System.Serializable]
    public class ReplayFile
    {
        public ReplayMeta meta;
        public List<ReplayCommand> commands;
        public int version = 1;
    }

    // 录制状态
    private ReplayState state = ReplayState.Idle;
    private ReplayFile currentRecording;
    private ReplayFile currentPlayback;
    private int playbackIndex = 0;
    private float playbackSpeed = 1f;
    private float replayStartTime;
    private float currentReplayTime;
    
    // 回放列表
    private List<ReplayMeta> savedReplays = new List<ReplayMeta>();
    private const int MAX_REPLAYS = 20;

    // 回调
    public event Action<ReplayCommand> OnReplayCommand;
    public event Action<ReplayState> OnStateChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LoadReplayIndex();
    }

    void Update()
    {
        if (state == ReplayState.Playing)
        {
            ProcessPlayback();
        }
    }

    // ==================== 录制 ====================

    /// <summary>
    /// 开始录制
    /// </summary>
    public void StartRecording(string playerName, string mapName, string difficulty, bool isRoguelike)
    {
        if (state != ReplayState.Idle) return;

        currentRecording = new ReplayFile
        {
            meta = new ReplayMeta
            {
                replayId = $"replay_{DateTime.Now:yyyyMMdd_HHmmss}_{UnityEngine.Random.Range(1000, 9999)}",
                playerName = playerName,
                mapName = mapName,
                difficulty = difficulty,
                date = DateTime.Now,
                isRoguelike = isRoguelike,
                towersUsed = new List<string>(),
                modifiers = new List<string>()
            },
            commands = new List<ReplayCommand>()
        };

        state = ReplayState.Recording;
        replayStartTime = Time.time;
        OnStateChanged?.Invoke(state);
        Debug.Log($"[Replay] 开始录制: {currentRecording.meta.replayId}");
    }

    /// <summary>
    /// 记录指令
    /// </summary>
    public void RecordCommand(CommandType type, string towerId = null, string enemyId = null,
        float x = 0, float y = 0, float z = 0, float value = 0, string extra = null)
    {
        if (state != ReplayState.Recording) return;

        currentRecording.commands.Add(new ReplayCommand
        {
            timestamp = Time.time - replayStartTime,
            type = type,
            towerId = towerId,
            enemyId = enemyId,
            x = x, y = y, z = z,
            value = value,
            extraData = extra
        });
    }

    /// <summary>
    /// 停止录制并保存
    /// </summary>
    public ReplayMeta StopRecording(int stageReached, int totalWaves, int totalKills, 
        int totalGoldEarned, int score, int roguelikeStage = 0)
    {
        if (state != ReplayState.Recording) return null;

        currentRecording.meta.stageReached = stageReached;
        currentRecording.meta.totalWaves = totalWaves;
        currentRecording.meta.totalKills = totalKills;
        currentRecording.meta.totalGoldEarned = totalGoldEarned;
        currentRecording.meta.score = score;
        currentRecording.meta.duration = Time.time - replayStartTime;
        currentRecording.meta.roguelikeStage = roguelikeStage;

        // 保存
        SaveReplay(currentRecording);

        // 添加到索引
        savedReplays.Insert(0, currentRecording.meta);
        if (savedReplays.Count > MAX_REPLAYS)
            savedReplays.RemoveAt(savedReplays.Count - 1);

        SaveReplayIndex();

        state = ReplayState.Idle;
        OnStateChanged?.Invoke(state);
        
        Debug.Log($"[Replay] 录制完成: {currentRecording.meta.replayId} " +
                  $"(关卡{stageReached}, 击杀{totalKills}, 分数{score}, " +
                  $"时长{currentRecording.meta.duration:F1}秒)");

        return currentRecording.meta;
    }

    /// <summary>
    /// 便捷录制方法
    /// </summary>
    public void RecordPlaceTower(string towerId, Vector3 position, string towerType)
    {
        RecordCommand(CommandType.PlaceTower, towerId, null, 
            position.x, position.y, position.z, extra: towerType);
    }

    public void RecordUpgradeTower(string towerId, int newLevel)
    {
        RecordCommand(CommandType.UpgradeTower, towerId, null, value: newLevel);
    }

    public void RecordSellTower(string towerId)
    {
        RecordCommand(CommandType.SellTower, towerId);
    }

    public void RecordUseSkill(string skillId, Vector2 target)
    {
        RecordCommand(CommandType.UseSkill, skillId, null, target.x, target.y);
    }

    public void RecordDamage(string sourceId, string targetId, float damage)
    {
        RecordCommand(CommandType.DamageDealt, sourceId, targetId, value: damage);
    }

    public void RecordGoldChange(int amount)
    {
        RecordCommand(CommandType.GoldChange, value: amount);
    }

    public void RecordHpChange(int amount)
    {
        RecordCommand(CommandType.HpChange, value: amount);
    }

    public void RecordEnemyDeath(string enemyId, string killerId)
    {
        RecordCommand(CommandType.EnemyDeath, enemyId, killerId);
    }

    // ==================== 回放 ====================

    /// <summary>
    /// 加载并播放回放
    /// </summary>
    public bool LoadReplay(string replayId)
    {
        string path = Path.Combine(Application.persistentDataPath, "replays", $"{replayId}.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[Replay] 回放文件不存在: {replayId}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            currentPlayback = JsonUtility.FromJson<ReplayFile>(json);
            
            if (currentPlayback?.commands == null)
            {
                Debug.LogWarning("[Replay] 回放文件格式错误");
                return false;
            }

            // 按时间戳排序
            currentPlayback.commands.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

            playbackIndex = 0;
            currentReplayTime = 0;
            state = ReplayState.Paused;
            
            Debug.Log($"[Replay] 加载回放: {replayId} ({currentPlayback.meta.duration:F1}秒, " +
                      $"{currentPlayback.commands.Count}条指令)");
            OnStateChanged?.Invoke(state);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Replay] 加载回放失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 播放回放
    /// </summary>
    public void Play()
    {
        if (currentPlayback == null) return;
        state = ReplayState.Playing;
        OnStateChanged?.Invoke(state);
        Debug.Log($"[Replay] 播放回放 (x{playbackSpeed})");
    }

    /// <summary>
    /// 暂停回放
    /// </summary>
    public void Pause()
    {
        if (state != ReplayState.Playing) return;
        state = ReplayState.Paused;
        OnStateChanged?.Invoke(state);
    }

    /// <summary>
    /// 停止回放
    /// </summary>
    public void Stop()
    {
        state = ReplayState.Idle;
        currentPlayback = null;
        playbackIndex = 0;
        OnStateChanged?.Invoke(state);
    }

    /// <summary>
    /// 设置播放速度
    /// </summary>
    public void SetSpeed(float speed)
    {
        playbackSpeed = Mathf.Clamp(speed, 0.25f, 8f);
    }

    /// <summary>
    /// 跳转到指定时间
    /// </summary>
    public void SeekTo(float time)
    {
        if (currentPlayback == null) return;
        
        currentReplayTime = Mathf.Clamp(time, 0, currentPlayback.meta.duration);
        
        // 找到对应时间点的指令索引
        playbackIndex = 0;
        while (playbackIndex < currentPlayback.commands.Count &&
               currentPlayback.commands[playbackIndex].timestamp < currentReplayTime)
        {
            playbackIndex++;
        }
    }

    private void ProcessPlayback()
    {
        if (currentPlayback == null) return;

        currentReplayTime += Time.deltaTime * playbackSpeed;

        // 执行当前时间之前的所有指令
        while (playbackIndex < currentPlayback.commands.Count &&
               currentPlayback.commands[playbackIndex].timestamp <= currentReplayTime)
        {
            OnReplayCommand?.Invoke(currentPlayback.commands[playbackIndex]);
            playbackIndex++;
        }

        // 回放结束
        if (currentReplayTime >= currentPlayback.meta.duration)
        {
            state = ReplayState.Idle;
            OnStateChanged?.Invoke(state);
            Debug.Log($"[Replay] 回放结束");
        }
    }

    // ==================== 文件操作 ====================

    private void SaveReplay(ReplayFile replay)
    {
        string dir = Path.Combine(Application.persistentDataPath, "replays");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, $"{replay.meta.replayId}.json");
        string json = JsonUtility.ToJson(replay, true);
        File.WriteAllText(path, json);
    }

    public bool DeleteReplay(string replayId)
    {
        string path = Path.Combine(Application.persistentDataPath, "replays", $"{replayId}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
            savedReplays.RemoveAll(r => r.replayId == replayId);
            SaveReplayIndex();
            return true;
        }
        return false;
    }

    private void SaveReplayIndex()
    {
        string path = Path.Combine(Application.persistentDataPath, "replays", "index.json");
        string json = JsonUtility.ToJson(new ReplayIndexWrapper { replays = savedReplays }, true);
        File.WriteAllText(path, json);
    }

    private void LoadReplayIndex()
    {
        string path = Path.Combine(Application.persistentDataPath, "replays", "index.json");
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                var wrapper = JsonUtility.FromJson<ReplayIndexWrapper>(json);
                if (wrapper?.replays != null)
                    savedReplays = wrapper.replays;
            }
            catch { }
        }
    }

    [System.Serializable]
    private class ReplayIndexWrapper
    {
        public List<ReplayMeta> replays;
    }

    // ==================== Getter ====================

    public ReplayState GetState() => state;
    public bool IsRecording() => state == ReplayState.Recording;
    public bool IsPlaying() => state == ReplayState.Playing;
    public ReplayMeta GetCurrentMeta() => currentPlayback?.meta ?? currentRecording?.meta;
    public List<ReplayMeta> GetSavedReplays() => savedReplays;
    public float GetCurrentReplayTime() => currentReplayTime;
    public float GetReplayDuration() => currentPlayback?.meta.duration ?? 0;
    public float GetPlaybackSpeed() => playbackSpeed;
    public int GetTotalCommands() => 
        currentRecording?.commands.Count ?? currentPlayback?.commands.Count ?? 0;

    /// <summary>
    /// 获取回放进度 (0-1)
    /// </summary>
    public float GetPlaybackProgress()
    {
        if (currentPlayback == null || currentPlayback.meta.duration <= 0) return 0;
        return Mathf.Clamp01(currentReplayTime / currentPlayback.meta.duration);
    }

    /// <summary>
    /// 导出回放为共享格式
    /// </summary>
    public string ExportReplay(string replayId)
    {
        string path = Path.Combine(Application.persistentDataPath, "replays", $"{replayId}.json");
        if (!File.Exists(path)) return null;
        return File.ReadAllText(path);
    }
}
