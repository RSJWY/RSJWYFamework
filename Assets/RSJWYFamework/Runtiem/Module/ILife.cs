using System.Reflection;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 只需要生命周期的接口
    /// </summary>
    public interface ILife
    {
        int Priority { get; }
        void LifeUpdate();
        void LifePerSecondUpdate();
        void LifeFixedUpdate();
        void LifeLateUpdate();
    }
} 