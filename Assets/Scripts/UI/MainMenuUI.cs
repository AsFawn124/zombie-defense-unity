using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 主菜单UI
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("主界面")]
    public GameObject MainPanel;
    public Button StartButton;
    public Button SettingsButton;
    public Button HelpButton;
    public Button ExitButton;
    
    [Header("设置界面")]
    public GameObject SettingsPanel;
    public Slider BGMVolumeSlider;
    public Slider SFXVolumeSlider;
    public Button CloseSettingsButton;
    
    [Header("帮助界面")]
    public GameObject HelpPanel;
    public Button CloseHelpButton;
    
    [Header("高分显示")]
    public Text HighScoreText;
    public Text HighWaveText;
    
    [Header("其他")]
    public GameObject LoadingPanel;
    public Slider LoadingSlider;
    
    private void Start()
    {
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
        
        // 播放主界面BGM
        AudioManager.Instance?.PlayMainBGM();
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
    }
    
    /// <summary>
    /// 开始游戏
    /// </summary>
    private void OnStartClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        StartCoroutine(LoadGameScene());
    }
    
    /// <summary>
    /// 加载游戏场景
    /// </summary>
    private System.Collections.IEnumerator LoadGameScene()
    {
        LoadingPanel?.SetActive(true);
        
        AsyncOperation operation = SceneManager.LoadSceneAsync("GameScene");
        operation.allowSceneActivation = false;
        
        while (operation.progress < 0.9f)
        {
            if (LoadingSlider != null)
            {
                LoadingSlider.value = operation.progress;
            }
            yield return null;
        }
        
        LoadingSlider.value = 1f;
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
        
        // 初始化滑块值
        if (BGMVolumeSlider != null && AudioManager.Instance != null)
        {
            BGMVolumeSlider.value = AudioManager.Instance.BGMVolume;
        }
        if (SFXVolumeSlider != null && AudioManager.Instance != null)
        {
            SFXVolumeSlider.value = AudioManager.Instance.SFXVolume;
        }
    }
    
    /// <summary>
    /// 关闭设置
    /// </summary>
    private void OnCloseSettingsClick()
    {
        AudioManager.Instance?.PlayButtonClick();
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
    }
    
    /// <summary>
    /// 关闭帮助
    /// </summary>
    private void OnCloseHelpClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        ShowMainPanel();
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    private void OnExitClick()
    {
        AudioManager.Instance?.PlayButtonClick();
        
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
            HighScoreText.text = $"最高分: {highScore}";
        }
        
        if (HighWaveText != null)
        {
            int highWave = SaveManager.Instance?.GetHighWave() ?? 0;
            HighWaveText.text = $"最高波次: {highWave}";
        }
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
