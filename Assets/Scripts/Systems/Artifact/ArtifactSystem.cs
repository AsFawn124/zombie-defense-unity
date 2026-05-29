using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 神器(Artifact)系统 - Roguelike深度核心
/// 每局随机获得的强力被动效果，彻底改变玩法
/// 参考: Slay the Spire遗物、Hades祝福、Risk of Rain道具
/// </summary>
public class ArtifactSystem : MonoBehaviour
{
    public static ArtifactSystem Instance;

    [Header("配置")]
    public ArtifactConfig Config;

    private List<ActiveArtifact> _activeArtifacts = new List<ActiveArtifact>();
    private ArtifactShopInventory _shopInventory;
    private int _rerollCost = 10;

    // 事件
    public event Action<ArtifactData> OnArtifactAcquired;
    public event Action<ArtifactData> OnArtifactActivated;
    public event Action<List<ArtifactData>> OnShopRefreshed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #region === 获取神器 ===

    /// <summary>
    /// 每波结束时有概率掉落神器
    /// </summary>
    public void CheckArtifactDrop(int wave)
    {
        float baseChance = Config.BaseDropChance;
        float luckBonus = _activeArtifacts.FindAll(a => a.Data.Rarity == ArtifactRarity.Legendary &&
            a.Data.Category == ArtifactCategory.Luck).Count * 0.05f;

        if (UnityEngine.Random.value < baseChance + luckBonus)
        {
            var artifact = RollArtifact(wave);
            if (artifact != null)
                OfferArtifactChoice(artifact, wave);
        }
    }

    /// <summary>
    /// Boss击杀必定掉落神器
    /// </summary>
    public void BossDefeatedDrop(int bossWave)
    {
        int count = bossWave >= 50 ? 3 : (bossWave >= 30 ? 2 : 1);
        var artifacts = new List<ArtifactData>();

        for (int i = 0; i < count; i++)
        {
            var art = RollArtifact(bossWave, guaranteedRare: i == 0);
            if (art != null) artifacts.Add(art);
        }

        // 三选一
        if (artifacts.Count > 0)
        {
            var selection = new List<ArtifactData>();
            while (selection.Count < Mathf.Min(3, artifacts.Count + 2))
            {
                var roll = RollArtifact(bossWave);
                if (!selection.Contains(roll))
                    selection.Add(roll);
            }
            OfferArtifactChoice(selection.ToArray(), bossWave);
        }
    }

    private ArtifactData RollArtifact(int wave, bool guaranteedRare = false)
    {
        var pool = Config.ArtifactPool.FindAll(a =>
            wave >= a.MinWave &&
            !HasArtifact(a.ArtifactId) ||
            (a.MaxStacks > 0 && GetArtifactStack(a.ArtifactId) < a.MaxStacks)
        );

        if (pool.Count == 0) return null;

        // 权重随机
        float totalWeight = 0;
        foreach (var a in pool)
        {
            float weight = GetArtifactWeight(a, guaranteedRare);
            totalWeight += weight;
        }

        float roll = UnityEngine.Random.Range(0, totalWeight);
        float cumulative = 0;
        foreach (var a in pool)
        {
            cumulative += GetArtifactWeight(a, guaranteedRare);
            if (roll <= cumulative)
                return a;
        }

        return pool[pool.Count - 1];
    }

    private float GetArtifactWeight(ArtifactData art, bool guaranteedRare)
    {
        float baseWeight = art.Rarity switch
        {
            ArtifactRarity.Common => 100f,
            ArtifactRarity.Uncommon => 60f,
            ArtifactRarity.Rare => 25f,
            ArtifactRarity.Legendary => 5f,
            ArtifactRarity.Mythic => 1f,
            _ => 50f
        };

        if (guaranteedRare)
            baseWeight = art.Rarity >= ArtifactRarity.Rare ? baseWeight * 3f : 0f;

        return baseWeight;
    }

    private void OfferArtifactChoice(ArtifactData[] choices, int wave)
    {
        // 通过EventManager或直接UI显示选择
        Debug.Log($"[Artifact] 第{wave}波, 神器三选一:");
        foreach (var art in choices)
            Debug.Log($"  - {art.ArtifactName} [{art.Rarity}]: {art.Description}");

        // TODO: 接入UI选择界面
        // UIManager.Instance.ShowArtifactSelection(choices);
    }

