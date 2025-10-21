using System;
using System.Collections.Concurrent;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 对象池统计信息
    /// </summary>
    public class ObjectPoolStats
    {
        private readonly Func<int> _getPoolCount;
        private int _totalCreated;
        private int _totalDestroyed;
        private int _totalGets;
        private int _totalReleases;
        
        internal ObjectPoolStats(Func<int> getPoolCount)
        {
            _getPoolCount = getPoolCount ?? throw new ArgumentNullException(nameof(getPoolCount));
        }
        
        /// <summary>
        /// 总共创建的对象数量
        /// </summary>
        public int TotalCreated => _totalCreated;
        
        /// <summary>
        /// 总共销毁的对象数量
        /// </summary>
        public int TotalDestroyed => _totalDestroyed;
        
        /// <summary>
        /// 总共获取对象的次数
        /// </summary>
        public int TotalGets => _totalGets;
        
        /// <summary>
        /// 总共回收对象的次数
        /// </summary>
        public int TotalReleases => _totalReleases;
        
        /// <summary>
        /// 当前池中的对象数量
        /// </summary>
        public int CurrentPooled => _getPoolCount();
        
        /// <summary>
        /// 当前活跃的对象数量（已创建但未在池中的对象）
        /// </summary>
        public int CurrentActive => _totalCreated - _totalDestroyed - CurrentPooled;
        
        /// <summary>
        /// 池的命中率（从池中获取对象的比例）
        /// </summary>
        public float HitRate => _totalGets > 0 ? (float)(_totalGets - (_totalCreated - _totalDestroyed)) / _totalGets : 0f;
        
        /// <summary>
        /// 重置所有统计数据
        /// </summary>
        internal void Reset()
        {
            _totalCreated = 0;
            _totalDestroyed = 0;
            _totalGets = 0;
            _totalReleases = 0;
        }
        
        /// <summary>
        /// 记录对象创建
        /// </summary>
        internal void RecordCreate()
        {
            System.Threading.Interlocked.Increment(ref _totalCreated);
        }
        
        /// <summary>
        /// 记录对象销毁
        /// </summary>
        internal void RecordDestroy()
        {
            System.Threading.Interlocked.Increment(ref _totalDestroyed);
        }
        
        /// <summary>
        /// 记录对象获取
        /// </summary>
        internal void RecordGet()
        {
            System.Threading.Interlocked.Increment(ref _totalGets);
        }
        
        /// <summary>
        /// 记录对象回收
        /// </summary>
        internal void RecordRelease()
        {
            System.Threading.Interlocked.Increment(ref _totalReleases);
        }
        
        /// <summary>
        /// 获取统计信息的字符串表示
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public override string ToString()
        {
            return $"ObjectPoolStats: Created={TotalCreated}, Destroyed={TotalDestroyed}, " +
                   $"Gets={TotalGets}, Releases={TotalReleases}, Pooled={CurrentPooled}, " +
                   $"Active={CurrentActive}, HitRate={HitRate:P2}";
        }
    }
}