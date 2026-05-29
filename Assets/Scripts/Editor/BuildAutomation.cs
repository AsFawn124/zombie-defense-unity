using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 构建自动化工具 - 一键打包WebGL/微信小游戏
/// 使用方法: Unity菜单 Tools → Build → [平台]
/// </summary>
public class BuildAutomation : MonoBehaviour
{
#if UNITY_EDITOR

    [MenuItem("Tools/Build/WebGL Release", false, 100)]
    public static void BuildWebGLRelease()
    {
        BuildWebGL("release");
    }

    [MenuItem("Tools/Build/WebGL Development", false, 101)]
    public static void BuildWebGLDevelopment()
    {
        BuildWebGL("development");
    }

    [MenuItem("Tools/Build/WeChat MiniGame", false, 110)]
    public static void BuildWeChatMiniGame()
    {
        BuildWeChat();
    }

    [MenuItem("Tools/Build/All Platforms", false, 120)]
    public static void BuildAll()
    {
        BuildWebGLRelease();
        BuildWeChat();
    }

    private static void BuildWebGL(string buildType)
    {
        Debug.Log($"[Build] 开始构建 WebGL ({buildType})...");

        // 1. 构建前检查
        if (!PreBuildChecks()) return;

        // 2. 设置PlayerSettings
        PlayerSettings.WebGL.memorySize = 512;    // 微信小游戏限制512MB
        PlayerSettings.WebGL.exceptionSupport = buildType == "development"
            ? WebGLExceptionSupport.FullWithStacktrace
            : WebGLExceptionSupport.ExplicitlyThrownOnly;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.dataCaching = true;

        // 3. 设置输出路径
        string buildPath = $"Builds/WebGL_{buildType}_{PlayerSettings.bundleVersion}";

        // 4. 收集场景
        string[] scenes = new[] {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/GameScene.unity"
        };

        // 5. 执行构建
        var report = BuildPipeline.BuildPlayer(scenes, buildPath,
            BuildTarget.WebGL,
            buildType == "development" ? BuildOptions.Development : BuildOptions.None);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Build] WebGL构建成功! 路径: {buildPath}");

            // 6. 后处理: 优化index.html
            PostProcessWebGLBuild(buildPath);

