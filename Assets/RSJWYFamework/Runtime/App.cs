using RSJWYFamework.Runtime;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 🐶 全局应用访问入口 (Facade)
    /// <para>小码酱为您准备的快捷访问门面，再也不用写长长的 ModuleManager.GetModule 啦！</para>
    /// </summary>
    public static class App
    {
        /// <summary>
        /// 获取模块 (快捷方式)
        /// <para>性能说明：内部使用字典查找 (O(1))，速度飞快，请放心食用！🍗</para>
        /// </summary>
        public static T Get<T>() where T : class, IModule
        {
            return ModuleManager.GetModule<T>();
        }

        #region 核心模块快捷访问 (Lazy Dog 模式)

        // 🐶 TODO: 主人，如果您还有其他自定义模块，请按照下面的格式在这里添加哦！
        // public static YourModule Your => Get<YourModule>();

        public static EventManager Event => Get<EventManager>();
        public static DataManager Data => Get<DataManager>();
        public static AppConfigManager Config => Get<AppConfigManager>();
        public static YooAssetManager YooAsset => Get<YooAssetManager>();
        public static TimerExecutorManager Timer => Get<TimerExecutorManager>();
        public static ScreenManager Screen => Get<ScreenManager>();
        public static StateMachineManager StateMachine => Get<StateMachineManager>();
        public static HybirdCLRManager HybridCLR => Get<HybirdCLRManager>();
        
        #endregion
    }
}
