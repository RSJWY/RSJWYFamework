using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using RSJWYFamework.Runtime;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 主线程执行器
    /// <remarks>把某一些操作转移到unity主线程下执行</remarks>
    /// </summary>
    [Module]
    public class UnityMainThreadDispatcher : ModuleBase
    {
        #region 私有字段
        /// <summary>
        /// 主线程执行队列
        /// </summary>
        private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();
        /// <summary>
        /// 延迟执行队列
        /// </summary>
        private static readonly ConcurrentQueue<DelayedAction> _delayedQueue = new ConcurrentQueue<DelayedAction>();
        /// <summary>
        /// 条件执行队列
        /// </summary>
        private static readonly ConcurrentQueue<ConditionalAction> _conditionalQueue = new ConcurrentQueue<ConditionalAction>();
        
        /// <summary>
        /// 每帧最大执行操作数量，防止卡顿
        /// </summary>
        private static int _maxActionsPerFrame = 50;
        
        /// <summary>
        /// 每帧最大执行时间（毫秒），防止卡顿
        /// </summary>
        private static float _maxExecutionTimeMs = 5.0f;
        
        /// <summary>
        /// 统计信息
        /// </summary>
        private static int _totalExecutedActions = 0;
        /// <summary>
        /// 失败执行操作数量
        /// </summary>
        private static int _totalFailedActions = 0;
        /// <summary>
        /// 当前队列大小
        /// </summary>
        private static int _currentQueueSize = 0;
        
        #endregion

        #region 公共属性
        
        /// <summary>
        /// 每帧最大执行操作数量
        /// </summary>
        public static int MaxActionsPerFrame
        {
            get => _maxActionsPerFrame;
            set => _maxActionsPerFrame = Mathf.Max(1, value);
        }
        
        /// <summary>
        /// 每帧最大执行时间（毫秒）
        /// </summary>
        public static float MaxExecutionTimeMs
        {
            get => _maxExecutionTimeMs;
            set => _maxExecutionTimeMs = Mathf.Max(0.1f, value);
        }
        
        /// <summary>
        /// 当前队列中待执行的操作数量
        /// </summary>
        public static int QueueSize => _currentQueueSize;
        
        /// <summary>
        /// 总执行操作数
        /// </summary>
        public static int TotalExecutedActions => _totalExecutedActions;
        
        /// <summary>
        /// 总失败操作数
        /// </summary>
        public static int TotalFailedActions => _totalFailedActions;
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 将操作加入主线程执行队列
        /// </summary>
        /// <param name="action">要执行的操作</param>
        public static void Enqueue(Action action)
        {
            if (action == null)
            {
                AppLogger.Warning("[MainThreadDispatcher] 尝试加入空操作");
                return;
            }
            
            _executionQueue.Enqueue(action);
            _currentQueueSize++;
        }
        
        /// <summary>
        /// 延迟执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="delaySeconds">延迟时间（秒）</param>
        public static void EnqueueDelayed(Action action, float delaySeconds)
        {
            if (action == null)
            {
                AppLogger.Warning("[MainThreadDispatcher] 尝试加入空的延迟操作");
                return;
            }
            
            if (delaySeconds < 0)
            {
                AppLogger.Warning("[MainThreadDispatcher] 延迟时间不能为负数，将立即执行");
                Enqueue(action);
                return;
            }
            
            var executeTime = Time.time + delaySeconds;
            _delayedQueue.Enqueue(new DelayedAction(action, executeTime));
        }
        
        /// <summary>
        /// 条件执行操作（只检查condition一次，如果不满足执行条件则直接丢弃）
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="condition">执行条件</param>
        public static void EnqueueConditionalOnlyOne(Action action, Func<bool> condition)
        {
            EnqueueConditional(action, condition, 0, 0f);
        }
        
        /// <summary>
        /// 条件执行操作（持续检查直到条件满足或达到最大重试次数）
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="condition">执行条件</param>
        /// <param name="maxRetries">最大重试次数，-1表示无限重试，0表示只检查一次</param>
        /// <param name="retryInterval">重试间隔时间（秒）</param>
        public static void EnqueueConditional(Action action, Func<bool> condition, int maxRetries, float retryInterval = 0.1f)
        {
            if (action == null)
            {
                AppLogger.Warning("[MainThreadDispatcher] 尝试加入空的条件操作");
                return;
            }
            
            if (condition == null)
            {
                AppLogger.Warning("[MainThreadDispatcher] 条件函数为空，将直接执行操作");
                Enqueue(action);
                return;
            }
            
            if (maxRetries == 0)
            {
                // 一次性检查，不满足条件就丢弃
                Enqueue(() =>
                {
                    try
                    {
                        if (condition())
                        {
                            action();
                        }
                        else
                        {
                            //AppLogger.Log("[MainThreadDispatcher] 条件不满足，操作已被丢弃");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"[MainThreadDispatcher] 条件检查或执行时出错: {ex}");
                    }
                });
            }
            else
            {
                // 持续检查直到条件满足或达到重试次数
                var conditionalAction = new ConditionalAction(action, condition, maxRetries, retryInterval);
                _conditionalQueue.Enqueue(conditionalAction);
                //AppLogger.Log($"[MainThreadDispatcher] 添加条件操作到持续检查队列，最大重试次数: {(maxRetries == -1 ? "无限" : maxRetries.ToString())}");
            }
        }
        
        /// <summary>
        /// 清空所有待执行的操作
        /// </summary>
        public static void ClearQueue()
        {
            var clearedCount = 0;
            while (_executionQueue.TryDequeue(out _))
            {
                clearedCount++;
            }
            
            while (_delayedQueue.TryDequeue(out _))
            {
                clearedCount++;
            }
            
            while (_conditionalQueue.TryDequeue(out _))
            {
                clearedCount++;
            }
            
            _currentQueueSize = 0;
            AppLogger.Log($"[MainThreadDispatcher] 清空队列，共清除 {clearedCount} 个操作");
        }
        
        /// <summary>
        /// 获取统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public static string GetStatistics()
        {
            return $"[MainThreadDispatcher] 统计信息 - 队列大小: {_currentQueueSize}, " +
                   $"总执行: {_totalExecutedActions}, 总失败: {_totalFailedActions}, " +
                   $"成功率: {(_totalExecutedActions > 0 ? (float)(_totalExecutedActions - _totalFailedActions) / _totalExecutedActions * 100 : 0):F1}%";
        }
        
        #endregion

        #region ModuleBase 实现
        
        public override void Initialize()
        {
            AppLogger.Log("[MainThreadDispatcher] 主线程调度器初始化完成");
        }

        public override void Shutdown()
        {
            ClearQueue();
            AppLogger.Log("[MainThreadDispatcher] 主线程调度器已关闭");
        }

        public override void LifeUpdate()
        {
            ProcessDelayedActions();
            ProcessConditionalActions();
            ProcessImmediateActions();
        }
        
        #endregion

        #region 私有方法
        
        /// <summary>
        /// 处理条件执行的操作
        /// </summary>
        private void ProcessConditionalActions()
        {
            var currentTime = Time.time;
            var tempQueue = new Queue<ConditionalAction>();
            //取出所有队列数据
            while (_conditionalQueue.TryDequeue(out var conditionalAction))
            {
                // 检查是否到了重试时间
                if (conditionalAction.NextRetryTime <= currentTime)
                {
                    try
                    {
                        // 检查条件是否满足
                        if (conditionalAction.Condition())
                        {
                            // 条件满足，执行操作
                            Enqueue(conditionalAction.Action);
                        }
                        else
                        {
                            // 条件不满足，检查是否需要重试
                            conditionalAction.CurrentRetries++;
                            
                            if (conditionalAction.MaxRetries == -1 || conditionalAction.CurrentRetries < conditionalAction.MaxRetries)
                            {
                                // 需要继续重试，更新下一次重试时间
                                conditionalAction.NextRetryTime = currentTime + conditionalAction.RetryInterval;
                                tempQueue.Enqueue(conditionalAction);
                            }
                            else
                            {
                                // 达到最大重试次数，丢弃操作
                                //AppLogger.Warning($"[MainThreadDispatcher] 条件操作达到最大重试次数 {conditionalAction.MaxRetries}，操作已被丢弃");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"[MainThreadDispatcher] 条件检查时出错: {ex}，操作已被丢弃");
                    }
                }
                else
                {
                    // 还未到重试时间，放回队列
                    tempQueue.Enqueue(conditionalAction);
                }
            }
            
            // 将需要继续等待的操作放回队列
            while (tempQueue.Count > 0)
            {
                _conditionalQueue.Enqueue(tempQueue.Dequeue());
            }
        }
        
        /// <summary>
        /// 处理延迟执行的操作
        /// </summary>
        private void ProcessDelayedActions()
        {
            var currentTime = Time.time;
            var readyActions = new List<Action>();
            
            // 收集所有准备执行的延迟操作
            var tempQueue = new Queue<DelayedAction>();
            while (_delayedQueue.TryDequeue(out var delayedAction))
            {
                if (delayedAction.ExecuteTime <= currentTime)
                {
                    readyActions.Add(delayedAction.Action);
                }
                else
                {
                    tempQueue.Enqueue(delayedAction);
                }
            }
            
            // 将未到时间的操作放回队列
            while (tempQueue.Count > 0)
            {
                _delayedQueue.Enqueue(tempQueue.Dequeue());
            }
            
            // 执行准备好的操作
            foreach (var action in readyActions)
            {
                Enqueue(action);
            }
        }
        
        /// <summary>
        /// 处理立即执行的操作
        /// </summary>
        private void ProcessImmediateActions()
        {
            var stopwatch = Stopwatch.StartNew();
            var executedCount = 0;
            
            // 限制每帧执行的操作数量和时间
            //必须满足以下条件：
            //1. 队列中有待执行的操作
            //2. 已执行的操作数量小于最大允许数量
            //3. 已执行的时间小于最大允许时间
            while (_executionQueue.TryDequeue(out var action) && 
                   executedCount < _maxActionsPerFrame && 
                   stopwatch.ElapsedMilliseconds < _maxExecutionTimeMs)
            {
                try
                {
                    action?.Invoke();
                    _totalExecutedActions++;
                    executedCount++;
                    _currentQueueSize--;
                }
                catch (Exception ex)
                {
                    _totalFailedActions++;
                    _currentQueueSize--;
                    AppLogger.Error($"[MainThreadDispatcher] 执行操作时出错: {ex}");
                }
            }
            
            stopwatch.Stop();
            
            // 统计判断，如果执行时间过长或队列积压严重，记录警告
            if (stopwatch.ElapsedMilliseconds > _maxExecutionTimeMs * 1.5f)
            {
                AppLogger.Warning($"[MainThreadDispatcher] 执行时间过长: {stopwatch.ElapsedMilliseconds}ms, 队列剩余: {_currentQueueSize}");
            }
            else if (_currentQueueSize > 100)
            {
                AppLogger.Warning($"[MainThreadDispatcher] 队列积压严重: {_currentQueueSize} 个操作待执行");
            }
        }
        
        #endregion

        
        #region 内部类型
        
        /// <summary>
        /// 延迟执行的操作
        /// </summary>
        private struct DelayedAction
        {
            /// <summary>
            /// 要执行的操作
            /// </summary>
            public Action Action;
            /// <summary>
            /// 执行时间
            /// </summary>
            public float ExecuteTime;
            
            public DelayedAction(Action action, float executeTime)
            {
                Action = action;
                ExecuteTime = executeTime;
            }
        }
        
        /// <summary>
        /// 条件执行的操作
        /// </summary>
        private struct ConditionalAction
        {
            /// <summary>
            /// 要执行的操作
            /// </summary>
            public Action Action;
            /// <summary>
            /// 执行条件
            /// </summary>
            public Func<bool> Condition;
            /// <summary>
            /// 最大重试次数，-1表示无限重试
            /// </summary>
            public int MaxRetries;
            /// <summary>
            /// 当前调用条件函数次数
            /// </summary>
            public int CurrentRetries;
            /// <summary>
            /// 重试间隔（秒）
            /// </summary>
            public float RetryInterval;
            /// <summary>
            /// 下一次重试时间
            /// </summary>  
            public float NextRetryTime;
            
            public ConditionalAction(Action action, Func<bool> condition, int maxRetries = -1, float retryInterval = 0.1f)
            {
                Action = action;
                Condition = condition;
                MaxRetries = maxRetries;
                CurrentRetries = 0;
                RetryInterval = retryInterval;
                NextRetryTime = Time.time;
            }
        }
        
        #endregion

    }
}