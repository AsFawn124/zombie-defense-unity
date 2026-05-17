using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 商业化系统 - TASK-044~048
/// 抽卡系统、商城系统、支付系统、广告优化
/// </summary>
public class CommerceManager : MonoBehaviour
{
    public static CommerceManager Instance;

    [Header("商业化配置")]
    public CommerceConfig Config;

    // 抽卡数据
    private GachaData _gachaData;
    private List<GachaRecord> _gachaHistory = new List<GachaRecord>();

    // 商城数据
    private ShopData _shopData;

    // 支付数据
    private PaymentData _paymentData;

    // 事件
    public event Action<GachaResult> OnGachaResult;
    public event Action<string> OnPurchaseComplete;
    public event Action<string> OnAdWatched;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadAllData();
    }

    #region === 抽卡系统 (TASK-044~045) ===

    /// <summary>
    /// 单抽
    /// </summary>
    public GachaResult SinglePull(GachaPoolType poolType)
    {
        if (!CanAffordGacha(false))
        {
            Debug.LogWarning("[Gacha] 钻石不足");
            return null;
        }

        DeductGachaCost(false);
        return ExecutePull(poolType, 1);
    }

    /// <summary>
    /// 十连抽
    /// </summary>
    public GachaResult MultiPull(GachaPoolType poolType)
    {
        if (!CanAffordGacha(true))
        {
            Debug.LogWarning("[Gacha] 钻石不足");
            return null;
        }

        DeductGachaCost(true);
        return ExecutePull(poolType, 10);
    }

    private GachaResult ExecutePull(GachaPoolType poolType, int count)
    {
        var result = new GachaResult
        {
            PoolType = poolType,
            Results = new List<GachaItem>(),
            PullCount = count
        };

        var pool = Config.GetPool(poolType);
        if (pool == null) return result;

        // 更新抽卡计数
        if (poolType == GachaPoolType.Newbie)
        {
            _gachaData.NewbiePulls += count;
            if (_gachaData.NewbiePulls >= pool.GuaranteePull)
            {
                _gachaData.NewbieGuaranteed = true;
            }
        }

        for (int i = 0; i < count; i++)
        {
            GachaItem item;

            // 保底检查
            if (ShouldTriggerGuarantee(poolType))
            {
                item = GetGuaranteedItem(poolType);
                ResetPityCounter(poolType);
            }
            else
            {
                item = RollItem(poolType);
                UpdatePityCounter(poolType, item);
            }

            result.Results.Add(item);
            result.TotalRarityScore += (int)item.Rarity;

            // 记录历史
            _gachaHistory.Add(new GachaRecord
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Rarity = item.Rarity,
                PoolType = poolType,
                PullTime = DateTime.UtcNow
            });
        }

        // 新手池20抽后关闭
        if (poolType == GachaPoolType.Newbie && _gachaData.NewbiePulls >= 20)
        {
            _gachaData.NewbiePoolClosed = true;
        }

        // 统计
        _gachaData.TotalPulls += count;
        result.PityCounter = GetPityCounter(poolType);

        SaveGachaData();
        OnGachaResult?.Invoke(result);

        return result;
    }

    private bool ShouldTriggerGuarantee(GachaPoolType poolType)
    {
        return GetPityCounter(poolType) >= Config.GuaranteePull;
    }

    private GachaItem GetGuaranteedItem(GachaPoolType poolType)
    {
        // 保底必出传说
        var legendaries = Config.GetItemsByRarity(GachaItemRarity.Legendary);
        if (legendaries.Count > 0)
        {
            return legendaries[UnityEngine.Random.Range(0, legendaries.Count)];
        }
        return RollItem(poolType);
    }

    private GachaItem RollItem(GachaPoolType poolType)
    {
        var pool = Config.GetPool(poolType);
        if (pool == null) return null;

        float roll = UnityEngine.Random.value;
        float cumulative = 0;

        foreach (var rate in pool.Rates)
        {
            cumulative += rate.Probability;
            if (roll <= cumulative)
            {
                var items = Config.GetItemsByRarity(rate.Rarity);
                if (items.Count > 0)
                {
                    var item = items[UnityEngine.Random.Range(0, items.Count)];
                    return new GachaItem
                    {
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        Rarity = rate.Rarity,
                        IsNew = IsNewItem(item.ItemId)
                    };
                }
            }
        }

        return null; // fallback
    }

    private void UpdatePityCounter(GachaPoolType poolType, GachaItem item)
    {
        if (item.Rarity >= GachaItemRarity.Epic)
        {
            ResetPityCounter(poolType);
        }
        else
        {
            IncrementPityCounter(poolType);
        }
    }

    private void IncrementPityCounter(GachaPoolType poolType)
    {
        switch (poolType)
        {
            case GachaPoolType.Standard: _gachaData.StandardPity++; break;
            case GachaPoolType.Limited: _gachaData.LimitedPity++; break;
            case GachaPoolType.Equipment: _gachaData.EquipmentPity++; break;
        }
    }

    private void ResetPityCounter(GachaPoolType poolType)
    {
        switch (poolType)
        {
            case GachaPoolType.Standard: _gachaData.StandardPity = 0; break;
            case GachaPoolType.Limited: _gachaData.LimitedPity = 0; break;
            case GachaPoolType.Equipment: _gachaData.EquipmentPity = 0; break;
        }
    }

    private int GetPityCounter(GachaPoolType poolType)
    {
        return poolType switch
        {
            GachaPoolType.Standard => _gachaData.StandardPity,
            GachaPoolType.Limited => _gachaData.LimitedPity,
            GachaPoolType.Equipment => _gachaData.EquipmentPity,
            _ => 0
        };
    }

    private bool IsNewItem(string itemId)
    {
        string obtainedItems = PlayerPrefs.GetString("gacha_obtained", "");
        return !obtainedItems.Contains(itemId);
    }

    private bool CanAffordGacha(bool isTenPull)
    {
        int cost = isTenPull ? Config.GachaMultiCost : Config.GachaSingleCost;
        return _gachaData.Diamonds >= cost;
    }

    private void DeductGachaCost(bool isTenPull)
    {
        int cost = isTenPull ? Config.GachaMultiCost : Config.GachaSingleCost;
        _gachaData.Diamonds -= cost;
    }

    #endregion

    #region === 商城系统 (TASK-046) ===

    /// <summary>
    /// 购买商品
    /// </summary>
    public bool PurchaseShopItem(string itemId)
    {
        var item = Config.FindShopItem(itemId);
        if (item == null) return false;

        if (item.IsLimited && item.PurchasedCount >= item.MaxPurchase)
        {
            Debug.LogWarning("[Shop] 限购商品已达到购买上限");
            return false;
        }

        // 检查货币
        switch (item.CurrencyType)
        {
            case CurrencyType.Gold:
                if (!CanAffordGold(item.Price)) return false;
                DeductGold(item.Price);
                break;
            case CurrencyType.Diamond:
                if (!CanAffordDiamond(item.Price)) return false;
                DeductDiamond(item.Price);
                break;
        }

        item.PurchasedCount++;
        GrantItem(item);

        OnPurchaseComplete?.Invoke(itemId);
        SaveShopData();
        return true;
    }

    /// <summary>
    /// 购买钻石
    /// </summary>
    public bool PurchaseDiamonds(string packageId)
    {
        var package = Config.FindDiamondPackage(packageId);
        if (package == null) return false;

        // 实际项目中调用支付SDK
        Debug.Log($"[Commerce] 购买钻石包: {package.PackageName} - ¥{package.Price}");

        _gachaData.Diamonds += package.DiamondAmount;
        _paymentData.TotalDiamondsPurchased += package.DiamondAmount;

        SavePaymentData();
        OnPurchaseComplete?.Invoke(packageId);
        return true;
    }

    /// <summary>
    /// 购买月卡
    /// </summary>
    public bool PurchaseMonthlyCard()
    {
        if (_paymentData.HasMonthlyCard)
        {
            Debug.LogWarning("[Commerce] 已拥有月卡");
            return false;
        }

        _paymentData.HasMonthlyCard = true;
        _paymentData.MonthlyCardStartTime = DateTime.UtcNow;
        _paymentData.MonthlyDaysRemaining = 30;
        _gachaData.Diamonds += Config.MonthlyCardInstantDiamonds;

        SavePaymentData();
        OnPurchaseComplete?.Invoke("monthly_card");
        return true;
    }

    /// <summary>
    /// 领取月卡每日奖励
    /// </summary>
    public bool ClaimMonthlyDailyReward()
    {
        if (!_paymentData.HasMonthlyCard || _paymentData.MonthlyDaysRemaining <= 0) return false;

        if (_paymentData.LastMonthlyClaim.Date == DateTime.UtcNow.Date)
        {
            Debug.LogWarning("[Commerce] 今日已领取月卡奖励");
            return false;
        }

        _gachaData.Diamonds += Config.MonthlyDailyDiamonds;
        _paymentData.LastMonthlyClaim = DateTime.UtcNow;
        _paymentData.MonthlyDaysRemaining--;

        if (_paymentData.MonthlyDaysRemaining <= 0)
        {
            _paymentData.HasMonthlyCard = false;
        }

        SavePaymentData();
        return true;
    }

    private bool CanAffordGold(int amount) => GameManager.Instance != null && GameManager.Instance.Gold >= amount;
    private bool CanAffordDiamond(int amount) => _gachaData.Diamonds >= amount;
    private void DeductGold(int amount) => GameManager.Instance?.SpendGold(amount);
    private void DeductDiamond(int amount) => _gachaData.Diamonds -= amount;

    private void GrantItem(ShopItem item)
    {
        switch (item.RewardType)
        {
            case ShopRewardType.Gold:
                GameManager.Instance?.AddGold(item.RewardAmount);
                break;
            case ShopRewardType.Diamond:
                _gachaData.Diamonds += item.RewardAmount;
                break;
        }
    }

    #endregion

    #region === 广告系统 (TASK-048) ===

    /// <summary>
    /// 观看激励视频
    /// </summary>
    public bool WatchRewardedAd(AdRewardType rewardType)
    {
        if (!CanWatchAd(rewardType)) return false;

        // 记录观看
        switch (rewardType)
        {
            case AdRewardType.Revive: _paymentData.DailyRevives++; break;
            case AdRewardType.DoubleGold: _paymentData.DailyDoubleGold++; break;
            case AdRewardType.FreeGacha: _paymentData.DailyFreeGacha++; break;
        }

        // 播放广告
        WeChatManager.Instance?.ShowRewardedAd((success) =>
        {
            if (success)
            {
                GrantAdReward(rewardType);
                OnAdWatched?.Invoke(rewardType.ToString());
            }
        });

        SavePaymentData();
        return true;
    }

    private bool CanWatchAd(AdRewardType rewardType)
    {
        int dailyLimit = rewardType switch
        {
            AdRewardType.Revive => Config.DailyReviveLimit,
            AdRewardType.DoubleGold => Config.DailyDoubleGoldLimit,
            AdRewardType.FreeGacha => Config.DailyFreeGachaLimit,
            _ => 5
        };

        int used = rewardType switch
        {
            AdRewardType.Revive => _paymentData.DailyRevives,
            AdRewardType.DoubleGold => _paymentData.DailyDoubleGold,
            AdRewardType.FreeGacha => _paymentData.DailyFreeGacha,
            _ => 0
        };

        if (used >= dailyLimit)
        {
            Debug.LogWarning($"[Ad] 今日{rewardType}广告已达上限");
            return false;
        }

        // 插屏广告频次控制
        if (rewardType == AdRewardType.Interstitial)
        {
            float cooldown = (float)(DateTime.UtcNow - _paymentData.LastInterstitialAd).TotalMinutes;
            if (cooldown < Config.InterstitialCooldownMinutes)
            {
                Debug.LogWarning($"[Ad] 插屏广告冷却中，剩余{Config.InterstitialCooldownMinutes - cooldown:F0}分钟");
                return false;
            }
            _paymentData.LastInterstitialAd = DateTime.UtcNow;
        }

        return true;
    }

    private void GrantAdReward(AdRewardType rewardType)
    {
        switch (rewardType)
        {
            case AdRewardType.Revive:
                GameManager.Instance?.RevivePlayer();
                break;
            case AdRewardType.DoubleGold:
                GameManager.Instance?.SetGoldMultiplier(2f, 30f);
                break;
            case AdRewardType.FreeGacha:
                SinglePull(GachaPoolType.Standard);
                break;
            case AdRewardType.SpeedUp:
                // 加速建造
                break;
        }
    }

    #endregion

    #region === 数据持久化 ===

    private void LoadAllData()
    {
        string json = PlayerPrefs.GetString("gacha_data", "");
        _gachaData = !string.IsNullOrEmpty(json) ? JsonUtility.FromJson<GachaData>(json) : new GachaData { Diamonds = 100 };

        json = PlayerPrefs.GetString("shop_data", "");
        _shopData = !string.IsNullOrEmpty(json) ? JsonUtility.FromJson<ShopData>(json) : new ShopData();

        json = PlayerPrefs.GetString("payment_data", "");
        _paymentData = !string.IsNullOrEmpty(json) ? JsonUtility.FromJson<PaymentData>(json) : new PaymentData();
    }

    private void SaveGachaData() { PlayerPrefs.SetString("gacha_data", JsonUtility.ToJson(_gachaData)); PlayerPrefs.Save(); }
    private void SaveShopData() { PlayerPrefs.SetString("shop_data", JsonUtility.ToJson(_shopData)); PlayerPrefs.Save(); }
    private void SavePaymentData() { PlayerPrefs.SetString("payment_data", JsonUtility.ToJson(_paymentData)); PlayerPrefs.Save(); }

    #endregion

    #region === 公共属性 ===

    public int Diamonds => _gachaData.Diamonds;
    public int TotalPulls => _gachaData.TotalPulls;
    public List<GachaRecord> GachaHistory => _gachaHistory;
    public bool HasMonthlyCard => _paymentData.HasMonthlyCard;

    #endregion
}

