using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 技能管理器 - Roguelike技能系统
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }
    
    [Header("技能数据库")]
    public List<SkillData> AllSkills = new List<SkillData>();
    
    [Header("UI引用")]
    public SkillSelectionUI SkillSelectionUI;
    
    // 运行时数据
    private List<SkillData> acquiredSkills = new List<SkillData>();
    private System.Random random = new System.Random();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 初始化技能数据库
        InitializeSkills();
        
        // 订阅波次完成事件
        GameManager.Instance.OnWaveEnd += ShowSkillSelection;
    }
    
    /// <summary>
    /// 初始化技能数据
    /// </summary>
    private void InitializeSkills()
    {
        // 攻击力提升
        AllSkills.Add(new SkillData
        {
            SkillId = "dmg_up_1",
            SkillName = "火力强化",
            Description = "攻击力 +20%",
            SkillType = SkillType.DamageUp,
            Value = 0.2f,
            Icon = null,
            Rarity = SkillRarity.Common
        });
        
        AllSkills.Add(new SkillData
        {
            SkillId = "dmg_up_2",
            SkillName = "致命打击",
            Description = "攻击力 +35%",
            SkillType = SkillType.DamageUp,
            Value = 0.35f,
            Icon = null,
            Rarity = SkillRarity.Rare
        });
        
        // 射程提升
        AllSkills.Add(new SkillData
        {
            SkillId = "range_up_1",
            SkillName = "望远镜",
            Description = "攻击范围 +25%",
            SkillType = SkillType.RangeUp,
            Value = 0.25f,
            Icon = null,
            Rarity = SkillRarity.Common
        });
        
        // 射速提升
        AllSkills.Add(new SkillData
        {
            SkillId = "firerate_up_1",
            SkillName = "快速装填",
            Description = "攻击速度 +20%",
            SkillType = SkillType.FireRateUp,
            Value = 0.2f,
            Icon = null,
            Rarity = SkillRarity.Common
        });
        
        AllSkills.Add(new SkillData
        {
            SkillId = "firerate_up_2",
            SkillName = "极速射击",
            Description = "攻击速度 +40%",
            SkillType = SkillType.FireRateUp,
            Value = 0.4f,
            Icon = null,
            Rarity = SkillRarity.Rare
        });
        
        // 穿透
        AllSkills.Add(new SkillData
        {
            SkillId = "pierce_1",
            SkillName = "穿甲弹",
            Description = "子弹可穿透 2 个敌人",
            SkillType = SkillType.Pierce,
            Value = 2,
            Icon = null,
            Rarity = SkillRarity.Rare
        });
        
        AllSkills.Add(new SkillData
        {
            SkillId = "pierce_2",
            SkillName = "贯穿射击",
            Description = "子弹可穿透 4 个敌人",
            SkillType = SkillType.Pierce,
            Value = 4,
            Icon = null,
            Rarity = SkillRarity.Epic
        });
        
        // 溅射
        AllSkills.Add(new SkillData
        {
            SkillId = "splash_1",
            SkillName = "爆裂弹",
            Description = "子弹造成范围伤害",
            SkillType = SkillType.Splash,
            Value = 1.5f,
            Icon = null,
            Rarity = SkillRarity.Rare
        });
        
        // 暴击
        AllSkills.Add(new SkillData
        {
            SkillId = "crit_1",
            SkillName = "精准瞄准",
            Description = "暴击率 +15%",
            SkillType = SkillType.CritRateUp,
            Value = 0.15f,
            Icon = null,
            Rarity = SkillRarity.Common
        });
        
        // 金币获取
        AllSkills.Add(new SkillData
        {
            SkillId = "gold_1",
            SkillName = "贪婪",
            Description = "金币获取 +30%",
            SkillType = SkillType.GoldBonus,
            Value = 0.3f,
            Icon = null,
            Rarity = SkillRarity.Common
        });
        
        // 多重射击
        AllSkills.Add(new SkillData
        {
            SkillId = "multishot_1",
            SkillName = "双重射击",
            Description = "每次发射 2 发子弹",
            SkillType = SkillType.MultiShot,
            Value = 2,
            Icon = null,
            Rarity = SkillRarity.Epic
        });
        
        // 减速效果
        AllSkills.Add(new SkillData
        {
            SkillId = "slow_1",
            SkillName = "冰冻弹",
            Description = "子弹使敌人减速 30%",
            SkillType = SkillType.SlowEffect,
            Value = 0.3f,
            Icon = null,
            Rarity = SkillRarity.Rare
        });
    }
    
    /// <summary>
    /// 显示技能选择界面
    /// </summary>
    private void ShowSkillSelection()
    {
        List<SkillData> options = GetRandomSkills(3);
        
        if (SkillSelectionUI != null)
        {
            SkillSelectionUI.Show(options);
        }
        else
        {
            // 如果没有UI，自动选择第一个
            SelectSkill(options[0]);
        }
    }
    
    /// <summary>
    /// 获取随机技能
    /// </summary>
    private List<SkillData> GetRandomSkills(int count)
    {
        List<SkillData> availableSkills = new List<SkillData>(AllSkills);
        List<SkillData> result = new List<SkillData>();
        
        // 根据波次调整稀有度概率
        int waveNumber = GameManager.Instance.CurrentWave;
        
        for (int i = 0; i < count && availableSkills.Count > 0; i++)
        {
            // 根据波次增加高级技能出现概率
            SkillRarity targetRarity = GetRarityByWave(waveNumber);
            
            // 尝试找到对应稀有度的技能
            List<SkillData> raritySkills = availableSkills.FindAll(s => s.Rarity == targetRarity);
            
            if (raritySkills.Count > 0)
            {
                SkillData selected = raritySkills[random.Next(raritySkills.Count)];
                result.Add(selected);
                availableSkills.Remove(selected);
            }
            else
            {
                // 如果没有对应稀有度的技能，随机选择
                int index = random.Next(availableSkills.Count);
                result.Add(availableSkills[index]);
                availableSkills.RemoveAt(index);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 根据波次获取目标稀有度
    /// </summary>
    private SkillRarity GetRarityByWave(int wave)
    {
        float roll = Random.value;
        
        if (wave < 5)
        {
            // 前期以普通技能为主
            if (roll < 0.7f) return SkillRarity.Common;
            if (roll < 0.95f) return SkillRarity.Rare;
            return SkillRarity.Epic;
        }
        else if (wave < 15)
        {
            // 中期
            if (roll < 0.4f) return SkillRarity.Common;
            if (roll < 0.8f) return SkillRarity.Rare;
            return SkillRarity.Epic;
        }
        else
        {
            // 后期
            if (roll < 0.2f) return SkillRarity.Common;
            if (roll < 0.6f) return SkillRarity.Rare;
            return SkillRarity.Epic;
        }
    }
    
    /// <summary>
    /// 选择技能
    /// </summary>
    public void SelectSkill(SkillData skill)
    {
        acquiredSkills.Add(skill);
        
        // 应用技能效果到防御塔
        Tower tower = FindObjectOfType<Tower>();
        if (tower != null)
        {
            tower.ApplySkill(skill);
        }
        
        Debug.Log($"获得技能: {skill.SkillName}");
        
        // 恢复游戏
        GameManager.Instance.ResumeGame();
    }
    
    /// <summary>
    /// 获取已获得的技能
    /// </summary>
    public List<SkillData> GetAcquiredSkills()
    {
        return new List<SkillData>(acquiredSkills);
    }
    
    /// <summary>
    /// 重置技能
    /// </summary>
    public void ResetSkills()
    {
        acquiredSkills.Clear();
    }
}

/// <summary>
/// 技能数据
/// </summary>
[System.Serializable]
public class SkillData
{
    public string SkillId;
    public string SkillName;
    public string Description;
    public SkillType SkillType;
    public float Value;
    public Sprite Icon;
    public SkillRarity Rarity;
}

/// <summary>
/// 技能类型
/// </summary>
public enum SkillType
{
    DamageUp,       // 攻击力提升
    RangeUp,        // 射程提升
    FireRateUp,     // 射速提升
    Pierce,         // 穿透
    Splash,         // 溅射
    CritRateUp,     // 暴击率提升
    GoldBonus,      // 金币加成
    MultiShot,      // 多重射击
    SlowEffect      // 减速效果
}

/// <summary>
/// 技能稀有度
/// </summary>
public enum SkillRarity
{
    Common,     // 普通（白色）
    Rare,       // 稀有（蓝色）
    Epic,       // 史诗（紫色）
    Legendary   // 传说（橙色）
}
