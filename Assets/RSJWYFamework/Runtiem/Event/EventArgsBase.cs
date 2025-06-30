namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 事件内容载体
    /// </summary>
    public abstract class EventArgsBase
    {
        public abstract void Release();
        /// <summary>
        /// 消息发送者
        /// </summary>
        public object Sender;

    }
}