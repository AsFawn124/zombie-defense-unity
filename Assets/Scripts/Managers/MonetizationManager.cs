/**
 * MonetizationManager - 商业化变现管理器
 * 支持内购/IAP/广告/通行证/礼包/首充/订阅
 * 对标市面商业游戏变现体系
 */
using System;
using System.Collections.Generic;
using UnityEngine;

public class MonetizationManager : MonoBehaviour
{
    public static MonetizationManager Instance;

    // ==================== 货币体系 ====================
    public enum CurrencyType { Gold, Gems, Energy, SkinTokens, SeasonPoints }

    [System.Serializable]
    public class CurrencyBalance
    {
        public CurrencyType type;
        public int amount;
        public int totalEarned;
        public int totalSpent;
    }

    // ==================== 商品类型 ====================
    public enum ProductType
    {
        // 消耗品
        GoldPack, GemPack, EnergyPack, PremiumCurrency,
        // 礼包
        StarterPack, BeginnerPack, GrowthPack, MonthlyPack,
        WeeklyPack, DailyDeal, FlashSale,
        // 通行证
        BattlePass, PremiumPass,
        // 订阅
        MonthlySubscription, WeeklySubscription,
        // 皮肤
        SkinSingle, SkinBundle,
        // 功能解锁
        UnlockFeature, ExtraSlot, SpeedBoost,
        // 广告
        RewardedVideo, Interstitial, Banner
    }

    [System.Serializable]
    public class ShopProduct
    {
        public string id;
        public ProductType type;
        public string name;
        public string description;
        public string iconId;
        public double price;           // 人民币价格
        public CurrencyType rewardType;
        public int rewardAmount;
        public int bonusAmount;         // 加赠数量
        public bool isLimited;          // 限购
        public int limitedCount;
        public int purchasedCount;
        public DateTime validUntil;
        public float discount;          // 折扣 0-1
        public string[] extraRewards;   // 额外奖励 {type:amount}
        public bool isFirstPurchase;    // 首充标识
        public int firstPurchaseBonus;  // 首充加赠
    }

    // ==================== 通行证系统 ====================
    [System.Serializable]
    public class BattlePassConfig
    {
        public int seasonId;
        public string seasonName;
        public int totalLevels = 50;
        public int pricePremium;        // 高级通行证价格(钻石)
        public int priceElite;          // 精英通行证价格(钻石)
        public List<PassLevel> freeRewards;
        public List<PassLevel> premiumRewards;
        public List<PassLevel> eliteRewards;
    }

    [System.Serializable]
    public class PassLevel
    {
        public int level;
        public int expRequired;
        public RewardItem[] rewards;
    }

    [System.Serializable]
    public class RewardItem
    {
        public string type;
        public string id;
        public int amount;
        public string rarity;
    }

    // ==================== 广告系统 ====================
    public enum AdPlacement
    {
        DoubleReward,       // 战斗结束后双倍奖励
        ExtraGold,          // 商店额外金币
        FreeGacha,          // 免费抽卡
        Revive,             // 战斗复活
        SpeedUp,            // 加速孵化/建造
        ExtraEnergy,        // 额外体力
        SkipCooldown,       // 跳过冷却
        DailyBonus          // 每日奖励
    }

    [System.Serializable]
    public class AdConfig
    {
        public AdPlacement placement;
        public string description;
        public int dailyLimit;
        public int usedToday;
        public CurrencyType rewardType;
        public int rewardAmount;
        public float cooldownMinutes;
        public DateTime lastShown;
    }

    // ==================== 促销系统 ====================
    [System.Serializable]
    public class Promotion
    {
        public string id;
        public string title;
        public string description;
        public string bannerUrl;
        public ProductType productType;
        public double originalPrice;
        public double salePrice;
        public float discount;
        public DateTime startTime;
        public DateTime endTime;
        public int totalStock;
        public int soldCount;
        public bool isFlashSale;
        public int purchaseLimit;
    }

