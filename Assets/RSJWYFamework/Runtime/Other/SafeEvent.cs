using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 安全事件工具类 - 静态方法封装
/// <para>核心功能：异常隔离（单个订阅者报错不影响其他人）、日志追踪</para>
/// <para>注意：使用 GetInvocationList 会产生 GC Alloc，请避免在 Update 等高频逻辑中频繁调用。</para>
/// </summary>
public static class SafeEvent
{
    #region Action 封装 (无返回值)
    
    /// <summary>
    /// 安全调用 Action，单个异常不会中断后续监听者
    /// </summary>
    public static void Invoke(this Action action, Object context = null, bool logException = true)
    {
        if (action == null) return;
        
        var delegates = action.GetInvocationList();
        foreach (var d in delegates)
        {
            try
            {
                ((Action)d)();
            }
            catch (Exception e)
            {
                if (logException) LogException(d, e, context);
            }
        }
    }

    public static void Invoke<T>(this Action<T> action, T param, Object context = null, bool logException = true)
    {
        if (action == null) return;
        
        var delegates = action.GetInvocationList();
        foreach (var d in delegates)
        {
            try
            {
                ((Action<T>)d)(param);
            }
            catch (Exception e)
            {
                if (logException) LogException(d, e, context);
            }
        }
    }

    public static void Invoke<T1, T2>(this Action<T1, T2> action, T1 arg1, T2 arg2, Object context = null, bool logException = true)
    {
        if (action == null) return;
        
        var delegates = action.GetInvocationList();
        foreach (var d in delegates)
        {
            try
            {
                ((Action<T1, T2>)d)(arg1, arg2);
            }
            catch (Exception e)
            {
                if (logException) LogException(d, e, context);
            }
        }
    }
    
    #endregion

    #region Func 封装 (有返回值)
    
    /// <summary>
    /// 安全调用 Func，返回所有成功执行的结果列表（失败的被忽略）
    /// </summary>
    public static List<TResult> Invoke<TResult>(this Func<TResult> func, Object context = null, bool logException = true)
    {
        var results = new List<TResult>();
        if (func == null) return results;
        
        var delegates = func.GetInvocationList();
        foreach (var d in delegates)
        {
            try
            {
                var result = ((Func<TResult>)d)();
                results.Add(result);
            }
            catch (Exception e)
            {
                if (logException) LogException(d, e, context);
            }
        }
        return results;
    }

    public static List<TResult> Invoke<T, TResult>(this Func<T, TResult> func, T param, Object context = null, bool logException = true)
    {
        var results = new List<TResult>();
        if (func == null) return results;
        
        var delegates = func.GetInvocationList();
        foreach (var d in delegates)
        {
            try
            {
                var result = ((Func<T, TResult>)d)(param);
                results.Add(result);
            }
            catch (Exception e)
            {
                if (logException) LogException(d, e, context);
            }
        }
        return results;
    }
    
    #endregion

    #region 严格模式（遇到异常立即中断）
    
    /// <summary>
    /// 严格调用：遇到第一个异常立即停止并抛出（传统行为，用于关键路径）
    /// </summary>
    public static void InvokeStrict(this Action action, Object context = null)
    {
        try
        {
            action?.Invoke();
        }
        catch (Exception e)
        {
            LogException(null, e, context);
            throw; // 重新抛出，中断执行
        }
    }
    
    #endregion

    #region 工具方法
    
    private static void LogException(Delegate d, Exception e, Object context)
    {
        string targetName = d?.Target?.GetType().Name ?? "Static";
        string methodName = d?.Method.Name ?? "Unknown";
        string msg = $"[SafeEvent] 执行异常: {targetName}.{methodName} >> {e.Message}";
        
        if (context != null)
            Debug.LogError($"{msg}\nCtx: {context.name}", context);
        else
            Debug.LogError($"{msg}\n{e.StackTrace}");
    }
    
    #endregion
}

/// <summary>
/// 安全事件包装类 - 基类，提供锁对象
/// </summary>
public abstract class SafeActionBase
{
    protected readonly object _lock = new object();
}

/// <summary>
/// 安全事件包装类 - 替代直接使用 Action 字段
/// 提供属性封装，强制安全检查，线程安全订阅
/// </summary>
[System.Serializable]
public class SafeAction<T> : SafeActionBase
{
    private Action<T> _action;
    
    /// <summary>
    /// 订阅事件（线程安全）
    /// </summary>
    public event Action<T> OnInvoke
    {
        add { lock(_lock) { _action += value; } }
        remove { lock(_lock) { _action -= value; } }
    }

    /// <summary>
    /// 触发事件（安全调用，遍历容错）
    /// </summary>
    public void Invoke(T arg, Object context = null)
    {
        // 扩展方法 Invoke 内部会处理 null check
        _action.Invoke(arg, context);
    }

    /// <summary>
    /// 移除所有监听
    /// </summary>
    public void Clear() { lock(_lock) { _action = null; } }
    
    public bool HasListeners => _action != null;
}

/// <summary>
/// 无参版本
/// </summary>
[System.Serializable]
public class SafeAction : SafeActionBase
{
    private Action _action;
    
    public event Action OnInvoke
    {
        add { lock(_lock) { _action += value; } }
        remove { lock(_lock) { _action -= value; } }
    }

    public void Invoke(Object context = null) => _action.Invoke(context);
    public void Clear() { lock(_lock) { _action = null; } }
    public bool HasListeners => _action != null;
}

/// <summary>
/// 带返回值的安全事件 (无参)
/// </summary>
public class SafeFunc<TResult> : SafeActionBase
{
    private Func<TResult> _func;
    
    public event Func<TResult> OnInvoke
    {
        add { lock(_lock) { _func += value; } }
        remove { lock(_lock) { _func -= value; } }
    }

    /// <summary>
    /// 触发并收集所有成功返回值
    /// </summary>
    public List<TResult> Invoke(Object context = null) => _func.Invoke(context);

    /// <summary>
    /// 触发并只取第一个成功返回值（短路逻辑）
    /// </summary>
    public TResult InvokeFirst(TResult defaultValue = default, Object context = null)
    {
        if (_func == null) return defaultValue;
        
        foreach (var d in _func.GetInvocationList())
        {
            try
            {
                return ((Func<TResult>)d)();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SafeFunc] 执行失败 [{d.Method.Name}]: {e.Message}", context);
            }
        }
        return defaultValue;
    }
    
    public void Clear() { lock(_lock) { _func = null; } }
}

/// <summary>
/// 带返回值的安全事件 (带参)
/// </summary>
public class SafeFunc<T, TResult> : SafeActionBase
{
    private Func<T, TResult> _func;
    
    public event Func<T, TResult> OnInvoke
    {
        add { lock(_lock) { _func += value; } }
        remove { lock(_lock) { _func -= value; } }
    }

    /// <summary>
    /// 触发并收集所有成功返回值
    /// </summary>
    public List<TResult> Invoke(T arg, Object context = null) => _func.Invoke(arg, context);
    
    /// <summary>
    /// 触发并只取第一个成功返回值（短路逻辑）
    /// </summary>
    public TResult InvokeFirst(T arg, TResult defaultValue = default, Object context = null)
    {
        if (_func == null) return defaultValue;
        
        foreach (var d in _func.GetInvocationList())
        {
            try
            {
                return ((Func<T, TResult>)d)(arg);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SafeFunc] 执行失败 [{d.Method.Name}]: {e.Message}", context);
            }
        }
        return defaultValue;
    }
    
    public void Clear() { lock(_lock) { _func = null; } }
}
