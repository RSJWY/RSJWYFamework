using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 模块性能监控器
    /// </summary>
    public static class ModulePerformanceMonitor
    {
        private static readonly Dictionary<string, PerformanceData> _performanceData = new();
        private static readonly Dictionary<string, Stopwatch> _activeTimers = new();
        private static bool _isEnabled = false;
        
        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }
        
        /// <summary>
        /// 性能数据结构
        /// </summary>
        public class PerformanceData
        {
            public long TotalExecutionTime { get; set; }
            public long CallCount { get; set; }
            public long MaxExecutionTime { get; set; }
            public long MinExecutionTime { get; set; } = long.MaxValue;
            public double AverageExecutionTime => CallCount > 0 ? (double)TotalExecutionTime / CallCount : 0;
        }
        
        /// <summary>
        /// 开始性能监控
        /// </summary>
        public static void StartTimer(string key)
        {
            if (!_isEnabled) return;
            
            if (!_activeTimers.ContainsKey(key))
            {
                _activeTimers[key] = new Stopwatch();
            }
            
            _activeTimers[key].Restart();
        }
        
        /// <summary>
        /// 结束性能监控
        /// </summary>
        public static void EndTimer(string key)
        {
            if (!_isEnabled || !_activeTimers.ContainsKey(key)) return;
            
            var timer = _activeTimers[key];
            timer.Stop();
            
            var elapsedTicks = timer.ElapsedTicks;
            
            if (!_performanceData.ContainsKey(key))
            {
                _performanceData[key] = new PerformanceData();
            }
            
            var data = _performanceData[key];
            data.TotalExecutionTime += elapsedTicks;
            data.CallCount++;
            data.MaxExecutionTime = Math.Max(data.MaxExecutionTime, elapsedTicks);
            data.MinExecutionTime = Math.Min(data.MinExecutionTime, elapsedTicks);
        }
        
        /// <summary>
        /// 获取性能数据
        /// </summary>
        public static PerformanceData GetPerformanceData(string key)
        {
            return _performanceData.TryGetValue(key, out var data) ? data : null;
        }
        
        /// <summary>
        /// 获取所有性能数据
        /// </summary>
        public static Dictionary<string, PerformanceData> GetAllPerformanceData()
        {
            return new Dictionary<string, PerformanceData>(_performanceData);
        }
        
        /// <summary>
        /// 清除性能数据
        /// </summary>
        public static void ClearPerformanceData()
        {
            _performanceData.Clear();
            _activeTimers.Clear();
        }
        
        /// <summary>
        /// 输出性能报告
        /// </summary>
        public static void LogPerformanceReport()
        {
            if (!_isEnabled) return;
            
            UnityEngine.Debug.Log("=== 模块性能报告 ===");
            foreach (var kvp in _performanceData)
            {
                var key = kvp.Key;
                var data = kvp.Value;
                var avgMs = data.AverageExecutionTime * 1000.0 / Stopwatch.Frequency;
                var maxMs = data.MaxExecutionTime * 1000.0 / Stopwatch.Frequency;
                var minMs = data.MinExecutionTime * 1000.0 / Stopwatch.Frequency;
                
                UnityEngine.Debug.Log($"{key}: 调用次数={data.CallCount}, 平均耗时={avgMs:F3}ms, 最大耗时={maxMs:F3}ms, 最小耗时={minMs:F3}ms");
            }
        }
    }
}