using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 音频管理器 - 统一管理游戏音效和音乐
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("音频源")]
    public AudioSource BGMSource;           // 背景音乐源
    public AudioSource SFXSource;           // 音效源
    public AudioSource UISource;            // UI音效源
    
    [Header("音量设置")]
    [Range(0f, 1f)]
    public float BGMVolume = 0.5f;
    [Range(0f, 1f)]
    public float SFXVolume = 0.8f;
    [Range(0f, 1f)]
    public float UIVolume = 0.8f;
    
    [Header("音频剪辑")]
    public AudioClip MainBGM;               // 主界面BGM
    public AudioClip BattleBGM;             // 战斗BGM
    public AudioClip BossBGM;               // BOSS战BGM
    public AudioClip VictoryBGM;            // 胜利BGM
    public AudioClip DefeatBGM;             // 失败BGM
    
    [Header("音效")]
    public AudioClip ButtonClick;           // 按钮点击
    public AudioClip TowerShoot;            // 塔射击
    public AudioClip EnemyHit;              // 敌人受击
    public AudioClip EnemyDeath;            // 敌人死亡
    public AudioClip WaveStart;             // 波次开始
    public AudioClip WaveComplete;          // 波次完成
    public AudioClip SkillSelect;           // 选择技能
    public AudioClip LevelUp;               // 升级
    public AudioClip GoldPick;              // 拾取金币
    public AudioClip BaseDamage;            // 基地受伤
    public AudioClip GameOver;              // 游戏结束
    
    [Header("资源路径")]
    private const string AUDIO_PATH = "Audio/";
    
    /// <summary>
    /// 从Resources加载音频
    /// </summary>
    public void LoadAudioFromResources()
    {
        MainBGM = Resources.Load<AudioClip>(AUDIO_PATH + "bgm_main");
        BattleBGM = Resources.Load<AudioClip>(AUDIO_PATH + "bgm_battle");
        BossBGM = Resources.Load<AudioClip>(AUDIO_PATH + "bgm_boss");
        VictoryBGM = Resources.Load<AudioClip>(AUDIO_PATH + "bgm_victory");
        DefeatBGM = Resources.Load<AudioClip>(AUDIO_PATH + "bgm_defeat");
        
        ButtonClick = Resources.Load<AudioClip>(AUDIO_PATH + "sfx_button");
        TowerShoot = Resources.Load<AudioClip>(AUDIO_PATH + "sfx_shoot");
        EnemyHit = Resources.Load<AudioClip>(AUDIO_PATH + "sfx_hit");
        EnemyDeath = Resources.Load<AudioClip>(AUDIO_PATH + "sfx_explosion");
        WaveStart = Resources.Load<AudioClip>(AUDIO_PATH + "sfx_wave_start");
        GoldPick = Resources.Load<AudioClip>(AUDIO_PATH + "sfx_coin");
        
        Debug.Log("音频资源加载完成");
    }
    
    // 音频缓存
    private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();
    private AudioClip currentBGM;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        LoadVolumeSettings();
    }
    
    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitializeAudioSources()
    {
        // 确保有BGM源
        if (BGMSource == null)
        {
            GameObject bgmObj = new GameObject("BGM_Source");
            bgmObj.transform.SetParent(transform);
            BGMSource = bgmObj.AddComponent<AudioSource>();
            BGMSource.loop = true;
            BGMSource.playOnAwake = false;
        }
        
        // 确保有SFX源
        if (SFXSource == null)
        {
            GameObject sfxObj = new GameObject("SFX_Source");
            sfxObj.transform.SetParent(transform);
            SFXSource = sfxObj.AddComponent<AudioSource>();
            SFXSource.loop = false;
            SFXSource.playOnAwake = false;
        }
        
        // 确保有UI源
        if (UISource == null)
        {
            GameObject uiObj = new GameObject("UI_Source");
            uiObj.transform.SetParent(transform);
            UISource = uiObj.AddComponent<AudioSource>();
            UISource.loop = false;
            UISource.playOnAwake = false;
        }
    }
    
    #region BGM控制
    
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBGM(AudioClip clip, bool fadeIn = true)
    {
        if (clip == null || clip == currentBGM) return;
        
        currentBGM = clip;
        
        if (fadeIn)
        {
            StartCoroutine(FadeInBGM(clip, 1f));
        }
        else
        {
            BGMSource.clip = clip;
            BGMSource.volume = BGMVolume;
            BGMSource.Play();
        }
    }
    
    /// <summary>
    /// 播放主界面BGM
    /// </summary>
    public void PlayMainBGM()
    {
        PlayBGM(MainBGM);
    }
    
    /// <summary>
    /// 播放战斗BGM
    /// </summary>
    public void PlayBattleBGM()
    {
        PlayBGM(BattleBGM);
    }
    
    /// <summary>
    /// 播放BOSS战BGM
    /// </summary>
    public void PlayBossBGM()
    {
        PlayBGM(BossBGM);
    }
    
    /// <summary>
    /// 播放胜利BGM
    /// </summary>
    public void PlayVictoryBGM()
    {
        PlayBGM(VictoryBGM);
    }
    
    /// <summary>
    /// 播放失败BGM
    /// </summary>
    public void PlayDefeatBGM()
    {
        PlayBGM(DefeatBGM);
    }
    
    /// <summary>
    /// 停止BGM
    /// </summary>
    public void StopBGM(bool fadeOut = true)
    {
        if (fadeOut)
        {
            StartCoroutine(FadeOutBGM(1f));
        }
        else
        {
            BGMSource.Stop();
        }
        currentBGM = null;
    }
    
    /// <summary>
    /// 暂停BGM
    /// </summary>
    public void PauseBGM()
    {
        BGMSource.Pause();
    }
    
    /// <summary>
    /// 恢复BGM
    /// </summary>
    public void ResumeBGM()
    {
        BGMSource.UnPause();
    }
    
    #endregion
    
    #region 音效播放
    
    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        SFXSource.PlayOneShot(clip, SFXVolume * volumeScale);
    }
    
    /// <summary>
    /// 播放UI音效
    /// </summary>
    public void PlayUI(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        UISource.PlayOneShot(clip, UIVolume * volumeScale);
    }
    
    // 便捷方法
    public void PlayButtonClick() => PlayUI(ButtonClick);
    public void PlayTowerShoot() => PlaySFX(TowerShoot, 0.5f);
    public void PlayEnemyHit() => PlaySFX(EnemyHit, 0.3f);
    public void PlayEnemyDeath() => PlaySFX(EnemyDeath);
    public void PlayWaveStart() => PlaySFX(WaveStart);
    public void PlayWaveComplete() => PlaySFX(WaveComplete);
    public void PlaySkillSelect() => PlayUI(SkillSelect);
    public void PlayLevelUp() => PlaySFX(LevelUp);
    public void PlayGoldPick() => PlaySFX(GoldPick, 0.5f);
    public void PlayBaseDamage() => PlaySFX(BaseDamage);
    public void PlayGameOver() => PlaySFX(GameOver);
    
    #endregion
    
    #region 音量控制
    
    /// <summary>
    /// 设置BGM音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        BGMVolume = Mathf.Clamp01(volume);
        BGMSource.volume = BGMVolume;
        SaveVolumeSettings();
    }
    
    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
    }
    
    /// <summary>
    /// 设置UI音量
    /// </summary>
    public void SetUIVolume(float volume)
    {
        UIVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
    }
    
    /// <summary>
    /// 静音切换
    /// </summary>
    public void ToggleMute()
    {
        AudioListener.pause = !AudioListener.pause;
    }
    
    /// <summary>
    /// 保存音量设置
    /// </summary>
    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("BGMVolume", BGMVolume);
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
        PlayerPrefs.SetFloat("UIVolume", UIVolume);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 加载音量设置
    /// </summary>
    private void LoadVolumeSettings()
    {
        BGMVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        UIVolume = PlayerPrefs.GetFloat("UIVolume", 0.8f);
        
        BGMSource.volume = BGMVolume;
    }
    
    #endregion
    
    #region 淡入淡出
    
    private System.Collections.IEnumerator FadeInBGM(AudioClip clip, float duration)
    {
        float startVolume = 0f;
        BGMSource.clip = clip;
        BGMSource.volume = startVolume;
        BGMSource.Play();
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            BGMSource.volume = Mathf.Lerp(startVolume, BGMVolume, elapsed / duration);
            yield return null;
        }
        
        BGMSource.volume = BGMVolume;
    }
    
    private System.Collections.IEnumerator FadeOutBGM(float duration)
    {
        float startVolume = BGMSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            BGMSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        
        BGMSource.Stop();
        BGMSource.volume = BGMVolume;
    }
    
    #endregion
}
