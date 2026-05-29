using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RoguelikeModeManager - 肉鸽模式管理器
/// 每局随机获取塔、技能、神器组合，build不同策略
/// </summary>
public class RoguelikeModeManager : MonoBehaviour
{
    public static RoguelikeModeManager Instance { get; private set; }

    // 肉鸽运行状态
    [System.Serializable]
    public enum RunState
    {
        Inactive,
        Drafting,       // 选牌阶段
        Battle,         // 战斗阶段
        Shop,           // 商店阶段
        BossChallenge,  // Boss挑战
        RunComplete     // 本轮结束
    }

    // 塔牌
    [System.Serializable]
    public class TowerCard
    {
        public string id;
        public string towerName;
        public TowerType type;
        public Rarity rarity;
        public int level;
        public string description;
        public Sprite icon;
        public List<ModifierCard> attachedMods;
    }

    // 神器/强化牌
    [System.Serializable]
    public class ModifierCard
    {
        public string id;
        public string name;
        public ModifierType type;
        public Rarity rarity;
        public float value;
        public string description;
    }

    // 事件牌
    [System.Serializable]
    public class EventCard
    {
        public string id;
        public string name;
        public string description;
        public EventEffect effect;
        public string[] choices;
    }

    public enum TowerType
    {
        Arrow, Cannon, Ice, Lightning, Poison, Laser, 
        Tesla, Missile, Turret, Void, Flame, Frost,
        Storm, Gravity, Chrono, Shadow
    }

    public enum ModifierType
    {
        // 全局强化
        DamageUp, AttackSpeedUp, RangeUp, CritUp,
        // 特殊效果
        ChainLightning, PierceShot, SplashDamage, Lifesteal,
        Execute, SlowField, BurnEffect, FreezeChance,
        // 经济强化
        GoldBoost, DiscountCard, StartBonus, InterestRate,
        // 生存强化
        LifeSteal, ArmorUp, DodgeChance, Regeneration,
        ShieldOnKill, DeathPrevent, DamageReflect, VampiricAura,
        // 特殊
        DoubleShot, Ricochet, Overclock, Disintegrate,
        GravityWell, TimeWarp, Duplicate, VoidRift
    }

    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

    public enum EventEffect
    {
        GainGold, LoseGold, GainHp, LoseHp,
        RandomCard, RemoveCard, UpgradeCard,
        DuplicateCard, CurseCard, BlessCard
    }

    // 运行状态
    private RunState currentState = RunState.Inactive;
    private int currentStage = 0;
    private int totalStages = 20;
    private List<TowerCard> deck = new List<TowerCard>();
    private List<ModifierCard> activeModifiers = new List<ModifierCard>();
    private int gold = 100;
    private int hp = 20;
    private int maxHp = 20;
    private Dictionary<string, int> killsPerTower = new Dictionary<string, int>();
    private int totalKills = 0;
    private int totalGoldEarned = 0;

    // 配置
    private const int MAX_TOWERS = 8;
    private const int MAX_MODIFIERS = 12;
    private const int DRAFT_CHOICES = 3;
    private const int BASE_GOLD_PER_STAGE = 50;
    private const int GOLD_PER_KILL = 10;
    private const int SHOP_REFRESH_COST = 10;

    private System.Random rng;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==================== 运行流程 ====================

    /// <summary>
    /// 开始新一局肉鸽
    /// </summary>
    public void StartNewRun(int seed = -1)
    {
        rng = seed >= 0 ? new System.Random(seed) : new System.Random();
        
        deck.Clear();
        activeModifiers.Clear();
        killsPerTower.Clear();
        
        gold = 100;
        hp = maxHp = 20;
        currentStage = 0;
        totalKills = 0;
        totalGoldEarned = 0;

        // 初始选牌
        currentState = RunState.Drafting;
        Debug.Log($"[Roguelike] 新一局开始! 种子: {(seed >= 0 ? seed.ToString() : "随机")}");
    }