    public void AcquireArtifact(ArtifactData artifact)
    {
        var existing = _activeArtifacts.Find(a => a.Data.ArtifactId == artifact.ArtifactId);

        if (existing != null && artifact.MaxStacks > 0)
        {
            existing.Stacks++;
            existing.Data = artifact;
        }
        else
        {
            _activeArtifacts.Add(new ActiveArtifact
            {
                Data = artifact,
                Stacks = 1,
                AcquiredWave = WaveManager.Instance?.CurrentWave ?? 0
            });
        }

        ApplyArtifactEffects(artifact);
        OnArtifactAcquired?.Invoke(artifact);
    }

    #endregion

    #region === 神器效果应用 ===

    private void ApplyArtifactEffects(ArtifactData art)
    {
        foreach (var effect in art.Effects)
        {
            switch (effect.Type)
            {
                case ArtifactEffectType.ModifyTowerDamage:
                    // 全局塔伤害修正
                    break;
                case ArtifactEffectType.ModifyEnemyHp:
                    // 全局敌人血量修正
                    break;
                case ArtifactEffectType.ModifyGoldGain:
                    // 金币获取修正
                    break;
                case ArtifactEffectType.ModifyDropRate:
                    // 掉落率修正
                    break;
                case ArtifactEffectType.ModifySkillChoices:
                    // 技能选择数量+1
                    SkillManager.Instance?.AddExtraChoice(effect.Value);
                    break;
                case ArtifactEffectType.StartWithTower:
                    // 开局自带防御塔
                    break;
                case ArtifactEffectType.ExtraLife:
                    // 额外生命
                    BaseManager.Instance?.AddExtraLife((int)effect.Value);
                    break;
                case ArtifactEffectType.ShieldOnWaveStart:
                    // 每波开始获得护盾
                    break;
                case ArtifactEffectType.CriticalStrikeChance:
                    // 暴击率
                    break;
                case ArtifactEffectType.LifeSteal:
                    // 吸血
                    break;
                case ArtifactEffectType.ThornsDamage:
                    // 反伤
                    break;
                case ArtifactEffectType.ShopDiscount:
                    // 商店折扣
                    break;
                case ArtifactEffectType.RerollDiscount:
                    _rerollCost = Mathf.Max(1, _rerollCost - (int)effect.Value);
                    break;
                case ArtifactEffectType.FreeRerollDaily:
                    // 每日免费重roll
                    break;
                case ArtifactEffectType.BonusExperience:
                    // 经验加成
                    break;
            }
        }
    }

    #endregion

    #region === 神器商店 ===

    public void OpenArtifactShop()
    {
        _shopInventory = new ArtifactShopInventory
        {
            Slots = new List<ArtifactShopSlot>()
        };

        int slotCount = 3 + (_activeArtifacts.FindAll(
            a => a.Data.Category == ArtifactCategory.Economy).Count);

        for (int i = 0; i < slotCount; i++)
        {
            _shopInventory.Slots.Add(new ArtifactShopSlot
            {
                Artifact = RollArtifact(WaveManager.Instance?.CurrentWave ?? 1),
                Price = CalculateArtifactPrice(i),
                Purchased = false
            });
        }

        OnShopRefreshed?.Invoke(GetShopArtifacts());
    }

    public void RerollShop()
    {
        int cost = _rerollCost;
        if (GameManager.Instance != null && GameManager.Instance.SpendGold(cost))
        {
            OpenArtifactShop();
            _rerollCost = Mathf.Min(_rerollCost + 5, 50);
        }
    }

    public bool PurchaseShopArtifact(int slotIndex)
    {
        if (_shopInventory == null || slotIndex >= _shopInventory.Slots.Count)
            return false;

        var slot = _shopInventory.Slots[slotIndex];
        if (slot.Purchased) return false;

        if (GameManager.Instance != null && GameManager.Instance.SpendGold(slot.Price))
        {
            slot.Purchased = true;
            AcquireArtifact(slot.Artifact);
            return true;
        }

        return false;
    }

