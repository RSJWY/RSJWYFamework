namespace RSJWYFamework.Runtime
{
    /// <summary>
            /// TCP服务端事件
            /// </summary>
            public abstract class TCPServerSoketEventArgs:EventArgsBase
            {
                /// <summary>
                /// 消息的关联客户端
                /// </summary>
                public ClientSocketToken ClientSocketToken;
                /// <summary>
                /// 消息的载体
                /// </summary>
                public byte[] msgBase;
            }
            /// <summary>
            /// 客户端连接上来的事件
            /// </summary>
            public sealed class ServerClientConnectedCallBackEventArgs :TCPServerSoketEventArgs
            {
            }
            /// <summary>
            /// 客户端离线的事件
            /// </summary>
            public sealed class ServerCloseClientCallBackEventArgs : TCPServerSoketEventArgs
            {
            }
            /// <summary>
            /// 收到客户端发来的消息
            /// </summary>
            public sealed class FromClientReceiveMsgCallBackEventArgs : TCPServerSoketEventArgs
            {
            }
            /// <summary>
            /// 向客户端发送消息
            /// </summary>
            public sealed class ServerToClientMsgEventArgs : TCPServerSoketEventArgs
            {
            }
            /// <summary>
            /// 向所有客户端发送消息
            /// </summary>
            public sealed class ServerToClientMsgAllEventArgs : TCPServerSoketEventArgs
            {
            }
            /// <summary>
            /// 服务端状态事件
            /// </summary>
            public sealed class ServerStatusEventArgs : EventArgsBase
            {
                public NetServerStatus status;
            }
}