    /// <summary>
    /// 选牌阶段 - 获取可选的塔牌
    /// </summary>
    public List<TowerCard> GetDraftChoices()
    {
        if (currentState != RunState.Drafting) return null;

        var choices = new List<TowerCard>();
        var types = Enum.GetValues(typeof(TowerType));
        var usedNames = new HashSet<string>();
        
        for (int i = 0; i < DRAFT_CHOICES; i++)
        {
            TowerType type = (TowerType)types.GetValue(rng.Next(types.Length));
            
            // 确保不重复
            int attempts = 0;
            while (deck.Exists(t => t.type == type) && attempts < 20)
            {
                type = (TowerType)types.GetValue(rng.Next(types.Length));
                attempts++;
            }

            Rarity rarity = RollRarity();
            var card = new TowerCard
            {
                id = $"card_{type}_{currentStage}_{i}",
                towerName = $"{rarity} {type} Tower {GetStagePrefix()}",
                type = type,
                rarity = rarity,
                level = 1 + (int)rarity,
                description = GenerateTowerDescription(type, rarity),
                attachedMods = rarity >= Rarity.Epic ? GenerateRandomMods(1) : new List<ModifierCard>()
            };
            
            choices.Add(card);
        }

        return choices;
    }

    /// <summary>
    /// 选择一张牌加入卡组
    /// </summary>
    public bool DraftCard(TowerCard card)
    {
        if (deck.Count >= MAX_TOWERS)
        {
            // 需要替换
            Debug.Log("[Roguelike] 塔位已满，请选择要替换的塔");
            return false;
        }

        deck.Add(card);
        killsPerTower[card.id] = 0;
        
        Debug.Log($"[Roguelike] 选择: {card.towerName} ({card.rarity})");

        // 进入下一阶段
        AdvanceToNextStage();
        return true;
    }

    // ==================== 商店系统 ====================

    /// <summary>
    /// 获取商店物品
    /// </summary>
    public List<ShopItem> GetShopItems()
    {
        var items = new List<ShopItem>();
        
        // 随机生成商品
        int itemCount = 3 + (int)(rng.NextDouble() * 3); // 3-5个商品
        for (int i = 0; i < itemCount; i++)
        {
            ShopItemType type = (ShopItemType)rng.Next(5);
            items.Add(GenerateShopItem(type));
        }

        return items;
    }

    public class ShopItem
    {
        public string id;
        public ShopItemType type;
        public string name;
        public string description;
        public int cost;
        public Rarity rarity;
        public TowerCard towerCard;
        public ModifierCard modifierCard;
    }

    public enum ShopItemType { TowerCard, Modifier, Heal, Upgrade, RemoveCard }

    private ShopItem GenerateShopItem(ShopItemType type)
    {
        var item = new ShopItem { type = type };
        Rarity rarity = RollRarity();
        int baseCost = 0;

        switch (type)
        {
            case ShopItemType.TowerCard:
                TowerType towerType = (TowerType)rng.Next(16);
                item.towerCard = new TowerCard
                {
                    id = $"shop_tower_{currentStage}_{rng.Next(1000)}",
                    towerName = $"{rarity} {towerType}",
                    type = towerType,
                    rarity = rarity,
                    level = 1 + (int)rarity
                };
                baseCost = 30 + (int)rarity * 30;
                item.name = item.towerCard.towerName;
                item.description = $"新塔: {towerType}";
                break;

            case ShopItemType.Modifier:
                item.modifierCard = GenerateRandomModifier(rarity);
                baseCost = 20 + (int)rarity * 20;
                item.name = item.modifierCard.name;
                item.description = item.modifierCard.description;
                break;

            case ShopItemType.Heal:
                baseCost = 30;
                item.name = "生命恢复";
                item.description = "恢复5点生命值";
                item.rarity = Rarity.Common;
                break;

            case ShopItemType.Upgrade:
                baseCost = 50 + currentStage * 5;
                item.name = "塔升级";
                item.description = "随机升级一座塔+2级";
                item.rarity = Rarity.Uncommon;
                break;

            case ShopItemType.RemoveCard:
                baseCost = 20;
                item.name = "移除塔牌";
                item.description = "从卡组中移除一座塔";
                item.rarity = Rarity.Common;
                break;
        }

        item.cost = baseCost + rng.Next(-10, 11);
        item.id = $"shop_{type}_{currentStage}_{rng.Next(1000)}";
        return item;
    }

