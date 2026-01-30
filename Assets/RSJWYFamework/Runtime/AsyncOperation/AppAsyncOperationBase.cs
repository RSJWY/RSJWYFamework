using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 异步操作基类
    /// </summary>
    public abstract class AppAsyncOperationBase : IComparable<AppAsyncOperationBase>
    {
        /// <summary>
        /// 异步操作完成回调
        /// </summary>
        private Action<AppAsyncOperationBase> _callback;
        /// <summary>
        /// 异步操作名称
        /// </summary>
        private string _asyncOperationName = null;

        /// <summary>
        /// 所有子任务
        /// </summary>
        internal readonly List<AppAsyncOperationBase> Childs = new List<AppAsyncOperationBase>(10);

        /// <summary>
        /// 是否已经执行结束
        /// </summary>
        internal bool IsFinish { private set; get; } = false;

        /// <summary>
        /// 任务优先级
        /// </summary>
        public uint Priority { set; get; } = 0;

        /// <summary>
        /// 任务状态
        /// </summary>
        public AppAsyncOperationStatus Status { get; protected set; } = AppAsyncOperationStatus.None;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; protected set; }

        /// <summary>
        /// 处理进度
        /// </summary>
        public float Progress { get; protected set; }

        /// <summary>
        /// 所属包裹名称
        /// </summary>
        public string AsyncOperationName
        {
            get
            {
                return _asyncOperationName;
            }
        }

        /// <summary>
        /// 是否已经完成
        /// </summary>
        public bool IsDone
        {
            get
            {
                return Status == AppAsyncOperationStatus.Failed || Status == AppAsyncOperationStatus.Succeed;
            }
        }

        /// <summary>
        /// 完成事件
        /// </summary>
        public event Action<AppAsyncOperationBase> Completed
        {
            add
            {
                if (IsDone)
                    value.Invoke(this);
                else
                    _callback += value;
            }
            remove
            {
                _callback -= value;
            }
        }




        
        /// <summary>
        /// 内部开始异步操作
        /// </summary>
        internal abstract void InternalStart();
        /// <summary>
        /// 内部更新异步操作
        /// </summary>
        internal abstract void InternalUpdate();
        
        /// <summary>
        /// 内部秒更新异步操作
        /// </summary>
        internal abstract void InternalSecondUpdate();
        
        /// <summary>
        /// 内部秒更新异步操作（不考虑时间缩放）
        /// </summary>
        internal abstract void InternalSecondUnScaleTimeUpdate();



        /// <summary>
        /// 内部取消异步操作
        /// </summary>
        internal virtual void InternalAbort()
        {
        }
        /// <summary>
        /// 内部等待异步操作完成
        /// <remarks>
        /// 这是在同步方法中等待异步操作完成
        /// </remarks>
        /// </summary>
        /*internal virtual void InternalWaitForAsyncComplete()
        {
            throw new System.NotImplementedException($"异步操作未实现：{_asyncOperationName}--{this.GetType().Name}");
        }*/
        /// <summary>
        /// 获取异步操作说明
        /// <remarks>
        /// 子类实现，返回异步操作说明
        /// </remarks>
        /// </summary>
        /// <returns></returns>
        internal virtual string InternalGetDesc()
        {
            return string.Empty;
        }

        internal void InternalException(Exception ex)
        {
            _utcs.TrySetException(ex);
            _ctr.Dispose();
        }
        
        /// <summary>
        /// 设置包裹名称
        /// </summary>
        internal void SetAsyncOperationName(string asyncOperationName)
        {
            _asyncOperationName = asyncOperationName;
        }
        /// <summary>
        /// 添加子任务
        /// 子任务的刷新将由父任务刷新
        /// </summary>
        internal void AddChildOperation(AppAsyncOperationBase child)
        {
#if UNITY_EDITOR
            if (Childs.Contains(child))
                throw new Exception($"The child node {child.GetType().Name} already exists !");
#endif

            Childs.Add(child);
        }

        /// <summary>
        /// 获取异步操作说明
        /// </summary>
        internal string GetOperationDesc()
        {
            return InternalGetDesc();
        }

        /// <summary>
        /// 开始异步操作
        /// </summary>
        internal void StartOperation()
        {
            if (Status == AppAsyncOperationStatus.None)
            {
                Status = AppAsyncOperationStatus.Processing;

                // 开始记录
                DebugBeginRecording();

                // 开始任务
                InternalStart();
            }
        }

        /// <summary>
        /// 更新异步操作
        /// </summary>
        internal void UpdateOperation()
        {
            if (IsDone == false)
            {
                // 更新记录
                DebugUpdateRecording();

                // 更新任务
                InternalUpdate();
            }

            if (IsDone && IsFinish == false)
            {
                IsFinish = true;

                // 进度百分百完成
                Progress = 1f;

                // 结束记录
                DebugEndRecording();

                //注意：如果完成回调内发生异常，会导致Task无限期等待
                _callback?.Invoke(this);

                //设置异步任务完成
                if (_utcs != null)
                    _utcs.TrySetResult();
                _ctr.Dispose();
            }
        }
        /// <summary>
        /// 秒更新异步操作
        /// </summary>
        internal void SecondUpdate()
        {
            if (IsDone == false)
            {
                // 更新任务
                InternalSecondUpdate();
            }
            if (IsDone && IsFinish == false)
            {
                IsFinish = true;

                // 进度百分百完成
                Progress = 1f;

                //注意：如果完成回调内发生异常，会导致Task无限期等待
                _callback?.Invoke(this);

                //设置异步任务完成
                if (_utcs != null)
                    _utcs.TrySetResult();
                _ctr.Dispose();
            }
        }
        /// <summary>
        /// 秒更新异步操作（不受时间缩放影响）
        /// </summary>
        internal void SecondUnScaleTimeUpdate()
        {
            if (IsDone == false)
            {
                // 更新任务
                InternalSecondUnScaleTimeUpdate();
            }
            if (IsDone && IsFinish == false)
            {
                IsFinish = true;

                // 进度百分百完成
                Progress = 1f;

                //注意：如果完成回调内发生异常，会导致Task无限期等待
                _callback?.Invoke(this);

                //设置异步任务完成
                if (_utcs != null)
                    _utcs.TrySetResult();
                _ctr.Dispose();
            }
        }
        
        

        /// <summary>
        /// 终止异步任务
        /// </summary>
        internal void AbortOperation()
        {
            foreach (var child in Childs)
            {
                child.AbortOperation();
            }

            if (IsDone == false)
            {
                Status = AppAsyncOperationStatus.Failed;
                Error = "user abort";
                AppLogger.Warning($"Async Operaiton {this.GetType().Name} has been abort !");
                InternalAbort();
                //设置异步被取消
                _utcs.TrySetCanceled(cancellationToken);
                _ctr.Dispose();
            }
        }

        /// <summary>
        /// 执行While循环
        /// <remarks>
        /// 为内部实现，可考虑加到InternalWaitForAsyncComplete中
        /// 模拟Update调用
        /// </remarks>
        /// </summary>
        /*protected bool ExecuteWhileDone()
        {
            if (IsDone == false)
            {
                // 执行更新逻辑
                InternalUpdate();

                // 当执行次数用完时
                _whileFrame--;
                if (_whileFrame <= 0)
                {
                    // 执行次数用完时，设置失败，可能本操作不适合同步执行
                    Status = AppAsyncOperationStatus.Failed;
                    Error = $"Operation {_asyncOperationName}--{this.GetType().Name} failed to wait for async complete !";
                    AppLogger.Error(Error);
                }
            }
            return IsDone;
        }*/
        /// <summary>
        /// 清理
        /// </summary>
        internal void ClearCompletedCallback()
        {
            _callback = null;
        }

        /// <summary>
        /// 等待异步执行完毕
        /// <remarks>
        /// 这个函数是把异步操作转委托同步执行（也就是同步等待异步操作完成）
        /// </remarks>
        /// </summary>
        /*public void WaitForAsyncComplete()
        {
            if (IsDone)
                return;

            //TODO 防止异步操作被挂起陷入无限死循环！
            // 例如：文件解压任务或者文件导入任务！
            if (Status == AppAsyncOperationStatus.None)
            {
                StartOperation();
            }

            if (IsWaitForAsyncComplete == false)
            {
                IsWaitForAsyncComplete = true;
                //调用继承的函数实现，一般内部也是while循环检查ExecuteWhileDone
                InternalWaitForAsyncComplete();
            }
        }*/
        

        #region 调试信息
        /// <summary>
        /// 开始的时间
        /// </summary>
        public string BeginTime = string.Empty;

        /// <summary>
        /// 处理耗时（单位：毫秒）
        /// </summary>
        public long ProcessTime { protected set; get; }

        // 加载耗时统计
        private Stopwatch _watch = null;

        [Conditional("DEBUG")]
        private void DebugBeginRecording()
        {
            if (_watch == null)
            {
                BeginTime = SpawnTimeToString(UnityEngine.Time.realtimeSinceStartup);
                _watch = Stopwatch.StartNew();
            }
        }

        [Conditional("DEBUG")]
        private void DebugUpdateRecording()
        {
            if (_watch != null)
            {
                ProcessTime = _watch.ElapsedMilliseconds;
            }
        }

        [Conditional("DEBUG")]
        private void DebugEndRecording()
        {
            if (_watch != null)
            {
                ProcessTime = _watch.ElapsedMilliseconds;
                _watch = null;
            }
        }

        private string SpawnTimeToString(float spawnTime)
        {
            float h = UnityEngine.Mathf.FloorToInt(spawnTime / 3600f);
            float m = UnityEngine.Mathf.FloorToInt(spawnTime / 60f - h * 60f);
            float s = UnityEngine.Mathf.FloorToInt(spawnTime - m * 60f - h * 3600f);
            return h.ToString("00") + ":" + m.ToString("00") + ":" + s.ToString("00");
        }
        #endregion

        #region 异步编程相关
        
        
        /// <summary>
        /// 外部请求取消令牌
        /// </summary>
        CancellationToken cancellationToken;
        private CancellationTokenRegistration _ctr;
        /// <summary>
        /// 异步操作任务源
        /// </summary>
        private UniTaskCompletionSource _utcs;
        /// <summary>
        /// 异步操作任务
        /// 简易的异步实现，可不使用回调直接进行异步等待
        /// </summary>
        public UniTask ToUniTask(CancellationToken cancellationToken = default)
        {
            if (Status == AppAsyncOperationStatus.None)
            {
                // 自动将任务注册到系统并启动
                // 使用当前类名作为默认名称，防止名称为空
                string opName = AsyncOperationName ?? this.GetType().Name;
                AppAsyncOperationSystem.StartOperation(opName, this);
            }

            if (_utcs != null)
            {
                // 重入保护：如果已经创建了 Task，直接返回
                // 忽略新的 CancellationToken，因为无法替换已注册的回调
                if (cancellationToken != this.cancellationToken && cancellationToken != default)
                {
                    AppLogger.Warning($"AsyncOperation {this.GetType().Name} ToUniTask called twice with different CancellationToken. The second token will be ignored.");
                }
                return _utcs.Task;
            }

            _utcs = new UniTaskCompletionSource();
            if (IsDone)
                _utcs.TrySetResult();

            this.cancellationToken = cancellationToken;
            if (cancellationToken.CanBeCanceled)
            {
                _ctr = cancellationToken.Register(AbortOperation);
            }
            return _utcs.Task;
        }  
        #endregion

        /// <summary>
        /// 比较异步操作优先级
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(AppAsyncOperationBase other)
        {
            return other.Priority.CompareTo(this.Priority);
        }
    }
}
