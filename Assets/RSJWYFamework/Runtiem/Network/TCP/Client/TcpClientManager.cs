using System;
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime.Default.Manager
{
    /// <summary>
    /// 客户端的控制器
    /// </summary>
    public class TcpClientManager : ISocketTCPClientController
    {
        private TcpClientService tcpsocket;
        private bool reLock = false;
        private ISocketMsgBodyEncrypt m_SocketMsgBodyEncrypt; 
        public void Init()
        {
            reLock = false;
            Main.Main.EventModle.BindEventRecord<ClientSendToServerEventArgs>(ClientSendToServerMsg);
            Main.Main.AddLife(this);
            tcpsocket = new();
            tcpsocket.SocketTcpClientController = this;
            tcpsocket.m_SocketMsgEncode = new ProtobufSocketMsgEncode();
            tcpsocket.m_MsgBodyEncrypt = new ProtobufSocketMsgBodyEncrypt();
        }

        public void Close()
        {
            reLock = true;
            Main.Main.EventModle.UnBindEventRecord<ClientSendToServerEventArgs>(ClientSendToServerMsg);
            Main.Main.RemoveLife(this);
            tcpsocket?.Quit();
        }


        public void InitTCPClient(string ip = "127.0.0.1", int port = 6100)
        {
            //客户端的
            if (ip != "127.0.0.1")
            {
                //指定目标IP
                //检查IP和Port是否合法
                if (Utility.Utility.SocketTool.MatchIP(ip) && Utility.Utility.SocketTool.MatchPort(port))
                {
                    tcpsocket.Connect(ip, port);
                    return;
                }
            }
            else
            {
                //使用默认IP
                //检查Port是否合法
                if (Utility.Utility.SocketTool.MatchPort(port))
                {
                    tcpsocket.Connect("127.0.0.1", port);
                    return;
                }
            }

            //全部匹配失败，使用默认
            tcpsocket.Connect("127.0.0.1", 6000); //开启链接服务器
        }


        public void ClientSendToServerMsg(object sender, RecordEventArgsBase eventArgsBase)
        {
            if (eventArgsBase is ClientSendToServerEventArgs args)
                ClientSendToServerMsg(args.msgBase);
        }
        public void ClientSendToServerMsg(object msg)
        {
            tcpsocket?.SendMessage(msg as MsgBase);
        }

       
        public void ClientStatus(NetClientStatus eventEnum)
        {
            var _event= new ClientStatusEventArgs
            {
                Sender = this,
                netClientStatus = eventEnum
            };
            Main.Main.EventModle.Fire(_event);
            
        }

        public void ReceiveMsgCallBack(object msgBase)
        {
            var _event= new ClientReceivesMSGFromServer
            {
                Sender = null,
                msgBase = msgBase as MsgBase
            };
            Main.Main.EventModle.Fire(_event);
        }

        public void Update(float time, float deltaTime)
        {
            tcpsocket?.TCPUpdate();
        }

        public void UpdatePerSecond(float time)
        {
            if (tcpsocket.Status==NetClientStatus.Close||tcpsocket?.Status==NetClientStatus.Fail&&reLock==false)
            {
                RSJWYLogger.Warning($"检测到服务器链接关闭，重新连接服务器");
                tcpsocket.Connect();
            }
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

