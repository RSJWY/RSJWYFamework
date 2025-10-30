namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 一个干净的，对外部开放的异步操作中间层
    /// </summary>
    public abstract class AppGameAsyncOperation:AppAsyncOperationBase
    {
        internal override void InternalStart()
        {
            OnStart();
        }
        internal override void InternalUpdate()
        {
            OnUpdate();
        }
        internal override void InternalAbort()
        {
            OnAbort();
        }
        internal override void InternalWaitForAsyncComplete()
        {
            OnWaitForAsyncComplete();
        }
        internal override void InternalSecondUpdate()
        {
            OnSecondUpdate();
        }
        
        internal override void InternalSecondUnScaleTimeUpdate()
        {
            OnSecondUpdateUnScaleTime();
        }



        /// <summary>
        /// 异步操作开始
        /// </summary>
        protected abstract void OnStart();

        /// <summary>
        /// 异步操作更新
        /// </summary>
        protected abstract void OnUpdate();
        /// <summary>
        /// 异步操作秒更新
        /// </summary>
        protected abstract void OnSecondUpdate();

        /// <summary>
        /// 异步操作终止
        /// </summary>
        protected abstract void OnAbort();
        /// <summary>
        /// 异步操作秒更新（不考虑时间缩放）
        /// </summary>
        protected abstract void OnSecondUpdateUnScaleTime();



        /// <summary>
        /// 同步等待异步等待完成
        /// </summary>
        protected abstract void OnWaitForAsyncComplete();

        /// <summary>
        /// 异步操作系统是否繁忙
        /// </summary>
        public bool IsBusy()
        {
            return AppAsyncOperationSystem.IsBusy;
        }

        /// <summary>
        /// 终止异步操作
        /// <remarks>继承的子类调用取消请求</remarks>
        /// </summary>
        public void Abort()
        {
            AbortOperation();
        }
    }
}