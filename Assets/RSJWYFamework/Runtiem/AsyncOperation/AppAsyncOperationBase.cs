using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using RSJWYFamework.Runtiem.Logger;

namespace RSJWYFamework.Runtiem.AsyncOperation
{
    /// <summary>
    /// 异步操作基类
    /// </summary>
    public abstract class AppAsyncOperationBase
    {
        
        private Action<AppAsyncOperationBase> _callback;
        private string _packageName = null;
        private int _whileFrame = 1000;

        /// <summary>
        /// 所有子任务
        /// </summary>
        internal readonly List<AppAsyncOperationBase> Childs = new List<AppAsyncOperationBase>(10);

        /// <summary>
        /// 等待异步执行完成
        /// </summary>
        internal bool IsWaitForAsyncComplete { private set; get; } = false;

        /// <summary>
        /// 是否已经完成
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
        public string PackageName
        {
            get
            {
                return _packageName;
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

      

        internal abstract void InternalStart();
        internal abstract void InternalUpdate();

        internal virtual void InternalAbort()
        {
        }
        internal virtual void InternalWaitForAsyncComplete()
        {
            throw new System.NotImplementedException($"异步操作未实现：{this.GetType().Name}");
        }
        internal virtual string InternalGetDesc()
        {
            return string.Empty;
        }

        internal void InternalException(Exception ex)
        {
            _utcs.TrySetException(ex);
        }

        /// <summary>
        /// 设置包裹名称
        /// </summary>
        internal void SetPackageName(string packageName)
        {
            _packageName = packageName;
        }

        /// <summary>
        /// 添加子任务
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
                AppLogger.Warning($"Async operaiton {this.GetType().Name} has been abort !");
                InternalAbort();
                //设置异步被取消
                _utcs.TrySetCanceled(cancellationToken);
            }
        }

        /// <summary>
        /// 清理
        /// </summary>
        internal void ClearCompleted()
        {
            _callback = null;
        }

        /// <summary>
        /// 等待异步执行完毕
        /// 异步操作转换为同步执行，强制当前线程等待直到操作完成，使用时需注意！！！
        /// </summary>
        public void WaitForAsyncComplete()
        {
            if (IsDone)
                return;

            //TODO 防止异步操作被挂起陷入无限死循环！
            // 例如：文件解压任务或者文件导入任务！
            if (Status == AppAsyncOperationStatus.None)
            {
                StartOperation();
            }

            IsWaitForAsyncComplete = true;
            InternalWaitForAsyncComplete();
        }
        /// <summary>
        /// 执行While循环
        /// 为内部实现，可考虑加到InternalWaitForAsyncComplete中
        /// </summary>
        protected bool ExecuteWhileDone()
        {
            if (IsDone == false)
            {
                // 执行更新逻辑
                InternalUpdate();

                // 当执行次数用完时
                _whileFrame--;
                if (_whileFrame <= 0)
                {
                    Status = AppAsyncOperationStatus.Failed;
                    Error = $"Operation {this.GetType().Name} failed to wait for async complete !";
                    AppLogger.Error(Error);
                }
            }
            return IsDone;
        }

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
        /// 异步操作任务
        /// 简易的异步实现，可不使用回调直接进行异步等待
        /// </summary>
        public UniTask UniTask(CancellationToken cancellationToken = default)
        {
            if (_utcs == null)
            {
                _utcs = new UniTaskCompletionSource();
                if (IsDone)
                    _utcs.TrySetResult();
            }
            this.cancellationToken = cancellationToken;
            // 注册取消回调
            cancellationToken.Register(AbortOperation);
            return _utcs.Task;
        }  
        /// <summary>
        /// 外部请求取消令牌
        /// </summary>
        CancellationToken cancellationToken;
        private UniTaskCompletionSource _utcs;
        #endregion
    }
}