using UnityEngine;

/// <summary>
/// 游戏平衡数据 - ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "GameBalanceData", menuName = "Game/Balance Data")]
public class GameBalanceData : ScriptableObject
{
    [Header("难度曲线")]
    public AnimationCurve HealthCurve = AnimationCurve.EaseInOut(0, 1, 50, 10);
    public AnimationCurve SpeedCurve = AnimationCurve.EaseInOut(0, 1, 50, 2);
    public AnimationCurve CountCurve = AnimationCurve.EaseInOut(0, 5, 50, 50);
    
    [Header("经济")]
    public int StartGold = 100;
    public int WaveClearBonus = 50;
    public float InterestRate = 0.1f;
    
    [Header("防御塔")]
    public TowerBalanceData[] TowerData;
    
    [Header("敌人")]
    public EnemyBalanceData[] EnemyData;
    
    [Header("技能")]
    public SkillBalanceData[] SkillData;
}

[System.Serializable]
public class TowerBalanceData
{
    public string TowerName;
    public float BaseDamage;
    public float BaseRange;
    public float BaseFireRate;
    public int BaseCost;
    public int UpgradeCost;
    public float DamagePerLevel;
    public float RangePerLevel;
    public float FireRatePerLevel;
}

[System.Serializable]
public class EnemyBalanceData
{
    public string EnemyName;
    public float BaseHealth;
    public float BaseSpeed;
    public int GoldReward;
    public int ScoreReward;
    public float HealthGrowth;
    public float SpeedGrowth;
}

[System.Serializable]
public class SkillBalanceData
{
    public string SkillName;
    public SkillType Type;
    public float BaseValue;
    public float ValuePerLevel;
    public int MaxLevel;
}
