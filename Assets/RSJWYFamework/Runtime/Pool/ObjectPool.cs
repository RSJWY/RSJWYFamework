using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 增强版对象池
    /// </summary>
    public class ObjectPool<T> : IDisposable where T : class, new()
    {
        /// <summary>
        /// 池子
        /// </summary>
        protected readonly ConcurrentStack<T> _objectQueue = new();
        
        /// <summary>
        /// 对象追踪集合（用于验证回收的对象是否来自当前池）
        /// </summary>
        protected readonly HashSet<T> _trackedObjects = new();
        
        /// <summary>
        /// 追踪对象的锁
        /// </summary>
        protected readonly object _trackingLock = new object();
        
        /// <summary>
        /// 配置信息
        /// </summary>
        protected readonly ObjectPoolConfig _config;
        
        /// <summary>
        /// 统计信息
        /// </summary>
        protected readonly ObjectPoolStats _stats;
        
        /// <summary>
        /// 是否已释放
        /// </summary>
        protected bool _disposed = false;

        /// <summary>
        /// 池子里当前未使用物体数量
        /// </summary>
        public int Count => _objectQueue.Count;
        
        /// <summary>
        /// 创建物体时回调
        /// </summary>
        protected readonly Action<T> _onCreate;
        
        /// <summary>
        /// 销毁物体时回调
        /// </summary>
        protected readonly Action<T> _onDestroy;
        
        /// <summary>
        /// 获取物体时回调
        /// </summary>
        protected readonly Action<T> _onGet;
        
        /// <summary>
        /// 回收物体时回调
        /// </summary>
        protected readonly Action<T> _onRelease;
        
        /// <summary>
        /// 创建池（使用默认配置）
        /// </summary>
        /// <param name="onCreate">创建时执行的事件</param>
        /// <param name="onDestroy">销毁时执行的事件</param>
        /// <param name="onGet">获取时执行的事件</param>
        /// <param name="onRelease">回收物体时执行的回调</param>
        /// <param name="limit">最大数量限制</param>
        /// <param name="initCount">初始化数量</param>
        public ObjectPool(Action<T> onCreate, Action<T> onDestroy, Action<T> onGet, Action<T> onRelease,
            int limit, int initCount) 
            : this(onCreate, onDestroy, onGet, onRelease, new ObjectPoolConfig 
            { 
                MaxSize = limit, 
                InitialSize = initCount 
            })
        {
        }
        
        /// <summary>
        /// 创建池（使用配置对象）
        /// </summary>
        /// <param name="onCreate">创建时执行的事件</param>
        /// <param name="onDestroy">销毁时执行的事件</param>
        /// <param name="onGet">获取时执行的事件</param>
        /// <param name="onRelease">回收物体时执行的回调</param>
        /// <param name="config">配置对象</param>
        public ObjectPool(Action<T> onCreate, Action<T> onDestroy, Action<T> onGet, Action<T> onRelease,
            ObjectPoolConfig config = null)
        {
            _config = config ?? ObjectPoolConfig.Default;
            
            if (!_config.IsValid())
            {
                throw new ArgumentException("对象池配置无效", nameof(config));
            }
            
            _onCreate = onCreate;
            _onDestroy = onDestroy;
            _onGet = onGet;
            _onRelease = onRelease;
            
            // 创建统计对象
            _stats = _config.EnableStats ? new ObjectPoolStats(() => _objectQueue.Count) : null;
            
            // 预创建对象
            Prewarm(_config.InitialSize);
        }
        
        /// <summary>
        /// 预热池子，预先创建指定数量的对象
        /// </summary>
        /// <param name="count">预创建的对象数量</param>
        public virtual void Prewarm(int count)
        {
            if (count <= 0) return;
            
            var items = new List<T>();
            for (int i = 0; i < count && i < _config.MaxSize; i++)
            {
                items.Add(Get());
            }
            
            foreach (var item in items)
            {
                Release(item);
            }
        }
        
        /// <summary>
        /// 获取一个对象池内的对象
        /// </summary>
        /// <returns></returns>
        public virtual T Get()
        {
            ThrowIfDisposed();
            
            T item;
            bool fromPool = _objectQueue.TryPop(out item);
            
            if (!fromPool)
            {
                // 池中没有对象，创建新的
                item = new T();
                SafeInvoke(_onCreate, item, "OnCreate");
                _stats?.RecordCreate();
                
                // 添加到追踪集合
                if (_config.EnableTracking)
                {
                    lock (_trackingLock)
                    {
                        //记录到追踪集合，记录对象被借出去了
                        _trackedObjects.Add(item);
                    }
                }
            }
            
            SafeInvoke(_onGet, item, "OnGet");
            _stats?.RecordGet();
            
            return item;
        }

        /// <summary>
        /// 回收一个对象
        /// </summary>
        public virtual void Release(T item)
        {
            ThrowIfDisposed();
            
            if (item == null)
            {
                if (_config.LogCallbackExceptions)
                {
                    AppLogger.Warning("试图把一个空对象放到对象池中");
                }
                return;
            }
            
            // 验证对象是否来自当前池
            if (_config.EnableTracking)
            {
                lock (_trackingLock)
                {
                    //检查外部返还的对象是不是当前对象池借出去的对象
                    if (!_trackedObjects.Contains(item))
                    {
                        if (_config.LogCallbackExceptions)
                        {
                            AppLogger.Warning("试图回收一个不属于当前对象池的对象");
                        }
                        return;
                    }
                }
            }
            
            if (_objectQueue.Count < _config.MaxSize)
            {
                SafeInvoke(_onRelease, item, "OnRelease");
                _objectQueue.Push(item);
                _stats?.RecordRelease();
            }
            else
            {
                // 池已满，销毁对象
                SafeInvoke(_onDestroy, item, "OnDestroy");
                _stats?.RecordDestroy();
                
                // 从追踪集合中移除
                if (_config.EnableTracking)
                {
                    lock (_trackingLock)
                    {
                        _trackedObjects.Remove(item);
                    }
                }
            }
        }
        
        /// <summary>
        /// 清空池中的对象（不影响已借出的对象）
        /// </summary>
        public virtual void Clear()
        {
            ThrowIfDisposed();
            
            // 记录要从追踪集合中移除的对象
            var objectsToRemoveFromTracking = new List<T>();
            
            while (_objectQueue.TryPop(out var obj))
            {
                SafeInvoke(_onDestroy, obj, "OnDestroy");
                _stats?.RecordDestroy();
                objectsToRemoveFromTracking.Add(obj);
            }
            
            _objectQueue.Clear();
            
            // 只从追踪集合中移除已销毁的对象，保留已借出对象的追踪信息
            if (_config.EnableTracking)
            {
                lock (_trackingLock)
                {
                    foreach (var obj in objectsToRemoveFromTracking)
                    {
                        _trackedObjects.Remove(obj);
                    }
                }
            }
        }
        
        /// <summary>
        /// 强制清空所有对象，包括已借出的对象追踪信息
        /// 警告：调用此方法后，已借出的对象将无法正常回收
        /// </summary>
        public virtual void ClearAll()
        {
            ThrowIfDisposed();
            
            while (_objectQueue.TryPop(out var obj))
            {
                SafeInvoke(_onDestroy, obj, "OnDestroy");
                _stats?.RecordDestroy();
            }
            
            _objectQueue.Clear();
            
            if (_config.EnableTracking)
            {
                lock (_trackingLock)
                {
                    _trackedObjects.Clear();
                }
            }
        }
        
        /// <summary>
        /// 获取统计信息
        /// </summary>
        /// <returns>统计信息，如果未启用统计则返回null</returns>
        public ObjectPoolStats GetStats()
        {
            return _stats;
        }
        
        /// <summary>
        /// 安全调用回调方法，捕获异常
        /// </summary>
        /// <param name="action">要调用的方法</param>
        /// <param name="item">参数对象</param>
        /// <param name="actionName">方法名称（用于日志）</param>
        protected void SafeInvoke(Action<T> action, T item, string actionName)
        {
            if (action == null) return;
            
            try
            {
                action.Invoke(item);
            }
            catch (Exception ex)
            {
                if (_config.LogCallbackExceptions)
                {
                    AppLogger.Error($"对象池回调 {actionName} 执行失败: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 检查是否已释放，如果已释放则抛出异常
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ObjectPool<T>));
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Clear();
                _disposed = true;
            }
        }
    }
}