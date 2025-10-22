using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 定时任务基类
    /// </summary>
    public abstract class TimerTaskBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private float _currentTime;
        private bool _isFirstExecution = true;
        /// <summary>
        /// 任务ID
        /// </summary>
        public string TaskId { get; private set; }
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName { get; protected set; }
        /// <summary>
        /// 任务优先级
        /// </summary>
        public virtual int Priority { get; protected set; } = 0;
        /// <summary>
        /// 延迟执行时间（秒）
        /// </summary>
        public float DelayTime { get; protected set; }
        /// <summary>
        /// 执行间隔时间（秒）
        /// </summary>
        public float IntervalTime { get; protected set; }
        
        /// <summary>
        /// 最大执行次数
        /// </summary>
        public int MaxExecuteCount { get; protected set; }
        /// <summary>
        /// 当前执行次数
        /// </summary>
        public int CurrentExecuteCount { get; protected set; }
        /// <summary>
        /// 是否使用不受时间缩放影响的时间
        /// </summary>
        public bool UseUnscaledTime { get; protected set; }
        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted { get; protected set; }
        /// <summary>
        /// 是否已取消
        /// </summary>
        public bool IsCancelled => _cancellationTokenSource?.IsCancellationRequested ?? false;
        /// <summary>
        /// 下次执行时间（秒）
        /// </summary>
        public float NextExecuteTime { get; protected set; }
        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? default;

        protected TimerTaskBase(string taskName = null, float delayTime = 0f, float intervalTime = 0f, 
            int maxExecuteCount = 1, bool useUnscaledTime = false)
        {
            TaskId = Guid.NewGuid().ToString();
            TaskName = taskName ?? GetType().Name;
            DelayTime = delayTime;
            IntervalTime = intervalTime;
            MaxExecuteCount = maxExecuteCount;
            UseUnscaledTime = useUnscaledTime;
            
            _cancellationTokenSource = new CancellationTokenSource();
            Reset();
        }

        /// <summary>
        /// 重置任务状态
        /// </summary>
        public virtual void Reset()
        {
            _currentTime = 0f;
            CurrentExecuteCount = 0;
            IsCompleted = false;
            _isFirstExecution = true;
            
            // 计算下次执行时间
            NextExecuteTime = DelayTime;
            
            // 重新创建取消令牌
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }
        /// <summary>
        /// 更新任务状态
        /// </summary>
        /// <param name="deltaTime"> deltaTime </param>
        /// <returns> 是否需要执行任务 </returns>
        public virtual bool UpdateTask(float deltaTime)
        {
            if (IsCompleted || IsCancelled)
                return false;

            _currentTime += deltaTime;

            // 检查是否到达执行时间
            if (_currentTime >= NextExecuteTime)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 异步执行任务
        /// </summary>
        /// <returns> 任务完成的UniTask </returns>
        public async UniTask ExecuteAsync()
        {
            if (IsCompleted || IsCancelled)
                return;

            try
            {
                // 执行具体任务
                await OnExecuteAsync();
                
                CurrentExecuteCount++;
                
                // 检查是否完成
                if (MaxExecuteCount > 0 && CurrentExecuteCount >= MaxExecuteCount)
                {
                    IsCompleted = true;
                    return;
                }
                
                // 如果是重复任务，计算下次执行时间
                if (IntervalTime > 0)
                {
                    if (_isFirstExecution)
                    {
                        // 第一次执行后，使用间隔时间
                        NextExecuteTime = _currentTime + IntervalTime;
                        _isFirstExecution = false;
                    }
                    else
                    {
                        NextExecuteTime += IntervalTime;
                    }
                }
                else
                {
                    // 一次性任务
                    IsCompleted = true;
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被取消
                AppLogger.Log($"定时任务 {TaskName} 被取消");
            }
            catch (Exception ex)
            {
                AppLogger.Exception(new AppException($"定时任务 {TaskName} 执行时发生异常", ex));
                IsCompleted = true; // 发生异常时标记为完成
            }
        }

        /// <summary>
        /// 子类需要实现的具体任务逻辑
        /// </summary>
        protected abstract UniTask OnExecuteAsync();

        /// <summary>
        /// 取消任务
        /// </summary>
        public virtual void Cancel()
        {
            _cancellationTokenSource?.Cancel();
            AppLogger.Log($"定时任务 {TaskName} 已取消");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}