using System;
using UnityEngine;

namespace RSJWYFamework.Runtiem.Logger
{
    public class AppException : UnityException
    {
        // 构造函数，接受异常信息
        public AppException(string message) : base(message)
        {
        }
        public AppException(Exception inner) : base($"异常信息：{inner}")
        {
        }

        public AppException(string message, Exception inner) : base($"错误{inner}，异常信息：{message}")
        {
        }
    }
}