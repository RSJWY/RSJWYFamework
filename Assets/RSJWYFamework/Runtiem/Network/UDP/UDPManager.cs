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
        private Dictionary<string, UDPService> UDPServiceDict = new();


        /// <summary>
        /// 创建一个UDP服务
        /// </summary>
        /// <returns></returns>
        public CreateUDPService CreateUDPService(string ip, int port,string token="")
        {
            if (UDPServiceDict.ContainsKey(token))
            {
                AppLogger.Error($"已经存在Token：{token}，创建终止");
                return new CreateUDPService
                {
                    isSuccess = false,
                    Token = string.Empty
                };
            }
            if (Utility.SocketTool.MatchIP(ip) && Utility.SocketTool.MatchPort(port))
            {
                var udpService = new UDPService(ip, port);
                udpService.UDPServiceToken = token;
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
                var isSuccess = udpService.Bind();
                if (isSuccess)
                {
                    if (string.IsNullOrEmpty(token))
                    {
                        token = Utility.Timestamp.UnixTimestampMilliseconds.ToString();
                    }
                    UDPServiceDict.Add(token, udpService);
                    return new CreateUDPService
                    {
                        isSuccess = true,
                        Token = token
                    };
                }
                else
                {
                    return new CreateUDPService
                    {
                        isSuccess = true,
                        Token = string.Empty
                    };
                }
            }
            else
            {
                AppLogger.Error($"监听信息不合法！！{ip}:{port}");
                return new CreateUDPService
                {
                    isSuccess = false,
                    Token = string.Empty
                };
            }
        }
        
        public void CloseUDPService(string token = "")
        {
            if (string.IsNullOrEmpty(token))
            {
                AppLogger.Warning($"传入非法Token");
                return;
            }
            UDPServiceDict[token].Close();
            UDPServiceDict.Remove(token);
        }

        public bool SendUdpMessage(UDPSendMsg udpSendMsg)
        {
            if (UDPServiceDict.ContainsKey(udpSendMsg.UDPServiceToken))
            {
                return UDPServiceDict[udpSendMsg.UDPServiceToken].SendUdpMessage(udpSendMsg);
            }
            else
            {
                AppLogger.Warning($"UDPSevice服务类：{udpSendMsg.UDPServiceToken}不存在！数据丢弃");
                return false;
            }
            
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
            UDPServiceDict.Clear();
            ModuleManager.GetModule<EventManager>().BindEvent<UDPSendMsgEventArgs>(Callback);
        }
        
        public override void Shutdown()
        {
            UDPServiceDict.Clear();
            ModuleManager.GetModule<EventManager>().UnBindEvent<UDPSendMsgEventArgs>(Callback);
        }

        public override void ModuleUpdate()
        {
        }
    }

    public struct CreateUDPService
    {
        /// <summary>
        /// 是否创建成功
        /// </summary>
        public bool isSuccess;
        /// <summary>
        /// 创建成功后的ID
        /// </summary>
        public string Token;
    }
}