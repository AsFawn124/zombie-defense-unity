using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 程序化美术生成器 - 不依赖外部资源的高级占位美术
/// 生成赛博朋克风格的防御塔、敌人、UI元素
/// 包含: Mesh生成、Shader效果、粒子系统
/// </summary>
[CreateAssetMenu(fileName = "ProceduralArtConfig", menuName = "ZombieDefense/Procedural Art Config")]
public class ProceduralArtConfig : ScriptableObject
{
    public CyberpunkPalette Palette;
    public List<TowerArtSpec> TowerSpecs = new List<TowerArtSpec>();
    public List<EnemyArtSpec> EnemySpecs = new List<EnemyArtSpec>();
    public List<UISpec> UISpecs = new List<UISpec>();
    public List<VFXSpec> VFXSpecs = new List<VFXSpec>();
    public List<EnvironmentSpec> EnvironmentSpecs = new List<EnvironmentSpec>();
}

#region === 赛博朋克调色板 ===

[System.Serializable]
public class CyberpunkPalette
{
    // 主色调
    public Color DeepPurple = new Color(0.15f, 0.05f, 0.25f);    // #260840 深紫背景
    public Color ElectricBlue = new Color(0.0f, 0.85f, 1.0f);    // #00D9FF 电蓝
    public Color NeonPink = new Color(1.0f, 0.2f, 0.6f);         // #FF3399 霓虹粉
    public Color ToxicGreen = new Color(0.3f, 1.0f, 0.3f);       // #4DFF4D 毒绿

    // 辅助色
    public Color DarkSteel = new Color(0.12f, 0.12f, 0.15f);     // #1F1F26 暗钢
    public Color Silver = new Color(0.75f, 0.78f, 0.82f);        // #BFC7D1 银色
    public Color NeonOrange = new Color(1.0f, 0.45f, 0.0f);      // #FF7300 霓虹橙
    public Color BloodRed = new Color(0.9f, 0.1f, 0.15f);        // #E61A26 血红

    // 功能色
    public Color HealthGreen = new Color(0.2f, 1.0f, 0.3f);
    public Color ShieldBlue = new Color(0.3f, 0.7f, 1.0f);
    public Color DamageRed = new Color(1.0f, 0.15f, 0.1f);
    public Color GoldYellow = new Color(1.0f, 0.85f, 0.2f);
}

#endregion

#region === 防御塔美术规格 ===

[System.Serializable]
public class TowerArtSpec
{
    public string TowerId;
    public string TowerName;
    public TowerElement Element;
    public int Tier; // 1-3

    // 外观描述
    public string ShapeDescription;    // "六边形底座+旋转炮管+能量核心"
    public string MaterialDescription; // "暗钢底座/霓虹灯条/全息瞄准"
    public string AnimationDescription; // "炮管旋转/能量脉冲/射击回弹"

    // 尺寸
    public float BaseRadius = 0.5f;
    public float Height = 2f;

    // 颜色方案
    public Color PrimaryColor;
    public Color GlowColor;
    public Color AccentColor;

    // 特效需求
    public string MuzzleFlashType;      // 枪口火焰类型
    public string ProjectileTrailType;  // 弹道拖尾
    public string HitEffectType;        // 命中特效
    public string DeathEffectType;      // 摧毁特效

    // 音效需求
    public string ShootSFX;
    public string UpgradeSFX;
    public string DeploySFX;

    // 外包参考
    public string ReferenceImagePath;   // 参考图路径
    public string ArtStationKeywords;   // "cyberpunk turret mechanical glowing neon"

    // 预算
    public int EstimatedPolygons = 500;  // 目标面数(移动端优化)
    public int TextureSize = 512;        // 贴图尺寸
}

#endregion

#region === 敌人美术规格 ===

[System.Serializable]
public class EnemyArtSpec
{
    public string EnemyId;
    public string EnemyName;
    public EnemyArchetype Archetype;

    // 视觉描述
    public string VisualConcept;       // "半机械僵尸/裸露电路/发红光的义眼"
    public string Size;                // "Small/Medium/Large/Boss"
    public Color SkinColor;
    public Color GlowColor;
    public Color BloodColor;

