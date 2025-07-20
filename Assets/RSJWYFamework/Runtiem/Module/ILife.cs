
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
        void LifePerSecondUpdate();
        void LifeFixedUpdate();
        void LifeLateUpdate();
    }
} 