using System;
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 客户端的控制器
    /// </summary>
    [Module(100)]
    public class TcpClientManager : ModuleBase
    {
        private TcpClientService tcpsocket;
        private bool reLock = false;

        public void Bind(string ip , int port,ISocketMsgBodyEncrypt socketMsgBodyEncrypt)
        {
            if (Utility.SocketTool.MatchIP(ip) && Utility.SocketTool.MatchPort(port))
            {
                tcpsocket=new TcpClientService();
                tcpsocket.SocketTcpClientManager = this;
                tcpsocket.m_MsgBodyEncrypt = socketMsgBodyEncrypt;
                 tcpsocket.Connect();
            }
        }

        public void ClientSendToServerMsg(object sender, EventArgsBase eventArgsBase)
        {
            if (eventArgsBase is ClientSendToServerEventArgs args)
                ClientSendToServerMsg(args.msgBase);
        }
        public void ClientSendToServerMsg(byte[] msg)
        {
            tcpsocket?.SendMessage(msg);
        }

       
        public void ClientStatus(NetClientStatus eventEnum)
        {
            var _event= new ClientStatusEventArgs
            {
                Sender = this,
                netClientStatus = eventEnum
            };
            ModuleManager.GetModule<EventManager>().Fire(_event);
            
        }

        public void ReceiveMsgCallBack(byte[] msgBase)
        {
            var _event= new ClientReceivesMSGFromServer
            {
                Sender = null,
                msgBase = msgBase
            };
            ModuleManager.GetModule<EventManager>().Fire(_event);
        }

        public override void Initialize()
        {
            reLock = false;
            ModuleManager.GetModule<EventManager>().BindEvent<ClientSendToServerEventArgs>(ClientSendToServerMsg);
        }

        public override void Shutdown()
        { 
            reLock = true;
            ModuleManager.GetModule<EventManager>().BindEvent<ClientSendToServerEventArgs>(ClientSendToServerMsg);
            tcpsocket?.Quit();
        }

        public override void LifeUpdate()
        {
            tcpsocket?.TCPUpdate();
        }

        public override void LifePerSecondUpdate()
        {
            if (tcpsocket.Status==NetClientStatus.Close||tcpsocket?.Status==NetClientStatus.Fail&&reLock==false)
            {
                AppLogger.Warning($"检测到服务器链接关闭，重新连接服务器");
                tcpsocket.Connect();
            }
        }
    }
}

