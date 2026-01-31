using UnityEngine;
using TouchSocket.Core;
using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using RSJWYFamework.Runtime;
using System.IO;
using System.Globalization;
using LogLevel = TouchSocket.Core.LogLevel;

/// <summary>
/// 纯C#日志桥接器（不依赖MonoBehaviour）
/// </summary>
public static class UnityLoggerBridge
{
    private static FileLogger _fileLogger;
    private static bool _isInitialized;

    // 配置参数（纯C#类无法在Inspector设置，需代码定义或从配置文件读取）
    public static string LogDirectory { get; set; } = "Logs";
    public static int MaxFileSize { get; set; } = 1024 * 1024; // 1MB
    public static LogLevel LogLevel { get; set; } = LogLevel.Trace;
    public static int MaxLogRetentionDays { get; set; } = 3; // 日志保留天数

    /// <summary>
    /// 初始化入口
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void InitializeLogger()
    {
        Init();
        // 注册退出事件，替代 OnApplicationQuit
        Application.quitting -= Shutdown;
        Application.quitting += Shutdown;
        
        AppLogger.Log("初始化日志记录器");
    }

    /// <summary>
    /// 手动初始化（若需要延迟初始化）
    /// </summary>
    public static void Init()
    {
        if (_isInitialized) return;

        try
        {
            // 清理过期日志
            CleanUpOldLogs();

            // 初始化TouchSocket日志器
            _fileLogger = new FileLogger
            {
                LogLevel = LogLevel,
                MaxSize = MaxFileSize,
                CreateLogFolder = _ => $"{Application.streamingAssetsPath}/{LogDirectory}/{DateTime.Now:yyyy-MM-dd}",
            };

            // 注册Unity日志回调
            Application.logMessageReceivedThreaded += HandleUnityLog;
            
            // 注册Task异常回调，捕获异步任务中的漏网之鱼，守护最后一道防线。
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Debug.LogError($"捕获到Task任务中未处理的异常信息：{e.Exception}");
                e.SetObserved(); // 标记异常已处理，避免程序崩溃
            };
            UniTaskScheduler.UnobservedTaskException += (e) =>
            {
                Debug.LogError($"捕获到UniTask任务中未处理的异常信息：{e}");
            };
            
            _isInitialized = true;
            Debug.Log($"[UnityLoggerBridge] 初始化成功，日志路径：{Application.streamingAssetsPath}/{LogDirectory}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UnityLoggerBridge] 初始化失败：{e.Message}");
        }
    }

    /// <summary>
    /// 合并后的清理逻辑：删除过期的日期目录
    /// </summary>
    private static void CleanUpOldLogs()
    {
        try
        {
            var basePath = Path.Combine(Application.streamingAssetsPath, LogDirectory);
            if (!Directory.Exists(basePath)) return;

            var thresholdDate = DateTime.Now.Date.AddDays(-MaxLogRetentionDays);
            
            // 使用 EnumerateDirectories 减少内存分配
            foreach (var dir in Directory.EnumerateDirectories(basePath))
            {
                var dirName = Path.GetFileName(dir);
                // 尝试解析目录名为日期
                if (DateTime.TryParseExact(dirName, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dirDate))
                {
                    if (dirDate.Date < thresholdDate)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            Debug.Log($"[UnityLoggerBridge] 已删除过期日志目录：{dirName}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[UnityLoggerBridge] 删除日志目录失败：{dirName}，原因：{ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[UnityLoggerBridge] 清理日志目录发生异常：{e.Message}");
        }
    }

    /// <summary>
    /// 处理Unity日志
    /// </summary>
    private static void HandleUnityLog(string condition, string stackTrace, LogType type)
    {
        if (!_isInitialized || _fileLogger == null) return;

        var level = type switch
        {
            LogType.Error or LogType.Exception or LogType.Assert => LogLevel.Error,
            LogType.Warning => LogLevel.Warning,
            _ => LogLevel.Debug
        };

        var message = $"[{DateTime.Now:HH:mm:ss.fff}] [{type}] {condition}\n{stackTrace}";
        _fileLogger.Log(level, null, message, null);
    }

    /// <summary>
    /// 清理资源（退出前调用）
    /// </summary>
    public static void Shutdown()
    {
        if (!_isInitialized) return;

        Application.logMessageReceivedThreaded -= HandleUnityLog;
        Application.quitting -= Shutdown;
        
        _fileLogger?.Dispose();
        _fileLogger = null;
        _isInitialized = false;

        Debug.Log("[UnityLoggerBridge] 已清理资源");
    }
}
