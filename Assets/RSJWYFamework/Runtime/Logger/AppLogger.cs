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
        public static void Log(string info)
        {
            if (!Enabled(LogLevel.Log)) return;
            Debug.Log(info);
        }

        /// <summary>
        /// 警告
        /// </summary>
        [HideInCallstack]
        public static void Warning(string info)
        {
            if (!Enabled(LogLevel.Warning)) return;
            Debug.LogWarning(info);
        }
        /// <summary>
        /// 错误
        /// </summary>
        [HideInCallstack]
        public static void Error(string info)
        {
            if (!Enabled(LogLevel.Error)) return;
            Debug.LogError(info);
        }

        /// <summary>
        /// 异常
        /// </summary>
        [HideInCallstack]
        public static void Exception(System.Exception exception)
        {
            if (!Enabled(LogLevel.Exception)) return;
            Debug.LogException(exception);
        }
    }
}
