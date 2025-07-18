namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 模块接口
    /// <remarks>继承了ILife接口，大部分情况下模块都需要生命周期</remarks>
    /// </summary>
    public interface IModule:ILife
    {
        /// <summary>
        /// 模块初始化
        /// </summary>
        void Initialize();
        /// <summary>
        /// 模块卸载
        /// </summary>
        void Shutdown();
    }

}