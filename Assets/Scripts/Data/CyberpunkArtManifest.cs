using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 赛博朋克美术资源清单 - TASK-023~025
/// 定义场景、防御塔、敌人、UI所需的美术资源规格
/// 实际美术资源由美术团队根据此清单制作
/// </summary>
[CreateAssetMenu(fileName = "CyberpunkArtManifest", menuName = "Game/Cyberpunk Art Manifest")]
public class CyberpunkArtManifest : ScriptableObject
{
    [Header("=== 场景美术资源 (TASK-023) ===")]
    public SceneArtSpec[] SceneArts;

    [Header("=== 防御塔美术资源 (TASK-024) ===")]
    public TowerArtSpec[] TowerArts;

    [Header("=== 敌人美术资源 (TASK-025) ===")]
    public EnemyArtSpec[] EnemyArts;

    [Header("=== UI 美术资源 ===")]
    public UIArtSpec[] UIArts;

    /// <summary>
    /// 获取资源总数统计
    /// </summary>
    public ArtStatistics GetStatistics()
    {
        ArtStatistics stats = new ArtStatistics();
        stats.SceneCount = SceneArts?.Length ?? 0;
        stats.TowerCount = TowerArts?.Length ?? 0;
        stats.EnemyCount = EnemyArts?.Length ?? 0;
        stats.UICount = UIArts?.Length ?? 0;

        int totalVariants = 0;
        if (SceneArts != null)
            foreach (var s in SceneArts) totalVariants += s.Variants?.Length ?? 1;
        if (TowerArts != null)
            foreach (var t in TowerArts) totalVariants += t.Variants?.Length ?? 1;
        if (EnemyArts != null)
            foreach (var e in EnemyArts) totalVariants += e.Variants?.Length ?? 1;
        if (UIArts != null)
            foreach (var u in UIArts) totalVariants += u.Variants?.Length ?? 1;

        stats.TotalVariants = totalVariants;
        return stats;
    }
}

/// <summary>
/// 场景美术资源规格 - TASK-023
/// </summary>
[System.Serializable]
public class SceneArtSpec
{
    [Header("基本信息")]
    public string SceneName;                 // 场景名称
    public string AssetPath;                 // 资源路径
    [TextArea(2, 4)]
    public string Description;               // 描述

    [Header("技术规格")]
    public Vector2Int Resolution = new Vector2Int(1920, 1080);  // 分辨率
    public int LayerCount = 3;               // 视差层数

    [Header("赛博朋克元素")]
    public bool HasNeonSigns = true;         // 霓虹灯招牌
    public bool HasHolographicAds = true;    // 全息广告牌
    public bool HasRainEffect = true;        // 雨夜效果
    public bool HasFogEffect = true;         // 雾气效果
    public bool HasDataStreams = true;       // 数据流效果

    [Header("变体")]
    public SceneVariant[] Variants;          // 场景变体（日夜、天气等）

    [Header("进度")]
    public ArtProgressStatus Status = ArtProgressStatus.Pending;
    public string AssignedArtist;            // 负责美术
    public string EstimatedHours;            // 预计工时
    public string Notes;                     // 备注
}

/// <summary>
/// 防御塔美术资源规格 - TASK-024
/// </summary>
[System.Serializable]
public class TowerArtSpec
{
    [Header("基本信息")]
    public string TowerName;                 // 塔名称
    public string AssetPath;                 // 资源路径
    [TextArea(2, 4)]
    public string Description;

    [Header("视觉设计")]
    public TowerVisualStyle Style;           // 视觉风格

    [Header("动画需求")]
    public bool HasIdleAnimation = true;     // 待机动画
    public bool HasAttackAnimation = true;   // 攻击动画
    public bool HasUpgradeAnimation = true;  // 升级动画
    public bool HasDeployAnimation = true;   // 部署动画

    [Header("特效需求")]
    public bool HasLEDEffects = true;        // LED灯光
    public bool HasShieldEffect = true;      // 能量护盾
    public bool HasMuzzleFlash = true;       // 枪口闪光

    [Header("变体 (等级)")]
    public TowerLevelVariant[] Variants;     // 各等级变体

