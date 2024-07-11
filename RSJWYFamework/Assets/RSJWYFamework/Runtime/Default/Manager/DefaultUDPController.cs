using System;
using System.Net;
using System.Text;
using RSJWYFamework.Runtime.Event;
using RSJWYFamework.Runtime.Module;
using RSJWYFamework.Runtime.NetWork.Base;
using RSJWYFamework.Runtime.NetWork.Event;
using RSJWYFamework.Runtime.NetWork.Public;
using RSJWYFamework.Runtime.NetWork.UDP;
using RSJWYFamework.Runtime.Utility;
using UDPSendMsg = RSJWYFamework.Runtime.NetWork.Event.UDPSendMsg;

namespace RSJWYFamework.Runtime.Default.Manager
{
    /// <summary>
    /// UDP控制器
    /// </summary>
    public class DefaultUDPController:ISocketUDPController,IModule
    {
        UDPService _udpService;
        
        
        public void Init()
        {
            Main.Main.Instance.GetModule<DefaultEvenManager>().BindEvent<UDPSendMsg>(UDPSendMsgEvent);
            _udpService = new();
            _udpService.SocketUDPController = this;
        }

        
        public void Close()
        {           
            Main.Main.Instance.GetModule<DefaultEvenManager>().BindEvent<UDPSendMsg>(UDPSendMsgEvent);
            _udpService?.Close();
        } 
        private void UDPSendMsgEvent(object sender, EventArgsBase e)
        {
            if (e is not UDPSendMsg args) return;
            if (Utility.Utility.SocketTool.MatchPort(args.port)&&Utility.Utility.SocketTool.MatchIP(args.ip))
            {
                var bytes = Encoding.UTF8.GetBytes(args.command.ToString());
                _udpService.SendUdpMessage(args.ip,args.port,bytes);
            }
        }
        public void InitListen(string ip = "any", int port = 5000)
        {
            string lowerip= ip.ToLower();
            //检查是不是监听全部IP
            if (lowerip != "any")
            {
                //指定IP
                //检查IP和Port是否合法
                if (Utility.Utility.SocketTool.MatchIP(ip) && Utility.Utility.SocketTool.MatchPort(port))
                {
                    _udpService.Init(ip, port);
                    return;
                }
            }
            else
            {
                //监听全部IP
                //检查Port是否合法
                if (Utility.Utility.SocketTool.MatchPort(port))
                {
                    _udpService.Init(IPAddress.Any, port);
                    return;
                }
            }
            //全部错误则使用默认参数
            _udpService.Init(IPAddress.Any, 5000);
        }

        public void ReceiveMsgCallBack(UDPReciveMsg ReciveMsg)
        {
            string _strUTF8 = Encoding.UTF8.GetString(ReciveMsg.Bytes);
            string _strHex = BitConverter.ToString(ReciveMsg.Bytes);

            string _utf8 = _strUTF8.Replace(" ", "");
            string _hex = _strHex.Replace("-", " ");
            //Debug.Log($"UTF8:{IsHex(_utf8)}，HEX:{IsHex(_hex)}");
            //优先检查UTF8
            var msg = new UDPReceiveMsgCallBack();
            msg.port = ReciveMsg.remoteEndPoint.Port;
            msg.ip = ReciveMsg.remoteEndPoint.Address.ToString();
            if (Utility.Utility.SocketTool.IsHex(_utf8))
            {
                //UTF8是正确的指令
                msg.command.Append(_utf8);
                Main.Main.Instance.GetModule<DefaultEvenManager>().SendEvent(this,msg);
            }
            else
            {
                //Hex任何时刻都是16进值，则传出，交给使用者判断
                msg.command.Append(_hex);
                Main.Main.Instance.GetModule<DefaultEvenManager>().SendEvent(this,msg);
            }
        }
    }
}