            // 7. 生成构建报告
            GenerateBuildReport(buildPath, "WebGL", report.summary.totalSize);
        }
        else
        {
            Debug.LogError($"[Build] WebGL构建失败! 错误: {report.summary}");
        }
    }

    private static void BuildWeChat()
    {
        Debug.Log("[Build] 开始构建微信小游戏...");

        if (!PreBuildChecks()) return;

        // WeChat MiniGame settings
        PlayerSettings.WebGL.memorySize = 256; // 微信限制更严格
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.template = "APPLICATION:WeChatMiniGame"; // 自定义模板

        string buildPath = $"Builds/WeChat_{PlayerSettings.bundleVersion}";

        string[] scenes = new[] {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/GameScene.unity"
        };

        var report = BuildPipeline.BuildPlayer(scenes, buildPath,
            BuildTarget.WebGL, BuildOptions.None);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Build] 微信小游戏构建成功! 路径: {buildPath}");
            PostProcessWeChatBuild(buildPath);
        }
    }

    #region === 构建前检查 ===

    private static bool PreBuildChecks()
    {
        bool allPassed = true;

        // 检查场景
        if (!File.Exists("Assets/Scenes/MainMenu.unity"))
        {
            Debug.LogError("[Build] 缺少 MainMenu.unity 场景!");
            allPassed = false;
        }
        if (!File.Exists("Assets/Scenes/GameScene.unity"))
        {
            Debug.LogError("[Build] 缺少 GameScene.unity 场景!");
            allPassed = false;
        }

        // 检查Bundle Version
        if (string.IsNullOrEmpty(PlayerSettings.bundleVersion))
        {
            PlayerSettings.bundleVersion = "1.0.0";
        }

        // 检查资源
        CheckMissingAssets();

        // 检查编译错误
        if (EditorApplication.isCompiling)
        {
            Debug.LogError("[Build] 脚本正在编译中，请等待编译完成!");
            allPassed = false;
        }

        return allPassed;
    }

    private static void CheckMissingAssets()
    {
        var missingSprites = new[] {
            "Assets/Resources/Sprites/Tower_Base.png",
            "Assets/Resources/Sprites/Enemy_Normal.png",
            "Assets/Resources/Sprites/Base.png",
            "Assets/Resources/Sprites/Bullet.png",
        };

        foreach (var path in missingSprites)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Build] 缺少资源: {path} (将使用占位资源)");
            }
        }
    }

    #endregion

    #region === 构建后处理 ===

    private static void PostProcessWebGLBuild(string buildPath)
    {
        // 修改index.html - 添加微信适配
        string indexPath = Path.Combine(buildPath, "index.html");
        if (File.Exists(indexPath))
        {
            string html = File.ReadAllText(indexPath);

            // 添加meta viewport
            html = html.Replace("<head>", @"<head>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"">
    <meta name=""apple-mobile-web-app-capable"" content=""yes"">
    <meta name=""full-screen"" content=""yes"">");

            // 添加微信JS-SDK
            html = html.Replace("</body>", @"<script src=""https://res.wx.qq.com/open/js/jweixin-1.6.0.js""></script>
</body>");

            File.WriteAllText(indexPath, html);
        }

        // 压缩检查
        var buildData = Path.Combine(buildPath, "Build");
        if (Directory.Exists(buildData))
        {
            long totalSize = GetDirectorySize(buildData);
            Debug.Log($"[Build] 构建包大小: {totalSize / (1024 * 1024)}MB");

            if (totalSize > 20 * 1024 * 1024) // >20MB
            {
                Debug.LogWarning("[Build] ⚠️ 构建包超过20MB! 微信小游戏首包限制20MB，建议: "
                    + "\n  - 启用Brotli压缩"
                    + "\n  - 减小纹理分辨率"
                    + "\n  - 使用AssetBundle分包加载");
            }
        }
    }

    private static void PostProcessWeChatBuild(string buildPath)
    {
        PostProcessWebGLBuild(buildPath);

        // 生成 game.json
        string gameJson = @"{
    ""deviceOrientation"": ""portrait"",
    ""showStatusBar"": false,
    ""networkTimeout"": {
        ""request"": 10000,
        ""connectSocket"": 10000,
        ""uploadFile"": 10000,
        ""downloadFile"": 10000
    },
    ""workers"": """",
    ""subpackages"": [],
    ""plugins"": {}
}";
        File.WriteAllText(Path.Combine(buildPath, "game.json"), gameJson);

        // 生成 project.config.json
        string projectConfig = @"{
    ""description"": ""僵尸防线 - 赛博朋克塔防游戏"",
    ""packOptions"": {
        ""ignore"": []
    },
    ""setting"": {
        ""urlCheck"": true,
        ""es6"": true,
        ""enhance"": true,
        ""postcss"": true,
        ""minified"": true
    },
    ""compileType"": ""miniprogram"",
    ""appid"": ""your_appid_here"",
    ""projectname"": ""zombie-defense""
}";
        File.WriteAllText(Path.Combine(buildPath, "project.config.json"), projectConfig);
    }

    private static long GetDirectorySize(string path)
    {
        long size = 0;
        foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            size += new FileInfo(file).Length;
        return size;
    }

    private static void GenerateBuildReport(string buildPath, string platform, ulong totalSize)
    {
        string report = $@"=== 构建报告 ===
平台: {platform}
版本: {PlayerSettings.bundleVersion}
构建时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}
包体积: {totalSize / (1024 * 1024)}MB
输出路径: {buildPath}

场景:
  - MainMenu.unity
  - GameScene.unity

PlayerSettings:
  - 内存限制: {PlayerSettings.WebGL.memorySize}MB
  - 压缩: {PlayerSettings.WebGL.compressionFormat}
  - 异常处理: {PlayerSettings.WebGL.exceptionSupport}
";
        string reportPath = Path.Combine(buildPath, "BUILD_REPORT.txt");
        File.WriteAllText(reportPath, report);
        Debug.Log($"[Build] 构建报告已生成: {reportPath}");
    }

    #endregion

    #region === 自动化版本号 ===

    [MenuItem("Tools/Build/Bump Version", false, 90)]
    public static void BumpVersion()
    {
        string[] parts = PlayerSettings.bundleVersion.Split('.');
        int patch = int.Parse(parts[2]) + 1;
        PlayerSettings.bundleVersion = $"{parts[0]}.{parts[1]}.{patch}";
        Debug.Log($"[Build] 版本号更新: {PlayerSettings.bundleVersion}");
    }

    #endregion

#endif
}