    // ==================== 数据 ====================
    private Dictionary<CurrencyType, CurrencyBalance> currencies;
    private List<ShopProduct> shopProducts;
    private BattlePassConfig currentBattlePass;
    private int battlePassLevel = 0;
    private int battlePassExp = 0;
    private bool hasPremiumPass = false;
    private bool hasElitePass = false;
    private List<AdConfig> adConfigs;
    private List<Promotion> promotions;
    private int totalSpent = 0;
    private DateTime firstPurchaseDate;
    private bool isFirstPurchase = true;

    // 回调
    public event Action<CurrencyType, int> OnCurrencyChanged;
    public event Action<string, int, int> OnPurchaseComplete;
    public event Action OnFirstPurchase;

    void Awake() { Instance = this; }

    void Start()
    {
        InitializeCurrencies();
        InitializeShop();
        InitializeBattlePass();
        InitializeAds();
        InitializePromotions();
        LoadData();
    }

    // ==================== 货币管理 ====================

    private void InitializeCurrencies()
    {
        currencies = new Dictionary<CurrencyType, CurrencyBalance>
        {
            { CurrencyType.Gold, new CurrencyBalance { type = CurrencyType.Gold, amount = 500 } },
            { CurrencyType.Gems, new CurrencyBalance { type = CurrencyType.Gems, amount = 50 } },
            { CurrencyType.Energy, new CurrencyBalance { type = CurrencyType.Energy, amount = 100 } },
            { CurrencyType.SkinTokens, new CurrencyBalance { type = CurrencyType.SkinTokens, amount = 0 } },
            { CurrencyType.SeasonPoints, new CurrencyBalance { type = CurrencyType.SeasonPoints, amount = 0 } }
        };
    }

    public int GetCurrency(CurrencyType type) =>
        currencies.TryGetValue(type, out var c) ? c.amount : 0;

    public bool SpendCurrency(CurrencyType type, int amount)
    {
        if (!currencies.TryGetValue(type, out var c) || c.amount < amount)
            return false;

        c.amount -= amount;
        c.totalSpent += amount;
        OnCurrencyChanged?.Invoke(type, c.amount);
        SaveData();
        return true;
    }

    public void AddCurrency(CurrencyType type, int amount, string source = "")
    {
        if (!currencies.TryGetValue(type, out var c)) return;

        c.amount += amount;
        c.totalEarned += amount;
        OnCurrencyChanged?.Invoke(type, c.amount);
        SaveData();
    }

    // ==================== 商店系统 ====================

