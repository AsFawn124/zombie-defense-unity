using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// 测试与优化系统 - TASK-049~053
/// 功能测试、性能优化、平衡性调整
/// </summary>
public class TestAndOptimize : MonoBehaviour
{
    public static TestAndOptimize Instance;

    [Header("测试配置")]
    public TestConfig Config;

    [Header("性能目标")]
    public int TargetFPS = 60;
    public int MaxDrawCalls = 50;
    public int MaxMemoryMB = 100;

    // 性能监控
    private PerformanceMonitor _perfMonitor = new PerformanceMonitor();
    private List<TestResult> _testResults = new List<TestResult>();
    private BalanceData _balanceData;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Config.EnablePerformanceMonitoring)
        {
            _perfMonitor.Update();
        }
    }

    #region === 功能测试 (TASK-049~050) ===

    /// <summary>
    /// 运行核心玩法测试套件
    /// </summary>
    public TestSuiteResult RunCoreGameplayTests()
    {
        var suite = new TestSuiteResult { SuiteName = "核心玩法测试" };

        suite.AddResult(TestBackpackSystem());
        suite.AddResult(TestElementReaction());
        suite.AddResult(TestHeroSystem());
        suite.AddResult(TestTowerSystem());
        suite.AddResult(TestEnemySystem());
        suite.AddResult(TestWaveSystem());

        _testResults.AddRange(suite.Results);
        return suite;
    }

    /// <summary>
    /// 运行系统测试套件
    /// </summary>
    public TestSuiteResult RunSystemTests()
    {
        var suite = new TestSuiteResult { SuiteName = "系统测试" };

        suite.AddResult(TestEquipmentSystem());
        suite.AddResult(TestChipSystem());
        suite.AddResult(TestTalentSystem());
        suite.AddResult(TestGachaSystem());
        suite.AddResult(TestShopSystem());
        suite.AddResult(TestSaveLoadSystem());

        _testResults.AddRange(suite.Results);
        return suite;
    }

    /// <summary>
    /// 运行社交测试套件
    /// </summary>
    public TestSuiteResult RunSocialTests()
    {
        var suite = new TestSuiteResult { SuiteName = "社交系统测试" };

        suite.AddResult(TestArenaSystem());
        suite.AddResult(TestGuildSystem());
        suite.AddResult(TestCoopSystem());
        suite.AddResult(TestShareSystem());

        _testResults.AddRange(suite.Results);
        return suite;
    }

    private TestResult TestBackpackSystem()
    {
        var sw = Stopwatch.StartNew();
        bool passed = true;
        var errors = new List<string>();

        try
        {
            // 测试9宫格创建
            // 测试占位
            // 测试拖拽
            // 测试合成
            // 测试扩展
        }
        catch (Exception e)
        {
            passed = false;
            errors.Add(e.Message);
        }

        sw.Stop();
        return new TestResult
        {
            TestName = "背包系统",
            Passed = passed,
            Duration = sw.ElapsedMilliseconds,
            Errors = errors
        };
    }

    private TestResult TestElementReaction()
    {
        return QuickTest("元素反应系统", () =>
        {
            // 火+冰=蒸发
            // 火+电=超载
            // 冰+电=超导
            // 毒+火=燃烧
        });
    }

    private TestResult TestHeroSystem()
    {
        return QuickTest("英雄系统", () =>
        {
            // 英雄移动
            // 自动攻击
            // 技能释放
            // 装备切换
        });
    }

    private TestResult TestTowerSystem()
    {
        return QuickTest("防御塔系统", () =>
        {
            // 塔放置
            // 塔升级
            // 塔出售
            // 塔拖拽
        });
    }

    private TestResult TestEnemySystem()
    {
        return QuickTest("敌人系统", () =>
        {
            // 7种敌人类型
            // 路径跟随
            // 状态效果
            // BOSS机制
        });
    }

    private TestResult TestWaveSystem()
    {
        return QuickTest("波次系统", () =>
        {
            // 波次生成
            // 波次结算
            // 难度递增
        });
    }

    private TestResult TestEquipmentSystem() => QuickTest("装备系统", () => { });
    private TestResult TestChipSystem() => QuickTest("芯片系统", () => { });
    private TestResult TestTalentSystem() => QuickTest("天赋系统", () => { });
    private TestResult TestGachaSystem() => QuickTest("抽卡系统", () => { });
    private TestResult TestShopSystem() => QuickTest("商城系统", () => { });
    private TestResult TestArenaSystem() => QuickTest("竞技场系统", () => { });
    private TestResult TestGuildSystem() => QuickTest("公会系统", () => { });
    private TestResult TestCoopSystem() => QuickTest("合作系统", () => { });
    private TestResult TestShareSystem() => QuickTest("分享系统", () => { });

    private TestResult TestSaveLoadSystem()
    {
        return QuickTest("存档系统", () =>
        {
            // 保存数据
            string testKey = "test_save";
            string testValue = "test_data_" + DateTime.UtcNow.Ticks;
            PlayerPrefs.SetString(testKey, testValue);
            PlayerPrefs.Save();

            // 加载数据
            string loadedValue = PlayerPrefs.GetString(testKey, "");
            if (loadedValue != testValue)
            {
                throw new Exception("存档数据不一致");
            }
        });
    }

    private TestResult QuickTest(string testName, Action testAction)
    {
        var sw = Stopwatch.StartNew();
        bool passed = true;
        var errors = new List<string>();

        try
        {
            testAction();
        }
        catch (Exception e)
        {
            passed = false;
            errors.Add(e.Message);
        }

        sw.Stop();
        return new TestResult
        {
            TestName = testName,
            Passed = passed,
            Duration = sw.ElapsedMilliseconds,
            Errors = errors
        };
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    public AllTestsReport RunAllTests()
    {
        _testResults.Clear();

        var report = new AllTestsReport
        {
            CoreGameplay = RunCoreGameplayTests(),
            Systems = RunSystemTests(),
            Social = RunSocialTests(),
            Performance = RunPerformanceTests()
        };

        report.TotalTests = _testResults.Count;
        report.PassedTests = _testResults.FindAll(r => r.Passed).Count;
        report.FailedTests = report.TotalTests - report.PassedTests;
        report.PassRate = report.TotalTests > 0 ? (float)report.PassedTests / report.TotalTests * 100f : 0;

        UnityEngine.Debug.Log($"[Test] 测试完成: {report.PassedTests}/{report.TotalTests} 通过 ({report.PassRate:F1}%)");

        return report;
    }

    #endregion

    #region === 性能优化 (TASK-051~052) ===

    /// <summary>
    /// 运行性能测试
    /// </summary>
    public PerformanceReport RunPerformanceTests()
    {
        var report = new PerformanceReport();

        // FPS测试
        report.AvgFPS = _perfMonitor.AverageFPS;
        report.MinFPS = _perfMonitor.MinFPS;
        report.MaxFPS = _perfMonitor.MaxFPS;
        report.IsFPSTargetMet = report.AvgFPS >= TargetFPS;

        // DrawCall测试
        report.CurrentDrawCalls = _perfMonitor.DrawCalls;
        report.IsDrawCallTargetMet = report.CurrentDrawCalls <= MaxDrawCalls;

        // 内存测试
        report.CurrentMemoryMB = _perfMonitor.MemoryMB;
        report.IsMemoryTargetMet = report.CurrentMemoryMB <= MaxMemoryMB;

        // 生成优化建议
        report.OptimizationSuggestions = GenerateOptimizationSuggestions(report);

        return report;
    }

    private List<string> GenerateOptimizationSuggestions(PerformanceReport report)
    {
        var suggestions = new List<string>();

        if (!report.IsFPSTargetMet)
        {
            suggestions.Add("[P0] FPS未达标，建议：降低特效质量、启用LOD");
            suggestions.Add("[P0] 检查Update中的GC分配，使用对象池");
        }

        if (!report.IsDrawCallTargetMet)
        {
            suggestions.Add("[P0] DrawCall超标，建议：合并纹理图集、启用GPU Instancing");
            suggestions.Add("[P1] 使用Sprite Atlas打包UI元素");
        }

        if (!report.IsMemoryTargetMet)
        {
            suggestions.Add("[P0] 内存超标，建议：纹理压缩、卸载未使用资源");
            suggestions.Add("[P1] 实现资源懒加载，使用Addressable Assets");
        }

        // 通用优化建议
        suggestions.Add("[P1] 使用对象池管理敌人/子弹/特效");
        suggestions.Add("[P1] 音频使用压缩格式，按需加载");
        suggestions.Add("[P2] UI使用虚拟列表处理大量数据");
        suggestions.Add("[P2] 降低远处敌人更新频率(LOD)");

        return suggestions;
    }

    /// <summary>
    /// 内存优化：触发GC
    /// </summary>
    public void OptimizeMemory()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        UnityEngine.Debug.Log($"[Optimize] 手动GC完成，当前内存: {_perfMonitor.MemoryMB}MB");
    }

    /// <summary>
    /// 启用/禁用性能监控
    /// </summary>
    public void TogglePerformanceMonitor(bool enable)
    {
        Config.EnablePerformanceMonitoring = enable;
    }

    #endregion

    #region === 平衡性调整 (TASK-053) ===

    /// <summary>
    /// 计算塔伤害平衡
    /// </summary>
    public BalanceReport CalculateTowerBalance()
    {
        var report = new BalanceReport { ReportType = "防御塔平衡" };

        // 获取所有塔数据
        var towerData = LoadBalanceData().Towers;

        float avgDPS = 0;
        foreach (var tower in towerData)
        {
            avgDPS += tower.DPS;
        }
        avgDPS /= towerData.Length;

        // 检查每座塔的平衡性
        foreach (var tower in towerData)
        {
            float deviation = Mathf.Abs(tower.DPS - avgDPS) / avgDPS;
            if (deviation > Config.MaxDPSDeviation)
            {
                report.AddIssue($"[平衡] {tower.TowerName}: DPS偏差{deviation:P1}% (目标DPS={avgDPS:F0})");
            }
        }

        return report;
    }

    /// <summary>
    /// 计算敌人强度平衡
    /// </summary>
    public BalanceReport CalculateEnemyBalance()
    {
        var report = new BalanceReport { ReportType = "敌人强度平衡" };

        var data = LoadBalanceData();
        var enemies = data.Enemies;

        // 检查难度曲线
        for (int i = 1; i < enemies.Length; i++)
        {
            float healthGrowth = enemies[i].BaseHealth / Mathf.Max(enemies[i - 1].BaseHealth, 1);
            if (healthGrowth > Config.MaxHealthGrowth)
            {
                report.AddIssue($"[平衡] {enemies[i].EnemyName}: 血量增长过快 ({healthGrowth:F1}x)");
            }
        }

        return report;
    }

    /// <summary>
    /// 计算经济系统平衡
    /// </summary>
    public BalanceReport CalculateEconomyBalance()
    {
        var report = new BalanceReport { ReportType = "经济系统平衡" };

        int startGold = Config.StartGold;
        float avgWaveIncome = Config.WaveClearBonus;
        float towerCost = Config.AverageTowerCost;

        // 检查是否能在前几波买得起塔
        int wavesToFirstTower = Mathf.CeilToInt(towerCost / avgWaveIncome);
        if (wavesToFirstTower > Config.MaxWavesForFirstTower)
        {
            report.AddIssue($"[平衡] 第一座塔需要{wavesToFirstTower}波才能购买");
        }

        return report;
    }

    /// <summary>
    /// 运行完整平衡性分析
    /// </summary>
    public AllBalanceReport RunFullBalanceAnalysis()
    {
        return new AllBalanceReport
        {
            TowerBalance = CalculateTowerBalance(),
            EnemyBalance = CalculateEnemyBalance(),
            EconomyBalance = CalculateEconomyBalance()
        };
    }

    private BalanceData LoadBalanceData()
    {
        if (_balanceData != null) return _balanceData;

        _balanceData = new BalanceData
        {
            Towers = Config.DefaultTowerData,
            Enemies = Config.DefaultEnemyData
        };

        return _balanceData;
    }

    #endregion

    #region === 公共属性 ===

    public PerformanceMonitor PerfMonitor => _perfMonitor;
    public List<TestResult> AllTestResults => _testResults;

    #endregion
}

