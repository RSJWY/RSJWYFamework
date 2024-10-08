﻿using System;
using RSJWYFamework.Runtime.ReferencePool;

namespace RSJWYFamework.Runtime.Event
{
    /// <summary>
    /// 事件内容载体
    /// </summary>
    public abstract class EventArgsBase:IReference
    {
        public abstract void Release();
        /// <summary>
        /// 消息发送者
        /// </summary>
        public object Sender;

    }
    /// <summary>
    /// 记录类型的事件内容载体
    /// </summary>
    public abstract record RecordEventArgsBase
    {
        /// <summary>
        /// 消息发送者
        /// </summary>
        public object Sender;
    }
}