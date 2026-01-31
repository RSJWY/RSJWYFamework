using System;
using System.Collections.Generic;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 轻量级高性能对象池
    /// <para>Design Goals: Zero Allocation (Runtime), Fast Access (Stack), Safety Checks (Editor Only)</para>
    /// <para>注意：本类 <see cref="ObjectPool{T}"/> 非线程安全！仅限主线程使用，或确保同一时刻仅被一个线程访问。</para>
    /// </summary>
    /// <typeparam name="T">池化对象类型，必须是引用类型且包含无参构造函数</typeparam>
    public class ObjectPool<T> : IDisposable where T : class, new()
    {
        // 使用 Stack 而非 List 或 ConcurrentStack，以获得 O(1) 的存取速度和 LIFO 的缓存友好性
        private readonly Stack<T> _stack;
        
        // -------------------------------------------------------------------------
        // 生命周期回调委托
        // -------------------------------------------------------------------------
        
        /// <summary>当新对象被 new 出来时触发（仅执行一次）</summary>
        private readonly Action<T> _onCreate;
        
        /// <summary>当对象从池中取出时触发（每次 Get 都会执行）</summary>
        private readonly Action<T> _onGet;
        
        /// <summary>当对象归还到池中时触发（每次 Release 都会执行）</summary>
        private readonly Action<T> _onRelease;
        
        /// <summary>当对象被销毁时触发（池满或 Dispose 时执行）</summary>
        private readonly Action<T> _onDestroy;
        
        // -------------------------------------------------------------------------
        // 内部状态
        // -------------------------------------------------------------------------
        
        /// <summary>最大容量限制，超过此数量的回收请求将直接触发 Destroy</summary>
        private readonly int _maxSize;

#if UNITY_EDITOR
        // -------------------------------------------------------------------------
        // 编辑器专用调试信息 (Editor Only Debugging)
        // -------------------------------------------------------------------------
        
        /// <summary>追踪已借出的对象集合，用于检测“重复回收”和“回收未借出对象”的错误</summary>
        private readonly HashSet<T> _tracked;
        
        /// <summary>生命周期内创建的对象总数（包括池内和池外）</summary>
        private int _countAll;
        
        /// <summary>当前池内闲置对象的数量</summary>
        public int CountInactive => _stack.Count;
        
        /// <summary>当前已借出（活跃）对象的数量</summary>
        public int CountActive => _countAll - _stack.Count;
#else
        /// <summary>当前池内闲置对象的数量</summary>
        public int CountInactive => _stack.Count;
#endif

        /// <summary>
        /// 初始化对象池
        /// </summary>
        /// <param name="onCreate">创建新对象时的回调（例如：加载资源、初始化组件）</param>
        /// <param name="onGet">获取对象时的回调（例如：SetActive(true)、重置状态）</param>
        /// <param name="onRelease">回收对象时的回调（例如：SetActive(false)、解除引用）</param>
        /// <param name="onDestroy">销毁对象时的回调（例如：Destroy(gameObject)、释放非托管资源）</param>
        /// <param name="initSize">初始容量（预先创建并填充池子，避免运行时突发 GC）</param>
        /// <param name="maxSize">最大容量（超过此限制的对象回收时将被直接销毁，防止内存无限增长）</param>
        public ObjectPool(
            Action<T> onCreate = null,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int initSize = 0,
            int maxSize = 10000)
        {
            _onCreate = onCreate;
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxSize = maxSize;

            // 预分配 Stack 内存，容量至少为 16，避免频繁扩容（Resize）
            _stack = new Stack<T>(Math.Max(initSize, 16));

#if UNITY_EDITOR
            _tracked = new HashSet<T>();
#endif
            
            // 执行预热逻辑
            if (initSize > 0)
            {
                Prewarm(initSize);
            }
        }

        /// <summary>
        /// 预热池子
        /// <para>一次性创建指定数量的对象并填满池子</para>
        /// </summary>
        /// <param name="count">预热数量</param>
        private void Prewarm(int count)
        {
            if (count <= 0) return;
            
            // 临时数组用于存放创建的对象，避免边 Create 边 Push 导致逻辑复杂
            var temp = new T[count];
            for (int i = 0; i < count; i++)
            {
                temp[i] = Create();
            }
            
            // 统一回收进池
            for (int i = 0; i < count; i++)
            {
                Release(temp[i]);
            }
        }

        /// <summary>
        /// 内部创建方法
        /// </summary>
        private T Create()
        {
            var element = new T();
            _onCreate?.Invoke(element);
#if UNITY_EDITOR
            _countAll++;
#endif
            return element;
        }

        /// <summary>
        /// 从池中获取一个对象
        /// <para>复杂度：O(1)</para>
        /// </summary>
        /// <returns>可用的对象实例</returns>
        public T Get()
        {
            T element;
            if (_stack.Count == 0)
            {
                // 池空，创建新对象
                element = Create();
            }
            else
            {
                // 池不为空，弹出栈顶对象
                element = _stack.Pop();
            }

            // 执行获取回调（如 Reset、Enable）
            _onGet?.Invoke(element);
            
#if UNITY_EDITOR
            // 加入追踪集合，标记为“已借出”
            _tracked.Add(element);
#endif
            return element;
        }

        /// <summary>
        /// 将对象归还回池中
        /// <para>复杂度：O(1)</para>
        /// </summary>
        /// <param name="element">要归还的对象</param>
        public void Release(T element)
        {
#if UNITY_EDITOR
            // ---------------------------------------------------------
            // 调试检查：防止脏数据污染池子
            // ---------------------------------------------------------
            if (element == null)
            {
                AppLogger.Error("[ObjectPool] 试图回收 null 对象，操作已忽略。");
                return;
            }
            
            // 检查1：栈顶检查（最简单的重复回收检查）
            if (_stack.Count > 0 && ReferenceEquals(_stack.Peek(), element))
            {
                 AppLogger.Error("[ObjectPool] 致命错误：试图重复回收同一个对象（该对象位于栈顶）。");
                 return;
            }
            
            // 检查2：全量检查（确保对象确实是“借出”状态）
            if (!_tracked.Remove(element))
            {
                 AppLogger.Error("[ObjectPool] 警告：试图回收一个未被追踪的对象。\n" +
                                   "可能原因：1. 重复回收（Double Free）；\n" +
                                   "          2. 回收了不属于此池的对象；\n" +
                                   "          3. 该对象之前已被 Dispose 清理。");
                 // 注意：虽然报错，但为了鲁棒性，Release 模式下我们通常允许“收留”野生对象，
                 // 只要不超过 MaxSize。但在 Editor 下我们提示这个风险。
            }
#endif

            // 容量检查
            if (_stack.Count < _maxSize)
            {
                // 执行回收回调（如 Disable）
                _onRelease?.Invoke(element);
                _stack.Push(element);
            }
            else
            {
                // 池已满，直接销毁，避免内存无限增长
                _onDestroy?.Invoke(element);
#if UNITY_EDITOR
                _countAll--; // 修正总计数
#endif
            }
        }

        /// <summary>
        /// 清空池中所有对象
        /// <para>将调用 <see cref="_onDestroy"/> 回调并清空栈</para>
        /// </summary>
        public void Clear()
        {
            // 如果有销毁回调，则遍历调用
            if (_onDestroy != null)
            {
                foreach (var item in _stack)
                {
                    _onDestroy(item);
                }
            }
            
            _stack.Clear();
            
#if UNITY_EDITOR
            _countAll = 0; // 重置计数（注意：此时外部仍活跃的对象将变为“野生”状态）
            _tracked.Clear();
#endif
        }

        /// <summary>
        /// 销毁对象池
        /// <para>释放所有资源，等同于 <see cref="Clear"/></para>
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
    }
}