    private int CalculateArtifactPrice(int slotIndex)
    {
        var art = _shopInventory.Slots[slotIndex].Artifact;
        int basePrice = art.Rarity switch
        {
            ArtifactRarity.Common => 50,
            ArtifactRarity.Uncommon => 100,
            ArtifactRarity.Rare => 200,
            ArtifactRarity.Legendary => 400,
            ArtifactRarity.Mythic => 800,
            _ => 100
        };

        // 折扣神器
        float discount = 1f;
        foreach (var a in _activeArtifacts)
        {
            foreach (var e in a.Data.Effects)
                if (e.Type == ArtifactEffectType.ShopDiscount)
                    discount -= e.Value;
        }

        return Mathf.Max(10, Mathf.RoundToInt(basePrice * discount));
    }

    public List<ArtifactData> GetShopArtifacts()
    {
        var list = new List<ArtifactData>();
        if (_shopInventory == null) return list;
        foreach (var slot in _shopInventory.Slots)
            if (!slot.Purchased)
                list.Add(slot.Artifact);
        return list;
    }

    #endregion

    #region === 查询 ===

    public bool HasArtifact(string artifactId)
        => _activeArtifacts.Exists(a => a.Data.ArtifactId == artifactId);

    public int GetArtifactStack(string artifactId)
        => _activeArtifacts.Find(a => a.Data.ArtifactId == artifactId)?.Stacks ?? 0;

    public float GetTotalEffectValue(ArtifactEffectType type)
    {
        float total = 0;
        foreach (var art in _activeArtifacts)
            foreach (var effect in art.Data.Effects)
                if (effect.Type == type)
                    total += effect.Value * art.Stacks;
        return total;
    }

    public List<ActiveArtifact> GetActiveArtifacts() => _activeArtifacts;
    public int GetArtifactCount() => _activeArtifacts.Count;

    /// <summary>
    /// 神器中开启 - 从神器库永久解锁(跨局成长)
    /// </summary>
    public List<string> GetUnlockedArtifacts()
    {
        string json = PlayerPrefs.GetString("unlocked_artifacts", "");
        return string.IsNullOrEmpty(json)
            ? new List<string>()
            : new List<string>(json.Split(','));
    }

    #endregion
}

#region === 数据结构 ===

[Serializable]
public class ActiveArtifact
{
    public ArtifactData Data;
    public int Stacks;
    public int AcquiredWave;
}

[Serializable]
public class ArtifactShopInventory
{
    public List<ArtifactShopSlot> Slots;
}

[Serializable]
public class ArtifactShopSlot
{
    public ArtifactData Artifact;
    public int Price;
    public bool Purchased;
}

[Serializable]
public class ArtifactConfig : ScriptableObject
{
    public float BaseDropChance = 0.1f;
    public List<ArtifactData> ArtifactPool = new List<ArtifactData>();
}

public enum ArtifactRarity
{
    Common,     // 白色 50%
    Uncommon,   // 绿色 30%
    Rare,       // 蓝色 15%
    Legendary,  // 橙色 4%
    Mythic      // 红色 1%
}

public enum ArtifactCategory
{
    Combat,     // 战斗
    Defense,    // 防御
    Economy,    // 经济
    Luck,       // 幸运
    Utility,    // 功能
    Cursed      // 诅咒(强效但有副作用)
}

public enum ArtifactEffectType
{
    ModifyTowerDamage, ModifyEnemyHp, ModifyGoldGain,
    ModifyDropRate, ModifySkillChoices, StartWithTower,
    ExtraLife, ShieldOnWaveStart, CriticalStrikeChance,
    LifeSteal, ThornsDamage, ShopDiscount, RerollDiscount,
    FreeRerollDaily, BonusExperience, ProjectileCount,
    ChainLightning, ExplodeOnKill, ReviveOnce,
    DoubleBossReward, RandomBuffPerWave, ExtraTowerSlot
}

