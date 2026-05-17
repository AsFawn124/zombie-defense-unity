using UnityEngine;

/// <summary>
/// 赛博朋克主题数据 - TASK-022: 色彩方案制定
/// 定义赛博朋克风格的主色调、辅助色、强调色等色彩方案
/// </summary>
[CreateAssetMenu(fileName = "CyberpunkThemeData", menuName = "Game/Cyberpunk Theme Data")]
public class CyberpunkThemeData : ScriptableObject
{
    [Header("=== 主色调 (Primary Colors) ===")]
    [Tooltip("深紫色 - 主要用于背景和暗部区域")]
    public Color DeepPurple = new Color(0.12f, 0.05f, 0.25f);       // #1F0D40

    [Tooltip("电蓝色 - 主要用于UI边框和高亮")]
    public Color ElectricBlue = new Color(0.0f, 0.85f, 1.0f);        // #00D9FF

    [Tooltip("霓虹粉色 - 主要用于按钮悬停和强调元素")]
    public Color NeonPink = new Color(1.0f, 0.2f, 0.6f);            // #FF3399

    [Header("=== 辅助色 (Secondary Colors) ===")]
    [Tooltip("纯黑色 - 背景最深色")]
    public Color PureBlack = new Color(0.02f, 0.02f, 0.05f);        // #05050D

    [Tooltip("金属银色 - 边框、分隔线")]
    public Color MetallicSilver = new Color(0.7f, 0.75f, 0.8f);     // #B3BFCC

    [Tooltip("荧光绿色 - 数据流、终端文字")]
    public Color NeonGreen = new Color(0.2f, 1.0f, 0.3f);           // #33FF4D

    [Header("=== 强调色 (Accent Colors) ===")]
    [Tooltip("警告橙色 - 危险提示、低血量")]
    public Color WarningOrange = new Color(1.0f, 0.5f, 0.0f);       // #FF8000

    [Tooltip("危险红色 - 致命伤害、紧急状态")]
    public Color DangerRed = new Color(1.0f, 0.1f, 0.15f);          // #FF1A26

    [Tooltip("金黄色 - 金币、稀有度传说")]
    public Color GoldYellow = new Color(1.0f, 0.85f, 0.0f);         // #FFD900

    [Header("=== UI 元素色 (UI Element Colors) ===")]
    [Tooltip("按钮默认颜色")]
    public Color ButtonDefault = new Color(0.15f, 0.1f, 0.3f, 0.9f);

    [Tooltip("按钮悬停颜色")]
    public Color ButtonHover = new Color(0.2f, 0.15f, 0.4f, 1.0f);

    [Tooltip("按钮按下颜色")]
    public Color ButtonPressed = new Color(0.1f, 0.05f, 0.2f, 1.0f);

    [Tooltip("面板背景颜色")]
    public Color PanelBackground = new Color(0.08f, 0.05f, 0.15f, 0.95f);

    [Tooltip("文字主颜色")]
    public Color TextPrimary = new Color(0.9f, 0.95f, 1.0f);

    [Tooltip("文字次要颜色")]
    public Color TextSecondary = new Color(0.5f, 0.6f, 0.7f);

    [Tooltip("伤害数字颜色")]
    public Color DamageNormal = Color.white;
    [Tooltip("暴击伤害颜色")]
    public Color DamageCrit = new Color(1f, 0.85f, 0.0f);
    [Tooltip("元素伤害颜色 - 火")]
    public Color DamageFire = new Color(1f, 0.4f, 0.1f);
    [Tooltip("元素伤害颜色 - 冰")]
    public Color DamageIce = new Color(0.2f, 0.7f, 1f);
    [Tooltip("元素伤害颜色 - 电")]
    public Color DamageLightning = new Color(0.8f, 0.2f, 1f);
    [Tooltip("元素伤害颜色 - 毒")]
    public Color DamagePoison = new Color(0.3f, 0.9f, 0.2f);

