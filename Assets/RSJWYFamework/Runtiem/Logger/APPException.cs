using System;
using System.Collections;
using UnityEngine;

namespace Assets.RSJWYFamework.Runtiem.Logger
{
    public class APPException : UnityException
    {
        // 构造函数，接受异常信息
        public APPException(string message) : base(message)
        {
        }
        public APPException(Exception inner) : base($"异常信息：{inner}")
        {
        }

        public APPException(string message, Exception inner) : base($"错误{inner}，异常信息：{message}")
        {
        }
    }
}