    // 动画需求
    public string WalkStyle;           // "蹒跚/狂奔/爬行/漂浮"
    public string AttackAnimation;
    public string DeathAnimation;      // "爆炸/溶解/短路/倒地"
    public string SpecialAnimation;    // 特殊能力动画

    // 特效
    public string SpawnEffect;
    public string AbilityEffect;
    public string DeathEffect;

    // 音效
    public string IdleSFX;
    public string AttackSFX;
    public string DeathSFX;

    // 外包
    public string ReferenceImagePath;
    public string ArtStationKeywords;
    public int EstimatedPolygons = 300;
    public int TextureSize = 256;
}

#endregion

#region === UI美术规格 ===

[System.Serializable]
public class UISpec
{
    public string UIElementId;
    public string ElementName;
    public UIElementType Type;

    // 设计描述
    public string DesignDescription;
    public string AnimationDescription;

    // 尺寸规格 (适配微信小游戏 750×1334)
    public int Width;
    public int Height;
    public Vector2 AnchorPosition; // 锚点位置

    // 字体规格
    public string FontFamily = "Orbitron"; // 赛博朋克风格字体
    public int FontSize = 24;
    public Color TextColor;
    public bool UseGlowEffect;

    // 交互反馈
    public string PressEffect;     // "缩放0.95/颜色加深/边框闪烁"
    public string HoverEffect;     // "发光/放大/提示"
    public string DisabledStyle;   // "灰度/降低透明度"

    // 外包参考
    public string ReferenceUIPath;
    public string DribbbleKeywords;
}

public enum UIElementType
{
    Button, Panel, Slider, Toggle, ProgressBar,
    Icon, Card, Badge, Tooltip, Dialog,
    TabBar, NavigationBar, SkillCard, ItemSlot,
    CurrencyDisplay, Timer, HealthBar, EnergyBar
}

#endregion

#region === 视觉特效规格 ===

[System.Serializable]
public class VFXSpec
{
    public string VFXId;
    public string VFXName;
    public VFXType Type;

    // 粒子系统参数
    public int MaxParticles = 50;
    public float Duration = 1f;
    public float StartSize = 0.5f;
    public float EndSize = 1.5f;
    public Color StartColor;
    public Color EndColor;
    public float EmissionRate = 20f;

    // 运动
    public Vector3 Gravity = new Vector3(0, -2f, 0);
    public float Speed = 5f;
    public float Spread = 30f;

    // 渲染
    public string ShaderName = "Particles/Additive";
    public string MaterialName;

    // 音频同步
    public string SyncSFX;
    public float SFXDelay;
}

public enum VFXType
{
    Explosion, MuzzleFlash, Projectile, Hit,
    Buff, Debuff, Heal, Shield,
    Teleport, Electricity, Fire, Ice,
    Smoke, Sparks, Glitch, Scanline
}

#endregion

#region === 场景美术规格 ===

[System.Serializable]
public class EnvironmentSpec
{
    public string EnvId;
    public string EnvName;
    public EnvironmentTheme Theme;

    // 图层
    public string BackgroundLayer;     // 远景: 赛博都市天际线
    public string MidgroundLayer;      // 中景: 废墟/高架路/霓虹广告牌
    public string ForegroundLayer;     // 近景: 防御工事/路障

    // 动态元素
    public bool HasRain;
    public bool HasFog;
    public bool HasNeonFlicker;
    public bool HasHologramAds;
    public bool HasFlyingCars;

    // 光照
    public Color AmbientLight = new Color(0.15f, 0.08f, 0.2f);
    public Color DirectionalLight = new Color(0.5f, 0.5f, 0.8f);
    public float LightIntensity = 0.6f;

    // 后期处理
    public float BloomIntensity = 0.5f;
    public float ChromaticAberration = 0.1f;
    public float VignetteIntensity = 0.4f;
    public float ScanlinesIntensity = 0.05f;
    public Color ColorGrading = new Color(1.2f, 1.0f, 1.3f);

    // 外包参考
    public string ReferenceImagePath;
    public string ArtStationKeywords;
    public string MoodBoardPath;
}

public enum EnvironmentTheme
{
    NeonCity,       // 霓虹都市
    IndustrialZone, // 工业区
    Underground,    // 地下通道
    Rooftop,        // 天台
    Laboratory,     // 实验室
    Wasteland       // 废土
}

#endregion
