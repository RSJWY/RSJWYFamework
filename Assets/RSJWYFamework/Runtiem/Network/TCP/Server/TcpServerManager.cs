using System;
using System.Net;
using RSJWYFamework.Runtime.Default.EventsLibrary;
using RSJWYFamework.Runtime.Event;
using RSJWYFamework.Runtime.Module;
using RSJWYFamework.Runtime.NetWork.Base;
using RSJWYFamework.Runtime.Network.Public;
using RSJWYFamework.Runtime.NetWork.TCP.Server;

namespace RSJWYFamework.Runtime.Default.Manager
{
    /// <summary>
    /// 服务器模块管理器，用于和Unity之间交互
    /// </summary>
    public class TcpServerManager : ISocketTCPServerController,ILife
    {
        private TcpServerService tcpsocket;

        private ISocketMsgBodyEncrypt m_SocketMsgBodyEncrypt; 
        
        public void Init()
        {
            Main.Main.EventModle.BindEventRecord<ServerToClientMsgEventArgs>(SendMsgToClientEvent);
            Main.Main.EventModle.BindEventRecord<ServerToClientMsgAllEventArgs>(SendMsgToClientAllEvent);
            Main.Main.AddLife(this);
            //检查是不是监听全部IP
            tcpsocket = new();
            tcpsocket.TcpServerController = this;
            tcpsocket.m_SocketMsgEncode = new ProtobufSocketMsgEncode();
            tcpsocket.m_MsgBodyEncrypt = new ProtobufSocketMsgBodyEncrypt();
        }

        public void Close()
        {
            Main.Main.EventModle.UnBindEventRecord<ServerToClientMsgEventArgs>(SendMsgToClientEvent);
            Main.Main.EventModle.UnBindEventRecord<ServerToClientMsgAllEventArgs>(SendMsgToClientAllEvent);
            Main.Main.RemoveLife(this);
            tcpsocket?.Quit();
        }


        public void InitServer(string ip = "any", int port = 6100)
        {
            if (ip != "any")
            {
                //指定IP
                //检查IP和Port是否合法
                if (Utility.Utility.SocketTool.MatchIP(ip) && Utility.Utility.SocketTool.MatchPort(port))
                {
                    tcpsocket.Init(ip, port);
                    return;
                }
            }
            else
            {
                //监听全部IP
                //检查Port是否合法
                if (Utility.Utility.SocketTool.MatchPort(port))
                {
                    tcpsocket.Init(IPAddress.Any, port);
                    return;
                }
            }

            //全部错误则使用默认参数
            tcpsocket.Init(IPAddress.Any, 6000);
        }

        /// <summary>
        /// 接收发送消息事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgsBase"></param>
        public void SendMsgToClientEvent(object sender, RecordEventArgsBase eventArgsBase)
        {
            if (eventArgsBase is ServerToClientMsgEventArgs args)
                SendMsgToClient(args.msgBase,args.ClientSocketToken);
        }
        /// <summary>
        /// 接收广播所有消息事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgsBase"></param>
        public void SendMsgToClientAllEvent(object sender, RecordEventArgsBase eventArgsBase)
        {
            if (eventArgsBase is not ServerToClientMsgAllEventArgs args) return;
            SendMsgToClientAll(args.msgBase);
        }

        public void SendMsgToClientAll(object msgBase)
        {
            foreach (var token in tcpsocket.ClientDic)
            {
                tcpsocket.SendMessage(msgBase as MsgBase, token.Value);
            }
        }
        public void SendMsgToClient(object msgBase,ClientSocketToken clientSocketToken)
        {
            tcpsocket?.SendMessage(msgBase as MsgBase,clientSocketToken);
        }


        public void ClientConnectedCallBack(ClientSocketToken clientSocketToken)
        {
            var _event = new ServerClientConnectedCallBackEventArgs
            {
                Sender = this,
                ClientSocketToken = clientSocketToken,
                msgBase = null
            };
            Main.Main.EventModle.Fire(_event);
        }

        public void CloseClientReCallBack(ClientSocketToken clientSocketToken)
        {
            var _event = new ServerCloseClientCallBackEventArgs
            {
                Sender = this,
                ClientSocketToken = clientSocketToken,
                msgBase = null
            };
            Main.Main.EventModle.Fire(_event);
        }

        public void ServerServiceStatus(NetServerStatus netServerStatus)
        {
            var _event = new ServerStatusEventArgs
            {
                Sender = this,
                status = netServerStatus
            };
            Main.Main.EventModle.Fire( _event);
        }

        public void FromClientReceiveMsgCallBack(ClientSocketToken clientSocketToken, object msgBase)
        {
            var _event= new FromClientReceiveMsgCallBackEventArgs
            {
                Sender = this,
                ClientSocketToken = clientSocketToken,
                msgBase = msgBase as MsgBase
            };
            Main.Main.EventModle.Fire(_event);
        }

        public void Update(float time, float deltaTime)
        {
            
        }

        public void UpdatePerSecond(float time)
        {
        }

        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }

        public uint Priority()
        {
            return 50;
        }
    }
}