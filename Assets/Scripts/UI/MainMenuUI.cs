using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 主菜单UI - 赛博朋克升级版 (TASK-026)
/// 新增: 3D场景展示、霓虹灯按钮效果、数据流转场动画、全息投影提示
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("=== 主界面 ===")]
    public GameObject MainPanel;
    public Button StartButton;
    public Button SettingsButton;
    public Button HelpButton;
    public Button ExitButton;

    [Header("=== 3D场景展示 (新增) ===")]
    public GameObject SceneDisplay3D;
    public Transform CameraPivot;
    [Range(1f, 30f)]
    public float RotationSpeed = 5f;
    public bool AutoRotate = true;

    [Header("=== 霓虹按钮装饰 (新增) ===")]
    public Image[] ButtonGlowEffects;
    public Image TitleGlowEffect;

    [Header("=== 设置界面 ===")]
    public GameObject SettingsPanel;
    public Slider BGMVolumeSlider;
    public Slider SFXVolumeSlider;
    public Button CloseSettingsButton;

    [Header("=== 帮助界面 ===")]
    public GameObject HelpPanel;
    public Button CloseHelpButton;

    [Header("=== 高分显示 ===")]
    public Text HighScoreText;
    public Text HighWaveText;
    public Text VersionText;

    [Header("=== 加载界面 ===")]
    public GameObject LoadingPanel;
    public Slider LoadingSlider;
    public Text LoadingTipText;
    public string[] LoadingTips;

    [Header("=== 赛博朋克主题 ===")]
    public CyberpunkThemeData ThemeData;
    public CyberpunkThemeUI ThemeUI;

    private void Start()
    {
        // 查找主题组件
        if (ThemeUI == null)
            ThemeUI = GetComponent<CyberpunkThemeUI>();
        if (ThemeUI == null)
            ThemeUI = FindObjectOfType<CyberpunkThemeUI>();

        // 绑定按钮事件
        StartButton?.onClick.AddListener(OnStartClick);
        SettingsButton?.onClick.AddListener(OnSettingsClick);
        HelpButton?.onClick.AddListener(OnHelpClick);
        ExitButton?.onClick.AddListener(OnExitClick);

        CloseSettingsButton?.onClick.AddListener(OnCloseSettingsClick);
        CloseHelpButton?.onClick.AddListener(OnCloseHelpClick);

        // 绑定音量滑块
        BGMVolumeSlider?.onValueChanged.AddListener(OnBGMVolumeChanged);
        SFXVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);

        // 初始化UI
        ShowMainPanel();
        UpdateHighScoreDisplay();
        ApplyCyberpunkStyling();

        // 播放主界面BGM
        AudioManager.Instance?.PlayMainBGM();

        // 开场转场动画
        if (ThemeUI != null)
        {
            StartCoroutine(ThemeUI.TransitionIn());
        }

        // 显示版本号
        if (VersionText != null)
        {
            VersionText.text = $"v{Application.version} // 赛博朋克";
        }
    }

    private void Update()
    {
        // 3D场景自动旋转
        if (AutoRotate && CameraPivot != null && SceneDisplay3D != null && SceneDisplay3D.activeSelf)
        {
            CameraPivot.Rotate(Vector3.up, RotationSpeed * Time.deltaTime);
        }

        // 按钮霓虹光效脉冲
        UpdateButtonGlowEffects();
    }

    /// <summary>
    /// 显示主界面
    /// </summary>
    private void ShowMainPanel()
    {
        MainPanel?.SetActive(true);
        SettingsPanel?.SetActive(false);
        HelpPanel?.SetActive(false);
        LoadingPanel?.SetActive(false);

        if (SceneDisplay3D != null)
            SceneDisplay3D.SetActive(true);

        AutoRotate = true;
    }

    /// <summary>
    /// 开始游戏 - 带数据流转场
    /// </summary>
    private void OnStartClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        StartCoroutine(StartGameWithTransition());
    }

    /// <summary>
    /// 带赛博朋克转场的开始游戏流程
    /// </summary>
    private IEnumerator StartGameWithTransition()
    {
        // 步骤1: 全息投影提示
        if (ThemeUI != null)
        {
            ThemeUI.ShowHoloProjection("CONNECTING TO GRID...");
            yield return new WaitForSeconds(0.3f);
            ThemeUI.ShowHoloProjection("DEPLOYING DEFENSES...");
            yield return new WaitForSeconds(0.3f);
        }

        // 步骤2: 故障效果
        if (ThemeUI != null)
        {
            ThemeUI.TriggerGlitch();
            yield return new WaitForSeconds(0.15f);
        }

        // 步骤3: 转场退出
        if (ThemeUI != null)
        {
            yield return ThemeUI.TransitionOut();
        }

        // 步骤4: 加载游戏场景
        LoadingPanel?.SetActive(true);
        if (SceneDisplay3D != null)
            SceneDisplay3D.SetActive(false);

        // 显示随机加载提示
        if (LoadingTipText != null && LoadingTips != null && LoadingTips.Length > 0)
        {
            LoadingTipText.text = LoadingTips[Random.Range(0, LoadingTips.Length)];
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync("GameScene");
        operation.allowSceneActivation = false;

        float fakeProgress = 0f;
        while (operation.progress < 0.9f || fakeProgress < 0.95f)
        {
            // 平滑进度条（赛博朋克数据流风格）
            float targetProgress = Mathf.Max(operation.progress / 0.9f, fakeProgress + 0.01f);
            fakeProgress = Mathf.Lerp(fakeProgress, targetProgress, Time.deltaTime * 3f);

            if (LoadingSlider != null)
            {
                LoadingSlider.value = fakeProgress;
            }

            // 偶尔更换加载提示
            if (LoadingTipText != null && LoadingTips != null && LoadingTips.Length > 0
                && Random.value < 0.01f)
            {
                LoadingTipText.text = LoadingTips[Random.Range(0, LoadingTips.Length)];
            }

            yield return null;
        }

        LoadingSlider.value = 1f;
        if (LoadingTipText != null)
        {
            LoadingTipText.text = "GRID CONNECTED // LAUNCHING...";
        }
        yield return new WaitForSeconds(0.5f);

        operation.allowSceneActivation = true;
    }

    /// <summary>
    /// 打开设置
    /// </summary>
    private void OnSettingsClick()
    {
        AudioManager.Instance?.PlayButtonClick();

        MainPanel?.SetActive(false);
        SettingsPanel?.SetActive(true);
        if (SceneDisplay3D != null)
            SceneDisplay3D.SetActive(false);

        // 初始化滑块值
        if (BGMVolumeSlider != null && AudioManager.Instance != null)
        {
            BGMVolumeSlider.value = AudioManager.Instance.BGMVolume;
        }
        if (SFXVolumeSlider != null && AudioManager.Instance != null)
        {
            SFXVolumeSlider.value = AudioManager.Instance.SFXVolume;
        }

        // 设置面板全息投影
        ThemeUI?.ShowHoloProjection("SYSTEM CONFIGURATION");
    }

    /// <summary>
    /// 关闭设置
    /// </summary>
    private void OnCloseSettingsClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        ThemeUI?.HideHoloProjection();
        ShowMainPanel();
    }

    /// <summary>
    /// 打开帮助
    /// </summary>
    private void OnHelpClick()
    {
        AudioManager.Instance?.PlayButtonClick();

        MainPanel?.SetActive(false);
        HelpPanel?.SetActive(true);
        if (SceneDisplay3D != null)
            SceneDisplay3D.SetActive(false);

        ThemeUI?.ShowHoloProjection("DATABASE ACCESS // HELP");
    }

    /// <summary>
    /// 关闭帮助
    /// </summary>
    private void OnCloseHelpClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        ThemeUI?.HideHoloProjection();
        ShowMainPanel();
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    private void OnExitClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        StartCoroutine(ExitWithTransition());
    }

    private IEnumerator ExitWithTransition()
    {
        ThemeUI?.ShowHoloProjection("DISCONNECTING FROM GRID...");
        ThemeUI?.TriggerGlitch();
        yield return new WaitForSeconds(0.3f);

        if (ThemeUI != null)
        {
            yield return ThemeUI.TransitionOut();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// BGM音量变化
    /// </summary>
    private void OnBGMVolumeChanged(float value)
    {
        AudioManager.Instance?.SetBGMVolume(value);
    }

    /// <summary>
    /// 音效音量变化
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
    }

    /// <summary>
    /// 更新高分显示
    /// </summary>
    private void UpdateHighScoreDisplay()
    {
        if (HighScoreText != null)
        {
            int highScore = SaveManager.Instance?.GetHighScore() ?? 0;
            HighScoreText.text = $"[HIGH SCORE] {highScore:N0}";
        }

        if (HighWaveText != null)
        {
            int highWave = SaveManager.Instance?.GetHighWave() ?? 0;
            HighWaveText.text = $"[MAX WAVE] {highWave}";
        }
    }

    /// <summary>
    /// 应用赛博朋克主题样式
    /// </summary>
    private void ApplyCyberpunkStyling()
    {
        if (ThemeUI == null || ThemeData == null) return;

        // 样式化所有按钮
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (var btn in allButtons)
        {
            ThemeUI.StyleButtonAsNeon(btn);
        }

        // 样式化面板
        Image[] allPanels = GetComponentsInChildren<Image>(true);
        foreach (var panel in allPanels)
        {
            if (panel.sprite == null && panel.raycastTarget && panel.name.Contains("Panel"))
            {
                ThemeUI.StylePanelAsHolo(panel);
            }
        }
    }

    /// <summary>
    /// 更新按钮霓虹光效
    /// </summary>
    private void UpdateButtonGlowEffects()
    {
        if (ThemeData == null) return;

        // 标题发光脉冲
        if (TitleGlowEffect != null)
        {
            Color glowColor = ThemeData.GetPulsingNeonColor(ThemeData.NeonPink);
            TitleGlowEffect.color = glowColor;
        }

        // 按钮发光效果
        if (ButtonGlowEffects != null)
        {
            foreach (var glow in ButtonGlowEffects)
            {
                if (glow == null) continue;
                Color btnGlow = ThemeData.GetPulsingNeonColor(ThemeData.ElectricBlue, Time.time + glow.GetInstanceID() * 0.5f);
                glow.color = Color.Lerp(glow.color, btnGlow, Time.deltaTime * 3f);
            }
        }
    }

    /// <summary>
    /// 公共方法：显示全息提示
    /// </summary>
    public void ShowHoloTip(string message)
    {
        ThemeUI?.ShowHoloProjection(message);
    }

    /// <summary>
    /// 公共方法：隐藏全息提示
    /// </summary>
    public void HideHoloTip()
    {
        ThemeUI?.HideHoloProjection();
    }

    private void OnDestroy()
    {
        // 移除事件监听
        StartButton?.onClick.RemoveListener(OnStartClick);
        SettingsButton?.onClick.RemoveListener(OnSettingsClick);
        HelpButton?.onClick.RemoveListener(OnHelpClick);
        ExitButton?.onClick.RemoveListener(OnExitClick);

        CloseSettingsButton?.onClick.RemoveListener(OnCloseSettingsClick);
        CloseHelpButton?.onClick.RemoveListener(OnCloseHelpClick);

        BGMVolumeSlider?.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        SFXVolumeSlider?.onValueChanged.RemoveListener(OnSFXVolumeChanged);
    }
}