    private void InitializeShop()
    {
        shopProducts = new List<ShopProduct>
        {
            // 首充礼包
            new ShopProduct
            {
                id = "first_purchase", type = ProductType.StarterPack,
                name = "新人首充大礼包", description = "仅限一次! SSR塔+5000金币+200钻石",
                price = 6.00, rewardType = CurrencyType.Gems, rewardAmount = 200,
                bonusAmount = 5000, isFirstPurchase = true, firstPurchaseBonus = 500,
                extraRewards = new[] { "tower:SSR:1", "skin:cyberpunk:1" },
                discount = 0.83f
            },
            // 钻石包
            new ShopProduct
            {
                id = "gems_60", type = ProductType.GemPack,
                name = "60钻石", description = "基础钻石包",
                price = 6.00, rewardType = CurrencyType.Gems, rewardAmount = 60
            },
            new ShopProduct
            {
                id = "gems_300", type = ProductType.GemPack,
                name = "300钻石", description = "超值钻石包",
                price = 30.00, rewardType = CurrencyType.Gems, rewardAmount = 300, bonusAmount = 30
            },
            new ShopProduct
            {
                id = "gems_680", type = ProductType.GemPack,
                name = "680钻石", description = "豪华钻石包",
                price = 68.00, rewardType = CurrencyType.Gems, rewardAmount = 680, bonusAmount = 100
            },
            new ShopProduct
            {
                id = "gems_1280", type = ProductType.GemPack,
                name = "1280钻石", description = "超级钻石包",
                price = 128.00, rewardType = CurrencyType.Gems, rewardAmount = 1280, bonusAmount = 280
            },
            new ShopProduct
            {
                id = "gems_6480", type = ProductType.GemPack,
                name = "6480钻石", description = "至尊钻石包 (+50%)",
                price = 648.00, rewardType = CurrencyType.Gems, rewardAmount = 6480, bonusAmount = 3240
            },
            // 月卡
            new ShopProduct
            {
                id = "monthly_card", type = ProductType.MonthlySubscription,
                name = "月度会员卡", description = "每日100钻石+经验+20%，持续30天",
                price = 30.00, rewardType = CurrencyType.Gems, rewardAmount = 3000,
                extraRewards = new[] { "exp_boost:20:30" }
            },
            // 通行证
            new ShopProduct
            {
                id = "battle_pass_premium", type = ProductType.PremiumPass,
                name = "高级通行证", description = "解锁通行证高级奖励路线",
                price = 68.00, rewardType = CurrencyType.SeasonPoints, rewardAmount = 1000
            },
            // 限时礼包
            new ShopProduct
            {
                id = "weekly_deal", type = ProductType.WeeklyPack,
                name = "每周特惠", description = "本周限定! 超高性价比",
                price = 18.00, rewardType = CurrencyType.Gems, rewardAmount = 200,
                bonusAmount = 200, discount = 0.5f, isLimited = true, limitedCount = 1
            }
        };

        // 每日特惠（每天随机）
        GenerateDailyDeals();
    }

    public List<ShopProduct> GetShopProducts() => shopProducts;
    public List<ShopProduct> GetProductsByType(ProductType type) =>
        shopProducts.FindAll(p => p.type == type);

    // ==================== 购买逻辑 ====================

    public bool PurchaseProduct(string productId)
    {
        var product = shopProducts.Find(p => p.id == productId);
        if (product == null) return false;

        // 限购检查
        if (product.isLimited && product.purchasedCount >= product.limitedCount)
        {
            Debug.LogWarning($"[Shop] {product.name} 已售罄");
            return false;
        }

        // 模拟支付流程（实际接入微信支付/支付宝）
        Debug.Log($"[Shop] 发起支付: {product.name} - ¥{product.price}");

        // 支付成功回调
        CompletePurchase(product);
        return true;
    }

    private void CompletePurchase(ShopProduct product)
    {
        // 发放基础奖励
        AddCurrency(product.rewardType, product.rewardAmount + product.bonusAmount);
        
        // 首充检测
        if (isFirstPurchase)
        {
            isFirstPurchase = false;
            firstPurchaseDate = DateTime.Now;
            OnFirstPurchase?.Invoke();
            
            // 首充额外奖励
            if (product.firstPurchaseBonus > 0)
                AddCurrency(CurrencyType.Gems, product.firstPurchaseBonus);
        }

        // 额外奖励处理
        if (product.extraRewards != null)
        {
            foreach (var reward in product.extraRewards)
            {
                var parts = reward.Split(':');
                if (parts[0] == "tower") Debug.Log($"[Shop] 获得 {parts[1]} 塔!");
                if (parts[0] == "skin") Debug.Log($"[Shop] 获得皮肤: {parts[1]}");
                if (parts[0] == "gold") AddCurrency(CurrencyType.Gold, int.Parse(parts[1]));
            }
        }

        // 累计充值
        product.purchasedCount++;
        totalSpent += (int)product.price;

        // 购买通行证
        if (product.type == ProductType.PremiumPass)
            hasPremiumPass = true;

        OnPurchaseComplete?.Invoke(productId, (int)product.price, totalSpent);
        SaveData();

        Debug.Log($"[Shop] ✅ 购买成功: {product.name} (累计消费: ¥{totalSpent})");
    }

