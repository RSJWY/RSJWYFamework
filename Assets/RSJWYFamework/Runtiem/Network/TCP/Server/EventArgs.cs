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
                public Guid ServerHandle { get;private set; }
                /// <summary>
                /// 连接上来的客户端Handle
                /// </summary>
                public Guid ClientHandle { get;private set; }
                public ServerClientConnectedCallBackEventArgs(Guid serverHandle, Guid clientHandle)
                {
                    ServerHandle = serverHandle;
                    ClientHandle = clientHandle;
                }
            }
            /// <summary>
            /// 客户端离线的事件
            /// </summary>
            public sealed class ServerCloseClientCallBackEventArgs : TCPServerSoketBaseEventArgs
            {
                /// <summary>
                /// 发出的TCPServerHandle
                /// </summary>
                public Guid ServerHandle { get; private set; }
                /// <summary>
                /// 发生离线的客户端Handle
                /// </summary>
                public Guid ClientHandle { get;private set; }
                public ServerCloseClientCallBackEventArgs(Guid serverHandle, Guid clientHandle)
                {
                    ServerHandle = serverHandle;
                    ClientHandle = clientHandle;
                }
            }
            /// <summary>
            /// 服务端状态事件
            /// </summary>
            public sealed class ServerStatusEventArgs : TCPServerSoketBaseEventArgs
            {
                /// <summary>
                /// 服务端Handle
                /// </summary>
                public Guid ServerHandle { get; private set; }
                /// <summary>
                /// 服务端状态
                /// </summary>
                public NetServerStatus status{ get; private set; }
             
                public ServerStatusEventArgs(Guid serverHandle,NetServerStatus status)
                {
                    ServerHandle = serverHandle;
                    this.status = status;
                }
            }
            /// <summary>
            /// 收到客户端发来的消息
            /// </summary>
            public sealed class FromClientReceiveMsgCallBackEventArgs : TCPServerSoketBaseEventArgs
            {
                /*
                /// <summary>
                /// 消息容器
                /// </summary>
                public TCPClientToServerMsg msgContainer { get;internal set; }
                */
                
                /// <summary>
                /// 消息TCPServer Handle
                /// </summary>
                public Guid TCPServerHandle { get; private set; }
        
                /// <summary>
                /// 消息UDPClient Handle
                /// </summary>
                public Guid TCPClientHandle { get; private set; }
        
                /// <summary>
                /// 消息数据
                /// </summary>
                public byte[] msgBytes{ get; private set; }
        
                /// <summary>
                /// 接收是否成功
                /// </summary>
                public bool Success { get; private set; }
        
                /// <summary>
                /// 接收失败原因
                /// </summary>
                public string Error { get; private set; }

                public FromClientReceiveMsgCallBackEventArgs(Guid serverHandle, Guid clientHandle,byte[] msgBytes,bool success,string error)
                {
                    TCPServerHandle = serverHandle;
                    TCPClientHandle = clientHandle;
                    this.msgBytes = msgBytes;
                    Success = success;
                    Error = error;
                }
            }
            /// <summary>
            /// 向客户端发送消息
            /// <remarks>
            /// 指定服务端客户端发送
            /// </remarks>
            /// </summary>
            public sealed class ServerToClientMsgEventArgs : TCPServerSoketBaseEventArgs
            {
                //public readonly TCPServertToClientMsg msgContainer;
                /// <summary>
                /// 消息发送Token，用于本机发送完成回调唯一标记
                /// </summary>
                public Guid MsgToken{ get; private set; }
                /// <summary>
                /// 消息数据
                /// </summary>
                public byte[] data{ get; private set; }
                /// <summary>
                /// 消息服务器Handle
                /// </summary>
                public Guid ServerHandle{ get; private set; }
        
                /// <summary>
                /// 消息客户端Handle
                /// </summary>
                public Guid ClientHandle{ get; private set; }
                
                /// <summary>
                /// 构造函数
                /// </summary>
                public ServerToClientMsgEventArgs(Guid msgToken,byte[] data,Guid serverHandle,Guid clientHandle)
                {
                    MsgToken = msgToken;
                    this.data = data;
                    ServerHandle = serverHandle;
                    ClientHandle = clientHandle;
                }
                
            }
            /// <summary>
            /// 向所有服务端连上来的所有客户端发送消息
            /// </summary>
            public sealed class SendMsgToAllServerAllClient : TCPServerSoketBaseEventArgs
            {
                /// <summary>
                /// 消息数据
                /// </summary>
                public byte[] data{ get; private set; }
                
                /// <summary>
                /// 构造函数
                /// </summary>
                public SendMsgToAllServerAllClient(byte[] data)
                {
                    this.data = data;
                }
            }
            /// <summary>
            /// 通过指定服务端向连上来的所有客户端发送消息
            /// </summary>
            public sealed class SendMsgToServerAllClient : TCPServerSoketBaseEventArgs
            {
                /// <summary>
                /// 服务端Handle
                /// </summary>
                public Guid ServerHandle{ get; private set; }
                /// <summary>
                /// 消息数据
                /// </summary>
                public byte[] data{ get; private set; }
                
                /// <summary>
                /// 构造函数
                /// </summary>
                public SendMsgToServerAllClient(Guid serverHandle,byte[] data)
                {
                    ServerHandle = serverHandle;
                    this.data = data;
                }
            }
            /// <summary>
            /// 向客户端发送消息完成回调
            /// </summary>
            public sealed class SendMsgToClientCallBackEventArgs : TCPServerSoketBaseEventArgs
            {
                public TCPServertToClientMsgCallBack CallBack { get; private set; }
                /// <summary>
                /// 构造函数
                /// </summary>
                internal SendMsgToClientCallBackEventArgs(TCPServertToClientMsgCallBack callBack)
                {
                    CallBack = callBack;
                }
            }
}