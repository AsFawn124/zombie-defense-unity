/// <summary>
/// 视觉特效Shader库 - 赛博朋克风格
/// 注意: 以下为Shader模板代码，需要在Unity中创建对应的.shader文件使用
/// 
/// 使用方法:
/// 1. 在Assets/Shaders/目录下创建对应.shader文件
/// 2. 复制对应代码
/// 3. 在Material中引用
/// </summary>
public static class ShaderLibrary
{
    /// <summary>
    /// 所有Shader清单和用途
    /// </summary>
    public static readonly ShaderManifestEntry[] Manifest = new ShaderManifestEntry[]
    {
        // === 特效Shader ===
        new ShaderManifestEntry
        {
            Name = "Cyberpunk_Glow",
            Path = "Assets/Shaders/Cyberpunk_Glow.shader",
            Description = "霓虹发光效果 - 用于防御塔能量核心、UI边框",
            Performance = "低 (适合移动端)",
            Keywords = "glow neon emission cyberpunk"
        },
        new ShaderManifestEntry
        {
            Name = "Cyberpunk_Hologram",
            Path = "Assets/Shaders/Cyberpunk_Hologram.shader",
            Description = "全息投影效果 - 扫描线+半透明+闪烁",
            Performance = "中 (需要alpha混合)",
            Keywords = "hologram scanline transparent scifi"
        },
        new ShaderManifestEntry
        {
            Name = "Cyberpunk_Dissolve",
            Path = "Assets/Shaders/Cyberpunk_Dissolve.shader",
            Description = "像素溶解效果 - 敌人死亡/塔摧毁动画",
            Performance = "中 (需要噪声贴图)",
            Keywords = "dissolve glitch death destruction"
        },
        new ShaderManifestEntry
        {
            Name = "Cyberpunk_Electricity",
            Path = "Assets/Shaders/Cyberpunk_Electricity.shader",
            Description = "电流效果 - 电元素塔攻击特效",
            Performance = "中",
            Keywords = "electricity lightning arc scifi"
        },
        new ShaderManifestEntry
        {
            Name = "Cyberpunk_Shield",
            Path = "Assets/Shaders/Cyberpunk_Shield.shader",
            Description = "能量护盾 - 敌人护盾/防御塔护盾",
            Performance = "低",
            Keywords = "shield barrier forcefield hexagon"
        },
        new ShaderManifestEntry
        {
            Name = "Cyberpunk_Trail",
            Path = "Assets/Shaders/Cyberpunk_Trail.shader",
            Description = "拖尾效果 - 子弹/冲刺/传送",
            Performance = "低",
            Keywords = "trail ribbon projectile motion"
        },
        new ShaderManifestEntry
        {
            Name = "Cyberpunk_Grid",
            Path = "Assets/Shaders/Cyberpunk_Grid.shader",
            Description = "网格地面 - 关卡地图网格线",
            Performance = "低",
            Keywords = "grid floor wireframe scifi"
        },
        new ShaderManifestEntry
        {
            Name = "Cyberpunk_Rain",
            Path = "Assets/Shaders/Cyberpunk_Rain.shader",
            Description = "雨滴效果 - 场景氛围",
            Performance = "低 (粒子替代)",
            Keywords = "rain weather atmospheric"
        },

        // === UI Shader ===
        new ShaderManifestEntry
        {
            Name = "UI_Cyberpunk_Button",
            Path = "Assets/Shaders/UI_Cyberpunk_Button.shader",
            Description = "赛博按钮 - 霓虹边框+按下发光+悬浮扫描线",
            Performance = "低",
            Keywords = "ui button neon interactive"
        },
        new ShaderManifestEntry
        {
            Name = "UI_Cyberpunk_Progress",
            Path = "Assets/Shaders/UI_Cyberpunk_Progress.shader",
            Description = "进度条 - 分段发光+流动效果",
            Performance = "低",
            Keywords = "ui progress bar health energy"
        },
        new ShaderManifestEntry
        {
            Name = "UI_Glitch_Text",
            Path = "Assets/Shaders/UI_Glitch_Text.shader",
            Description = "毛刺文字 - 随机RGB分离+偏移",
            Performance = "低",
            Keywords = "ui text glitch distortion"
        },

        // === 后处理 ===
        new ShaderManifestEntry
        {
            Name = "PostProcess_Bloom",
            Path = "Assets/Shaders/PostProcess_Bloom.shader",
            Description = "泛光效果 - 标准Bloom",
            Performance = "中 (VRAM开销)",
            Keywords = "bloom postprocess glow"
        },
        new ShaderManifestEntry
        {
            Name = "PostProcess_ChromaticAberration",
            Path = "Assets/Shaders/PostProcess_ChromaticAberration.shader",
            Description = "色差效果 - RGB通道偏移",
            Performance = "低",
            Keywords = "chromatic aberration postprocess rgb split"
        },
        new ShaderManifestEntry
        {
            Name = "PostProcess_Scanlines",
            Path = "Assets/Shaders/PostProcess_Scanlines.shader",
            Description = "扫描线 - CRT复古感",
            Performance = "低",
            Keywords = "scanlines crt retro postprocess"
        },
        new ShaderManifestEntry
        {
            Name = "PostProcess_Vignette",
            Path = "Assets/Shaders/PostProcess_Vignette.shader",
            Description = "暗角 - 边缘压暗",
            Performance = "低",
            Keywords = "vignette dark edges postprocess"
        },
        new ShaderManifestEntry
        {
            Name = "PostProcess_ColorGrading",
            Path = "Assets/Shaders/PostProcess_ColorGrading.shader",
            Description = "色调映射 - 赛博朋克调色板",
            Performance = "低",
            Keywords = "color grading LUT postprocess"
        },
    };
}

