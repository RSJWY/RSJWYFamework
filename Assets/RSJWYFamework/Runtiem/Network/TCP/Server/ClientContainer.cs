using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 服务器模块 单独客户端容器，存储客户端的相关信息等
    /// </summary>
    internal class ClientSocketContainer
    {
        /// <summary>
        /// 心跳包维持记录
        /// </summary>
        public long lastPingTime;

        /// <summary>  
        /// 客户端IP地址  
        /// </summary>  
        public IPAddress IPAddress;

        /// <summary>  
        /// 远程地址  
        /// </summary>  
        public EndPoint Remote;

        /// <summary>  
        /// 连接时间  
        /// </summary>  
        public DateTime ConnectTime;

        /// <summary>
        /// 存储连接的客户端
        /// </summary>
        internal Socket socket;
        /// <summary>
        /// 存储数据-接收到的数据暂存容器
        /// </summary>
        internal ByteArrayMemory ReadBuff;

        /// <summary>
        /// 客户端汇报的ID
        /// 作为客户端的唯一标识符，以免出现同一个客户端多链接，
        /// 需要做一个定时检查，出现重复拒绝链接以及移除已有链接，重新发起连接
        /// </summary>
        /// <returns></returns>
        public Guid TokenID;
        /// <summary>
        /// 所属的server
        /// </summary>
        internal TcpServerService ServerService;

        /// <summary>
        /// 写
        /// </summary>
        internal SocketAsyncEventArgs readSocketAsyncEA;
        /// <summary>
        /// 读
        /// </summary>
        internal SocketAsyncEventArgs writeSocketAsyncEA;
        /// <summary>
        /// 目标消息
        /// </summary>
        internal ConcurrentQueue<ServerToClientMsgContainer> sendQueue;

        /// <summary>
        /// 通知多线程自己跳出
        /// </summary>
        internal CancellationTokenSource cts;

        /// <summary>
        /// 消息发送线程
        /// </summary>
        internal Thread msgSendThread;
        
        /// <summary>
        /// 消息发送线程
        /// </summary>
        internal Thread PingPongThread;
        
        /// <summary>
        /// 消息队列发送锁
        /// </summary>
        internal object msgSendThreadLock ;

        
        /// <summary>
        /// 关闭
        /// </summary>
        internal void Close()
        {
            try
            {
                lock (msgSendThreadLock)
                {
                    //释放锁，继续执行信息发送
                    Monitor.Pulse(msgSendThreadLock);
                }
                cts?.Cancel();
                socket?.Shutdown(SocketShutdown.Both);
                socket?.Close();
                //本条数据发送完成，激活线程，继续处理下一条
            }
            catch (Exception e)
            {
                AppLogger.Warning($"客户端关闭时发生错误！{e}");
            }
        }
        /// <summary>
        /// 心跳包检测
        /// </summary>
        public void PongThread()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    Thread.Sleep(1000);//本线程可以每秒检测一次
                    //检测心跳包是否超时的计算
                    //获取当前时间
                    long timeNow = Utility.Timestamp.UnixTimestampMilliseconds;
                    if (timeNow-lastPingTime>ServerService.pingInterval*4)
                    {
                        ServerService.CloseClientSocket(this);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error( $"检测心跳包时发生错误：{ex.Message}");
                    if (cts.Token.IsCancellationRequested)
                    {
                        AppLogger.Warning( $"请求取消任务");
                        break;
                    }
                }
            }
        }
    }
    
    /// <summary>
    ///消息发送数据容器
    /// </summary>
    internal class ServerToClientMsgContainer
    {
        /// <summary>
        /// 消息目标服务器
        /// </summary>
        internal ClientSocketContainer TargetContainer;
        /// <summary>
        /// 已转换完成的消息数组
        /// </summary>
        internal ByteArrayMemory SendBytes;
        
        /// <summary>
        /// 消息发送Token，用于本机发送完成回调唯一标记
        /// </summary>
        public Guid MsgToken;

    }

    /// <summary>
    /// TCP服务器发送给客户端的消息
    /// </summary>
    public struct TCPServertToClientMsg
    {
        /// <summary>
        /// 消息发送Token，用于本机发送完成回调唯一标记
        /// </summary>
        public Guid MsgToken;
        /// <summary>
        /// 消息数据
        /// </summary>
        public byte[] data;
        
        /// <summary>
        /// 消息服务器Handle
        /// </summary>
        public Guid ServerHandle;
        
        /// <summary>
        /// 消息客户端Handle
        /// </summary>
        public Guid ClientHandle;
        
    }
    
    /// <summary>
    /// TCP服务器发送给客户端的消息回调
    /// </summary>
    public struct TCPServertToClientMsgCallBack
    {
        /// <summary>
        /// 是否发送成功
        /// </summary>
        public bool Success{ get; internal set; }
        /// <summary>
        /// 发送失败
        /// </summary>
        public string Error { get; internal set; }
        
        /// <summary>
        /// 消息Token
        /// </summary>
        public Guid MsgToken{ get; internal set; }
        
        /// <summary>
        /// 消息TCPServer Handle
        /// </summary>
        public Guid TCPServerHandle { get; internal set; }
        
        /// <summary>
        /// 消息UDPClient Handle
        /// </summary>
        public Guid TCPClientHandle { get; internal set; }
    }
    /// <summary>
    /// 服务器接收到的来自客户端消息容器
    /// </summary>
    public struct TCPClientToServerMsg
    {
        /// <summary>
        /// 消息TCPServer Handle
        /// </summary>
        public Guid TCPServerHandle { get; internal set; }
        
        /// <summary>
        /// 消息UDPClient Handle
        /// </summary>
        public Guid TCPClientHandle { get; internal set; }
        
        /// <summary>
        /// 消息数据
        /// </summary>
        public byte[] msgBytes{ get; internal set; }
        
        /// <summary>
        /// 接收是否成功
        /// </summary>
        public bool Success { get; internal set; }
        
        /// <summary>
        /// 接收失败原因
        /// </summary>
        public string Error { get; internal set; }
    }
}
