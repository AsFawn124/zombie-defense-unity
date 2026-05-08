using UnityEngine;
using System;

/// <summary>
/// 游戏管理器 - 单例模式
/// 管理游戏状态、分数、波次等核心数据
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("游戏状态")]
    public GameState CurrentState { get; private set; } = GameState.Menu;
    
    [Header("游戏数据")]
    public int CurrentWave = 0;
    public int Score = 0;
    public int Gold = 0;
    public int KillCount = 0;
    
    [Header("事件")]
    public Action OnWaveStart;
    public Action OnWaveEnd;
    public Action OnGameOver;
    public Action<int> OnGoldChanged;
    public Action<int> OnScoreChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 初始化游戏
        ResetGame();
        
        // 加载音频资源
        AudioManager.Instance?.LoadAudioFromResources();
    }
    
    /// <summary>
    /// 重置游戏数据
    /// </summary>
    public void ResetGame()
    {
        CurrentWave = 0;
        Score = 0;
        Gold = 100; // 初始金币
        KillCount = 0;
        CurrentState = GameState.Menu;
        
        OnGoldChanged?.Invoke(Gold);
        OnScoreChanged?.Invoke(Score);
    }
    
    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame()
    {
        CurrentState = GameState.Playing;
        StartWave();
    }
    
    /// <summary>
    /// 开始新波次
    /// </summary>
    public void StartWave()
    {
        CurrentWave++;
        OnWaveStart?.Invoke();
        Debug.Log($"第 {CurrentWave} 波开始！");
    }
    
    /// <summary>
    /// 结束当前波次
    /// </summary>
    public void EndWave()
    {
        OnWaveEnd?.Invoke();
        Debug.Log($"第 {CurrentWave} 波结束！");
        
        // 显示技能选择界面
        ShowSkillSelection();
    }
    
    /// <summary>
    /// 显示技能选择
    /// </summary>
    private void ShowSkillSelection()
    {
        CurrentState = GameState.SkillSelection;
        // TODO: 打开技能选择UI
        Debug.Log("显示技能选择界面");
    }
    
    /// <summary>
    /// 游戏结束
    /// </summary>
    public void GameOver()
    {
        CurrentState = GameState.GameOver;
        OnGameOver?.Invoke();
        Debug.Log($"游戏结束！存活波次: {CurrentWave}, 击杀数: {KillCount}");
    }
    
    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
        }
    }
    
    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
        }
    }
    
    /// <summary>
    /// 添加金币
    /// </summary>
    public void AddGold(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }
    
    /// <summary>
    /// 消耗金币
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (Gold >= amount)
        {
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 添加分数
    /// </summary>
    public void AddScore(int amount)
    {
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }
    
    /// <summary>
    /// 增加击杀数
    /// </summary>
    public void AddKill()
    {
        KillCount++;
    }
}

/// <summary>
/// 游戏状态枚举
/// </summary>
public enum GameState
{
    Menu,           // 主菜单
    Playing,        // 游戏中
    Paused,         // 暂停
    SkillSelection, // 技能选择
    GameOver        // 游戏结束
}
