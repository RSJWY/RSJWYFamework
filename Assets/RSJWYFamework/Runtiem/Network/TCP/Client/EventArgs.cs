namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 客户端状态消息
    /// </summary>
    public sealed class ClientStatusEventArgs : EventArgsBase
    {
        public NetClientStatus netClientStatus;
    }
    /// <summary>
    /// 向服务器发送消息
    /// </summary>
    public sealed class ClientSendToServerEventArgs : TCPClientSoketEventArgs
    {
    }
    /// <summary>
    /// 接收到服务器发来的消息
    /// </summary>
    public sealed class ClientReceivesMSGFromServer: TCPClientSoketEventArgs
    {
    }
    /// <summary>
    /// TCP客户端基类
    /// </summary>
    public abstract class TCPClientSoketEventArgs:EventArgsBase
    {
        /// <summary>
        /// 消息载体
        /// </summary>
        public byte[] msgBase;
    }
}