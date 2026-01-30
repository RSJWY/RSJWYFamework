using IngameDebugConsole;
using UnityEngine;
namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 注册诊断系统的控制台指令
    /// </summary>
    public class DiagnosticConsoleCommands : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterCommands()
        {
            // 注册开启/关闭性能监控的指令
            DebugLogConsole.AddCommand("perf.monitor", "Toggle Module Performance Monitor (true/false)", (string state) => {
                bool enable = state.ToLower() == "true" || state == "1";
                ModulePerformanceMonitor.IsEnabled = enable;
                // 同时确保 ModuleManager 的开关也是打开的，否则不会调用 StartTimer
                if (enable)
                {
                    ModuleManager.EnablePerformanceMonitoring = true;
                }
                Debug.Log($"Module Performance Monitor is now {(ModulePerformanceMonitor.IsEnabled ? "ENABLED" : "DISABLED")}");
            });

            // 注册打印报告的指令
            DebugLogConsole.AddCommand("perf.report", "Print Module Performance Report", () => {
                if (!ModulePerformanceMonitor.IsEnabled)
                {
                    Debug.LogWarning("Module Performance Monitor is DISABLED. Use 'perf.monitor true' to enable.");
                }
                ModulePerformanceMonitor.LogPerformanceReport();
            });

            // 注册清理数据的指令
            DebugLogConsole.AddCommand("perf.clear", "Clear Performance Data", () => {
                ModulePerformanceMonitor.ClearPerformanceData();
                Debug.Log("Module Performance Data Cleared.");
            });
        }
    }
}