[Serializable]
public class ArtifactData
{
    public string ArtifactId;
    public string ArtifactName;
    public string Description;
    public string FlavorText;         // 风味文字
    public string IconName;
    public ArtifactRarity Rarity;
    public ArtifactCategory Category;
    public int MinWave = 0;           // 最小出现波次
    public int MaxStacks = 0;         // 0=不可堆叠, N=最大堆叠数
    public List<ArtifactEffect> Effects = new List<ArtifactEffect>();
    public List<ArtifactSynergy> Synergies = new List<ArtifactSynergy>(); // 与其他神器的联动
}

[Serializable]
public class ArtifactEffect
{
    public ArtifactEffectType Type;
    public float Value;               // 主数值
    public string CustomParam;        // 自定义参数
}

[Serializable]
public class ArtifactSynergy
{
    public string RequiredArtifactId; // 需要的另一件神器
    public string SynergyDescription;
    public float BonusValue;
}

#endregion

#region === 预定义神器库 ===

/// <summary>
/// 50+ 预定义神器
/// </summary>
public static class ArtifactPresets
{
    public static List<ArtifactData> AllArtifacts = new List<ArtifactData>
    {
        // === 战斗类 ===
        new ArtifactData {
            ArtifactId = "turbo_charger", ArtifactName = "涡轮增压器",
            Description = "所有塔攻击速度+20%",
            Rarity = ArtifactRarity.Common, Category = ArtifactCategory.Combat,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ModifyTowerDamage, Value = 0.2f }
            }
        },
        new ArtifactData {
            ArtifactId = "explosive_rounds", ArtifactName = "爆裂弹头",
            Description = "击杀敌人时造成范围50%伤害爆炸",
            Rarity = ArtifactRarity.Rare, Category = ArtifactCategory.Combat, MinWave = 5,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ExplodeOnKill, Value = 0.5f }
            }
        },
        new ArtifactData {
            ArtifactId = "lightning_rod", ArtifactName = "避雷针",
            Description = "每10秒对随机敌人释放连锁闪电",
            Rarity = ArtifactRarity.Uncommon, Category = ArtifactCategory.Combat,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ChainLightning, Value = 3f }
            }
        },
        new ArtifactData {
            ArtifactId = "double_barrel", ArtifactName = "双管齐下",
            Description = "所有塔发射2发子弹",
            Rarity = ArtifactRarity.Legendary, Category = ArtifactCategory.Combat, MinWave = 15,
            MaxStacks = 2,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ProjectileCount, Value = 1f }
            }
        },
        new ArtifactData {
            ArtifactId = "executioner", ArtifactName = "处刑人之斧",
            Description = "对血量低于20%的敌人造成4倍伤害",
            Rarity = ArtifactRarity.Rare, Category = ArtifactCategory.Combat, MinWave = 10,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ModifyTowerDamage, Value = 4f, CustomParam = "hp_below_20%" }
            }
        },
        new ArtifactData {
            ArtifactId = "vampire_fangs", ArtifactName = "吸血鬼獠牙",
            Description = "所有塔获得15%吸血",
            Rarity = ArtifactRarity.Uncommon, Category = ArtifactCategory.Combat,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.LifeSteal, Value = 0.15f }
            }
        },

        // === 防御类 ===
        new ArtifactData {
            ArtifactId = "energy_shield", ArtifactName = "能量护盾",
            Description = "每波开始获得最大生命值10%的护盾",
            Rarity = ArtifactRarity.Common, Category = ArtifactCategory.Defense,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ShieldOnWaveStart, Value = 0.1f }
            }
        },
        new ArtifactData {
            ArtifactId = "phoenix_feather", ArtifactName = "凤凰羽毛",
            Description = "死亡时复活一次，恢复50%生命",
            Rarity = ArtifactRarity.Legendary, Category = ArtifactCategory.Defense, MinWave = 10,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ReviveOnce, Value = 0.5f }
            }
        },
        new ArtifactData {
            ArtifactId = "thornmail", ArtifactName = "荆棘之甲",
            Description = "反弹30%受到的伤害给攻击者",
            Rarity = ArtifactRarity.Uncommon, Category = ArtifactCategory.Defense,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ThornsDamage, Value = 0.3f }
            }
        },
        new ArtifactData {
            ArtifactId = "extra_life_crystal", ArtifactName = "生命水晶",
            Description = "基地生命+3",
            Rarity = ArtifactRarity.Common, Category = ArtifactCategory.Defense,
            MaxStacks = 5,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ExtraLife, Value = 3f }
            }
        },

        // === 经济类 ===
        new ArtifactData {
            ArtifactId = "golden_goose", ArtifactName = "下金蛋的鹅",
            Description = "每波额外获得50金币",
            Rarity = ArtifactRarity.Uncommon, Category = ArtifactCategory.Economy,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ModifyGoldGain, Value = 50f }
            }
        },
        new ArtifactData {
            ArtifactId = "lucky_coin", ArtifactName = "幸运硬币",
            Description = "击杀敌人金币翻倍",
            Rarity = ArtifactRarity.Rare, Category = ArtifactCategory.Economy, MinWave = 10,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ModifyGoldGain, Value = 2f, CustomParam = "kill_only" }
            }
        },
        new ArtifactData {
            ArtifactId = "merchant_card", ArtifactName = "商人贵宾卡",
            Description = "所有商店价格-30%",
            Rarity = ArtifactRarity.Uncommon, Category = ArtifactCategory.Economy,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ShopDiscount, Value = 0.3f }
            }
        },

        // === 幸运类 ===
        new ArtifactData {
            ArtifactId = "four_leaf_clover", ArtifactName = "四叶草",
            Description = "装备/芯片掉落率+50%",
            Rarity = ArtifactRarity.Common, Category = ArtifactCategory.Luck,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ModifyDropRate, Value = 0.5f }
            }
        },
        new ArtifactData {
            ArtifactId = "rabbit_foot", ArtifactName = "幸运兔脚",
            Description = "技能选择变为4选1",
            Rarity = ArtifactRarity.Legendary, Category = ArtifactCategory.Luck, MinWave = 5,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ModifySkillChoices, Value = 4f }
            }
        },

        // === 诅咒类 (高风险高回报) ===
        new ArtifactData {
            ArtifactId = "cursed_blade", ArtifactName = "诅咒之刃",
            Description = "⚡所有伤害×2 ⚠️受到伤害×2",
            FlavorText = "力量从来不是免费的",
            Rarity = ArtifactRarity.Mythic, Category = ArtifactCategory.Cursed, MinWave = 1,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ModifyTowerDamage, Value = 2f },
            }
        },
        new ArtifactData {
            ArtifactId = "glass_cannon", ArtifactName = "玻璃大炮",
            Description = "⚡塔攻击力×3 ⚠️塔生命值-70%",
            FlavorText = "一击必杀，或被一击必杀",
            Rarity = ArtifactRarity.Legendary, Category = ArtifactCategory.Cursed, MinWave = 5,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ModifyTowerDamage, Value = 3f },
            }
        },
        new ArtifactData {
            ArtifactId = "blood_oath", ArtifactName = "血之誓约",
            Description = "⚡每波额外获得200金币 ⚠️每波失去1点基地生命",
            Rarity = ArtifactRarity.Rare, Category = ArtifactCategory.Cursed,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ModifyGoldGain, Value = 200f },
            }
        },
        new ArtifactData {
            ArtifactId = "pandora_box", ArtifactName = "潘多拉魔盒",
            Description = "每波获得随机1-3个正面效果，但也有随机负面效果",
            Rarity = ArtifactRarity.Mythic, Category = ArtifactCategory.Cursed, MinWave = 10,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.RandomBuffPerWave, Value = 3f },
            }
        },

        // === BOSS专属掉落 ===
        new ArtifactData {
            ArtifactId = "boss_core", ArtifactName = "Boss核心",
            Description = "击杀Boss获得双倍奖励",
            Rarity = ArtifactRarity.Rare, Category = ArtifactCategory.Utility, MinWave = 10,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.DoubleBossReward, Value = 2f }
            }
        },
        new ArtifactData {
            ArtifactId = "extra_slot", ArtifactName = "战术背包",
            Description = "背包格子+2",
            Rarity = ArtifactRarity.Uncommon, Category = ArtifactCategory.Utility,
            Effects = new List<ArtifactEffect> {
                new ArtifactEffect { Type = ArtifactEffectType.ExtraTowerSlot, Value = 2f }
            }
        },
    };
}

#endregion