    // ==================== 通行证系统 ====================

    private void InitializeBattlePass()
    {
        currentBattlePass = new BattlePassConfig
        {
            seasonId = 1,
            seasonName = "赛博朋克·觉醒",
            totalLevels = 50,
            pricePremium = 680,
            priceElite = 1280,
            freeRewards = new List<PassLevel>(),
            premiumRewards = new List<PassLevel>(),
            eliteRewards = new List<PassLevel>()
        };

        // 生成50级奖励
        for (int i = 1; i <= 50; i++)
        {
            int expRequired = 100 * i;
            
            // 免费线
            currentBattlePass.freeRewards.Add(new PassLevel
            {
                level = i, expRequired = expRequired,
                rewards = i % 5 == 0 ?
                    new[] { new RewardItem { type = "gems", amount = 30 } } :
                    new[] { new RewardItem { type = "gold", amount = 100 * i } }
            });

            // 高级线
            currentBattlePass.premiumRewards.Add(new PassLevel
            {
                level = i, expRequired = expRequired,
                rewards = new[]
                {
                    new RewardItem { type = i % 10 == 0 ? "tower_ssr" : "gold", amount = 200 * i },
                    i % 5 == 0 ? new RewardItem { type = "gems", amount = 50 } : 
                        new RewardItem { type = "skin_token", amount = 5 }
                }
            });

            // 精英线 (额外奖励)
            currentBattlePass.eliteRewards.Add(new PassLevel
            {
                level = i, expRequired = expRequired,
                rewards = new[]
                {
                    new RewardItem { type = "skin_token", amount = 10 },
                    i % 10 == 0 ? new RewardItem { type = "skin_legendary", amount = 1, rarity = "legendary" } :
                        new RewardItem { type = "gems", amount = 30 }
                }
            });
        }
    }

    public void AddBattlePassExp(int exp)
    {
        battlePassExp += exp;
        while (battlePassLevel < currentBattlePass.totalLevels)
        {
            var nextLevel = currentBattlePass.freeRewards[battlePassLevel];
            if (battlePassExp >= nextLevel.expRequired)
            {
                battlePassLevel++;
                ClaimFreeReward(battlePassLevel);
                if (hasPremiumPass) ClaimPremiumReward(battlePassLevel);
                if (hasElitePass) ClaimEliteReward(battlePassLevel);
            }
            else break;
        }
        SaveData();
    }

    private void ClaimFreeReward(int level)
    {
        var reward = currentBattlePass.freeRewards[level - 1];
        foreach (var r in reward.rewards)
            ApplyReward(r);
    }

    private void ClaimPremiumReward(int level)
    {
        var reward = currentBattlePass.premiumRewards[level - 1];
        foreach (var r in reward.rewards)
            ApplyReward(r);
    }

    private void ClaimEliteReward(int level)
    {
        var reward = currentBattlePass.eliteRewards[level - 1];
        foreach (var r in reward.rewards)
            ApplyReward(r);
    }

    private void ApplyReward(RewardItem reward)
    {
        switch (reward.type)
        {
            case "gold": AddCurrency(CurrencyType.Gold, reward.amount); break;
            case "gems": AddCurrency(CurrencyType.Gems, reward.amount); break;
            case "skin_token": AddCurrency(CurrencyType.SkinTokens, reward.amount); break;
        }
    }

    // ==================== 广告系统 ====================

