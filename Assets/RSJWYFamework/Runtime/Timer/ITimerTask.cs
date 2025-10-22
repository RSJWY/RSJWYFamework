using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 定时任务接口
    /// </summary>
    public interface ITimerTask
    {
        /// <summary>
        /// 任务唯一标识
        /// </summary>
        string TaskId { get; }
        
        /// <summary>
        /// 任务名称
        /// </summary>
        string TaskName { get; }
        
        /// <summary>
        /// 任务优先级
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// 延迟时间（秒）
        /// </summary>
        float DelayTime { get; }
        
        /// <summary>
        /// 间隔时间（秒），如果为0则表示只执行一次
        /// </summary>
        float IntervalTime { get; }
        
        /// <summary>
        /// 最大执行次数，-1表示无限次
        /// </summary>
        int MaxExecuteCount { get; }
        
        /// <summary>
        /// 当前已执行次数
        /// </summary>
        int CurrentExecuteCount { get; }
        
        /// <summary>
        /// 是否使用不受时间缩放影响的时间
        /// </summary>
        bool UseUnscaledTime { get; }
        
        /// <summary>
        /// 任务是否已完成
        /// </summary>
        bool IsCompleted { get; }
        
        /// <summary>
        /// 任务是否已取消
        /// </summary>
        bool IsCancelled { get; }
        
        /// <summary>
        /// 下次执行时间
        /// </summary>
        float NextExecuteTime { get; }
        
        /// <summary>
        /// 取消令牌
        /// </summary>
        CancellationToken CancellationToken { get; }
        
        /// <summary>
        /// 执行任务
        /// </summary>
        /// <returns>返回UniTask以支持异步操作</returns>
        UniTask ExecuteAsync();
        
        /// <summary>
        /// 取消任务
        /// </summary>
        void Cancel();
        
        /// <summary>
        /// 重置任务状态
        /// </summary>
        void Reset();
        
        /// <summary>
        /// 更新任务状态
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        /// <returns>是否需要执行任务</returns>
        bool UpdateTask(float deltaTime);
    }
}