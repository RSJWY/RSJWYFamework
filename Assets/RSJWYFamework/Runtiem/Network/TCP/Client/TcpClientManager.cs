using System;
using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 客户端的控制器
    /// </summary>
    [Module()]
    public class TcpClientManager : ModuleBase
    {
        private TcpClientService tcpsocket;
        private readonly ConcurrentDictionary<Guid, TcpClientService> tcpClientDic = new();
        
        
        public override void Initialize()
        {
            ModuleManager.GetModule<EventManager>().BindEvent<ClientSendToServerEventArgs>(ClientSendToServerMsg);
        }

        public override void Shutdown()
        { 
            ModuleManager.GetModule<EventManager>().BindEvent<ClientSendToServerEventArgs>(ClientSendToServerMsg);
            tcpsocket?.Quit();
        }
        /// <summary>
        /// 客户端是否存在
        /// </summary>
        public bool IsExistServer(Guid serverHandle)
        {
            return tcpClientDic.ContainsKey(serverHandle);
        }
        
        public Guid Bind(string ip , int port,ISocketMsgBodyEncrypt socketMsgBodyEncrypt)
        {
            try
            {
                if (!Utility.SocketTool.MatchIP(ip) || !Utility.SocketTool.MatchPort(port))
                {
                    AppLogger.Error($"无效的地址: {ip}:{port}");
                    return Guid.Empty;
                }
                var handle = Guid.NewGuid();
                var service = new TcpClientService(ip, port, this, handle, socketMsgBodyEncrypt);
                service.Connect();
                if (!tcpClientDic.TryAdd(handle,service))
                {
                    service.Close();
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

        /// <summary>
        /// 向服务器发送消息
        /// </summary>
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

        public override void LifeUpdate()
        {
            tcpsocket?.TCPUpdate();
        }

        public override void LifePerSecondUpdate()
        {

            if (tcpsocket?.Status==NetClientStatus.Close||tcpsocket?.Status==NetClientStatus.Fail)
            {
                AppLogger.Warning($"检测到服务器链接关闭，重新连接服务器");
                tcpsocket.Connect();
            }
        }
    }
}

