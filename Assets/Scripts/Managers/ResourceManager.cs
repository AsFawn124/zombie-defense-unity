using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 资源管理器 - 统一管理游戏资源加载
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    
    [Header("资源路径")]
    private const string AUDIO_PATH = "Audio/";
    private const string SPRITE_PATH = "Sprites/";
    private const string PREFAB_PATH = "Prefabs/";
    private const string EFFECT_PATH = "Effects/";
    private const string CONFIG_PATH = "PrefabConfigs/";
    
    // 资源缓存
    private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
    private Dictionary<string, ScriptableObject> configCache = new Dictionary<string, ScriptableObject>();
    
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
    
    #region 音频资源
    
    /// <summary>
    /// 加载音频
    /// </summary>
    public AudioClip LoadAudio(string name)
    {
        if (audioCache.ContainsKey(name))
        {
            return audioCache[name];
        }
        
        AudioClip clip = Resources.Load<AudioClip>(AUDIO_PATH + name);
        if (clip != null)
        {
            audioCache[name] = clip;
        }
        else
        {
            Debug.LogWarning($"音频资源未找到: {name}");
        }
        
        return clip;
    }
    
    /// <summary>
    /// 预加载所有音频
    /// </summary>
    public void PreloadAllAudio()
    {
        string[] audioNames = new string[]
        {
            "bgm_main", "bgm_battle", "bgm_boss", "bgm_victory", "bgm_defeat",
            "sfx_shoot", "sfx_hit", "sfx_explosion", "sfx_button", 
            "sfx_upgrade", "sfx_coin", "sfx_wave_start"
        };
        
        foreach (string name in audioNames)
        {
            LoadAudio(name);
        }
        
        Debug.Log("所有音频资源预加载完成");
    }
    
    #endregion
    
    #region 精灵资源
    
    /// <summary>
    /// 加载精灵
    /// </summary>
    public Sprite LoadSprite(string name)
    {
        if (spriteCache.ContainsKey(name))
        {
            return spriteCache[name];
        }
        
        Sprite sprite = Resources.Load<Sprite>(SPRITE_PATH + name);
        if (sprite != null)
        {
            spriteCache[name] = sprite;
        }
        else
        {
            Debug.LogWarning($"精灵资源未找到: {name}");
        }
        
        return sprite;
    }
    
    /// <summary>
    /// 预加载所有精灵
    /// </summary>
    public void PreloadAllSprites()
    {
        string[] spriteNames = new string[]
        {
            "Tower_Base", "Tower_Sniper", "Tower_Cannon",
            "Enemy_Normal", "Enemy_Fast", "Enemy_Tank", "Enemy_Bomber", 
            "Enemy_Healer", "Enemy_Split", "Enemy_Elite", "Enemy_Boss",
            "Base", "Bullet", "Range_Indicator"
        };
        
        foreach (string name in spriteNames)
        {
            LoadSprite(name);
        }
        
        Debug.Log("所有精灵资源预加载完成");
    }
    
    #endregion
    
    #region 预制体资源
    
    /// <summary>
    /// 加载预制体
    /// </summary>
    public GameObject LoadPrefab(string name)
    {
        if (prefabCache.ContainsKey(name))
        {
            return prefabCache[name];
        }
        
        GameObject prefab = Resources.Load<GameObject>(PREFAB_PATH + name);
        if (prefab != null)
        {
            prefabCache[name] = prefab;
        }
        else
        {
            Debug.LogWarning($"预制体资源未找到: {name}");
        }
        
        return prefab;
    }
    
    /// <summary>
    /// 实例化预制体
    /// </summary>
    public GameObject InstantiatePrefab(string name, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject prefab = LoadPrefab(name);
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, position, rotation, parent);
            return instance;
        }
        return null;
    }
    
    #endregion
    
    #region 特效资源
    
    /// <summary>
    /// 加载特效
    /// </summary>
    public GameObject LoadEffect(string name)
    {
        return Resources.Load<GameObject>(EFFECT_PATH + name);
    }
    
    /// <summary>
    /// 播放特效
    /// </summary>
    public void PlayEffect(string name, Vector3 position, Quaternion rotation)
    {
        GameObject effectPrefab = LoadEffect(name);
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, rotation);
            Destroy(effect, 2f);
        }
    }
    
    #endregion
    
    #region 配置资源
    
    /// <summary>
    /// 加载配置
    /// </summary>
    public T LoadConfig<T>(string name) where T : ScriptableObject
    {
        string key = typeof(T).Name + "_" + name;
        
        if (configCache.ContainsKey(key))
        {
            return configCache[key] as T;
        }
        
        T config = Resources.Load<T>(CONFIG_PATH + name);
        if (config != null)
        {
            configCache[key] = config;
        }
        else
        {
            Debug.LogWarning($"配置资源未找到: {name}");
        }
        
        return config;
    }
    
    #endregion
    
    #region 资源清理
    
    /// <summary>
    /// 清理资源缓存
    /// </summary>
    public void ClearCache()
    {
        audioCache.Clear();
        spriteCache.Clear();
        prefabCache.Clear();
        configCache.Clear();
        
        Resources.UnloadUnusedAssets();
        
        Debug.Log("资源缓存已清理");
    }
    
    /// <summary>
    /// 预加载所有资源
    /// </summary>
    public void PreloadAllResources()
    {
        PreloadAllAudio();
        PreloadAllSprites();
        
        Debug.Log("所有资源预加载完成");
    }
    
    #endregion
}
