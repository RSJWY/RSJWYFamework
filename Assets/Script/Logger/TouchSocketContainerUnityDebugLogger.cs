using System;
using System.Text;
using TouchSocket.Core;
using UnityEngine;

/// <summary>
/// Unity 日志插件
/// <remarks>注入日志容器</remarks>
/// </summary>
public class TouchSocketContainerUnityDebugLogger : LoggerBase
{
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
    protected override void WriteLog(LogLevel logLevel, object source, string message, Exception exception)
    {
        lock (typeof(ConsoleLogger))
        {
            var logString = new StringBuilder();
            logString.Append(DateTime.Now.ToString(this.DateTimeFormat));
            logString.Append(" | ");

            logString.Append(logLevel.ToString());
            logString.Append(" | ");
            logString.Append(message);

            if (exception != null)
            {
                logString.Append(" | ");
                logString.Append($"[Exception Message]：{exception.Message}");
                logString.Append($"[Stack Trace]：{exception.StackTrace}");
            }

            switch (logLevel)
            {
                case LogLevel.Warning:
                    Debug.LogWarning(logString.ToString());
                    break;

                case LogLevel.Error:
                    if (exception != null)
                    {
                        throw exception;
                        //Debug.LogError(exception);
                    }
                    else
                    {
                        Debug.LogError(logString.ToString());
                    }

                    break;

                case LogLevel.Info:
                default:
                    Debug.Log(logString.ToString());
                    break;
            }
        }
    }
}