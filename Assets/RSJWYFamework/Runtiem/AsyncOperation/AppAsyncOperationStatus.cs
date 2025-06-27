namespace RSJWYFamework.Runtiem.AsyncOperation
{
    public class AppAsyncOperationStatus
    {
        /// <summary>
        /// 操作状态
        /// </summary>
        public enum RAsyncOperationStatus
        {
            /// <summary>
            /// 默认
            /// </summary>
            None,
            /// <summary>
            /// 正在加工
            /// </summary>
            Processing,
            /// <summary>
            /// 成功
            /// </summary>
            Succeed,
            /// <summary>
            /// 失败
            /// </summary>
            Failed
        }
    }
}