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
        public bool Bind(string ip, int port)
        {
            if (Utility.SocketTool.MatchIP(ip) && Utility.SocketTool.MatchPort(port))
            {
                udpService = new UDPService(ip, port,this);
                return udpService.Bind();
            }
            else
            {
                AppLogger.Error($"监听信息不合法！！{ip}:{port}");
                return false;
            }
        }
        /// <summary>
        /// 关闭
        /// </summary>
        public void CloseUDPService()
        {
            udpService?.Close();
        }
        /// <summary>
        /// 接收数据进行广播
        /// </summary>
        /// <param name="udpReciveMsg"></param>
        internal void ReciveMsgCallBack(UDPReciveMsg udpReciveMsg)
        {
            ModuleManager.GetModule<EventManager>().Fire(new UDPReciveMsgEventArgs
            {
                Sender = this,
                UDPReciveMsg = udpReciveMsg
            });
        }

        /// <summary>
        /// 消息发送完成后的信息，返回是否发送成功
        /// </summary>
        /// <param name="udpSendCallBack"></param>
        internal void SendMsgCallBack(UDPSendCallBack udpSendCallBack)
        {
            ModuleManager.GetModule<EventManager>().Fire(new UDPSendCallBackEventArgs
            {
                Sender = this,
                UDPSendCallBack = udpSendCallBack
            });
        }
        
        
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="udpSendMsg"></param>
        /// <returns></returns>
        public bool SendUdpMessage(UDPSendMsg udpSendMsg)
        {
            return udpService.SendUdpMessage(udpSendMsg);
        }
        void OnSendMessage(object sender, EventArgsBase eventArgsBase)
        {
            if (eventArgsBase is UDPSendMsgEventArgs args)
            {
                SendUdpMessage(args.UDPSendMsg);
            }
        }
        public override void Initialize()
        {
            ModuleManager.GetModule<EventManager>().BindEvent<UDPSendMsgEventArgs>(OnSendMessage);
        }
        
        public override void Shutdown()
        {
            ModuleManager.GetModule<EventManager>().UnBindEvent<UDPSendMsgEventArgs>(OnSendMessage);
            CloseUDPService();
        }

        public override void LifeUpdate()
        {
        }
    }
    
}