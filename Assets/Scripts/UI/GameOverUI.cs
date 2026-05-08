using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏结束界面
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("结果面板")]
    public GameObject VictoryPanel;
    public GameObject DefeatPanel;
    
    [Header("统计信息")]
    public Text FinalScoreText;
    public Text FinalWaveText;
    public Text KillCountText;
    public Text HighScoreText;
    public Text HighWaveText;
    
    [Header("新纪录")]
    public GameObject NewRecordBadge;
    public Animator RecordAnimator;
    
    [Header("按钮")]
    public Button RestartButton;
    public Button MenuButton;
    public Button ShareButton;
    
    [Header("奖励")]
    public Text RewardText;
    public Button DoubleRewardButton;
    
    private int currentReward = 0;
    private bool isVictory = false;
    
    private void Start()
    {
        // 绑定按钮事件
        RestartButton?.onClick.AddListener(OnRestartClick);
        MenuButton?.onClick.AddListener(OnMenuClick);
        ShareButton?.onClick.AddListener(OnShareClick);
        DoubleRewardButton?.onClick.AddListener(OnDoubleRewardClick);
        
        // 初始隐藏
        gameObject.SetActive(false);
        
        // 订阅游戏结束事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += ShowGameOver;
        }
    }
    
    /// <summary>
    /// 显示游戏结束界面
    /// </summary>
    private void ShowGameOver()
    {
        gameObject.SetActive(true);
        
        // 判断胜利还是失败
        isVictory = BaseManager.Instance != null && BaseManager.Instance.CurrentHealth > 0;
        
        if (isVictory)
        {
            ShowVictory();
        }
        else
        {
            ShowDefeat();
        }
        
        // 更新统计
        UpdateStats();
        
        // 播放音效
        if (isVictory)
        {
            AudioManager.Instance?.PlayVictoryBGM();
        }
        else
        {
            AudioManager.Instance?.PlayDefeatBGM();
        }
    }
    
    /// <summary>
    /// 显示胜利界面
    /// </summary>
    private void ShowVictory()
    {
        VictoryPanel?.SetActive(true);
        DefeatPanel?.SetActive(false);
    }
    
    /// <summary>
    /// 显示失败界面
    /// </summary>
    private void ShowDefeat()
    {
        VictoryPanel?.SetActive(false);
        DefeatPanel?.SetActive(true);
    }
    
    /// <summary>
    /// 更新统计数据
    /// </summary>
    private void UpdateStats()
    {
        if (GameManager.Instance == null) return;
        
        int score = GameManager.Instance.Score;
        int wave = GameManager.Instance.CurrentWave;
        int kills = GameManager.Instance.KillCount;
        
        // 当前成绩
        if (FinalScoreText != null)
            FinalScoreText.text = $"得分: {score}";
        
        if (FinalWaveText != null)
            FinalWaveText.text = $"波次: {wave}";
        
        if (KillCountText != null)
            KillCountText.text = $"击杀: {kills}";
        
        // 检查新纪录
        bool newScoreRecord = false;
        bool newWaveRecord = false;
        
        if (SaveManager.Instance != null)
        {
            int highScore = SaveManager.Instance.GetHighScore();
            int highWave = SaveManager.Instance.GetHighWave();
            
            if (score > highScore)
            {
                SaveManager.Instance.SaveHighScore(score);
                newScoreRecord = true;
            }
            
            if (wave > highWave)
            {
                SaveManager.Instance.SaveHighWave(wave);
                newWaveRecord = true;
            }
            
            // 更新显示
            if (HighScoreText != null)
                HighScoreText.text = $"最高分: {SaveManager.Instance.GetHighScore()}";
            
            if (HighWaveText != null)
                HighWaveText.text = $"最高波次: {SaveManager.Instance.GetHighWave()}";
            
            // 保存总击杀
            SaveManager.Instance.AddTotalKills(kills);
        }
        
        // 显示新纪录徽章
        if (NewRecordBadge != null)
        {
            NewRecordBadge.SetActive(newScoreRecord || newWaveRecord);
            
            if ((newScoreRecord || newWaveRecord) && RecordAnimator != null)
            {
                RecordAnimator.SetTrigger("NewRecord");
            }
        }
        
        // 计算奖励
        currentReward = CalculateReward(score, wave, kills);
        if (RewardText != null)
        {
            RewardText.text = $"+{currentReward}";
        }
    }
    
    /// <summary>
    /// 计算奖励
    /// </summary>
    private int CalculateReward(int score, int wave, int kills)
    {
        int reward = score / 100;
        reward += wave * 10;
        reward += kills;
        return reward;
    }
    
    /// <summary>
    /// 重新开始
    /// </summary>
    private void OnRestartClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }
    
    /// <summary>
    /// 返回主菜单
    /// </summary>
    private void OnMenuClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
    
    /// <summary>
    /// 分享
    /// </summary>
    private void OnShareClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        
        // 分享功能（微信SDK）
        string shareText = $"我在《僵尸防线》中存活了 {GameManager.Instance?.CurrentWave} 波，击杀了 {GameManager.Instance?.KillCount} 个僵尸！你能超过我吗？";
        
        Debug.Log($"分享: {shareText}");
        
        // TODO: 接入微信分享SDK
        // WeChatManager.Instance?.Share(shareText);
    }
    
    /// <summary>
    /// 双倍奖励（观看广告）
    /// </summary>
    private void OnDoubleRewardClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        
        // TODO: 播放激励视频广告
        // AdManager.Instance?.ShowRewardedAd(OnAdRewarded);
        
        Debug.Log("播放广告获取双倍奖励");
    }
    
    /// <summary>
    /// 广告奖励回调
    /// </summary>
    private void OnAdRewarded()
    {
        currentReward *= 2;
        if (RewardText != null)
        {
            RewardText.text = $"+{currentReward} (双倍)";
        }
        
        // 禁用按钮
        if (DoubleRewardButton != null)
        {
            DoubleRewardButton.interactable = false;
        }
    }
    
    private void OnDestroy()
    {
        RestartButton?.onClick.RemoveListener(OnRestartClick);
        MenuButton?.onClick.RemoveListener(OnMenuClick);
        ShareButton?.onClick.RemoveListener(OnShareClick);
        DoubleRewardButton?.onClick.RemoveListener(OnDoubleRewardClick);
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= ShowGameOver;
        }
    }
}
