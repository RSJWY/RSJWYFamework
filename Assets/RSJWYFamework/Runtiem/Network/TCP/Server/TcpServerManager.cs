using System;
using System.Collections.Concurrent;
using System.Net;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 服务器模块管理器，用于和Unity之间交互
    /// </summary>
    [Module()]
    public class TcpServerManager :ModuleBase
    {
        /// <summary>
        /// TCP字典
        /// </summary>
        private readonly ConcurrentDictionary<Guid, TcpServerService> tcpServiceDic = new();


        public override void Initialize()
        {
            ModuleManager.GetModule<EventManager>().BindEvent<ServerToClientMsgEventArgs>(SendMsgToClientEvent);
            ModuleManager.GetModule<EventManager>().BindEvent<SendMsgToAllServerAllClient>(SendMsgToAllServerAllClientEvent);
            ModuleManager.GetModule<EventManager>().BindEvent<SendMsgToServerAllClient>(SendMsgToServerAllClientEvent);
        }


        public override void Shutdown()
        {
            ModuleManager.GetModule<EventManager>().UnBindEvent<ServerToClientMsgEventArgs>(SendMsgToClientEvent);
            ModuleManager.GetModule<EventManager>().UnBindEvent<SendMsgToAllServerAllClient>(SendMsgToAllServerAllClientEvent);
            ModuleManager.GetModule<EventManager>().UnBindEvent<SendMsgToServerAllClient>(SendMsgToServerAllClientEvent);
            CloseAllServer();
        }
        /// <summary>
        /// 服务端是否存在
        /// </summary>
        public bool IsExistServer(Guid serverHandle)
        {
            return tcpServiceDic.ContainsKey(serverHandle);
        }
        /// <summary>
        /// 客户端是否存在
        /// </summary>
        /// <remarks>注意，最好结合IsExistServer处理</remarks>
        /// <returns>如果服务端不存在，则直接返回false，需要结合IsExistServer处理</returns>
        public bool IsExistClient(Guid serverHandle, Guid clientHandle)
        {
            if (tcpServiceDic.TryGetValue(serverHandle, out var tcpService) )
            {
                return tcpService.ClientDic.ContainsKey(clientHandle);
            }
            return false;
        }

        
       
        /// <summary>
        /// 启动一个新的服务端
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public Guid Bind(string ip , int port)
        {
            try
            {
                if (!Utility.SocketTool.MatchIP(ip) || !Utility.SocketTool.MatchPort(port))
                {
                    AppLogger.Error($"无效的地址: {ip}:{port}");
                    return Guid.Empty;
                }
                var handle = Guid.NewGuid();
                var service = new TcpServerService(ip, port, this, handle);
                service.Bind();
                if (!tcpServiceDic.TryAdd(handle,service))
                {
                    service.CloseServer();
                    AppLogger.Error($"Handle冲突: {handle}");
                    return Guid.Empty;
                }
                return handle;
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Bind失败: {ex.Message}");
                return Guid.Empty;
            }
        }
        public override void LifeUpdate()
        {
        }
        
        
        /// <summary>
        /// 接收广播所有消息事件
        /// </summary>
        public void SendMsgToAllServerAllClientEvent(object sender, EventArgsBase eventArgsBase)
        {
            if (eventArgsBase is SendMsgToAllServerAllClient args) 
                SendMsgToAllServerAllClient(args.data);
        }
        /// <summary>
        /// 接收通过指定服务端向已连接设备广播消息
        /// </summary>
        private void SendMsgToServerAllClientEvent(object sender, EventArgsBase eventArgsBase)
        {
            if (eventArgsBase is SendMsgToServerAllClient args) 
                SendMsgToServerAllClient(args.data,args.ServerHandle);
        }
        /// <summary>
        /// 向所有连接上来设备所有客户端广播消息
        /// </summary>
        /// <param name="msgBytes"></param>
        public void SendMsgToAllServerAllClient(byte[] msgBytes)
        {
            foreach (var serveice in tcpServiceDic)
            {
                SendMsgToServerAllClient(msgBytes,serveice.Key);
            }
        }

        /// <summary>
        /// 向指定服务端的所有客户端广播消息
        /// </summary>
        /// <param name="msgBase"></param>
        public void SendMsgToServerAllClient(byte[] msgBytes,Guid serverHandle)
        {
            if (tcpServiceDic.TryGetValue(serverHandle, out var service))
            {
                service.SendToAllClientMessage(msgBytes);
            }
            else
            {
                AppLogger.Error($"服务端Handle不存在: {serverHandle}");
            }
        }
        
        /// <summary>
        /// 向指定客户端发送消息
        /// </summary>
        /// <param name="msgBase"></param>
        /// <param name="clientSocketContainer"></param>
        public void SendMsgToClient(TCPServertToClientMsg msgContainer)
        {
            if (tcpServiceDic.TryGetValue(msgContainer.ServerHandle,out var tcpServerService))
            {
                tcpServerService?.SendMessage(msgContainer.data,msgContainer.ClientHandle,msgContainer.MsgToken);
            }
            else
            {
                AppLogger.Error($"服务端Handle不存在: {msgContainer.ServerHandle}");
            }
        }
        
        /// <summary>
        /// 接收发送消息事件
        /// </summary>
        public void SendMsgToClientEvent(object sender, EventArgsBase eventArgsBase)
        {
            if (eventArgsBase is ServerToClientMsgEventArgs args)
                SendMsgToClient(args.msgContainer);
        }
        
        /// <summary>
        /// 客户端链接上来
        /// </summary>
        public void ClientConnectedCallBack(Guid serverHandle,Guid clientHandle)
        {
            var _event = new ServerClientConnectedCallBackEventArgs
            {
                Sender = this,
                ServerHandle = serverHandle,
                ClientHandle = clientHandle,
            };
            ModuleManager.GetModule<EventManager>().Fire(_event);
        }

        /// <summary>
        /// 客户端离线
        /// </summary>
        public void CloseClientReCallBack(Guid serverHandle,Guid clientHandle)
        {
            var _event = new ServerCloseClientCallBackEventArgs
            {
                Sender = this,
                ServerHandle = serverHandle,
                ClientHandle = clientHandle,
            };
            ModuleManager.GetModule<EventManager>().Fire(_event);
        }

        /// <summary>
        /// 服务端状态更新
        /// </summary>
        /// <param name="netServerStatus"></param>
        public void ServerServiceStatus(Guid serverHandle,NetServerStatus netServerStatus)
        {
            var _event = new ServerStatusEventArgs
            {
                Sender = this,
                ServerHandle = serverHandle,
                status = netServerStatus
            };
            ModuleManager.GetModule<EventManager>().Fire(_event);
        }

        internal void FromClientReceiveMsgCallBack(TCPClientToServerMsg msgContainer)
        {
            var _event= new FromClientReceiveMsgCallBackEventArgs
            {
                Sender = this,
                msgContainer = msgContainer
            };
            ModuleManager.GetModule<EventManager>().Fire(_event);
        }

        /// <summary>
        /// 关闭指定服务端
        /// </summary>
        /// <param name="serverHandle"></param>
        void CloseServer(Guid serverHandle)
        {
            if (tcpServiceDic.TryGetValue(serverHandle, out var tcpServerService))
            {
                tcpServerService.CloseServer();
                tcpServiceDic.TryRemove(serverHandle, out tcpServerService);
            }
        }
        /// <summary>
        /// 关闭所有服务端
        /// </summary>
        void CloseAllServer()
        {
            foreach (var server in tcpServiceDic)
            {
                server.Value.CloseServer();
            }
        }
    }
}