    private void InitializeAds()
    {
        adConfigs = new List<AdConfig>
        {
            new AdConfig { placement = AdPlacement.DoubleReward, description = "战斗奖励翻倍", dailyLimit = 5, rewardType = CurrencyType.Gold, rewardAmount = 200, cooldownMinutes = 5 },
            new AdConfig { placement = AdPlacement.FreeGacha, description = "免费抽卡", dailyLimit = 3, rewardType = CurrencyType.Gems, rewardAmount = 10, cooldownMinutes = 30 },
            new AdConfig { placement = AdPlacement.Revive, description = "战斗复活", dailyLimit = 3, cooldownMinutes = 10 },
            new AdConfig { placement = AdPlacement.ExtraEnergy, description = "补充体力", dailyLimit = 5, rewardType = CurrencyType.Energy, rewardAmount = 20, cooldownMinutes = 15 },
            new AdConfig { placement = AdPlacement.SpeedUp, description = "加速60分钟", dailyLimit = 3, cooldownMinutes = 60 },
            new AdConfig { placement = AdPlacement.DailyBonus, description = "每日福利", dailyLimit = 1, rewardType = CurrencyType.Gems, rewardAmount = 30, cooldownMinutes = 1440 },
        };
    }

    public bool ShowRewardedAd(AdPlacement placement)
    {
        var config = adConfigs.Find(a => a.placement == placement);
        if (config == null || config.usedToday >= config.dailyLimit) return false;

        // 模拟广告播放
        config.usedToday++;
        config.lastShown = DateTime.Now;

        AddCurrency(config.rewardType, config.rewardAmount);
        Debug.Log($"[Ad] 📺 激励广告 - {placement}: +{config.rewardAmount} {config.rewardType}");
        return true;
    }

    // ==================== 促销系统 ====================

    private void InitializePromotions()
    {
        var now = DateTime.Now;
        promotions = new List<Promotion>
        {
            new Promotion
            {
                id = "flash_001", title = "限时闪购", description = "2小时后结束!",
                productType = ProductType.GemPack,
                originalPrice = 68.00, salePrice = 34.00, discount = 0.5f,
                startTime = now, endTime = now.AddHours(2),
                totalStock = 100, isFlashSale = true, purchaseLimit = 1
            }
        };
    }

    private void GenerateDailyDeals()
    {
        // 每日随机生成特价商品（由外部每日重置调用）
    }

    public void DailyReset()
    {
        foreach (var ad in adConfigs) ad.usedToday = 0;
        GenerateDailyDeals();
        SaveData();
    }

    // ==================== 持久化 ====================

    private void LoadData()
    {
        try
        {
            var json = PlayerPrefs.GetString("monetization_data", "");
            if (string.IsNullOrEmpty(json)) return;
            var data = JsonUtility.FromJson<MonetizationSaveData>(json);
            totalSpent = data.totalSpent;
            isFirstPurchase = data.isFirstPurchase;
            battlePassLevel = data.battlePassLevel;
            battlePassExp = data.battlePassExp;
            hasPremiumPass = data.hasPremiumPass;
            hasElitePass = data.hasElitePass;
        }
        catch { }
    }

    private void SaveData()
    {
        var data = new MonetizationSaveData
        {
            totalSpent = totalSpent,
            isFirstPurchase = isFirstPurchase,
            battlePassLevel = battlePassLevel,
            battlePassExp = battlePassExp,
            hasPremiumPass = hasPremiumPass,
            hasElitePass = hasElitePass
        };
        PlayerPrefs.SetString("monetization_data", JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    [System.Serializable]
    private class MonetizationSaveData
    {
        public int totalSpent;
        public bool isFirstPurchase;
        public int battlePassLevel;
        public int battlePassExp;
        public bool hasPremiumPass;
        public bool hasElitePass;
    }

    // ==================== Getter ====================
    public BattlePassConfig GetBattlePass() => currentBattlePass;
    public int GetBattlePassLevel() => battlePassLevel;
    public int GetBattlePassExp() => battlePassExp;
    public bool HasPremiumPass() => hasPremiumPass;
    public bool HasElitePass() => hasElitePass;
    public int GetTotalSpent() => totalSpent;
    public bool IsFirstPurchase() => isFirstPurchase;
    public List<Promotion> GetActivePromotions() =>
        promotions.FindAll(p => p.endTime > DateTime.Now && p.soldCount < p.totalStock);
    public List<AdConfig> GetAdConfigs() => adConfigs;
}
