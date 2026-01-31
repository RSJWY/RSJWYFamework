using UnityEngine;

namespace RSJWYFamework.Runtime
{
    public enum LogLevel
    {
        Log,
        Warning,
        Error,
        Exception,
        None,
    }
    public class AppLogger
    {
        /// <summary>
        /// 日志输出等级
        /// </summary>
        public static LogLevel currentLogLevel = LogLevel.Log;
        /// <summary>
        /// 是否启用指定日志等级
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static bool Enabled(LogLevel level)
        {
            return currentLogLevel != LogLevel.None && level >= currentLogLevel;
        }

        [HideInCallstack]
        public static void Log(string info, Object context = null)
        {
            if (!Enabled(LogLevel.Log)) return;
            Debug.Log(info, context);
        }

        /// <summary>
        /// 警告
        /// </summary>
        [HideInCallstack]
        public static void Warning(string info, Object context = null)
        {
            if (!Enabled(LogLevel.Warning)) return;
            Debug.LogWarning(info, context);
        }
        /// <summary>
        /// 错误
        /// </summary>
        [HideInCallstack]
        public static void Error(string info, Object context = null)
        {
            if (!Enabled(LogLevel.Error)) return;
            Debug.LogError(info, context);
        }

        /// <summary>
        /// 异常
        /// </summary>
        [HideInCallstack]
        public static void Exception(System.Exception exception, Object context = null)
        {
            if (!Enabled(LogLevel.Exception)) return;
            Debug.LogException(exception, context);
        }
    }
}