    public bool BuyItem(ShopItem item)
    {
        if (gold < item.cost) return false;

        gold -= item.cost;

        switch (item.type)
        {
            case ShopItemType.TowerCard:
                if (deck.Count < MAX_TOWERS) deck.Add(item.towerCard);
                break;
            case ShopItemType.Modifier:
                if (activeModifiers.Count < MAX_MODIFIERS) activeModifiers.Add(item.modifierCard);
                break;
            case ShopItemType.Heal:
                hp = Math.Min(hp + 5, maxHp);
                break;
            case ShopItemType.Upgrade:
                if (deck.Count > 0)
                {
                    var toUpgrade = deck[rng.Next(deck.Count)];
                    toUpgrade.level += 2;
                }
                break;
            case ShopItemType.RemoveCard:
                if (deck.Count > 1)
                {
                    deck.RemoveAt(rng.Next(deck.Count));
                }
                break;
        }

        Debug.Log($"[Roguelike] 购买: {item.name} (-{item.cost}金币)");
        return true;
    }

    public void RefreshShop() { gold -= SHOP_REFRESH_COST; }

    // ==================== 战斗结算 ====================

    /// <summary>
    /// 关卡结算
    /// </summary>
    public StageResult CompleteStage(int enemiesKilled, int damageTaken, int goldEarned)
    {
        hp -= damageTaken;
        gold += goldEarned;
        totalGoldEarned += goldEarned;
        totalKills += enemiesKilled;

        // 金币利息（每50金+1利息，最多+5）
        int interest = Math.Min(gold / 50, 5);
        gold += interest;

        var result = new StageResult
        {
            stageCleared = true,
            enemiesKilled = enemiesKilled,
            goldEarned = goldEarned + interest,
            interestBonus = interest,
            hpRemaining = hp,
            totalGold = gold
        };

        // 奖励选牌
        if (currentStage % 3 == 0 && deck.Count < MAX_TOWERS)
        {
            result.draftReward = true;
        }

        // 每5关Boss
        currentStage++;
        if (currentStage % 5 == 0)
        {
            currentState = RunState.BossChallenge;
            result.isBossStage = true;
        }
        else
        {
            currentState = RunState.Shop;
        }

        // 通关检查
        if (currentStage >= totalStages)
        {
            currentState = RunState.RunComplete;
            result.runComplete = true;
        }

        Debug.Log($"[Roguelike] 关卡{currentStage}/{totalStages}完成! 击杀:{enemiesKilled} 收入:{goldEarned} HP:{hp} 金币:{gold}");
        return result;
    }

    public class StageResult
    {
        public bool stageCleared;
        public int enemiesKilled;
        public int goldEarned;
        public int interestBonus;
        public int hpRemaining;
        public int totalGold;
        public bool draftReward;
        public bool isBossStage;
        public bool runComplete;
    }

    public void EnterNextStage()
    {
        if (currentState == RunState.Shop)
            currentState = RunState.Battle;
        else if (currentState == RunState.BossChallenge)
            currentState = RunState.Battle;
    }

    public void EnterShop() => currentState = RunState.Shop;

    // ==================== 辅助函数 ====================

    private Rarity RollRarity()
    {
        double roll = rng.NextDouble() * 100;
        // 基础概率
        if (roll < 40) return Rarity.Common;
        if (roll < 70) return Rarity.Uncommon;
        if (roll < 88) return Rarity.Rare;
        if (roll < 96) return Rarity.Epic;
        if (roll < 99.5) return Rarity.Legendary;
        return Rarity.Mythic;
    }

    private List<ModifierCard> GenerateRandomMods(int count)
    {
        var mods = new List<ModifierCard>();
        for (int i = 0; i < count; i++)
            mods.Add(GenerateRandomModifier(RollRarity()));
        return mods;
    }

