using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RSJWYFamework.Runtime
{

    /// <summary>
    /// 条件定时任务
    /// 等待条件满足后执行动作
    /// </summary>
    public class ConditionalTimerTask : TimerTaskBase
    {
        /// <summary>
        /// 条件函数，返回true时执行动作
        /// </summary>
        private readonly Func<bool> _condition;
        /// <summary>
        /// 要执行的动作
        /// </summary>
        private readonly Action _action;
        /// <summary>
        /// 超时时间，-1表示无超时
        /// </summary>
        private readonly float _timeout;
        /// <summary>
        /// 已过去的时间
        /// </summary>
        private float _elapsedTime;

        /// <summary>
        /// 条件定时任务
        /// </summary>
        /// <param name="condition">条件函数，返回true时执行动作</param>
        /// <param name="action">要执行的动作</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="checkInterval">检查条件的时间间隔</param>
        /// <param name="timeout">超时时间，-1表示无超时</param>
        /// <param name="useUnscaledTime">是否使用未缩放时间</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ConditionalTimerTask(Func<bool> condition, Action action, string taskName = null, 
            float checkInterval = 0.1f, float timeout = -1f, bool useUnscaledTime = false)
            : base(taskName ?? "ConditionalTask", 0f, checkInterval, -1, useUnscaledTime)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _timeout = timeout;
            _elapsedTime = 0f;
        }

        /// <summary>
        /// 更新任务
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        /// <returns>是否完成任务</returns>
        public override bool UpdateTask(float deltaTime)
        {
            if (IsCompleted || IsCancelled)
                return false;

            _elapsedTime += deltaTime;

            // 检查超时
            if (_timeout > 0 && _elapsedTime >= _timeout)
            {
                AppLogger.Warning($"条件任务 {TaskName} 超时");
                IsCompleted = true;
                return false;
            }

            // 检查条件
            try
            {
                if (_condition())
                {
                    return true; // 条件满足，执行任务
                }
            }
            catch (Exception ex)
            {
                AppLogger.Exception(new AppException($"条件任务 {TaskName} 检查条件时发生异常", ex));
                IsCompleted = true;
                return false;
            }

            return base.UpdateTask(deltaTime);
        }

        /// <summary>
        /// 异步执行任务
        /// </summary>
        /// <returns> 任务完成的UniTask </returns>
        protected override async UniTask OnExecuteAsync()
        {
            CancellationToken.ThrowIfCancellationRequested();
            
            _action?.Invoke();
            
            // 条件任务执行一次后就完成
            IsCompleted = true;
            
            await UniTask.Yield();
        }
        
        /// <summary>
        /// 等待条件满足后执行
        /// </summary>
        /// <param name="condition">条件函数</param>
        /// <param name="action">要执行的动作</param>
        /// <param name="checkInterval">检查间隔（秒）</param>
        /// <param name="timeout">超时时间（秒），-1表示无超时</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="useUnscaledTime">是否使用不受时间缩放影响的时间</param>
        /// <returns>任务ID</returns>
        public static string WaitUntil(Func<bool> condition, Action action, float checkInterval = 0.1f, 
            float timeout = -1f, string taskName = null, bool useUnscaledTime = false)
        {
            var task = new ConditionalTimerTask(condition, action, taskName, checkInterval, timeout, useUnscaledTime);
            return TimerExecutorManager.AddTask(task);
        }

        /// <summary>
        /// 等待条件不满足后执行
        /// </summary>
        /// <param name="condition">条件函数</param>
        /// <param name="action">要执行的动作</param>
        /// <param name="checkInterval">检查间隔（秒）</param>
        /// <param name="timeout">超时时间（秒），-1表示无超时</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="useUnscaledTime">是否使用不受时间缩放影响的时间</param>
        /// <returns>任务ID</returns>
        public static string WaitWhile(Func<bool> condition, Action action, float checkInterval = 0.1f, 
            float timeout = -1f, string taskName = null, bool useUnscaledTime = false)
        {
            var task = new ConditionalTimerTask(() => !condition(), action, taskName, checkInterval, timeout, useUnscaledTime);
            return TimerExecutorManager.AddTask(task);
        }
    }
}