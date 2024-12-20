﻿using System.Diagnostics;
using RSJWYFamework.Runtime.Main;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RSJWYFamework.Runtime.Logger
{
    /// <summary>
    /// 日志等级
    /// </summary>
    public enum Loglevel
    {
        LOG,
        WARN,
        ERROR,
    }

    public static class RSJWYLogger
    {

        public static Loglevel Loglevel;
        /// <summary>
        /// 日志
        /// </summary>
        public static void Log(string info)
        {
            /*logger_rf.Debug(info);*/
            StackTrace stackTrace = new StackTrace(1, true);
            Debug.Log(info);
            /*if (!Debugger.IsLogging())
                Main.Main.ExceptionLogManager
                    .UnityLogMessageReceivedThreadedEvent(info, stackTrace.ToString(), LogType.Log);*/
        }
        /// <summary>
        /// 日志
        /// </summary>
        public static void Log(RSJWYFameworkEnum @enum,string info)
        {
            //logger_rf.Debug($"{@enum}:{info}");
            
            StackTrace stackTrace = new StackTrace(1, true);
            Debug.Log($"{@enum}:{info}");
            /*if (!Debugger.IsLogging())
                Main.Main.ExceptionLogManager
                    .UnityLogMessageReceivedThreadedEvent($"{@enum}:\n{info}", null, LogType.Log);*/
        }

        /// <summary>
        /// 警告
        /// </summary>
        public static void Warning(string info)
        {
           // logger_rf.Warn(info);
           StackTrace stackTrace = new StackTrace(1, true); 
           Debug.LogWarning(info);
            /*if (!Debugger.IsLogging())
            {
                // 获取堆栈跟踪信息
                StackTrace stackTrace = new StackTrace(1, true); // 1 表示跳过当前方法帧，true 表示包含文件信息
                Main.Main.ExceptionLogManager
                    .UnityLogMessageReceivedThreadedEvent(info, stackTrace.ToString(), LogType.Warning);
            }*//**/
        }
        /// <summary>
        /// 警告
        /// </summary>
        public static void Warning(RSJWYFameworkEnum @enum,string info)
        {
            //logger_rf.Warn($"{@enum}:{info}");
            
            StackTrace stackTrace = new StackTrace(1, true);
            Debug.LogWarning($"{@enum}:{info}");;
            /*if (!Debugger.IsLogging())
                Main.Main.ExceptionLogManager
                    .UnityLogMessageReceivedThreadedEvent($"{@enum}:\n{info}", null, LogType.Warning);*/
        }

        /// <summary>
        /// 错误
        /// </summary>
        public static void Error(string info)
        {
           // logger_rf.Error(info);
            
           StackTrace stackTrace = new StackTrace(1, true);
            Debug.LogError(info);
            /*if (!Debugger.IsLogging())
                Main.Main.ExceptionLogManager
                    .UnityLogMessageReceivedThreadedEvent(info, null, LogType.Error);*/
        }
        /// <summary>
        /// 错误
        /// </summary>
        public static void Error(RSJWYFameworkEnum @enum,string info)
        {
            //logger_rf.Error($"{@enum}:{info}");
            
            StackTrace stackTrace = new StackTrace(1, true);
            Debug.LogError($"{@enum}:{info}");;
            /*if (!Debugger.IsLogging())
                Main.Main.ExceptionLogManager
                    .UnityLogMessageReceivedThreadedEvent($"{@enum}:\n{info}", null, LogType.Error);*/
        }

        /// <summary>
        /// 异常
        /// </summary>
        public static void Exception(System.Exception exception)
        {
           // logger_rf.Fatal(exception);
            
           StackTrace stackTrace = new StackTrace(1, true);
           Debug.LogException(exception);
            /*if (!Debugger.IsLogging())
                Main.Main.ExceptionLogManager
                    .UnityLogMessageReceivedThreadedEvent(exception.Message, exception.StackTrace, LogType.Exception);*//**/
        }
        /// <summary>
        /// 异常
        /// </summary>
        public static void Exception(RSJWYFameworkEnum @enum,System.Exception exception)
        {
           // logger_rf.Fatal($"{@enum}:{exception.Message}");
            
           StackTrace stackTrace = new StackTrace(1, true);
           Debug.LogException(exception);
            /*if (!Debugger.IsLogging())
                Main.Main.ExceptionLogManager
                    .UnityLogMessageReceivedThreadedEvent($"{@enum}:{exception.Message}", exception.StackTrace, LogType.Exception);*/
        }

    }
}