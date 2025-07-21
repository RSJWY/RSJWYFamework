using System;

namespace RSJWYFamework.Runtime
{ 
            /// <summary>
            /// TCP服务端事件
            /// </summary>
            public abstract class TCPServerSoketBaseEventArgs:EventArgsBase
            {
                
            }
            /// <summary>
            /// 客户端连接上来的事件
            /// </summary>
            public sealed class ServerClientConnectedCallBackEventArgs :TCPServerSoketBaseEventArgs
            {
                /// <summary>
                /// 发出的TCPServerHandle
                /// </summary>
                public Guid ServerHandle { get;internal set; }
                /// <summary>
                /// 连接上来的客户端Handle
                /// </summary>
                public Guid ClientHandle { get;internal set; }
            }
            /// <summary>
            /// 客户端离线的事件
            /// </summary>
            public sealed class ServerCloseClientCallBackEventArgs : TCPServerSoketBaseEventArgs
            {
                /// <summary>
                /// 发出的TCPServerHandle
                /// </summary>
                public Guid ServerHandle { get; internal set; }
                /// <summary>
                /// 发生离线的客户端Handle
                /// </summary>
                public Guid ClientHandle { get;internal set; }
            }
            /// <summary>
            /// 收到客户端发来的消息
            /// </summary>
            public sealed class FromClientReceiveMsgCallBackEventArgs : TCPServerSoketBaseEventArgs
            {
                /// <summary>
                /// 消息容器
                /// </summary>
                public TCPClientToServerMsg msgContainer { get;internal set; }
            }
            /// <summary>
            /// 向客户端发送消息
            /// </summary>
            public sealed class ServerToClientMsgEventArgs : TCPServerSoketBaseEventArgs
            {
                public readonly TCPServertToClientMsg msgContainer;
            }
            /// <summary>
            /// 向所有服务端连上来的客户端发送消息
            /// </summary>
            public sealed class SendMsgToAllServerAllClient : TCPServerSoketBaseEventArgs
            {
                /// <summary>
                /// 消息数据
                /// </summary>
                public readonly byte[] data;
            }
            /// <summary>
            /// 通过指定服务端向连上来的客户端发送消息
            /// </summary>
            public sealed class SendMsgToServerAllClient : TCPServerSoketBaseEventArgs
            {
                /// <summary>
                /// 服务端Handle
                /// </summary>
                public readonly Guid ServerHandle;
                /// <summary>
                /// 消息数据
                /// </summary>
                public readonly byte[] data;
            }
            /// <summary>
            /// 服务端状态事件
            /// </summary>
            public sealed class ServerStatusEventArgs : TCPServerSoketBaseEventArgs
            {
                /// <summary>
                /// 服务端Handle
                /// </summary>
                public Guid ServerHandle { get; internal set; }
                /// <summary>
                /// 服务端状态
                /// </summary>
                public NetServerStatus status{ get; internal set; }
            }
}