#region === 商业化数据结构 ===

[System.Serializable]
public class CommerceConfig
{
    // 抽卡
    public int GachaSingleCost = 60;
    public int GachaMultiCost = 540;
    public int GuaranteePull = 90;
    public GachaPool[] Pools;
    public GachaItemData[] ItemPool;

    // 商城
    public ShopItem[] ShopItems;
    public DiamondPackage[] DiamondPackages;

    // 月卡
    public int MonthlyCardInstantDiamonds = 300;
    public int MonthlyDailyDiamonds = 100;

    // 广告
    public int DailyReviveLimit = 3;
    public int DailyDoubleGoldLimit = 5;
    public int DailyFreeGachaLimit = 5;
    public float InterstitialCooldownMinutes = 5f;

    public GachaPool GetPool(GachaPoolType type) => System.Array.Find(Pools, p => p.PoolType == type);
    public List<GachaItemData> GetItemsByRarity(GachaItemRarity rarity)
    {
        var items = new List<GachaItemData>();
        foreach (var item in ItemPool)
        {
            if (item.Rarity == rarity) items.Add(item);
        }
        return items;
    }
    public ShopItem FindShopItem(string id) => System.Array.Find(ShopItems, s => s.ItemId == id);
    public DiamondPackage FindDiamondPackage(string id) => System.Array.Find(DiamondPackages, p => p.PackageId == id);
}

