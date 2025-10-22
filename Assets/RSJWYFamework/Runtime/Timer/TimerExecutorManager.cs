using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 定时任务执行器
    /// 类似协程的定时任务系统，支持取消操作
    /// </summary>
    [Module]
    public class TimerExecutorManager : ModuleBase
    {
        /// <summary>
        /// 所有活跃的定时任务
        /// </summary>
        private readonly Dictionary<string, ITimerTask> _activeTasks = new Dictionary<string, ITimerTask>();
        
        /// <summary>
        /// 待添加的任务队列
        /// </summary>
        private readonly Queue<ITimerTask> _pendingTasks = new Queue<ITimerTask>();
        
        /// <summary>
        /// 待移除的任务ID列表
        /// </summary>
        private readonly List<string> _tasksToRemove = new List<string>();
        
        /// <summary>
        /// 线程锁
        /// </summary>
        private readonly object _lock = new object();
        
        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;
        
        /// <summary>
        /// 当前活跃任务数量
        /// </summary>
        public int ActiveTaskCount => _activeTasks.Count;

        public override void Initialize()
        {
            AppLogger.Log("定时任务执行器初始化完成");
        }

        public override void Shutdown()
        {
            // 取消所有任务
            CancelAllTasks();
            
            lock (_lock)
            {
                _activeTasks.Clear();
                _pendingTasks.Clear();
                _tasksToRemove.Clear();
            }
            
            AppLogger.Log("定时任务执行器已关闭");
        }

        public override void LifeUpdate()
        {
            ProcessPendingTasks();
            UpdateTasks(Time.deltaTime, false);
            RemoveCompletedTasks();
        }

        public override void LifePerSecondUpdateUnScaleTime()
        {
            UpdateTasks(Time.unscaledDeltaTime, true);
        }

        /// <summary>
        /// 处理待添加的任务
        /// </summary>
        private void ProcessPendingTasks()
        {
            lock (_lock)
            {
                while (_pendingTasks.Count > 0)
                {
                    var task = _pendingTasks.Dequeue();
                    if (!_activeTasks.ContainsKey(task.TaskId))
                    {
                        _activeTasks[task.TaskId] = task;
                        
                        if (EnableVerboseLogging)
                            AppLogger.Log($"添加定时任务: {task.TaskName} (ID: {task.TaskId})");
                    }
                }
            }
        }

        /// <summary>
        /// 更新任务
        /// </summary>
        private void UpdateTasks(float deltaTime, bool unscaledTimeUpdate)
        {
            var tasksToExecute = new List<ITimerTask>();
            
            lock (_lock)
            {
                foreach (var kvp in _activeTasks)
                {
                    var task = kvp.Value;
                    
                    // 跳过已完成或已取消的任务
                    if (task.IsCompleted || task.IsCancelled)
                    {
                        _tasksToRemove.Add(task.TaskId);
                        continue;
                    }
                    
                    // 根据任务设置选择合适的时间更新方式
                    bool shouldUpdate = unscaledTimeUpdate ? task.UseUnscaledTime : !task.UseUnscaledTime;
                    if (!shouldUpdate) continue;
                    
                    // 更新任务并检查是否需要执行
                    if (task.UpdateTask(deltaTime))
                    {
                        tasksToExecute.Add(task);
                    }
                }
            }
            
            // 执行需要执行的任务
            foreach (var task in tasksToExecute)
            {
                ExecuteTaskAsync(task).Forget();
            }
        }

        /// <summary>
        /// 异步执行任务
        /// </summary>
        private async UniTaskVoid ExecuteTaskAsync(ITimerTask task)
        {
            try
            {
                if (EnableVerboseLogging)
                    AppLogger.Log($"执行定时任务: {task.TaskName} (第{task.CurrentExecuteCount + 1}次)");
                
                await task.ExecuteAsync();
                
                if (task.IsCompleted && EnableVerboseLogging)
                    AppLogger.Log($"定时任务完成: {task.TaskName}");
            }
            catch (Exception ex)
            {
                AppLogger.Exception(new AppException($"执行定时任务 {task.TaskName} 时发生异常", ex));
            }
        }

        /// <summary>
        /// 移除已完成的任务
        /// </summary>
        private void RemoveCompletedTasks()
        {
            lock (_lock)
            {
                foreach (var taskId in _tasksToRemove)
                {
                    if (_activeTasks.TryGetValue(taskId, out var task))
                    {
                        _activeTasks.Remove(taskId);
                        
                        // 如果任务实现了IDisposable，则释放资源
                        if (task is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        
                        if (EnableVerboseLogging)
                            AppLogger.Log($"移除定时任务: {task.TaskName} (ID: {taskId})");
                    }
                }
                _tasksToRemove.Clear();
            }
        }

        #region 公共API

        /// <summary>
        /// 添加定时任务
        /// </summary>
        /// <param name="task">任务实例</param>
        /// <returns>任务ID</returns>
        public string AddTask(ITimerTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            lock (_lock)
            {
                _pendingTasks.Enqueue(task);
            }
            
            AppLogger.Log($"定时任务已加入队列: {task.TaskName}");
            return task.TaskId;
        }

        /// <summary>
        /// 创建并添加一个简单的延迟任务
        /// </summary>
        /// <param name="action">要执行的动作</param>
        /// <param name="delayTime">延迟时间（秒）</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="useUnscaledTime">是否使用不受时间缩放影响的时间</param>
        /// <returns>任务ID</returns>
        public string DelayCall(Action action, float delayTime, string taskName = null, bool useUnscaledTime = false)
        {
            var task = new ActionTimerTask(action, taskName, delayTime, 0f, 1, useUnscaledTime);
            return AddTask(task);
        }

        /// <summary>
        /// 创建并添加一个重复执行的任务
        /// </summary>
        /// <param name="action">要执行的动作</param>
        /// <param name="delayTime">延迟时间（秒）</param>
        /// <param name="intervalTime">间隔时间（秒）</param>
        /// <param name="maxExecuteCount">最大执行次数，-1表示无限次</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="useUnscaledTime">是否使用不受时间缩放影响的时间</param>
        /// <returns>任务ID</returns>
        public string RepeatCall(Action action, float delayTime, float intervalTime, int maxExecuteCount = -1, 
            string taskName = null, bool useUnscaledTime = false)
        {
            var task = new ActionTimerTask(action, taskName, delayTime, intervalTime, maxExecuteCount, useUnscaledTime);
            return AddTask(task);
        }

        /// <summary>
        /// 取消指定的任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功取消</returns>
        public bool CancelTask(string taskId)
        {
            lock (_lock)
            {
                if (_activeTasks.TryGetValue(taskId, out var task))
                {
                    task.Cancel();
                    AppLogger.Log($"取消定时任务: {task.TaskName}");
                    return true;
                }
            }
            
            AppLogger.Warning($"未找到要取消的任务: {taskId}");
            return false;
        }

        /// <summary>
        /// 取消指定名称的所有任务
        /// </summary>
        /// <param name="taskName">任务名称</param>
        /// <returns>取消的任务数量</returns>
        public int CancelTasksByName(string taskName)
        {
            int cancelledCount = 0;
            
            lock (_lock)
            {
                var tasksToCancel = _activeTasks.Values.Where(t => t.TaskName == taskName).ToList();
                foreach (var task in tasksToCancel)
                {
                    task.Cancel();
                    cancelledCount++;
                }
            }
            
            if (cancelledCount > 0)
                AppLogger.Log($"取消了 {cancelledCount} 个名为 '{taskName}' 的定时任务");
            
            return cancelledCount;
        }

        /// <summary>
        /// 取消所有任务
        /// </summary>
        public void CancelAllTasks()
        {
            int cancelledCount = 0;
            
            lock (_lock)
            {
                foreach (var task in _activeTasks.Values)
                {
                    if (!task.IsCancelled && !task.IsCompleted)
                    {
                        task.Cancel();
                        cancelledCount++;
                    }
                }
            }
            
            if (cancelledCount > 0)
                AppLogger.Log($"取消了 {cancelledCount} 个定时任务");
        }

        /// <summary>
        /// 检查任务是否存在
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否存在</returns>
        public bool HasTask(string taskId)
        {
            lock (_lock)
            {
                return _activeTasks.ContainsKey(taskId);
            }
        }

        /// <summary>
        /// 获取任务信息
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>任务实例，如果不存在则返回null</returns>
        public ITimerTask GetTask(string taskId)
        {
            lock (_lock)
            {
                _activeTasks.TryGetValue(taskId, out var task);
                return task;
            }
        }

        /// <summary>
        /// 获取所有活跃任务的信息
        /// </summary>
        /// <returns>任务信息列表</returns>
        public List<ITimerTask> GetAllActiveTasks()
        {
            lock (_lock)
            {
                return _activeTasks.Values.ToList();
            }
        }

        #endregion
    }

    /// <summary>
    /// 基于Action的简单定时任务实现
    /// </summary>
    internal class ActionTimerTask : TimerTaskBase
    {
        private readonly Action _action;

        public ActionTimerTask(Action action, string taskName = null, float delayTime = 0f, 
            float intervalTime = 0f, int maxExecuteCount = 1, bool useUnscaledTime = false)
            : base(taskName ?? "ActionTask", delayTime, intervalTime, maxExecuteCount, useUnscaledTime)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        protected override async UniTask OnExecuteAsync()
        {
            // 检查取消令牌
            CancellationToken.ThrowIfCancellationRequested();
            
            // 执行动作
            _action?.Invoke();
            
            // 如果需要异步等待，可以在这里添加
            await UniTask.Yield();
        }
    }
}