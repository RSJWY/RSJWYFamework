using UnityEngine;

namespace RSJWYFamework.Runtiem.Logger
{
    public enum LogLevel
    {
        None,
        Exception,
        Error,
        Warning,
        Log,
    }
    public class AppLogger
    {
        /// <summary>
        /// 日志输出等级
        /// </summary>
        public static LogLevel currentLogLevel = LogLevel.Log;

        [HideInCallstack]
        public static void Log(string info)
        {
            if (currentLogLevel < LogLevel.Log) return;
            Debug.Log(info);
        }

        /// <summary>
        /// 警告
        /// </summary>
        [HideInCallstack]
        public static void Warning(string info)
        {
            if (currentLogLevel < LogLevel.Warning) return;
            Debug.LogWarning(info);
        }
        /// <summary>
        /// 错误
        /// </summary>
        [HideInCallstack]
        public static void Error(string info)
        {
            if (currentLogLevel < LogLevel.Error) return;
            Debug.LogError(info);
        }

        /// <summary>
        /// 异常
        /// </summary>
        [HideInCallstack]
        public static void Exception(System.Exception exception)
        {
            if (currentLogLevel < LogLevel.Exception) return;
            Debug.LogException(exception);
        }
    }
}