[System.Serializable]
public class GachaPool
{
    public GachaPoolType PoolType;
    public string PoolName;
    public GachaPoolRate[] Rates;
    public int GuaranteePull;
}

[System.Serializable]
public class GachaPoolRate
{
    public GachaItemRarity Rarity;
    [Range(0, 1)]
    public float Probability;
}

[System.Serializable]
public class GachaItemData
{
    public string ItemId;
    public string ItemName;
    public GachaItemRarity Rarity;
}

[System.Serializable]
public class GachaResult
{
    public GachaPoolType PoolType;
    public List<GachaItem> Results;
    public int PullCount;
    public int TotalRarityScore;
    public int PityCounter;
}

[System.Serializable]
public class GachaItem
{
    public string ItemId;
    public string ItemName;
    public GachaItemRarity Rarity;
    public bool IsNew;
}

[System.Serializable]
public class GachaRecord
{
    public string ItemId;
    public string ItemName;
    public GachaItemRarity Rarity;
    public GachaPoolType PoolType;
    public DateTime PullTime;
}

[System.Serializable]
public class ShopItem
{
    public string ItemId;
    public string ItemName;
    public string Description;
    public CurrencyType CurrencyType;
    public int Price;
    public ShopRewardType RewardType;
    public int RewardAmount;
    public bool IsLimited;
    public int MaxPurchase;
    public int PurchasedCount;
}