    [Header("=== 霓虹发光参数 (Neon Glow Settings) ===")]
    [Tooltip("霓虹发光强度")]
    [Range(0f, 2f)]
    public float NeonGlowIntensity = 1.0f;

    [Tooltip("霓虹发光范围")]
    [Range(0f, 10f)]
    public float NeonGlowRange = 3.0f;

    [Tooltip("霓虹脉冲速度")]
    [Range(0.5f, 5f)]
    public float NeonPulseSpeed = 1.5f;

    [Header("=== 扫描线效果 (Scanline Settings) ===")]
    [Tooltip("是否启用扫描线效果")]
    public bool EnableScanlines = true;

    [Tooltip("扫描线间距（像素）")]
    [Range(1f, 8f)]
    public float ScanlineSpacing = 2f;

    [Tooltip("扫描线透明度")]
    [Range(0f, 0.3f)]
    public float ScanlineOpacity = 0.08f;

    [Header("=== 数据流效果 (Data Stream Settings) ===")]
    [Tooltip("是否启用数据流动画")]
    public bool EnableDataStream = true;

    [Tooltip("数据流字符集")]
    public string DataStreamChars = "01アイウエオカキクケコサシスセソタチツテトナニヌネノ";

    [Tooltip("数据流速度")]
    [Range(10f, 100f)]
    public float DataStreamSpeed = 30f;

    [Header("=== 故障效果 (Glitch Settings) ===")]
    [Tooltip("是否启用故障效果")]
    public bool EnableGlitch = true;

    [Tooltip("故障效果触发概率")]
    [Range(0f, 0.1f)]
    public float GlitchProbability = 0.02f;

    [Tooltip("故障效果持续时间")]
    [Range(0.05f, 0.5f)]
    public float GlitchDuration = 0.1f;

    [Header("=== 转场效果 (Transition Settings) ===")]
    [Tooltip("转场持续时间")]
    [Range(0.2f, 2f)]
    public float TransitionDuration = 0.5f;

    [Tooltip("数据流穿梭转场粒子数量")]
    [Range(10, 200)]
    public int TransitionParticleCount = 50;

    // ===== 辅助方法 =====

    /// <summary>
    /// 根据稀有度获取颜色
    /// </summary>
    public Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:     return MetallicSilver;
            case ItemRarity.Uncommon:   return NeonGreen;
            case ItemRarity.Rare:       return ElectricBlue;
            case ItemRarity.Epic:       return NeonPink;
            case ItemRarity.Legendary:  return WarningOrange;
            case ItemRarity.Mythic:     return DangerRed;
            case ItemRarity.Chromatic:  return new Color(1f, 0.5f, 1f, 1f); // 彩虹色
            default:                    return TextSecondary;
        }
    }

    /// <summary>
    /// 获取对应元素伤害的颜色
    /// </summary>
    public Color GetElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire:      return DamageFire;
            case ElementType.Ice:       return DamageIce;
            case ElementType.Lightning: return DamageLightning;
            case ElementType.Poison:    return DamagePoison;
            case ElementType.Wind:      return NeonGreen;
            default:                    return DamageNormal;
        }
    }

    /// <summary>
    /// 带脉冲动画的霓虹颜色
    /// </summary>
    public Color GetPulsingNeonColor(Color baseColor, float time = -1f)
    {
        if (time < 0) time = Time.time;
        float pulse = Mathf.Sin(time * NeonPulseSpeed) * 0.3f + 0.7f;
        return baseColor * (1f + pulse * NeonGlowIntensity * 0.2f);
    }
}

/// <summary>
/// 物品稀有度枚举
/// </summary>
public enum ItemRarity
{
    Common,     // 白
    Uncommon,   // 绿
    Rare,       // 蓝
    Epic,       // 紫
    Legendary,  // 橙
    Mythic,     // 红
    Chromatic   // 彩
}

/// <summary>
/// 元素类型枚举（如果尚未在其他文件中定义则使用此定义）
/// </summary>
public enum ElementType
{
    None = 0,
    Fire = 1,
    Ice = 2,
    Lightning = 3,
    Poison = 4,
    Wind = 5
}
