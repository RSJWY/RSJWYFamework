using System;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    
    /// <summary>
    /// 单例模式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SingletonBase<T> where T : class, new()
    {
        private static T _instance;
        private static readonly object _lockObject = new object();
 
        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (_lockObject)
                {
                    _instance ??= new T();
                }
                return _instance;
            }
        }
 
        // 防止子类直接调用构造函数
        protected SingletonBase()
        {
            if (_instance != null)
            {
                throw new Exception("单例类不应该直接被实例化。请使用<T>.Instance访问实例。");
            }
        }
    }
}