using System;
using System.Collections.Generic;
using System.Diagnostics;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 异步生命周期执行器
    /// </summary>
    [Module]
    public class AppAsyncOperationSystem:ModuleBase
    {
        
#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeInitialize()
        {
            DestroyAll();
        }
#endif

        /// <summary>
        /// 所有异步操作
        /// </summary>
        private static readonly List<AppAsyncOperationBase> _operations = new List<AppAsyncOperationBase>(1000);
        /// <summary>
        /// 新增的异步操作
        /// </summary>
        private static readonly List<AppAsyncOperationBase> _newList = new List<AppAsyncOperationBase>(1000);
        /// <summary>
        /// 异步操作开始回调
        /// </summary>
        private static Action<string, AppAsyncOperationBase> _startCallback = null;
        /// <summary>
        /// 异步操作完成回调
        /// </summary>
        private static Action<string, AppAsyncOperationBase> _finishCallback = null;
        
        
        // 计时器相关
        private static Stopwatch _watch;
        private static long _frameTime;

        /// <summary>
        /// 异步操作的最小时间片段
        /// </summary>
        public static long MaxTimeSlice { set; get; } = long.MaxValue;

        /// <summary>
        /// 处理器是否繁忙
        /// </summary>
        public static bool IsBusy
        {
            get
            {
                return _watch.ElapsedMilliseconds - _frameTime >= MaxTimeSlice;
            }
        }


        /// <summary>
        /// 初始化异步操作系统
        /// </summary>
        public override void Initialize()
        {
            _watch = Stopwatch.StartNew();
        }

        /// <summary>
        /// 是否检查处理器是否繁忙
        /// </summary>
        private bool checkBusy;
        /// <summary>
        /// 更新异步操作系统
        /// </summary>
        public override void LifeUpdate()
        {
            // 移除已经完成的异步操作
            // 注意：移除上一帧完成的异步操作
            for (int i = _operations.Count - 1; i >= 0; i--)
            {
                var operation = _operations[i];
                if (operation.IsFinish)
                {
                    _operations.RemoveAt(i);
                    operation.ClearCompletedCallback();
                }
            }

            // 添加新增的异步操作
            if (_newList.Count > 0)
            {
                bool sorting = false;
                foreach (var operation in _newList)
                {
                    if (operation.Priority > 0)
                    {
                        sorting = true;
                        break;
                    }
                }

                _operations.AddRange(_newList);
                _newList.Clear();

                // 重新排序优先级
                if (sorting)
                    _operations.Sort();
            }

            // 更新进行中的异步操作
            checkBusy = MaxTimeSlice < long.MaxValue;
            _frameTime = _watch.ElapsedMilliseconds;
            for (int i = 0; i < _operations.Count; i++)
            {
                // 检查处理器是否繁忙，繁忙则跳过本次Update
                if (checkBusy && IsBusy)
                    break;

                var operation = _operations[i];
                if (operation.IsFinish)
                    continue;

                operation.UpdateOperation();
            }
        }

        /// <summary>
        /// 秒更新异步操作系统
        /// </summary>
        public override void LifePerSecondUpdate()
        {
            // 更新进行中的异步操作
            checkBusy = MaxTimeSlice < long.MaxValue;
            _frameTime = _watch.ElapsedMilliseconds;
            for (int i = 0; i < _operations.Count; i++)
            {
                var operation = _operations[i];
                if (operation.IsFinish)
                    continue;

                operation.SecondUpdate();
            }
        }

        /// <summary>
        /// 秒更新异步操作系统（不受时间缩放影响）
        /// </summary>
        public override void LifePerSecondUpdateUnScaleTime()
        {
            // 更新进行中的异步操作
            checkBusy = MaxTimeSlice < long.MaxValue;
            _frameTime = _watch.ElapsedMilliseconds;
            for (int i = 0; i < _operations.Count; i++)
            {
                var operation = _operations[i];
                if (operation.IsFinish)
                    continue;

                operation.SecondUnScaleTimeUpdate();
            }
        }

        /// <summary>
        /// 销毁异步操作系统
        /// </summary>
        public static void DestroyAll()
        {
            _operations.Clear();
            _newList.Clear();
            _startCallback = null;
            _finishCallback = null;
            _watch = null;
            _frameTime = 0;
            MaxTimeSlice = long.MaxValue;
        }
        /// <summary>
        /// 通过异步操作名称清空异步操作
        /// </summary>
        /// <param name="asyncOperationName"></param>
        public static void ClearAsyncOperationName(string asyncOperationName)
        {
            // 终止临时队列里的任务
            foreach (var operation in _newList)
            {
                if (operation.AsyncOperationName == asyncOperationName)
                {
                    operation.AbortOperation();
                }
            }
            // 终止正在进行的任务
            foreach (var operation in _operations)
            {
                if (operation.AsyncOperationName == asyncOperationName)
                {
                    operation.AbortOperation();
                }
            }
        }

        /// <summary>
        /// 监听任务开始
        /// </summary>
        public static void RegisterStartCallback(Action<string, AppAsyncOperationBase> callback)
        {
            _startCallback = callback;
        }

        /// <summary>
        /// 监听任务结束
        /// </summary>
        public static void RegisterFinishCallback(Action<string, AppAsyncOperationBase> callback)
        {
            _finishCallback = callback;
        }

        /// <summary>
        /// 开始处理异步操作类
        /// </summary>
        public static void StartOperation(string asyncOperationName, AppAsyncOperationBase operation)
        {
            _newList.Add(operation);
            operation.SetAsyncOperationName(asyncOperationName);
            operation.StartOperation();
        }

        public override void Shutdown()
        {
            DestroyAll();
        }
        
        #region 调试信息
        internal static List<AppDebugOperationInfo> GetDebugOperationInfos(string asyncOperationName)
        {
            List<AppDebugOperationInfo> result = new List<AppDebugOperationInfo>(_operations.Count);
            foreach (var operation in _operations)
            {
                if (operation.AsyncOperationName == asyncOperationName)
                {
                    var operationInfo = GetDebugOperationInfo(operation);
                    result.Add(operationInfo);
                }
            }
            return result;
        }
        internal static AppDebugOperationInfo GetDebugOperationInfo(AppAsyncOperationBase operation)
        {
            var operationInfo = new AppDebugOperationInfo();
            operationInfo.OperationName = operation.GetType().Name;
            operationInfo.OperationDesc = operation.GetOperationDesc();
            operationInfo.Priority = operation.Priority;
            operationInfo.Progress = operation.Progress;
            operationInfo.BeginTime = operation.BeginTime;
            operationInfo.ProcessTime = operation.ProcessTime;
            operationInfo.Status = operation.Status.ToString();
            operationInfo.Childs = new List<AppDebugOperationInfo>(operation.Childs.Count);
            foreach (var child in operation.Childs)
            {
                var childInfo = GetDebugOperationInfo(child);
                operationInfo.Childs.Add(childInfo);
            }
            return operationInfo;
        }
        #endregion
    }
}