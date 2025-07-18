using System;
using System.Collections.Concurrent;
using RSJWYFamework.Runtime;
using UnityEngine;

namespace RSJWYFamework.Runtiem
{
    /// <summary>
    /// 主线程执行器
    /// <remarks>把某一些操作转移到unity主线程下执行</remarks>
    /// </summary>
    [Module]
    public class UnityMainThreadDispatcher : ModuleBase
    {
        private static UnityMainThreadDispatcher _instance;
        private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();
        

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
        }

        public override void Initialize()
        {
        }

        public override void Shutdown()
        {
        }

        public override void LifeUpdate()
        {
            // 执行所有排队操作
            while (_executionQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"[MainThreadDispatcher] 执行操作时出错: {ex}");
                }
            }
        }
    }
}