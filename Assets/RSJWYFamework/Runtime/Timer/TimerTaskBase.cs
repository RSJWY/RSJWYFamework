using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 定时任务基类
    /// </summary>
    public abstract class TimerTaskBase : ITimerTask
    {
        private CancellationTokenSource _cancellationTokenSource;
        private float _currentTime;
        private bool _isFirstExecution = true;
        
        public string TaskId { get; private set; }
        public string TaskName { get; protected set; }
        public virtual int Priority { get; protected set; } = 0;
        public float DelayTime { get; protected set; }
        public float IntervalTime { get; protected set; }
        public int MaxExecuteCount { get; protected set; }
        public int CurrentExecuteCount { get; protected set; }
        public bool UseUnscaledTime { get; protected set; }
        public bool IsCompleted { get; protected set; }
        public bool IsCancelled => _cancellationTokenSource?.IsCancellationRequested ?? false;
        public float NextExecuteTime { get; protected set; }
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

        public virtual void Cancel()
        {
            _cancellationTokenSource?.Cancel();
            AppLogger.Log($"定时任务 {TaskName} 已取消");
        }

        public virtual void Dispose()
        {
            Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}