[System.Serializable]
public class DiamondPackage
{
    public string PackageId;
    public string PackageName;
    public int Price; // RMB 分
    public int DiamondAmount;
    public int BonusDiamonds;
}

[System.Serializable]
public class GachaData
{
    public int Diamonds;
    public int TotalPulls;
    public int StandardPity;
    public int LimitedPity;
    public int EquipmentPity;
    public int NewbiePulls;
    public bool NewbieGuaranteed;
    public bool NewbiePoolClosed;
}

[System.Serializable]
public class ShopData
{
    // 限购跟踪
}

[System.Serializable]
public class PaymentData
{
    public bool HasMonthlyCard;
    public DateTime MonthlyCardStartTime;
    public int MonthlyDaysRemaining;
    public DateTime LastMonthlyClaim;
    public int TotalDiamondsPurchased;

    // 广告限制
    public int DailyRevives;
    public int DailyDoubleGold;
    public int DailyFreeGacha;
    public DateTime LastInterstitialAd;
}

public enum GachaPoolType
{
    Newbie,
    Standard,
    Limited,
    Equipment
}

public enum GachaItemRarity
{
    Normal = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3,
    Mythic = 4
}

public enum CurrencyType
{
    Gold,
    Diamond
}

public enum ShopRewardType
{
    Gold,
    Diamond
}

public enum AdRewardType
{
    Revive,
    DoubleGold,
    FreeGacha,
    SpeedUp,
    Interstitial
}

#endregion