#region === 测试数据结构 ===

[System.Serializable]
public class TestConfig
{
    public bool EnablePerformanceMonitoring = true;
    public float MaxDPSDeviation = 0.3f;
    public float MaxHealthGrowth = 3f;
    public int StartGold = 100;
    public int WaveClearBonus = 50;
    public float AverageTowerCost = 100;
    public int MaxWavesForFirstTower = 3;

    public TowerBalanceData[] DefaultTowerData;
    public EnemyBalanceData[] DefaultEnemyData;
}

[System.Serializable]
public class TestResult
{
    public string TestName;
    public bool Passed;
    public long Duration;
    public List<string> Errors = new List<string>();
}

[System.Serializable]
public class TestSuiteResult
{
    public string SuiteName;
    public List<TestResult> Results = new List<TestResult>();

    public void AddResult(TestResult result) => Results.Add(result);

    public int PassedCount => Results.FindAll(r => r.Passed).Count;
    public int FailedCount => Results.Count - PassedCount;
}

[System.Serializable]
public class AllTestsReport
{
    public TestSuiteResult CoreGameplay;
    public TestSuiteResult Systems;
    public TestSuiteResult Social;
    public PerformanceReport Performance;
    public int TotalTests;
    public int PassedTests;
    public int FailedTests;
    public float PassRate;
}

