using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TowerSkinSystem - 塔防皮肤/外观系统
/// 支持皮肤收集、装备、稀有度、特效
/// </summary>
public class TowerSkinSystem : MonoBehaviour
{
    public static TowerSkinSystem Instance { get; private set; }

    [System.Serializable]
    public class Skin
    {
        public string id;
        public string name;
        public string description;
        public string towerType;       // 适用塔类型
        public SkinRarity rarity;
        public SkinTheme theme;
        public bool isLimited;         // 限定皮肤
        public string seasonTag;       // 所属赛季
        public int requiredLevel;      // 解锁等级
        
        // 视觉配置
        public string meshVariant;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.white;
        public Color particleColor = Color.white;
        
        // 状态
        public bool unlocked;
        public bool equipped;
        public DateTime unlockedAt;

        // 特效
        public string attackEffectId;
        public string idleEffectId;
        public string killEffectId;
        public string projectileVariant;
    }

    [System.Serializable]
    public class SkinCollection
    {
        public string themeId;
        public string themeName;
        public List<string> skinIds;
        public SkinReward completeReward;
    }

    [System.Serializable]
    public class SkinReward
    {
        public string type;  // "gold", "gems", "skin", "effect"
        public int amount;
        public string id;
    }

    public enum SkinRarity
    {
        Common, Uncommon, Rare, Epic, Legendary, Limited, Mythic
    }

    public enum SkinTheme
    {
        Default, Cyberpunk, Neon, Steampunk, Ice, 
        Fire, Nature, Void, Golden, Pixel, 
        Galaxy, Candy, Skeleton, Robot, Dragon,
        Sakura, Ocean, Halloween, Christmas, Lunar
    }

    public enum AcquireMethod
    {
        Default,     // 默认解锁
        LevelUp,     // 等级解锁
        GoldPurchase,// 金币购买
        GemPurchase, // 钻石购买
        SeasonPass,  // 赛季通行证
        Event,       // 活动获取
        Achievement, // 成就获取
        Gacha,       // 抽奖池
        Limited      // 限定活动
    }

    // 皮肤数据
    private Dictionary<string, Skin> allSkins = new Dictionary<string, Skin>();
    private Dictionary<string, string> equippedSkins = new Dictionary<string, string>(); // towerType -> skinId
    private List<SkinCollection> collections = new List<SkinCollection>();
    
