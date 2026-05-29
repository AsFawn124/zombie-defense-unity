/**
 * GameStateMachine - 游戏状态机
 * 管理整个游戏的状态流转：Splash → Login → MainMenu → Battle → Result
 */
using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStateMachine : MonoBehaviour
{
    public static GameStateMachine Instance { get; private set; }

    // ==================== 状态定义 ====================
    public enum GameState
    {
        None,
        Splash,         // 启动画面
        Login,          // 登录/初始化
        MainMenu,       // 主菜单大厅
        Loading,        // 加载中（过渡状态）
        Battle,         // 战斗中
        BattlePaused,   // 战斗暂停
        Result,         // 结算界面
        Shop,           // 商店（独立界面）
        RoguelikeDraft, // 肉鸽选牌
        Cutscene,       // 过场动画
        Disconnected    // 断线重连
    }

    // ==================== 状态基类 ====================
    public abstract class StateBase
    {
        public abstract GameState StateType { get; }
        public abstract void OnEnter(object context = null);
        public abstract void OnExit();
        public virtual void OnUpdate() { }
        public virtual void OnGUI() { }

        protected void TransitionTo(GameState newState, object context = null)
        {
            Instance.GoToState(newState, context);
        }
    }

    // ==================== 数据 ====================
    private Dictionary<GameState, StateBase> states = new();
    private StateBase currentState;
    private GameState currentStateType = GameState.None;
    private GameState previousStateType = GameState.None;

    // 状态历史（用于返回）
    private Stack<GameState> stateHistory = new();

    // 回调
    public event Action<GameState, GameState> OnStateChanged; // (old, new)

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void Update()
    {
        currentState?.OnUpdate();
    }

    void OnGUI()
    {
        currentState?.OnGUI();
    }

    // ==================== 注册状态 ====================

    public void RegisterState(StateBase state)
    {
        states[state.StateType] = state;
    }

    public void RegisterStates(params StateBase[] newStates)
    {
        foreach (var s in newStates) RegisterState(s);
    }

    // ==================== 状态切换 ====================

    public void GoToState(GameState newState, object context = null)
    {
        if (!states.ContainsKey(newState))
        {
            Debug.LogError($"[StateMachine] 未注册状态: {newState}");
            return;
        }

        if (currentStateType == newState)
        {
            Debug.LogWarning($"[StateMachine] 已在 {newState}, 忽略重复切换");
            return;
        }

        // 记录历史
        if (currentState != null)
        {
            stateHistory.Push(currentStateType);
            if (stateHistory.Count > 20) // 限制历史深度
            {
                var temp = new GameState[stateHistory.Count];
                stateHistory.CopyTo(temp, 0);
                stateHistory.Clear();
                for (int i = 0; i < Math.Min(20, temp.Length); i++)
                    stateHistory.Push(temp[temp.Length - 1 - i]);
            }

            currentState.OnExit();
        }

        // 切换
        previousStateType = currentStateType;
        currentStateType = newState;
        currentState = states[newState];

        Debug.Log($"[StateMachine] {previousStateType} → {newState}");
        OnStateChanged?.Invoke(previousStateType, newState);
        EventBus.Instance?.Emit("StateChanged", newState);

        currentState.OnEnter(context);
    }

    /// <summary>
    /// 返回上一个状态
    /// </summary>
    public bool GoBack(object context = null)
    {
        if (stateHistory.Count == 0) return false;

        var prev = stateHistory.Pop();
        // 跳过一个状态（防止回到 loading/过渡态 卡死）
        while (stateHistory.Count > 0 && IsTransientState(prev))
            prev = stateHistory.Pop();

        GoToState(prev, context);
        return true;
    }

    private bool IsTransientState(GameState state)
    {
        return state == GameState.Loading || state == GameState.None;
    }

    // ==================== 便捷方法 ====================

    public void GoToMainMenu() => GoToState(GameState.MainMenu);
    public void GoToBattle(object battleConfig = null) => GoToState(GameState.Battle, battleConfig);
    public void GoToResult(object resultData = null) => GoToState(GameState.Result, resultData);
    public void GoToShop() => GoToState(GameState.Shop);
    public void GoToRoguelikeDraft() => GoToState(GameState.RoguelikeDraft);
    public void PauseBattle() => GoToState(GameState.BattlePaused);
    public void ResumeBattle() => GoToState(GameState.Battle);

    /// <summary>
    /// 通过加载状态过渡（用于需要异步加载的场景切换）
    /// </summary>
    public void TransitionThroughLoading(GameState target, string loadingMessage = "加载中...")
    {
        // Register a one-shot handler for Loading state
        EventBus.Instance?.Once("LoadingComplete", () => GoToState(target));
        GoToState(GameState.Loading, loadingMessage);
    }

    // ==================== Getter ====================
    public GameState CurrentState => currentStateType;
    public GameState PreviousState => previousStateType;
    public bool IsInState(GameState state) => currentStateType == state;
    public bool IsInBattle() => currentStateType == GameState.Battle || currentStateType == GameState.BattlePaused;
    public int StateHistoryCount => stateHistory.Count;
    public void ClearHistory() => stateHistory.Clear();
}
