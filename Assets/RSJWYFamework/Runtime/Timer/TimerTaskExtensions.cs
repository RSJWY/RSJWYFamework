using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 定时任务扩展方法
    /// 提供更便捷的定时任务创建和管理方式
    /// </summary>
    public static class TimerTaskExtensions
    {
        /// <summary>
        /// 获取定时任务执行器实例
        /// </summary>
        private static TimerExecutorManager TimerExecutorManager => ModuleManager.GetModule<TimerExecutorManager>();



        #region 静态便捷方法

        /// <summary>
        /// 延迟执行（静态方法）
        /// </summary>
        /// <param name="delayTime">延迟时间（秒）</param>
        /// <param name="action">要执行的动作</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="useUnscaledTime">是否使用不受时间缩放影响的时间</param>
        /// <returns>任务ID</returns>
        public static string Delay(float delayTime, Action action, string taskName = null, bool useUnscaledTime = false)
        {
            return TimerExecutorManager.DelayCall(action, delayTime, taskName, useUnscaledTime);
        }

        /// <summary>
        /// 重复执行（静态方法）
        /// </summary>
        /// <param name="delayTime">延迟时间（秒）</param>
        /// <param name="intervalTime">间隔时间（秒）</param>
        /// <param name="action">要执行的动作</param>
        /// <param name="maxExecuteCount">最大执行次数，-1表示无限次</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="useUnscaledTime">是否使用不受时间缩放影响的时间</param>
        /// <returns>任务ID</returns>
        public static string Repeat(float delayTime, float intervalTime, Action action, 
            int maxExecuteCount = -1, string taskName = null, bool useUnscaledTime = false)
        {
            return TimerExecutorManager.RepeatCall(action, delayTime, intervalTime, maxExecuteCount, taskName, useUnscaledTime);
        }

        /// <summary>
        /// 每帧执行指定次数
        /// </summary>
        /// <param name="action">要执行的动作</param>
        /// <param name="frameCount">执行帧数</param>
        /// <param name="taskName">任务名称</param>
        /// <returns>任务ID</returns>
        public static string RepeatForFrames(Action action, int frameCount, string taskName = null)
        {
            return TimerExecutorManager.RepeatCall(action, 0f, Time.fixedDeltaTime, frameCount, taskName, false);
        }

        /// <summary>
        /// 每秒执行
        /// </summary>
        /// <param name="action">要执行的动作</param>
        /// <param name="maxExecuteCount">最大执行次数，-1表示无限次</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="useUnscaledTime">是否使用不受时间缩放影响的时间</param>
        /// <returns>任务ID</returns>
        public static string EverySecond(Action action, int maxExecuteCount = -1, string taskName = null, bool useUnscaledTime = false)
        {
            return TimerExecutorManager.RepeatCall(action, 1f, 1f, maxExecuteCount, taskName, useUnscaledTime);
        }

        /// <summary>
        /// 下一帧执行
        /// </summary>
        /// <param name="action">要执行的动作</param>
        /// <param name="taskName">任务名称</param>
        /// <returns>任务ID</returns>
        public static string NextFrame(Action action, string taskName = null)
        {
            return TimerExecutorManager.DelayCall(action, Time.fixedDeltaTime, taskName, false);
        }

        #endregion

        #region 任务管理扩展

        /// <summary>
        /// 取消任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功取消</returns>
        public static bool CancelTimer(this string taskId)
        {
            return TimerExecutorManager.CancelTask(taskId);
        }

        /// <summary>
        /// 检查任务是否存在
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否存在</returns>
        public static bool HasTimer(this string taskId)
        {
            return TimerExecutorManager.HasTask(taskId);
        }

        /// <summary>
        /// 获取任务信息
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>任务实例</returns>
        public static ITimerTask GetTimer(this string taskId)
        {
            return TimerExecutorManager.GetTask(taskId);
        }

        #endregion

        #region 条件执行扩展

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

        #endregion
    }

    /// <summary>
    /// 条件定时任务
    /// 等待条件满足后执行动作
    /// </summary>
    public class ConditionalTimerTask : TimerTaskBase
    {
        private readonly Func<bool> _condition;
        private readonly Action _action;
        private readonly float _timeout;
        private float _elapsedTime;

        public ConditionalTimerTask(Func<bool> condition, Action action, string taskName = null, 
            float checkInterval = 0.1f, float timeout = -1f, bool useUnscaledTime = false)
            : base(taskName ?? "ConditionalTask", 0f, checkInterval, -1, useUnscaledTime)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _timeout = timeout;
            _elapsedTime = 0f;
        }

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

        protected override async UniTask OnExecuteAsync()
        {
            CancellationToken.ThrowIfCancellationRequested();
            
            _action?.Invoke();
            
            // 条件任务执行一次后就完成
            IsCompleted = true;
            
            await UniTask.Yield();
        }
    }
}