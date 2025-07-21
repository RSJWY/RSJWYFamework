
namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 只需要生命周期的接口
    /// </summary>
    public interface ILife
    {
        /// <summary>
        /// 生命周期执行优先级
        /// </summary>
        int Priority { get; }
        void LifeUpdate();
        /// <summary>
        /// 受时间缩放影响
        /// </summary>
        void LifePerSecondUpdate();
        /// <summary>
        /// 不受时间缩放影响
        /// </summary>
        void LifePerSecondUpdateUnScaleTime();
        void LifeFixedUpdate();
        
        void LifeLateUpdate();
    }
} 