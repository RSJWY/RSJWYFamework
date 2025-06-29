namespace RSJWYFamework.Runtiem
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

        /// <summary>
        /// 异步操作开始
        /// </summary>
        protected abstract void OnStart();

        /// <summary>
        /// 异步操作更新
        /// </summary>
        protected abstract void OnUpdate();

        /// <summary>
        /// 异步操作终止
        /// </summary>
        protected abstract void OnAbort();

        /// <summary>
        /// 异步等待完成
        /// </summary>
        protected virtual void OnWaitForAsyncComplete() { }

        /// <summary>
        /// 异步操作系统是否繁忙
        /// </summary>
        protected bool IsBusy()
        {
            return AppOperationSystem.IsBusy;
        }

        /// <summary>
        /// 终止异步操作
        /// </summary>
        protected void Abort()
        {
            AbortOperation();
        }
    }
}