    [Header("进度")]
    public ArtProgressStatus Status = ArtProgressStatus.Pending;
    public string AssignedArtist;
    public string EstimatedHours;
    public string Notes;
}

/// <summary>
/// 敌人美术资源规格 - TASK-025
/// </summary>
[System.Serializable]
public class EnemyArtSpec
{
    [Header("基本信息")]
    public string EnemyName;
    public string AssetPath;
    [TextArea(2, 4)]
    public string Description;
    public EnemyArtCategory Category;        // 敌人类别

    [Header("动画需求")]
    public bool HasWalkAnimation = true;
    public bool HasAttackAnimation = true;
    public bool HasDeathAnimation = true;
    public bool HasSpecialAnimation;         // 特殊技能动画

    [Header("特效需求")]
    public bool HasDamageFlash = true;       // 受击闪烁
    public bool HasSpawnEffect = true;       // 生成特效
    public bool HasDeathEffect = true;       // 死亡特效

    [Header("变体")]
    public EnemyVariant[] Variants;

    [Header("进度")]
    public ArtProgressStatus Status = ArtProgressStatus.Pending;
    public string AssignedArtist;
    public string EstimatedHours;
    public string Notes;
}

/// <summary>
/// UI美术资源规格
/// </summary>
[System.Serializable]
public class UIArtSpec
{
    public string UIName;
    public string AssetPath;
    [TextArea(2, 4)]
    public string Description;

    [Header("规格")]
    public Vector2Int Resolution = new Vector2Int(1920, 1080);
    public bool NeedsAnimation;

    [Header("变体")]
    public UIVariant[] Variants;

    [Header("进度")]
    public ArtProgressStatus Status = ArtProgressStatus.Pending;
    public string AssignedArtist;
    public string EstimatedHours;
    public string Notes;
}

#region === 子结构 ===

/// <summary>
/// 场景变体
/// </summary>
[System.Serializable]
public class SceneVariant
{
    public string VariantName;
    public string Description;
}

/// <summary>
/// 防御塔视觉风格
/// </summary>
[System.Serializable]
public class TowerVisualStyle
{
    public string StyleName;
    [TextArea(1, 3)]
    public string MechanicalDescription;     // 机械结构描述
    public Color PrimaryGlowColor = Color.cyan;    // 主发光色
    public Color SecondaryGlowColor = Color.magenta; // 次发光色
    public bool HasTransformingParts;         // 是否有变形部件
}

/// <summary>
/// 防御塔等级变体
/// </summary>
[System.Serializable]
public class TowerLevelVariant
{
    public int Level;                        // 等级 1-5
    public string VisualChanges;             // 视觉变化描述
    public Vector2Int Size = new Vector2Int(128, 128);  // 像素尺寸
}

/// <summary>
/// 敌人美术分类
/// </summary>
public enum EnemyArtCategory
{
    MechanicalZombie,    // 机械僵尸
    HackerProgram,       // 黑客程序
    CorporateSecurity,   // 企业安保
    CyberPsycho,         // 赛博精神病 (BOSS)
    Drone                // 无人机
}

/// <summary>
/// 敌人变体
/// </summary>
[System.Serializable]
public class EnemyVariant
{
    public string VariantName;
    public string Description;
    public Vector2Int Size = new Vector2Int(128, 128);
}

/// <summary>
/// UI变体
/// </summary>
[System.Serializable]
public class UIVariant
{
    public string VariantName;
    public string Description;
}

/// <summary>
/// 美术资源进度状态
/// </summary>
public enum ArtProgressStatus
{
    Pending,        // 待开始
    Concept,        // 概念设计
    InProgress,     // 制作中
    Review,         // 审核中
    Completed,      // 已完成
    Integrated      // 已集成
}

/// <summary>
/// 美术统计
/// </summary>
[System.Serializable]
public class ArtStatistics
{
    public int SceneCount;
    public int TowerCount;
    public int EnemyCount;
    public int UICount;
    public int TotalVariants;

    public int TotalAssets => SceneCount + TowerCount + EnemyCount + UICount;
}

#endregion