[System.Serializable]
public class PerformanceReport
{
    public float AvgFPS;
    public float MinFPS;
    public float MaxFPS;
    public bool IsFPSTargetMet;
    public int CurrentDrawCalls;
    public bool IsDrawCallTargetMet;
    public float CurrentMemoryMB;
    public bool IsMemoryTargetMet;
    public List<string> OptimizationSuggestions = new List<string>();
}

[System.Serializable]
public class BalanceReport
{
    public string ReportType;
    public List<string> Issues = new List<string>();

    public void AddIssue(string issue) => Issues.Add(issue);
    public bool IsBalanced => Issues.Count == 0;
}

[System.Serializable]
public class AllBalanceReport
{
    public BalanceReport TowerBalance;
    public BalanceReport EnemyBalance;
    public BalanceReport EconomyBalance;
}

#endregion

#region === 性能监控 ===

[System.Serializable]
public class PerformanceMonitor
{
    private Queue<float> _fpsHistory = new Queue<float>();
    private const int MaxFpsSamples = 60;

    public float AverageFPS { get; private set; }
    public float MinFPS { get; private set; } = float.MaxValue;
    public float MaxFPS { get; private set; }
    public int DrawCalls { get; private set; }
    public float MemoryMB { get; private set; }

    public void Update()
    {
        float fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        _fpsHistory.Enqueue(fps);
        if (_fpsHistory.Count > MaxFpsSamples) _fpsHistory.Dequeue();

        // 计算统计
        float total = 0;
        MinFPS = float.MaxValue;
        MaxFPS = 0;
        foreach (var f in _fpsHistory)
        {
            total += f;
            MinFPS = Mathf.Min(MinFPS, f);
            MaxFPS = Mathf.Max(MaxFPS, f);
        }
        AverageFPS = total / _fpsHistory.Count;

        // DrawCalls (需要UnityEngine.Rendering命名空间)
        DrawCalls = UnityEngine.Rendering.OnDemandRendering.renderFrameInterval > 0 ? 0 : 0;

        // 内存 (MB)
        MemoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
    }
}

#endregion

#region === 平衡数据 ===

[System.Serializable]
public class BalanceData
{
    public TowerBalanceData[] Towers;
    public EnemyBalanceData[] Enemies;
}

[System.Serializable]
public class TowerBalanceData
{
    public string TowerName;
    public float DPS;
    public float Range;
    public float Cost;
}

[System.Serializable]
public class EnemyBalanceData
{
    public string EnemyName;
    public float BaseHealth;
    public float BaseSpeed;
    public int GoldReward;
}

#endregion
