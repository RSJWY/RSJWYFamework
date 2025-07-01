using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// UDP控制器
    /// </summary>
    [Module(100)]
    public class UDPManager:ModuleBase
    {
        /// <summary>
        /// UDP服务字典
        /// </summary>
        private UDPService udpService;


        /// <summary>
        /// 创建一个UDP服务
        /// </summary>
        /// <returns></returns>
        public bool CreateUDPService(string ip, int port)
        {
            if (Utility.SocketTool.MatchIP(ip) && Utility.SocketTool.MatchPort(port))
            {
                udpService = new UDPService(ip, port);
                udpService.ReceiveMsgCallBack += ((_udpReciveMsg) =>
                {
                    ModuleManager.GetModule<EventManager>().Fire(new UDPReciveMsgEventArgs
                    {
                        Sender = this,
                        UDPReciveMsg = _udpReciveMsg
                    });
                });
                udpService.SendCallBack += ((_udpSendCallBack) =>
                {
                    ModuleManager.GetModule<EventManager>().Fire(new UDPSendCallBackEventArgs
                    {
                        Sender = this,
                        UDPSendCallBack = _udpSendCallBack
                    });
                });
                return udpService.Bind();
            }
            else
            {
                AppLogger.Error($"监听信息不合法！！{ip}:{port}");
                return false;
            }
        }
        
        public void CloseUDPService()
        {
            udpService?.Close();
        }

        public bool SendUdpMessage(UDPSendMsg udpSendMsg)
        {
            return udpService.SendUdpMessage(udpSendMsg);
        }
        private void Callback(object sender, EventArgsBase eventArgsBase)
        {
            if (eventArgsBase is UDPSendMsgEventArgs args)
            {
                SendUdpMessage(args.UDPSendMsg);
            }
        }
        public override void Initialize()
        {
            ModuleManager.GetModule<EventManager>().BindEvent<UDPSendMsgEventArgs>(Callback);
        }
        
        public override void Shutdown()
        {
            ModuleManager.GetModule<EventManager>().UnBindEvent<UDPSendMsgEventArgs>(Callback);
        }

        public override void ModuleUpdate()
        {
        }
    }
    
}