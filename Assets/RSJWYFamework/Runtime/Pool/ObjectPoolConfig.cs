using System;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 对象池配置
    /// </summary>
    [Serializable]
    public class ObjectPoolConfig
    {
        /// <summary>
        /// 池子最大容量
        /// </summary>
        public int MaxSize { get; set; } = 100;
        
        /// <summary>
        /// 初始化时预创建的对象数量
        /// </summary>
        public int InitialSize { get; set; } = 0;
        
        /// <summary>
        /// 是否启用对象追踪（用于验证回收的对象是否来自当前池）
        /// </summary>
        public bool EnableTracking { get; set; } = true;
        
        /// <summary>
        /// 是否启用统计功能
        /// </summary>
        public bool EnableStats { get; set; } = true;
        
        /// <summary>
        /// 是否在回调异常时记录日志
        /// </summary>
        public bool LogCallbackExceptions { get; set; } = true;
        
        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        /// <returns>配置是否有效</returns>
        public bool IsValid()
        {
            return MaxSize > 0 && InitialSize >= 0 && InitialSize <= MaxSize;
        }
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认配置实例</returns>
        public static ObjectPoolConfig Default => new ObjectPoolConfig();
    }
}