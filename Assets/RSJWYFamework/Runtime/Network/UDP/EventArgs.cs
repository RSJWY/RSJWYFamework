namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// UDP基本广播事件
    /// </summary>
    public abstract class UDPSoketBaseEventArgs:EventArgsBase
    {
                
    }
    public sealed class UDPSoketReciveMsgEventArgs: UDPSoketBaseEventArgs
    {
        public UDPReciveMsg UDPReciveMsg{get;internal set;}
    }
    public sealed class UDPSoketSendCallBackEventArgs: UDPSoketBaseEventArgs
    {
        public UDPSendCallBack UDPSendCallBack{get;internal set;}
    }
    public sealed class UDPSoketSendMsgEventArgs: UDPSoketBaseEventArgs
    {
        public UDPSendMsg UDPSendMsg{get;internal set;}
    }
}