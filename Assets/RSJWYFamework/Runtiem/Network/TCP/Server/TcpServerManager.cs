using System;
using System.Net;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 服务器模块管理器，用于和Unity之间交互
    /// </summary>
    [Module(100)]
    public class TcpServerManager :ModuleBase
    {
        private TcpServerService tcpsocket;
        


        public void Bind(string ip , int port)
        {
            if (Utility.SocketTool.MatchIP(ip) && Utility.SocketTool.MatchPort(port))
            {
                tcpsocket=new TcpServerService(ip,port,this);
                tcpsocket.Bind();
            }
        }

        /// <summary>
        /// 接收发送消息事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgsBase"></param>
        public void SendMsgToClientEvent(object sender, EventArgsBase eventArgsBase)
        {
            if (eventArgsBase is ServerToClientMsgEventArgs args)
                SendMsgToClient(args.msgBase,args.ClientSocketToken);
        }
        /// <summary>
        /// 接收广播所有消息事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgsBase"></param>
        public void SendMsgToClientAllEvent(object sender, EventArgsBase eventArgsBase)
        {
            if (eventArgsBase is not ServerToClientMsgAllEventArgs args) return;
            SendMsgToClientAll(args.msgBase);
        }
        /// <summary>
        /// 向所有连接上来设备广播消息
        /// </summary>
        /// <param name="msgBase"></param>
        public void SendMsgToClientAll(byte[] msgBase)
        {
            foreach (var token in tcpsocket.ClientDic)
            {
                tcpsocket.SendMessage(msgBase, token.Value);
            }
        }
        /// <summary>
        /// 向指定客户端发送消息
        /// </summary>
        /// <param name="msgBase"></param>
        /// <param name="clientSocketToken"></param>
        public void SendMsgToClient(byte[]  msgBase,ClientSocketToken clientSocketToken)
        {
            tcpsocket?.SendMessage(msgBase,clientSocketToken);
        }


        /// <summary>
        /// 客户端链接上来
        /// </summary>
        /// <param name="clientSocketToken"></param>
        public void ClientConnectedCallBack(ClientSocketToken clientSocketToken)
        {
            var _event = new ServerClientConnectedCallBackEventArgs
            {
                Sender = this,
                ClientSocketToken = clientSocketToken,
                msgBase = null
            };
            ModuleManager.GetModule<EventManager>().Fire(_event);
        }

        /// <summary>
        /// 客户端离线
        /// </summary>
        /// <param name="clientSocketToken"></param>
        public void CloseClientReCallBack(ClientSocketToken clientSocketToken)
        {
            var _event = new ServerCloseClientCallBackEventArgs
            {
                Sender = this,
                ClientSocketToken = clientSocketToken,
                msgBase = null
            };
            ModuleManager.GetModule<EventManager>().Fire(_event);
        }

        /// <summary>
        /// 服务端状态更新
        /// </summary>
        /// <param name="netServerStatus"></param>
        public void ServerServiceStatus(NetServerStatus netServerStatus)
        {
            var _event = new ServerStatusEventArgs
            {
                Sender = this,
                status = netServerStatus
            };
            ModuleManager.GetModule<EventManager>().Fire(_event);
        }

        public void FromClientReceiveMsgCallBack(ClientSocketToken clientSocketToken, byte[] msgBase)
        {
            var _event= new FromClientReceiveMsgCallBackEventArgs
            {
                Sender = this,
                ClientSocketToken = clientSocketToken,
                msgBase = msgBase
            };
            ModuleManager.GetModule<EventManager>().Fire(_event);
        }



        public override void Initialize()
        {
            ModuleManager.GetModule<EventManager>().BindEvent<ServerToClientMsgEventArgs>(SendMsgToClientEvent);
            ModuleManager.GetModule<EventManager>().BindEvent<ServerToClientMsgAllEventArgs>(SendMsgToClientAllEvent);
        }

        public override void Shutdown()
        {
            ModuleManager.GetModule<EventManager>().UnBindEvent<ServerToClientMsgEventArgs>(SendMsgToClientEvent);
            ModuleManager.GetModule<EventManager>().UnBindEvent<ServerToClientMsgAllEventArgs>(SendMsgToClientAllEvent);
            tcpsocket?.Quit();
        }

        public override void LifeUpdate()
        {
        }
    }
}