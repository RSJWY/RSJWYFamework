using System;
using System.Collections.Generic;
using System.Threading;

namespace RSJWYFamework.Runtime
{
    public class TaskIDHandler : IDisposable
    {
        // 存储WS传入的所有任务ID（自动去重）
        private readonly HashSet<string> wsTaskIDs = new HashSet<string>();

        // POST传入的基准ID（唯一）
        private string benchmarkID = string.Empty;

        // 是否已触发流程（触发后锁定，拒绝后续输入）
        private bool triggered = false;

        // 线程锁
        private readonly object lockObject = new object();

        // 触发后的回调委托
        public Action<string> OnTaskTriggered { get; set; }

        public TaskIDHandler()
        {
            Reset();
        }

        public void Dispose()
        {
            // 清理资源
            Reset();
            OnTaskTriggered = null;
        }

        /**
         * WS回调中调用：记录所有传入的任务ID（支持多线程调用）
         * @param taskID WS收到的任务ID
         */
        public void AddWSID(string taskID)
        {
            if (string.IsNullOrEmpty(taskID))
            {
                UnityEngine.Debug.LogWarning("AddWSID: 收到空ID，忽略");
                return;
            }

            lock (lockObject)
            {
                if (triggered)
                {
                    UnityEngine.Debug.Log($"AddWSID: 流程已触发，忽略WS ID: {taskID}");
                    return;
                }

                // 先判断ID是否已存在
                bool alreadyExists = wsTaskIDs.Contains(taskID);
                if (alreadyExists)
                {
                    UnityEngine.Debug.Log($"AddWSID: WS ID已存在: {taskID}");
                    return; // 已存在的ID无需处理
                }

                // 新ID，添加到集合
                wsTaskIDs.Add(taskID);
                UnityEngine.Debug.Log($"AddWSID: 记录WS ID: {taskID}，当前累计: {wsTaskIDs.Count}个");

                // 检查是否与基准ID匹配
                if (!string.IsNullOrEmpty(benchmarkID) && benchmarkID == taskID)
                {
                    UnityEngine.Debug.Log($"AddWSID: WS ID {taskID} 与基准ID匹配，触发流程");
                    if (OnTaskTriggered != null)
                    {
                        OnTaskTriggered.Invoke(benchmarkID);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("AddWSID: 未绑定OnTaskTriggered，无法触发流程");
                    }

                    triggered = true;
                }
            }
        }

        /**
         * POST回调中调用：设置基准ID并检查触发（支持多线程调用）
         * @param benchmarkID POST返回的基准任务ID（唯一）
         */
        public void SetBenchmarkID(string benchmarkID)
        {
            if (string.IsNullOrEmpty(benchmarkID))
            {
                UnityEngine.Debug.LogWarning("SetBenchmarkID: 收到空基准ID，忽略");
                return;
            }

            lock (lockObject)
            {
                // 先判断：已触发或已有基准ID，则忽略
                if (triggered || !string.IsNullOrEmpty(this.benchmarkID))
                {
                    UnityEngine.Debug.Log($"SetBenchmarkID: 流程已锁定或已有基准ID，忽略新ID: {benchmarkID}");
                    return;
                }

                // 再赋值：确认无基准ID后，才记录当前ID
                this.benchmarkID = benchmarkID;
                UnityEngine.Debug.Log($"SetBenchmarkID: 已设置基准ID: {this.benchmarkID}");

                // 检查WS集合中是否存在该ID
                if (wsTaskIDs.Contains(this.benchmarkID))
                {
                    UnityEngine.Debug.Log($"SetBenchmarkID: 基准ID {this.benchmarkID} 在WS中存在，触发流程");
                    if (OnTaskTriggered != null)
                    {
                        OnTaskTriggered.Invoke(this.benchmarkID);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("SetBenchmarkID: 未绑定OnTaskTriggered，无法触发流程");
                    }

                    triggered = true; // 触发后锁定
                }
                else
                {
                    UnityEngine.Debug.Log($"SetBenchmarkID: 基准ID {this.benchmarkID} 暂未在WS中找到，等待WS传入");
                }
            }
        }

        /**
         * 设置触发后的回调
         */
        public void SetOnTaskTriggered(Action<string> callback)
        {
            OnTaskTriggered = callback;
        }

        /**
         * 重置状态（新任务开始前调用，支持多线程）
         */
        public void Reset()
        {
            lock (lockObject)
            {
                wsTaskIDs.Clear();
                benchmarkID = string.Empty;
                triggered = false;
                UnityEngine.Debug.Log("TaskIDHandler: 状态已重置，准备接收新任务");
            }
        }
    }
}