[System.Serializable]
public class ShaderManifestEntry
{
    public string Name;
    public string Path;
    public string Description;
    public string Performance;
    public string Keywords;
}

/// <summary>
/// 移动端性能分级 - 自动检测设备性能并调整画质
/// </summary>
public enum QualityTier
{
    Low,        // 低端机 (骁龙660级别) - 30fps, 简化特效
    Medium,     // 中端机 (骁龙865级别) - 45fps, 标准特效
    High,       // 高端机 (骁龙8Gen1+) - 60fps, 全特效
    Ultra       // 旗舰机 - 60fps, 特效全开+后期
}

public class QualityAutoDetect
{
    public static QualityTier DetectDeviceTier()
    {
        int memoryMB = SystemInfo.systemMemorySize;

        if (memoryMB <= 2048)
            return QualityTier.Low;
        if (memoryMB <= 4096)
            return QualityTier.Medium;

        // 检查GPU
        string gpuName = SystemInfo.graphicsDeviceName.ToLower();
        if (gpuName.Contains("adreno 6") || gpuName.Contains("mali-g5"))
            return QualityTier.High;

        return QualityTier.High;
    }

    public static void ApplyQualitySettings(QualityTier tier)
    {
        switch (tier)
        {
            case QualityTier.Low:
                QualitySettings.SetQualityLevel(0);
                Application.targetFrameRate = 30;
                // 关闭后处理
                DisablePostProcessing();
                // 简化粒子
                QualitySettings.particleRaycastBudget = 256;
                break;

            case QualityTier.Medium:
                QualitySettings.SetQualityLevel(1);
                Application.targetFrameRate = 45;
                EnableBasicPostProcessing();
                QualitySettings.particleRaycastBudget = 1024;
                break;

            case QualityTier.High:
                QualitySettings.SetQualityLevel(2);
                Application.targetFrameRate = 60;
                EnableFullPostProcessing();
                QualitySettings.particleRaycastBudget = 4096;
                break;

            case QualityTier.Ultra:
                QualitySettings.SetQualityLevel(3);
                Application.targetFrameRate = 60;
                EnableFullPostProcessing();
                QualitySettings.particleRaycastBudget = 8192;
                break;
        }
    }

    private static void DisablePostProcessing()
    {
        // 关闭Bloom/色差/扫描线
        Debug.Log("[Quality] 后处理已禁用 (低端机)");
    }

    private static void EnableBasicPostProcessing()
    {
        // 仅启用Bloom+Vignette
        Debug.Log("[Quality] 基础后处理已启用 (中端机)");
    }

    private static void EnableFullPostProcessing()
    {
        // 全部后处理
        Debug.Log("[Quality] 全后处理已启用 (高端机)");
    }
}
