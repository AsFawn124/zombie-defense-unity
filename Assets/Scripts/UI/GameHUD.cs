using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏HUD界面
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("顶部信息")]
    public Text WaveText;
    public Text ScoreText;
    public Text GoldText;
    
    [Header("基地血量")]
    public Slider BaseHealthSlider;
    public Text BaseHealthText;
    
    [Header("敌人信息")]
    public Text EnemyCountText;
    
    [Header("技能显示")]
    public Transform SkillContainer;
    public GameObject SkillIconPrefab;
    
    [Header("控制按钮")]
    public Button PauseButton;
    public Button SpeedButton;
    
    [Header("暂停菜单")]
    public GameObject PausePanel;
    public Button ResumeButton;
    public Button RestartButton;
    public Button MenuButton;
    
    [Header("波次提示")]
    public GameObject WaveStartPanel;
    public Text WaveStartText;
    public Animator WaveStartAnimator;
    
    private void Start()
    {
        // 绑定按钮事件
        PauseButton?.onClick.AddListener(OnPauseClick);
        SpeedButton?.onClick.AddListener(OnSpeedClick);
        ResumeButton?.onClick.AddListener(OnResumeClick);
        RestartButton?.onClick.AddListener(OnRestartClick);
        MenuButton?.onClick.AddListener(OnMenuClick);
        
        // 订阅游戏事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveStart += OnWaveStart;
            GameManager.Instance.OnWaveEnd += OnWaveEnd;
            GameManager.Instance.OnGoldChanged += OnGoldChanged;
            GameManager.Instance.OnScoreChanged += OnScoreChanged;
        }
        
        // 初始化显示
        UpdateAllDisplays();
        PausePanel?.SetActive(false);
        WaveStartPanel?.SetActive(false);
    }
    
    private void Update()
    {
        // 实时更新
        UpdateBaseHealth();
        UpdateEnemyCount();
    }
    
    /// <summary>
    /// 更新所有显示
    /// </summary>
    private void UpdateAllDisplays()
    {
        UpdateWaveDisplay();
        UpdateScoreDisplay();
        UpdateGoldDisplay();
        UpdateBaseHealth();
    }
    
    /// <summary>
    /// 更新波次显示
    /// </summary>
    private void UpdateWaveDisplay()
    {
        if (WaveText != null && GameManager.Instance != null)
        {
            WaveText.text = $"波次: {GameManager.Instance.CurrentWave}";
        }
    }
    
    /// <summary>
    /// 更新分数显示
    /// </summary>
    private void UpdateScoreDisplay()
    {
        if (ScoreText != null && GameManager.Instance != null)
        {
            ScoreText.text = $"分数: {GameManager.Instance.Score}";
        }
    }
    
    /// <summary>
    /// 更新金币显示
    /// </summary>
    private void UpdateGoldDisplay()
    {
        if (GoldText != null && GameManager.Instance != null)
        {
            GoldText.text = $"金币: {GameManager.Instance.Gold}";
        }
    }
    
    /// <summary>
    /// 更新基地血量
    /// </summary>
    private void UpdateBaseHealth()
    {
        if (BaseManager.Instance == null) return;
        
        float healthPercent = (float)BaseManager.Instance.CurrentHealth / BaseManager.Instance.MaxHealth;
        
        if (BaseHealthSlider != null)
        {
            BaseHealthSlider.value = healthPercent;
        }
        
        if (BaseHealthText != null)
        {
            BaseHealthText.text = $"{BaseManager.Instance.CurrentHealth}/{BaseManager.Instance.MaxHealth}";
        }
    }
    
    /// <summary>
    /// 更新敌人数量
    /// </summary>
    private void UpdateEnemyCount()
    {
        if (EnemyCountText != null && WaveManager.Instance != null)
        {
            int count = WaveManager.Instance.GetActiveEnemyCount();
            EnemyCountText.text = $"敌人: {count}";
        }
    }
    
    /// <summary>
    /// 波次开始
    /// </summary>
    private void OnWaveStart()
    {
        UpdateWaveDisplay();
        ShowWaveStartAnimation();
    }
    
    /// <summary>
    /// 波次结束
    /// </summary>
    private void OnWaveEnd()
    {
        UpdateWaveDisplay();
    }
    
    /// <summary>
    /// 金币变化
    /// </summary>
    private void OnGoldChanged(int gold)
    {
        UpdateGoldDisplay();
    }
    
    /// <summary>
    /// 分数变化
    /// </summary>
    private void OnScoreChanged(int score)
    {
        UpdateScoreDisplay();
    }
    
    /// <summary>
    /// 显示波次开始动画
    /// </summary>
    private void ShowWaveStartAnimation()
    {
        if (WaveStartPanel == null) return;
        
        WaveStartPanel.SetActive(true);
        
        if (WaveStartText != null && GameManager.Instance != null)
        {
            WaveStartText.text = $"第 {GameManager.Instance.CurrentWave} 波";
        }
        
        if (WaveStartAnimator != null)
        {
            WaveStartAnimator.SetTrigger("Show");
        }
        
        // 播放音效
        AudioManager.Instance?.PlayWaveStart();
        
        // 延迟隐藏
        Invoke(nameof(HideWaveStartPanel), 2f);
    }
    
    /// <summary>
    /// 隐藏波次开始面板
    /// </summary>
    private void HideWaveStartPanel()
    {
        if (WaveStartPanel != null)
        {
            WaveStartPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 暂停按钮点击
    /// </summary>
    private void OnPauseClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        
        GameManager.Instance?.PauseGame();
        PausePanel?.SetActive(true);
    }
    
    /// <summary>
    /// 速度按钮点击
    /// </summary>
    private void OnSpeedClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        
        // 切换游戏速度
        float currentSpeed = Time.timeScale;
        if (currentSpeed == 1f)
        {
            Time.timeScale = 2f;
            if (SpeedButton != null)
            {
                Text btnText = SpeedButton.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = "2x";
            }
        }
        else
        {
            Time.timeScale = 1f;
            if (SpeedButton != null)
            {
                Text btnText = SpeedButton.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = "1x";
            }
        }
    }
    
    /// <summary>
    /// 恢复游戏
    /// </summary>
    private void OnResumeClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        
        GameManager.Instance?.ResumeGame();
        PausePanel?.SetActive(false);
    }
    
    /// <summary>
    /// 重新开始
    /// </summary>
    private void OnRestartClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }
    
    /// <summary>
    /// 返回主菜单
    /// </summary>
    private void OnMenuClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    /// <summary>
    /// 添加技能图标到显示
    /// </summary>
    public void AddSkillIcon(Sprite icon)
    {
        if (SkillContainer == null || SkillIconPrefab == null) return;
        
        GameObject iconObj = Instantiate(SkillIconPrefab, SkillContainer);
        Image iconImage = iconObj.GetComponent<Image>();
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
        }
    }
    
    private void OnDestroy()
    {
        // 移除事件监听
        PauseButton?.onClick.RemoveListener(OnPauseClick);
        SpeedButton?.onClick.RemoveListener(OnSpeedClick);
        ResumeButton?.onClick.RemoveListener(OnResumeClick);
        RestartButton?.onClick.RemoveListener(OnRestartClick);
        MenuButton?.onClick.RemoveListener(OnMenuClick);
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveStart -= OnWaveStart;
            GameManager.Instance.OnWaveEnd -= OnWaveEnd;
            GameManager.Instance.OnGoldChanged -= OnGoldChanged;
            GameManager.Instance.OnScoreChanged -= OnScoreChanged;
        }
    }
}