    // 货币
    private int skinTokens = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeDefaultSkins();
        LoadSkinData();
    }

    // ==================== 皮肤初始化 ====================

    private void InitializeDefaultSkins()
    {
        var defaultSkins = new List<(string type, SkinTheme theme, string name, SkinRarity rarity, Color primary, Color secondary)>
        {
            ("Arrow", SkinTheme.Default, "经典箭塔", SkinRarity.Common, 
                new Color(0.8f, 0.6f, 0.3f), new Color(0.5f, 0.4f, 0.2f)),
            ("Arrow", SkinTheme.Cyberpunk, "霓虹箭塔", SkinRarity.Rare,
                new Color(0f, 1f, 0.8f), new Color(1f, 0f, 1f)),
            ("Arrow", SkinTheme.Galaxy, "星辰箭塔", SkinRarity.Epic,
                new Color(0.2f, 0.1f, 0.6f), new Color(0.8f, 0.8f, 1f)),
            
            ("Cannon", SkinTheme.Default, "经典炮塔", SkinRarity.Common,
                new Color(0.5f, 0.5f, 0.5f), new Color(0.3f, 0.3f, 0.3f)),
            ("Cannon", SkinTheme.Steampunk, "蒸汽炮塔", SkinRarity.Rare,
                new Color(0.6f, 0.4f, 0.2f), new Color(0.8f, 0.7f, 0.3f)),
            ("Cannon", SkinTheme.Dragon, "龙焰炮塔", SkinRarity.Legendary,
                new Color(1f, 0.3f, 0f), new Color(0.8f, 0f, 0f)),
            
            ("Ice", SkinTheme.Default, "经典冰塔", SkinRarity.Common,
                new Color(0.3f, 0.7f, 1f), new Color(0.1f, 0.3f, 0.8f)),
            ("Ice", SkinTheme.Ice, "极寒冰塔", SkinRarity.Rare,
                new Color(0.5f, 0.8f, 1f), new Color(0.9f, 0.95f, 1f)),
            
            ("Lightning", SkinTheme.Default, "经典电塔", SkinRarity.Common,
                new Color(1f, 1f, 0f), new Color(0.8f, 0.6f, 0f)),
            ("Lightning", SkinTheme.Neon, "霓虹电塔", SkinRarity.Rare,
                new Color(1f, 0f, 1f), new Color(0f, 1f, 1f)),
            
            ("Laser", SkinTheme.Default, "经典激光塔", SkinRarity.Common,
                new Color(1f, 0.2f, 0.2f), new Color(0.5f, 0f, 0f)),
            ("Laser", SkinTheme.Void, "虚空激光塔", SkinRarity.Epic,
                new Color(0.3f, 0f, 0.5f), new Color(0.6f, 0f, 1f)),
            
            ("Tesla", SkinTheme.Robot, "机甲特斯拉", SkinRarity.Epic,
                new Color(0.8f, 0.8f, 0.9f), new Color(1f, 0.8f, 0f)),
            
            ("Missile", SkinTheme.Fire, "烈焰导弹", SkinRarity.Legendary,
                new Color(1f, 0.4f, 0f), new Color(1f, 0.8f, 0f)),
        };

        foreach (var (type, theme, name, rarity, primary, secondary) in defaultSkins)
        {
            string id = $"{type.ToLower()}_{theme.ToString().ToLower()}";
            var skin = new Skin
            {
                id = id,
                name = name,
                description = $"{theme}主题{type}塔皮肤",
                towerType = type,
                rarity = rarity,
                theme = theme,
                primaryColor = primary,
                secondaryColor = secondary,
                particleColor = secondary,
                unlocked = theme == SkinTheme.Default,
                equipped = theme == SkinTheme.Default,
                unlockedAt = theme == SkinTheme.Default ? DateTime.Now : DateTime.MinValue,
                projectileVariant = theme == SkinTheme.Default ? "default" : theme.ToString().ToLower()
            };
            allSkins[id] = skin;
        }
    }

    // ==================== 皮肤收集 ====================

    private void InitCollections()
    {
        collections = new List<SkinCollection>
        {
            new SkinCollection
            {
                themeId = "cyberpunk",
                themeName = "赛博朋克套装",
                skinIds = GetAllSkinIdsForTheme(SkinTheme.Cyberpunk),
                completeReward = new SkinReward { type = "effect", id = "cyberpunk_trail", amount = 1 }
            },
            new SkinCollection
            {
                themeId = "steampunk",
                themeName = "蒸汽朋克套装",
                skinIds = GetAllSkinIdsForTheme(SkinTheme.Steampunk),
                completeReward = new SkinReward { type = "skin", id = "cannon_steampunk_golden", amount = 1 }
            },
            new SkinCollection
            {
                themeId = "galaxy",
                themeName = "银河套装",
                skinIds = GetAllSkinIdsForTheme(SkinTheme.Galaxy),
                completeReward = new SkinReward { type = "gems", amount = 500 }
            }
        };
    }

    // ==================== 皮肤操作 ====================

    /// <summary>
    /// 解锁皮肤
    /// </summary>
    public bool UnlockSkin(string skinId, AcquireMethod method = AcquireMethod.Default)
    {
        if (!allSkins.TryGetValue(skinId, out var skin)) return false;
        if (skin.unlocked) return false;

        skin.unlocked = true;
        skin.unlockedAt = DateTime.Now;
        
        Debug.Log($"[Skin] 解锁皮肤: {skin.name} ({skin.rarity}) - {method}");
        SaveSkinData();
        
        // 检查套装
        CheckCollectionCompletion(skinId);
        return true;
    }

    /// <summary>
    /// 装备皮肤
    /// </summary>
    public bool EquipSkin(string skinId)
    {
        if (!allSkins.TryGetValue(skinId, out var skin)) return false;
        if (!skin.unlocked)
        {
            Debug.LogWarning($"[Skin] 皮肤未解锁: {skin.name}");
            return false;
        }

        // 取消同类塔的其他皮肤
        foreach (var s in allSkins.Values)
        {
            if (s.towerType == skin.towerType)
                s.equipped = false;
        }

        skin.equipped = true;
        equippedSkins[skin.towerType] = skinId;
        
        Debug.Log($"[Skin] 装备皮肤: {skin.name} -> {skin.towerType}塔");
        SaveSkinData();
        return true;
    }

    /// <summary>
    /// 购买皮肤（金币）
    /// </summary>
    public bool PurchaseSkin(string skinId)
    {
        if (!allSkins.TryGetValue(skinId, out var skin)) return false;
        if (skin.unlocked) return false;

        var costs = new Dictionary<SkinRarity, int>
        {
            { SkinRarity.Common, 500 },
            { SkinRarity.Uncommon, 1500 },
            { SkinRarity.Rare, 5000 },
            { SkinRarity.Epic, 15000 },
            { SkinRarity.Legendary, 50000 },
            { SkinRarity.Mythic, 100000 }
        };

        int cost = costs.ContainsKey(skin.rarity) ? costs[skin.rarity] : 10000;

        if (skin.isLimited)
        {
            Debug.LogWarning("[Skin] 限定皮肤无法通过金币购买");
            return false;
        }

        // TODO: 实际扣金币逻辑由 GameManager 管理
        return UnlockSkin(skinId, AcquireMethod.GoldPurchase);
    }

    /// <summary>
    /// 购买皮肤（皮肤令牌）
    /// </summary>
    public bool PurchaseSkinWithTokens(string skinId)
    {
        if (!allSkins.TryGetValue(skinId, out var skin)) return false;
        
        var tokenCosts = new Dictionary<SkinRarity, int>
        {
            { SkinRarity.Common, 10 },
            { SkinRarity.Uncommon, 30 },
            { SkinRarity.Rare, 80 },
            { SkinRarity.Epic, 200 },
            { SkinRarity.Legendary, 500 },
            { SkinRarity.Mythic, 1200 }
        };

        int cost = tokenCosts.ContainsKey(skin.rarity) ? tokenCosts[skin.rarity] : 300;
        if (skinTokens < cost) return false;

        skinTokens -= cost;
        return UnlockSkin(skinId, AcquireMethod.GoldPurchase);
    }

    // ==================== 套装检查 ====================

    private void CheckCollectionCompletion(string newlyUnlockedSkinId)
    {
        if (collections.Count == 0) InitCollections();

        foreach (var collection in collections)
        {
            if (!collection.skinIds.Contains(newlyUnlockedSkinId)) continue;

            bool allUnlocked = true;
            foreach (var sid in collection.skinIds)
            {
                if (!allSkins.TryGetValue(sid, out var s) || !s.unlocked)
                {
                    allUnlocked = false;
                    break;
                }
            }

            if (allUnlocked)
            {
                Debug.Log($"[Skin] 🎉 套装收集完成: {collection.themeName}!");
                AwardCollectionReward(collection);
            }
        }
    }

    private void AwardCollectionReward(SkinCollection collection)
    {
        switch (collection.completeReward.type)
        {
            case "gems":
                Debug.Log($"[Skin] 获得套装奖励: {collection.completeReward.amount}钻石");
                break;
            case "skin":
                if (allSkins.ContainsKey(collection.completeReward.id))
                    UnlockSkin(collection.completeReward.id);
                break;
            case "effect":
                Debug.Log($"[Skin] 解锁特效: {collection.completeReward.id}");
                break;
        }
    }

    // ==================== 辅助函数 ====================

    private List<string> GetAllSkinIdsForTheme(SkinTheme theme)
    {
        var ids = new List<string>();
        foreach (var skin in allSkins.Values)
        {
            if (skin.theme == theme)
                ids.Add(skin.id);
        }
        return ids;
    }

    /// <summary>
    /// 获取某类型塔当前装备的皮肤
    /// </summary>
    public Skin GetEquippedSkin(string towerType)
    {
        if (equippedSkins.TryGetValue(towerType, out string skinId))
        {
            if (allSkins.TryGetValue(skinId, out var skin))
                return skin;
        }
        
        // 返回默认皮肤
        string defaultId = $"{towerType.ToLower()}_default";
        return allSkins.TryGetValue(defaultId, out var defaultSkin) ? defaultSkin : null;
    }

    // ==================== 数据持久化 ====================

    private void LoadSkinData()
    {
        string json = PlayerPrefs.GetString("tower_skins", "");
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var data = JsonUtility.FromJson<SkinDataWrapper>(json);
            foreach (var sd in data.skins)
            {
                if (allSkins.TryGetValue(sd.id, out var skin))
                {
                    skin.unlocked = sd.unlocked;
                    skin.equipped = sd.equipped;
                    if (sd.equipped) equippedSkins[skin.towerType] = skin.id;
                }
            }
            skinTokens = data.skinTokens;
        }
        catch { }
    }

    private void SaveSkinData()
    {
        var data = new SkinDataWrapper();
        data.skins = new List<SkinSaveData>();
        foreach (var skin in allSkins.Values)
        {
            data.skins.Add(new SkinSaveData
            {
                id = skin.id,
                unlocked = skin.unlocked,
                equipped = skin.equipped
            });
        }
        data.skinTokens = skinTokens;
        PlayerPrefs.SetString("tower_skins", JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    [System.Serializable]
    private class SkinDataWrapper
    {
        public List<SkinSaveData> skins;
        public int skinTokens;
    }

    [System.Serializable]
    private class SkinSaveData
    {
        public string id;
        public bool unlocked;
        public bool equipped;
    }

    // ==================== Getter ====================

    public List<Skin> GetAllSkins() => new List<Skin>(allSkins.Values);
    public List<Skin> GetUnlockedSkins() => new List<Skin>(allSkins.Values).FindAll(s => s.unlocked);
    public List<Skin> GetSkinsForType(string towerType) => 
        new List<Skin>(allSkins.Values).FindAll(s => s.towerType == towerType);
    public List<SkinCollection> GetCollections() => collections;
    public int GetSkinTokens() => skinTokens;
    public void AddSkinTokens(int count) { skinTokens += count; SaveSkinData(); }

    /// <summary>
    /// 随机获取一个未解锁皮肤（抽奖用）
    /// </summary>
    public Skin RollRandomSkin(SkinRarity minRarity = SkinRarity.Uncommon)
    {
        var pool = new List<Skin>(allSkins.Values).FindAll(
            s => !s.unlocked && !s.isLimited && (int)s.rarity >= (int)minRarity
        );
        
        if (pool.Count == 0) return null;
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }
}