    private ModifierCard GenerateRandomModifier(Rarity rarity)
    {
        var types = Enum.GetValues(typeof(ModifierType));
        ModifierType type = (ModifierType)types.GetValue(rng.Next(types.Length));
        
        float value = 0;
        string descTemplate = "";
        
        switch (type)
        {
            case ModifierType.DamageUp: value = 0.15f + (int)rarity * 0.1f; descTemplate = "攻击力 +{0}%"; break;
            case ModifierType.AttackSpeedUp: value = 0.12f + (int)rarity * 0.08f; descTemplate = "攻速 +{0}%"; break;
            case ModifierType.RangeUp: value = 0.10f + (int)rarity * 0.08f; descTemplate = "射程 +{0}%"; break;
            case ModifierType.CritUp: value = 0.05f + (int)rarity * 0.05f; descTemplate = "暴击率 +{0}%"; break;
            case ModifierType.GoldBoost: value = 0.20f + (int)rarity * 0.10f; descTemplate = "金币收益 +{0}%"; break;
            case ModifierType.DiscountCard: value = 0.05f + (int)rarity * 0.05f; descTemplate = "商店折扣 -{0}%"; break;
            case ModifierType.ChainLightning: value = 1 + (int)rarity; descTemplate = "连锁闪电 ({0}目标)"; break;
            case ModifierType.SplashDamage: value = 0.30f + (int)rarity * 0.15f; descTemplate = "溅射伤害 {0}%"; break;
            case ModifierType.Lifesteal: value = 0.05f + (int)rarity * 0.05f; descTemplate = "吸血 {0}%"; break;
            case ModifierType.Regeneration: value = 1 + (int)rarity; descTemplate = "每秒恢复 {0}HP"; break;
            case ModifierType.DoubleShot: value = 0.10f + (int)rarity * 0.10f; descTemplate = "双重射击 {0}%概率"; break;
            default: value = 0.1f + (int)rarity * 0.1f; descTemplate = $"{type} +{0}%"; break;
        }

        return new ModifierCard
        {
            id = $"mod_{type}_{rng.Next(1000)}",
            name = $"{rarity} {type}",
            type = type,
            rarity = rarity,
            value = value,
            description = string.Format(descTemplate, Math.Round(value * 100, 0))
        };
    }

    private string GenerateTowerDescription(TowerType type, Rarity rarity)
    {
        var descriptions = new Dictionary<TowerType, string>
        {
            { TowerType.Arrow, "基础箭塔，快速攻击" },
            { TowerType.Cannon, "炮塔，范围伤害" },
            { TowerType.Ice, "冰塔，减速敌人" },
            { TowerType.Lightning, "电塔，连锁攻击" },
            { TowerType.Poison, "毒塔，持续伤害" },
            { TowerType.Laser, "激光塔，穿透攻击" },
            { TowerType.Tesla, "特斯拉塔，AOE电场" },
            { TowerType.Missile, "导弹塔，追踪目标" },
            { TowerType.Turret, "机枪塔，极高攻速" },
            { TowerType.Void, "虚空塔，即死效果" },
            { TowerType.Flame, "火焰塔，灼烧效果" },
            { TowerType.Frost, "极寒塔，冻结敌人" },
            { TowerType.Storm, "风暴塔，击退效果" },
            { TowerType.Gravity, "重力塔，聚怪效果" },
            { TowerType.Chrono, "时空塔，减速时间" },
            { TowerType.Shadow, "暗影塔，腐蚀护甲" }
        };

        return descriptions.TryGetValue(type, out string desc) ? $"{rarity}级 - {desc}" : $"{rarity}级 {type}塔";
    }

    private string GetStagePrefix() => currentStage <= 5 ? "初期" : currentStage <= 10 ? "中期" : currentStage <= 15 ? "后期" : "终局";

    private void AdvanceToNextStage()
    {
        if (currentState == RunState.BossChallenge)
        {
            currentStage++;
            currentState = currentStage >= totalStages ? RunState.RunComplete : RunState.Battle;
        }
        else
        {
            currentState = RunState.Battle;
        }
    }

    // ==================== Getter ====================
    public RunState GetCurrentState() => currentState;
    public int GetCurrentStage() => currentStage;
    public int GetTotalStages() => totalStages;
    public int GetGold() => gold;
    public int GetHP() => hp;
    public int GetMaxHP() => maxHp;
    public List<TowerCard> GetDeck() => deck;
    public List<ModifierCard> GetActiveModifiers() => activeModifiers;
    public int GetTotalKills() => totalKills;
    public int GetTotalGoldEarned() => totalGoldEarned;

    /// <summary>
    /// 计算塔的全局伤害加成
    /// </summary>
    public float GetDamageMultiplier()
    {
        float mult = 1f;
        foreach (var mod in activeModifiers)
        {
            if (mod.type == ModifierType.DamageUp)
                mult += mod.value;
        }
        return mult;
    }

    /// <summary>
    /// 检查是否拥有某个神器
    /// </summary>
    public bool HasModifier(ModifierType type)
    {
        return activeModifiers.Exists(m => m.type == type);
    }
}
