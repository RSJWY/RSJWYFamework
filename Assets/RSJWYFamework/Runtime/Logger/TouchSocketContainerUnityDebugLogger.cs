using System;
using System.Text;
using TouchSocket.Core;
using UnityEngine;

/// <summary>
/// Unity 日志插件
/// <remarks>TouchSocket注入日志容器</remarks>
/// </summary>
public class TouchSocketContainerUnityDebugLogger : LoggerBase
{
    // 缓存 StringBuilder 以避免频繁 GC
    private readonly StringBuilder _cachedSb = new StringBuilder();

    static TouchSocketContainerUnityDebugLogger()
    {
        Default = new TouchSocketContainerUnityDebugLogger();
    }

    private TouchSocketContainerUnityDebugLogger()
    {
    }

    /// <summary>
    /// 默认的实例
    /// </summary>
    public static TouchSocketContainerUnityDebugLogger Default { get; }

    /// <inheritdoc/>
    /// <param name="logLevel"></param>
    /// <param name="source"></param>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    [HideInCallstack] // 关键：隐藏此方法在控制台的堆栈，确保双击日志能跳转到业务代码
    protected override void WriteLog(LogLevel logLevel, object source, string message, Exception exception)
    {
        // 锁定 StringBuilder 实例，确保线程安全（虽然 Debug.Log 只能在主线程安全调用，但 Logger 可能被多线程调用）
        lock (_cachedSb)
        {
            _cachedSb.Clear();
            _cachedSb.Append(DateTime.Now.ToString(this.DateTimeFormat));
            _cachedSb.Append(" | ");

            _cachedSb.Append(logLevel.ToString());
            _cachedSb.Append(" | ");
            _cachedSb.Append(message);

            if (exception != null)
            {
                _cachedSb.Append(" | ");
                _cachedSb.Append($"[Exception Message]：{exception.Message}");
                _cachedSb.Append($"[Stack Trace]：{exception.StackTrace}");
            }

            var finalMessage = _cachedSb.ToString();

            switch (logLevel)
            {
                case LogLevel.Warning:
                    Debug.LogWarning(finalMessage);
                    break;

                case LogLevel.Error:
                    if (exception != null)
                    {
                        // 抛出异常会中断流程，视需求而定，这里保留原逻辑但建议仅LogException
                        Debug.LogError(finalMessage);
                        Debug.LogException(exception); 
                    }
                    else
                    {
                        Debug.LogError(finalMessage);
                    }
                    break;

                case LogLevel.Info:
                default:
                    Debug.Log(finalMessage);
                    break;
            }
        }
    }
}
