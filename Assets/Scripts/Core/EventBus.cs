/**
 * EventBus - 全局事件总线
 * 实现模块间解耦通信，支持优先级、一次性监听、调试日志
 */
using System;
using System.Collections.Generic;
using UnityEngine;

public class EventBus : MonoBehaviour
{
    public static EventBus Instance { get; private set; }

    private Dictionary<string, List<EventHandler>> listeners = new();
    private Dictionary<string, Queue<Action>> pendingEvents = new(); // 延迟事件（避免循环触发）
    private bool isDispatching = false;

    private class EventHandler
    {
        public Action<object> callback;
        public int priority;   // 优先级(越大越先执行)
        public bool once;      // 一次性监听
    }

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void LateUpdate()
    {
        // 处理延迟事件
        if (pendingEvents.Count > 0 && !isDispatching)
        {
            var snapshot = new Dictionary<string, Queue<Action>>(pendingEvents);
            pendingEvents.Clear();
            foreach (var kv in snapshot)
            {
                while (kv.Value.Count > 0)
                    kv.Value.Dequeue()?.Invoke();
            }
        }
    }

    // ==================== 注册 ====================

    public void On(string eventName, Action<object> callback, int priority = 0, bool once = false)
    {
        if (!listeners.ContainsKey(eventName))
            listeners[eventName] = new List<EventHandler>();

        listeners[eventName].Add(new EventHandler
        {
            callback = callback,
            priority = priority,
            once = once
        });

        // 保持按优先级排序
        listeners[eventName].Sort((a, b) => b.priority.CompareTo(a.priority));
    }

    public void Once(string eventName, Action<object> callback, int priority = 0)
    {
        On(eventName, callback, priority, once: true);
    }

    // 便捷无参数版本
    public void On(string eventName, Action callback, int priority = 0)
    {
        On(eventName, _ => callback(), priority);
    }

    public void Once(string eventName, Action callback, int priority = 0)
    {
        On(eventName, _ => callback(), priority, once: true);
    }

    // ==================== 触发 ====================

    public void Emit(string eventName, object data = null)
    {
        if (!listeners.ContainsKey(eventName)) return;

        if (isDispatching)
        {
            // 正在分发中，加入延迟队列
            if (!pendingEvents.ContainsKey(eventName))
                pendingEvents[eventName] = new Queue<Action>();
            var snapshot = new List<EventHandler>(listeners[eventName]);
            pendingEvents[eventName].Enqueue(() => DispatchToHandlers(eventName, snapshot, data));
            return;
        }

        var handlers = new List<EventHandler>(listeners[eventName]);
        DispatchToHandlers(eventName, handlers, data);
    }

    /// <summary>
    /// 下一帧触发（避免循环依赖）
    /// </summary>
    public void EmitNextFrame(string eventName, object data = null)
    {
        if (!pendingEvents.ContainsKey(eventName))
            pendingEvents[eventName] = new Queue<Action>();
        pendingEvents[eventName].Enqueue(() => Emit(eventName, data));
    }

    private void DispatchToHandlers(string eventName, List<EventHandler> handlers, object data)
    {
        isDispatching = true;
        var toRemove = new List<EventHandler>();

        foreach (var handler in handlers)
        {
            try
            {
                handler.callback?.Invoke(data);
                if (handler.once) toRemove.Add(handler);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventBus] Error in '{eventName}': {e.Message}\n{e.StackTrace}");
            }
        }

        // 清理一次性监听
        if (toRemove.Count > 0 && listeners.ContainsKey(eventName))
        {
            foreach (var h in toRemove)
                listeners[eventName].Remove(h);
        }

        isDispatching = false;
    }

    // ==================== 注销 ====================

    public void Off(string eventName, Action<object> callback = null)
    {
        if (!listeners.ContainsKey(eventName)) return;

        if (callback == null)
            listeners.Remove(eventName);
        else
            listeners[eventName].RemoveAll(h => h.callback == callback);
    }

    // ==================== 调试 ====================

    public void DebugDump()
    {
        Debug.Log("=== EventBus Listeners ===");
        foreach (var kv in listeners)
            Debug.Log($"  [{kv.Key}] → {kv.Value.Count} handlers");
    }

    public bool HasListener(string eventName) =>
        listeners.ContainsKey(eventName) && listeners[eventName].Count > 0;
}
