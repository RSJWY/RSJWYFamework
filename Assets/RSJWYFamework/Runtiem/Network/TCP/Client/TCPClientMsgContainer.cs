using System;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 客户端发送消息到服务端
    /// </summary>
    internal class SendClientToServerMsg
    {
        /// <summary>
        /// 消息发送Token，用于本机发送完成回调唯一标记
        /// </summary>
        public Guid MsgToken{get;private set;}

        /// <summary>
        /// 消息数据
        /// </summary>
        public ByteArrayMemory Data { get; private set; }

        public SendClientToServerMsg(ByteArrayMemory data,Guid msgToken)
        {
            MsgToken = msgToken;
            Data = data;
        }
    }
}