namespace RSJWYFamework.Runtime
{
    public interface IModule
    {
        /// <summary>
        /// 模块初始化
        /// </summary>
        void Initialize();
        /// <summary>
        /// 模块卸载
        /// </summary>
        void Shutdown();
        /// <summary>
        /// 模块帧更新
        /// </summary>
        void ModuleUpdate();
        /// <summary>
        /// 模块秒更新
        /// </summary>
        void ModulePerSecondUpdate();
        void ModuleFixedUpdate();
        void ModuleLateUpdate();
    }

}