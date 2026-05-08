using UnityEngine;

/// <summary>
/// 存档管理器
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    
    private const string HIGH_SCORE_KEY = "HighScore";
    private const string HIGH_WAVE_KEY = "HighWave";
    private const string TOTAL_KILLS_KEY = "TotalKills";
    private const string SETTINGS_KEY = "GameSettings";
    
    [System.Serializable]
    public class GameSettings
    {
        public float BGMVolume = 0.5f;
        public float SFXVolume = 0.8f;
        public bool IsMuted = false;
    }
    
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
    
    /// <summary>
    /// 保存最高分
    /// </summary>
    public void SaveHighScore(int score)
    {
        int currentHigh = GetHighScore();
        if (score > currentHigh)
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, score);
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// 获取最高分
    /// </summary>
    public int GetHighScore()
    {
        return PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }
    
    /// <summary>
    /// 保存最高波次
    /// </summary>
    public void SaveHighWave(int wave)
    {
        int currentHigh = GetHighWave();
        if (wave > currentHigh)
        {
            PlayerPrefs.SetInt(HIGH_WAVE_KEY, wave);
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// 获取最高波次
    /// </summary>
    public int GetHighWave()
    {
        return PlayerPrefs.GetInt(HIGH_WAVE_KEY, 0);
    }
    
    /// <summary>
    /// 保存总击杀数
    /// </summary>
    public void AddTotalKills(int kills)
    {
        int total = GetTotalKills();
        total += kills;
        PlayerPrefs.SetInt(TOTAL_KILLS_KEY, total);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 获取总击杀数
    /// </summary>
    public int GetTotalKills()
    {
        return PlayerPrefs.GetInt(TOTAL_KILLS_KEY, 0);
    }
    
    /// <summary>
    /// 保存设置
    /// </summary>
    public void SaveSettings(GameSettings settings)
    {
        string json = JsonUtility.ToJson(settings);
        PlayerPrefs.SetString(SETTINGS_KEY, json);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 加载设置
    /// </summary>
    public GameSettings LoadSettings()
    {
        string json = PlayerPrefs.GetString(SETTINGS_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            return new GameSettings();
        }
        return JsonUtility.FromJson<GameSettings>(json);
    }
    
    /// <summary>
    /// 清除所有存档
    /// </summary>
    public void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
