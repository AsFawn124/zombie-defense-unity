using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 远程配置管理器 - LiveOps基础设施
/// 支持: 远程参数下发、强制更新检测、功能开关、区服差异化配置
/// </summary>
public class RemoteConfigManager : MonoBehaviour
{
    public static RemoteConfigManager Instance;

    [Header("配置")]
    public string RemoteConfigUrl = "https://api.game.com/config";
    public float RefreshIntervalMinutes = 30f; // 自动刷新间隔
    public bool UseLocalFallback = true;

    // 远程配置缓存
    private Dictionary<string, ConfigEntry> _configCache = new Dictionary<string, ConfigEntry>();
    private Dictionary<string, FeatureFlag> _featureFlags = new Dictionary<string, FeatureFlag>();
    private RemoteConfigMetadata _metadata;

    private float _lastRefreshTime;
    private bool _isInitialized;

    // 事件
    public event Action OnConfigRefreshed;
    public event Action<string, object, object> OnConfigValueChanged; // key, oldValue, newValue
    public event Action<string, bool> OnFeatureFlagChanged; // flag, enabled

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeDefaultConfig();
        RefreshConfig();
    }

    private void Update()
    {
        if (_isInitialized && Time.time - _lastRefreshTime > RefreshIntervalMinutes * 60)
        {
            RefreshConfig();
        }
    }

    #region === 初始化默认配置 ===

    private void InitializeDefaultConfig()
    {
        // 战斗默认值
        SetDefault("battle.base_tower_damage", 100f, "基础塔伤害");
        SetDefault("battle.base_enemy_health", 50f, "基础敌人血量");
        SetDefault("battle.wave_gold_multiplier", 1.0f, "波次金币倍率");
        SetDefault("battle.max_waves", 100, "最大波次");

        // 经济默认值
        SetDefault("economy.gold_per_kill", 10f, "击杀金币");
        SetDefault("economy.diamond_per_ad", 5, "广告钻石");
        SetDefault("economy.merge_cost_multiplier", 2.0f, "合成花费倍率");

        // 掉落默认值
        SetDefault("drop.equipment_rate", 0.15f, "装备掉落率");
        SetDefault("drop.chip_rate", 0.10f, "芯片掉落率");
        SetDefault("drop.rare_multiplier", 1.0f, "稀有物品倍率");
        SetDefault("drop.event_item_rate", 0.25f, "活动道具掉落率");

        // 商业化默认值
        SetDefault("commerce.ad_cooldown", 300f, "广告冷却(秒)");
        SetDefault("commerce.free_skill_refresh_daily", 3, "每日免费刷新次数");
        SetDefault("commerce.battle_pass_price", 30, "通行证价格");

        // UI/UX默认值
        SetDefault("ui.show_damage_numbers", true, "显示伤害数字");
        SetDefault("ui.show_tower_range", true, "显示塔范围");
        SetDefault("ui.auto_skip_tutorial", false, "自动跳过教程");

        // 功能开关
        SetFeatureFlag("arena_enabled", true, "竞技场开关");
        SetFeatureFlag("guild_enabled", true, "公会开关");
        SetFeatureFlag("coop_enabled", true, "合作模式开关");
        SetFeatureFlag("season_enabled", true, "赛季开关");
        SetFeatureFlag("ugc_enabled", true, "UGC关卡编辑器");
        SetFeatureFlag("limited_gacha_enabled", false, "限定卡池(活动期间开启)");
        SetFeatureFlag("double_reward_event", false, "双倍奖励活动");
        SetFeatureFlag("boss_rush_event", false, "BOSS冲刺活动");
    }

    private void SetDefault<T>(string key, T value, string description)
    {
        _configCache[key] = new ConfigEntry
        {
            Key = key,
            Value = value.ToString(),
            DefaultValue = value.ToString(),
            Description = description,
            LastUpdated = DateTime.MinValue,
            ValueType = typeof(T).Name
        };
    }

    private void SetFeatureFlag(string key, bool defaultValue, string description)
    {
        _featureFlags[key] = new FeatureFlag
        {
            Key = key,
            Enabled = defaultValue,
            DefaultEnabled = defaultValue,
            Description = description
        };
    }

    #endregion

    #region === 远程刷新 ===

    /// <summary>
    /// 从远程服务器拉取最新配置
    /// </summary>
    public async void RefreshConfig()
    {
        _lastRefreshTime = Time.time;

        try
        {
            // 实际实现使用UnityWebRequest
            // using var www = UnityWebRequest.Get(RemoteConfigUrl + "?platform=wechat&version=" + Application.version);
            // await www.SendWebRequest();

            // 模拟远程配置
            var mockRemoteConfig = GetMockRemoteConfig();

            if (mockRemoteConfig != null)
            {
                ApplyRemoteConfig(mockRemoteConfig);
                _isInitialized = true;
                OnConfigRefreshed?.Invoke();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RemoteConfig] 刷新失败，使用本地配置: {e.Message}");
            if (!_isInitialized && UseLocalFallback)
            {
                _isInitialized = true;
            }
        }
    }

    private RemoteConfigResponse GetMockRemoteConfig()
    {
        // 模拟远程下发 - 接入真实服务端后替换
        return new RemoteConfigResponse
        {
            Version = "1.0.0",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Configs = new Dictionary<string, object>
            {
                // 双倍掉落活动期间
                ["drop.event_item_rate"] = IsFeatureEnabled("double_reward_event") ? 0.50f : 0.25f,
                // 难度动态调整
                ["battle.enemy_health_scale"] = 1.0f,
            },
            FeatureFlags = new Dictionary<string, bool>
            {
                ["arena_enabled"] = true,
                ["guild_enabled"] = true,
                ["coop_enabled"] = true,
                ["season_enabled"] = true,
                ["ugc_enabled"] = true,
                ["limited_gacha_enabled"] = false, // 运营手动开启
                ["double_reward_event"] = false,
                ["boss_rush_event"] = false,
            },
            ForceUpdate = new ForceUpdateInfo
            {
                MinimumVersion = "1.0.0",
                LatestVersion = "1.2.0",
                UpdateUrl = "https://game.com/update",
                ForceUpdateRequired = false
            },
            Announcements = new List<Announcement>
            {
                new Announcement
                {
                    Id = "ann_001",
                    Title = "欢迎来到僵尸防线!",
                    Content = "新赛季'赛博觉醒'已上线，全新英雄和装备等你来拿！",
                    Priority = 1,
                    ShowUntil = DateTime.UtcNow.AddDays(7).Ticks
                }
            }
        };
    }

    private void ApplyRemoteConfig(RemoteConfigResponse response)
    {
        // 应用参数配置
        if (response.Configs != null)
        {
            foreach (var kvp in response.Configs)
            {
                if (_configCache.ContainsKey(kvp.Key))
                {
                    var oldValue = _configCache[kvp.Key].Value;
                    var newValue = kvp.Value.ToString();
                    if (oldValue != newValue)
                    {
                        _configCache[kvp.Key].Value = newValue;
                        _configCache[kvp.Key].LastUpdated = DateTime.UtcNow;
                        OnConfigValueChanged?.Invoke(kvp.Key, oldValue, kvp.Value);
                    }
                }
                else
                {
                    _configCache[kvp.Key] = new ConfigEntry
                    {
                        Key = kvp.Key,
                        Value = kvp.Value.ToString(),
                        DefaultValue = kvp.Value.ToString(),
                        LastUpdated = DateTime.UtcNow
                    };
                }
            }
        }

        // 应用功能开关
        if (response.FeatureFlags != null)
        {
            foreach (var kvp in response.FeatureFlags)
            {
                if (_featureFlags.ContainsKey(kvp.Key))
                {
                    var old = _featureFlags[kvp.Key].Enabled;
                    if (old != kvp.Value)
                    {
                        _featureFlags[kvp.Key].Enabled = kvp.Value;
                        OnFeatureFlagChanged?.Invoke(kvp.Key, kvp.Value);
                    }
                }
                else
                {
                    _featureFlags[kvp.Key] = new FeatureFlag
                    {
                        Key = kvp.Key,
                        Enabled = kvp.Value,
                        DefaultEnabled = kvp.Value
                    };
                }
            }
        }

        // 检查强制更新
        if (response.ForceUpdate != null)
        {
            _metadata.ForceUpdateInfo = response.ForceUpdate;
        }

        // 公告
        if (response.Announcements != null)
        {
            _metadata.Announcements = response.Announcements;
        }

        SaveCachedConfig();
    }

    #endregion

    #region === 类型安全的读取接口 ===

    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (_configCache.TryGetValue(key, out var entry))
        {
            if (float.TryParse(entry.Value, out float val))
                return val;
        }
        return defaultValue;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (_configCache.TryGetValue(key, out var entry))
        {
            if (int.TryParse(entry.Value, out int val))
                return val;
        }
        return defaultValue;
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        if (_configCache.TryGetValue(key, out var entry))
        {
            if (bool.TryParse(entry.Value, out bool val))
                return val;
        }
        return defaultValue;
    }

    public string GetString(string key, string defaultValue = "")
    {
        if (_configCache.TryGetValue(key, out var entry))
            return entry.Value;
        return defaultValue;
    }

    public bool IsFeatureEnabled(string key)
    {
        if (_featureFlags.TryGetValue(key, out var flag))
            return flag.Enabled;
        return false;
    }

    public ForceUpdateInfo GetForceUpdateInfo() => _metadata?.ForceUpdateInfo;

    public List<Announcement> GetAnnouncements() => _metadata?.Announcements ?? new List<Announcement>();

    /// <summary>
    /// 获取所有配置项 (调试面板)
    /// </summary>
    public Dictionary<string, ConfigEntry> GetAllConfigs() => _configCache;
    public Dictionary<string, FeatureFlag> GetAllFeatureFlags() => _featureFlags;

    #endregion

    #region === AB测试支持 ===

    private string _abTestVariant;

    /// <summary>
    /// 获取当前AB测试分组
    /// </summary>
    public string GetABTestVariant()
    {
        if (string.IsNullOrEmpty(_abTestVariant))
        {
            _abTestVariant = PlayerPrefs.GetString("ab_test_variant", "control");
            // 首次分配时从服务器获取或随机
            if (_abTestVariant == "control" && !PlayerPrefs.HasKey("ab_test_variant"))
            {
                string[] variants = { "control", "variant_a", "variant_b" };
                _abTestVariant = variants[UnityEngine.Random.Range(0, variants.Length)];
                PlayerPrefs.SetString("ab_test_variant", _abTestVariant);
                PlayerPrefs.Save();
            }
        }
        return _abTestVariant;
    }

    /// <summary>
    /// 根据AB测试分组获取配置值
    /// </summary>
    public T GetABTestValue<T>(string configKey, Dictionary<string, T> variantValues, T defaultValue)
    {
        string variant = GetABTestVariant();
        if (variantValues.TryGetValue(variant, out var value))
            return value;
        return defaultValue;
    }

    #endregion

    #region === 持久化 ===

    private void SaveCachedConfig()
    {
        PlayerPrefs.SetString("remote_config_timestamp", DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }

    #endregion
}

#region === 数据结构 ===

[Serializable]
public class RemoteConfigResponse
{
    public string Version;
    public long Timestamp;
    public Dictionary<string, object> Configs;
    public Dictionary<string, bool> FeatureFlags;
    public ForceUpdateInfo ForceUpdate;
    public List<Announcement> Announcements;
}

[Serializable]
public class ConfigEntry
{
    public string Key;
    public string Value;
    public string DefaultValue;
    public string Description;
    public string ValueType;
    public DateTime LastUpdated;
}

[Serializable]
public class FeatureFlag
{
    public string Key;
    public bool Enabled;
    public bool DefaultEnabled;
    public string Description;
}

[Serializable]
public class ForceUpdateInfo
{
    public string MinimumVersion;
    public string LatestVersion;
    public string UpdateUrl;
    public bool ForceUpdateRequired;
}

[Serializable]
public class Announcement
{
    public string Id;
    public string Title;
    public string Content;
    public int Priority;
    public long ShowUntil; // DateTime.Ticks
}

[Serializable]
public class RemoteConfigMetadata
{
    public ForceUpdateInfo ForceUpdateInfo;
    public List<Announcement> Announcements = new List<Announcement>();
}

#endregion
