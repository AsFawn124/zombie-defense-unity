using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 游戏HUD界面 - 赛博朋克升级版 (TASK-027)
/// 新增: 赛博朋克风格HUD、像素风格伤害数字、技能特效优化、受击反馈增强
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("=== 顶部信息 ===")]
    public Text WaveText;
    public Text ScoreText;
    public Text GoldText;

    [Header("=== 基地血量 ===")]
    public Slider BaseHealthSlider;
    public Text BaseHealthText;
    public Image HealthFillImage;

    [Header("=== 敌人信息 ===")]
    public Text EnemyCountText;

    [Header("=== 技能显示 ===")]
    public Transform SkillContainer;
    public GameObject SkillIconPrefab;

    [Header("=== 控制按钮 ===")]
    public Button PauseButton;
    public Button SpeedButton;
    public Text SpeedButtonText;

    [Header("=== 暂停菜单 ===")]
    public GameObject PausePanel;
    public Button ResumeButton;
    public Button RestartButton;
    public Button MenuButton;

    [Header("=== 波次提示 ===")]
    public GameObject WaveStartPanel;
    public Text WaveStartText;
    public Animator WaveStartAnimator;

    [Header("=== 赛博朋克HUD (新增) ===")]
    public CyberpunkHUD CyberHUD;
    public CyberpunkThemeData ThemeData;

    [Header("=== 金币获取特效 (新增) ===")]
    public GameObject GoldGainEffect;
    public Transform GoldEffectContainer;

    private float _previousHealthPercent = 1f;

    private void Start()
    {
        // 自动查找组件
        if (CyberHUD == null)
            CyberHUD = GetComponent<CyberpunkHUD>();
        if (CyberHUD == null)
            CyberHUD = FindObjectOfType<CyberpunkHUD>();

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

        // 应用赛博朋克主题
        CyberHUD?.ApplyThemeToBattleUI();
    }

    private void Update()
    {
        // 实时更新
        float healthPercent = UpdateBaseHealth();
        UpdateEnemyCount();

        // 低血量警告
        CyberHUD?.UpdateLowHealthWarning(healthPercent);

        // 受击检测
        if (healthPercent < _previousHealthPercent)
        {
            float damagePercent = _previousHealthPercent - healthPercent;
            CyberHUD?.TriggerHitFeedback(damagePercent);
        }
        _previousHealthPercent = healthPercent;
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
    /// 更新波次显示 - 赛博朋克风格
    /// </summary>
    private void UpdateWaveDisplay()
    {
        if (WaveText != null && GameManager.Instance != null)
        {
            WaveText.text = $"[WAVE {GameManager.Instance.CurrentWave:D2}]";
        }
    }

    /// <summary>
    /// 更新分数显示
    /// </summary>
    private void UpdateScoreDisplay()
    {
        if (ScoreText != null && GameManager.Instance != null)
        {
            ScoreText.text = $"[SCORE] {GameManager.Instance.Score:N0}";
        }
    }

    /// <summary>
    /// 更新金币显示
    /// </summary>
    private void UpdateGoldDisplay()
    {
        if (GoldText != null && GameManager.Instance != null)
        {
            if (ThemeData != null)
            {
                GoldText.text = $"[CREDITS] <color=#{ColorUtility.ToHtmlStringRGB(ThemeData.GoldYellow)}>{GameManager.Instance.Gold}</color>";
            }
            else
            {
                GoldText.text = $"[CREDITS] {GameManager.Instance.Gold}";
            }
        }
    }

    /// <summary>
    /// 更新基地血量 - 返回血量百分比
    /// </summary>
    private float UpdateBaseHealth()
    {
        if (BaseManager.Instance == null) return 1f;

        float healthPercent = (float)BaseManager.Instance.CurrentHealth / BaseManager.Instance.MaxHealth;

        if (BaseHealthSlider != null)
        {
            BaseHealthSlider.value = healthPercent;
        }

        // 血量颜色变化（赛博朋克风格）
        if (HealthFillImage != null && ThemeData != null)
        {
            if (healthPercent > 0.6f)
                HealthFillImage.color = ThemeData.NeonGreen;
            else if (healthPercent > 0.3f)
                HealthFillImage.color = ThemeData.WarningOrange;
            else
                HealthFillImage.color = ThemeData.DangerRed;
        }

        if (BaseHealthText != null)
        {
            BaseHealthText.text = $"{BaseManager.Instance.CurrentHealth}/{BaseManager.Instance.MaxHealth}";
        }

        return healthPercent;
    }

    /// <summary>
    /// 更新敌人数量
    /// </summary>
    private void UpdateEnemyCount()
    {
        if (EnemyCountText != null && WaveManager.Instance != null)
        {
            int count = WaveManager.Instance.GetActiveEnemyCount();
            EnemyCountText.text = $"[HOSTILES] {count}";

            // 大量敌人时变红
            if (ThemeData != null && count > 20)
            {
                EnemyCountText.color = ThemeData.GetPulsingNeonColor(ThemeData.DangerRed);
            }
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

        // 金币获得特效
        if (GoldGainEffect != null && GoldEffectContainer != null)
        {
            GameObject effect = Instantiate(GoldGainEffect, GoldEffectContainer);
            Destroy(effect, 1.5f);
        }
    }

    /// <summary>
    /// 分数变化
    /// </summary>
    private void OnScoreChanged(int score)
    {
        UpdateScoreDisplay();
    }

    /// <summary>
    /// 显示波次开始动画 - 赛博朋克风格
    /// </summary>
    private void ShowWaveStartAnimation()
    {
        if (WaveStartPanel == null) return;

        WaveStartPanel.SetActive(true);

        if (WaveStartText != null && GameManager.Instance != null)
        {
            WaveStartText.text = $">> WAVE {GameManager.Instance.CurrentWave:D2} INCOMING <<";
        }

        if (WaveStartAnimator != null)
        {
            WaveStartAnimator.SetTrigger("Show");
        }

        // 播放音效
        AudioManager.Instance?.PlayWaveStart();

        // 故障效果
        var themeUI = FindObjectOfType<CyberpunkThemeUI>();
        if (themeUI != null)
        {
            themeUI.TriggerGlitch();
        }

        // 延迟隐藏
        Invoke(nameof(HideWaveStartPanel), 2.5f);
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

        // 暂停时触发轻微故障效果
        var themeUI = FindObjectOfType<CyberpunkThemeUI>();
        themeUI?.ShowHoloProjection("SYSTEM PAUSED");
    }

    /// <summary>
    /// 速度按钮点击
    /// </summary>
    private void OnSpeedClick()
    {
        AudioManager.Instance?.PlayButtonClick();

        float currentSpeed = Time.timeScale;
        if (currentSpeed == 1f)
        {
            Time.timeScale = 2f;
            if (SpeedButtonText != null) SpeedButtonText.text = ">> 2x";
            else if (SpeedButton != null)
            {
                var txt = SpeedButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = ">> 2x";
            }
        }
        else
        {
            Time.timeScale = 1f;
            if (SpeedButtonText != null) SpeedButtonText.text = "> 1x";
            else if (SpeedButton != null)
            {
                var txt = SpeedButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = "> 1x";
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

        var themeUI = FindObjectOfType<CyberpunkThemeUI>();
        themeUI?.HideHoloProjection();
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
        StartCoroutine(ReturnToMenuWithTransition());
    }

    private IEnumerator ReturnToMenuWithTransition()
    {
        var themeUI = FindObjectOfType<CyberpunkThemeUI>();
        themeUI?.ShowHoloProjection("RETURNING TO GRID...");

        yield return new WaitForSeconds(0.3f);

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

    /// <summary>
    /// 显示伤害数字（委托给CyberpunkHUD）
    /// </summary>
    public void ShowDamageAt(Vector3 worldPos, float damage, bool isCrit = false, ElementType element = ElementType.None)
    {
        CyberHUD?.ShowDamageNumber(worldPos, damage, isCrit, element);
    }

    /// <summary>
    /// 记录击杀
    /// </summary>
    public void RegisterKill()
    {
        CyberHUD?.RegisterKill();
    }

    /// <summary>
    /// 触发屏幕震动
    /// </summary>
    public void TriggerShake(float intensity = 0.5f)
    {
        CyberHUD?.TriggerShake(intensity);
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
