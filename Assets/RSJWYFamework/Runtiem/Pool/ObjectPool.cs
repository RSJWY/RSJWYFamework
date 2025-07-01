using System;
using System.Collections.Concurrent;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 对象池
    /// </summary>
    public class ObjectPool<T>where T:class,new()
    {
        /// <summary>
        /// 数量上限
        /// </summary>
        protected int _limit = 100;
        
        /// <summary>
        /// 池子
        /// </summary>
        protected ConcurrentStack<T> _objectQueue = new ();

        /// <summary>
        /// 池子里当前未使用物体数量
        /// </summary>
        protected int Count => _objectQueue.Count;
        /// <summary>
        /// 创建物体时回调
        /// </summary>
        protected Action<T> _onCreate;
        /// <summary>
        /// 销毁物体时回调
        /// </summary>
        protected Action<T> _onDestroy;
        /// <summary>
        /// 获取物体时回调
        /// </summary>
        protected Action<T> _onGet;
        /// <summary>
        /// 回收物体时回调
        /// </summary>
        protected Action<T> _onRelease;
        
        /// <summary>
        /// 创建池
        /// 创建时，如果初始数量大于最大数量，将以最大数量来初始化（取这两值最小值）
        /// </summary>
        /// <param name="limit">最大数量限制</param>
        /// <param name="initCount">初始化数量</param>
        /// <param name="onCreate">创建时执行的事件</param>
        /// <param name="onDestroy">销毁时执行的事件</param>
        /// <param name="onGet">获取时执行的事件</param>
        /// <param name="onRelease">回收物体时执行的回调</param>
        public ObjectPool(Action<T> onCreate,Action<T> onDestroy,Action<T> onGet,Action<T> onRelease,
            int limit,int initCount)
        {
            _limit = limit;
           _onCreate = onCreate;
           _onDestroy = onDestroy;
           _onGet = onGet;
           _onRelease = onRelease;

           var max = Math.Min(limit,initCount);
           for (int i = 0; i < max; i++)
           {
               var _obj= new T();
               _onCreate?.Invoke(_obj);
               _onRelease?.Invoke(_obj);
           }
        }
        /// <summary>
        /// 获取一个对象池内的对象
        /// </summary>
        /// <returns></returns>
        public virtual T Get()
        {
            if (_objectQueue.TryPop(out var popitem))
            {
                _onGet?.Invoke(popitem);
                return popitem;
            }
            else
            {
                var item= new T();
                _onCreate?.Invoke(item);
                _onGet?.Invoke(item);
                return item;
            }
        }

        /// <summary>
        /// 回收一个对象
        /// </summary>
        public virtual void Release(T item)
        {
            if (item == null)
            {
                AppLogger.Warning("试图把一个空对象放到对象池中");
                return;
            }
            if (_objectQueue.Count<_limit)
            {
                _onRelease?.Invoke(item);
                _objectQueue.Push(item);
            }
            else
            {
                _onDestroy?.Invoke(item);
                item = null;
            }
        }
        
        /// <summary>
        /// 清空所有对象
        /// </summary>
        public virtual void Clear()
        {
            while (_objectQueue.TryPop(out var _obj))
            {
                _obj = null;
            }
            _objectQueue.Clear();
        }
    }
}