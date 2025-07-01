namespace RSJWYFamework.Runtime
{
    public sealed class UDPReciveMsgEventArgs: EventArgsBase
    {
        public UDPReciveMsg UDPReciveMsg;
    }
    public sealed class UDPSendCallBackEventArgs: EventArgsBase
    {
        public UDPSendCallBack UDPSendCallBack;
    }
    public sealed class UDPSendMsgEventArgs: EventArgsBase
    {
        public UDPSendMsg UDPSendMsg;
    }
}