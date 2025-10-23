using UnityEngine;
using TouchSocket.Core;
using System;
using System.Threading.Tasks;
using RSJWYFamework.Runtime;
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
    

    /// <summary>
    /// 手动初始化（若需要延迟初始化）
    /// </summary>
    public static void Init()
    {
        if (_isInitialized) return;

        try
        {
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
            _isInitialized = true;

            Debug.Log($"[UnityLoggerBridge] 初始化成功，日志路径：{Application.streamingAssetsPath}/{LogDirectory}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UnityLoggerBridge] 初始化失败：{e.Message}");
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
        _fileLogger?.Dispose();
        _fileLogger = null;
        _isInitialized = false;

        Debug.Log("[UnityLoggerBridge] 已清理资源");
    }
}

/// <summary>
/// 创建一个物体，保证unity退出时清理资源
/// </summary>
public class UnityLogToFileServer : MonoBehaviour
{
    /// <summary>
    /// 仅有部分代码在本时机可用。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void InitializeLogger()
    {
        UnityLoggerBridge.Init();
        AppLogger.Log("初始化日志记录器");
    }

    /// <summary>
    /// 创建一个物体来借用unity生命周期，确保在unity退出时清理资源
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterShutdown()
    {
        // 监听应用退出事件（需通过MonoBehaviour，因为纯C#类无法直接监听）
        var obj = new GameObject("[UnityLogToFileServerHelper]");
        obj.AddComponent<UnityLogToFileServer>();
        DontDestroyOnLoad(obj);
        AppLogger.Log("注册Unity生命周期");
    }
    private void OnApplicationQuit()
    {
        UnityLoggerBridge.Shutdown();
    }
}