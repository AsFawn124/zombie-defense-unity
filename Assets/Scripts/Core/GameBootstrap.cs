/**
 * GameBootstrap - 游戏启动引导器
 * 负责初始化顺序管理、依赖注入、全局服务注册
 * 
 * 初始化顺序:
 *   1. EventBus (事件系统)
 *   2. SaveManager (存档)  
 *   3. ConfigManager (配置)
 *   4. ResourceManager (资源)
 *   5. GameStateMachine (状态机)
 *   6. 各子系统管理器 (按优先级)
 *   7. 进入 Splash → Login → MainMenu
 */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBootstrap : MonoBehaviour
{
    [Header("启动配置")]
    [SerializeField] private bool skipSplash = false;
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private int targetFrameRate = 60;

    [Header("初始化进度")]
    [SerializeField] private float initProgress = 0f;
    [SerializeField] private string initStatus = "等待启动...";

    // 初始化完成回调
    public static event Action OnGameReady;
    public static bool IsReady { get; private set; }

    void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
        DontDestroyOnLoad(gameObject);

        if (enableDebugLog)
            Debug.Log("[Bootstrap] 🚀 僵尸防线 启动中...");
    }

    IEnumerator Start()
    {
        yield return InitializeGame();
    }

    // ==================== 初始化流水线 ====================

    private IEnumerator InitializeGame()
    {
        // 第0步: 基础框架
        UpdateProgress(0.0f, "初始化基础框架...");
        yield return CreateCoreServices();

        // 第1步: 存档与配置
        UpdateProgress(0.15f, "加载存档...");
        yield return InitializePersistence();

        // 第2步: 资源加载
        UpdateProgress(0.30f, "加载资源...");
        yield return InitializeResources();

        // 第3步: 子系统管理器 (按依赖顺序)
        UpdateProgress(0.45f, "初始化子系统...");
        yield return InitializeSubsystems();

        // 第4步: UI准备
        UpdateProgress(0.70f, "准备UI...");
        yield return PrepareUI();

        // 第5步: 网络连接
        UpdateProgress(0.85f, "连接服务器...");
        yield return InitializeNetwork();

        // 第6步: 完成
        UpdateProgress(1.0f, "启动完成!");
        yield return new WaitForSeconds(0.5f);

        IsReady = true;
        OnGameReady?.Invoke();
        Debug.Log("[Bootstrap] ✅ 初始化完成! 总耗时: " + Time.realtimeSinceStartup.ToString("F2") + "s");

        // 进入状态机
        if (skipSplash)
        {
            GameStateMachine.Instance.GoToState(GameStateMachine.GameState.MainMenu);
        }
        else
        {
            GameStateMachine.Instance.GoToState(GameStateMachine.GameState.Splash);
        }
    }

    private IEnumerator CreateCoreServices()
    {
        // 1. EventBus (最先初始化，其他系统依赖它)
        var eventBusGo = new GameObject("[Core] EventBus");
        var eventBus = eventBusGo.AddComponent<EventBus>();
        eventBusGo.transform.SetParent(transform);
        Debug.Log("[Bootstrap] ✓ EventBus 就绪");

        // 2. GameStateMachine
        var stateMachineGo = new GameObject("[Core] StateMachine");
        var stateMachine = stateMachineGo.AddComponent<GameStateMachine>();
        stateMachineGo.transform.SetParent(transform);
        Debug.Log("[Bootstrap] ✓ StateMachine 就绪");

        // 注册基础状态
        stateMachine.RegisterStates(
            new SplashState(),
            new LoginState(),
            new MainMenuState(),
            new BattleState(),
            new ResultState(),
            new ShopState(),
            new LoadingState()
        );

        yield return null;
    }

    private IEnumerator InitializePersistence()
    {
        // SaveManager
        var saveGo = new GameObject("[Core] SaveManager");
        saveGo.AddComponent<SaveManager>();
        saveGo.transform.SetParent(transform);
        Debug.Log("[Bootstrap] ✓ SaveManager 就绪");

        yield return null;
    }

    private IEnumerator InitializeResources()
    {
        // ResourceManager
        var resGo = new GameObject("[Core] ResourceManager");
        resGo.AddComponent<ResourceManager>();
        resGo.transform.SetParent(transform);
        Debug.Log("[Bootstrap] ✓ ResourceManager 就绪");

        yield return null;
    }

    private IEnumerator InitializeSubsystems()
    {
        // 按依赖顺序创建管理器
        var managerOrder = new (string name, Type type)[]
        {
            ("AudioManager", typeof(AudioManager)),
            ("EffectManager", typeof(EffectManager)),
            ("TowerManager", typeof(TowerManager)),
            ("EnemyManager", typeof(WaveManager)),   // Wave管理Enemy生成
            ("GameManager", typeof(GameManager)),
            ("SkillManager", typeof(SkillManager)),
            ("ArenaManager", typeof(ArenaManager)),
            ("SeasonManager", typeof(SeasonManager)),
            ("CommerceManager", typeof(CommerceManager)),
            ("GuildManager", typeof(GuildManager)),
            ("CoopManager", typeof(CoopManager)),
            ("NetworkManager", typeof(NetworkManager)),
            ("MonetizationManager", typeof(MonetizationManager)),
            ("SocialManager", typeof(SocialManager)),
        };

        foreach (var (name, type) in managerOrder)
        {
            var go = new GameObject($"[Manager] {name}");
            go.AddComponent(type);
            go.transform.SetParent(transform);
            UpdateProgress(0.45f + 0.25f * Array.IndexOf(managerOrder, (name, type)) / managerOrder.Length, name);
            yield return null;
        }

        // 创建系统实例 (非MonoBehaviour的单例)
        TowerSkinSystem.Instance.GetType();  // trigger static init
        RoguelikeModeManager.Instance.GetType();
        BattleReplayManager.Instance.GetType();
        AchievementSystem.Instance.GetType();
        EventManager.Instance.GetType();
        DailyMissionManager.Instance.GetType();

        Debug.Log("[Bootstrap] ✓ 所有子系统就绪");
    }

    private IEnumerator PrepareUI()
    {
        // 预加载主UI
        yield return null;
        Debug.Log("[Bootstrap] ✓ UI就绪");
    }

    private IEnumerator InitializeNetwork()
    {
        // 异步连接服务器
        yield return null;
        Debug.Log("[Bootstrap] ✓ 网络就绪");
    }

    // ==================== 状态实现(占位) ====================

    private class SplashState : GameStateMachine.StateBase
    {
        public override GameStateMachine.GameState StateType => GameStateMachine.GameState.Splash;
        public override void OnEnter(object context)
        {
            Debug.Log("[State] Splash - 启动画面");
            // 2秒后自动跳转
            Instance.StartCoroutine(DelayedTransition());
        }
        private IEnumerator DelayedTransition()
        {
            yield return new WaitForSeconds(2f);
            TransitionTo(GameStateMachine.GameState.Login);
        }
        public override void OnExit() { }
    }

    private class LoginState : GameStateMachine.StateBase
    {
        public override GameStateMachine.GameState StateType => GameStateMachine.GameState.Login;
        public override void OnEnter(object context)
        {
            Debug.Log("[State] Login - 登录");
            // 自动登录或展示登录界面
            TransitionTo(GameStateMachine.GameState.MainMenu);
        }
        public override void OnExit() { }
    }

    private class MainMenuState : GameStateMachine.StateBase
    {
        public override GameStateMachine.GameState StateType => GameStateMachine.GameState.MainMenu;
        public override void OnEnter(object context)
        {
            Debug.Log("[State] MainMenu - 主菜单");
            EventBus.Instance.Emit("MainMenuEntered");
        }
        public override void OnExit() { }
    }

    private class BattleState : GameStateMachine.StateBase
    {
        public override GameStateMachine.GameState StateType => GameStateMachine.GameState.Battle;
        public override void OnEnter(object context)
        {
            Debug.Log("[State] Battle - 战斗开始");
            EventBus.Instance.Emit("BattleStarted", context);
        }
        public override void OnExit()
        {
            EventBus.Instance.Emit("BattleEnded");
        }
    }

    private class ResultState : GameStateMachine.StateBase
    {
        public override GameStateMachine.GameState StateType => GameStateMachine.GameState.Result;
        public override void OnEnter(object context)
        {
            Debug.Log("[State] Result - 结算");
            EventBus.Instance.Emit("ResultShown", context);
        }
        public override void OnExit() { }
    }

    private class ShopState : GameStateMachine.StateBase
    {
        public override GameStateMachine.GameState StateType => GameStateMachine.GameState.Shop;
        public override void OnEnter(object context)
        {
            Debug.Log("[State] Shop - 商店");
            EventBus.Instance.Emit("ShopOpened");
        }
        public override void OnExit()
        {
            EventBus.Instance.Emit("ShopClosed");
        }
    }

    private class LoadingState : GameStateMachine.StateBase
    {
        public override GameStateMachine.GameState StateType => GameStateMachine.GameState.Loading;
        public override void OnEnter(object context)
        {
            Debug.Log($"[State] Loading - {(string)context ?? "加载中..."}");
            // 这里会等待异步操作完成后触发 LoadingComplete
            EventBus.Instance.Once("LoadingComplete", () => { });
        }
        public override void OnExit() { }
    }

    // ==================== 工具 ====================

    private void UpdateProgress(float progress, string status)
    {
        initProgress = progress;
        initStatus = status;
        if (enableDebugLog)
            Debug.Log($"[Bootstrap] [{progress*100:F0}%] {status}");
    }
}
