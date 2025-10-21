using UnityEngine;

namespace RSJWYFamework.Runtime
{
    [Module]
    [ModuleDependency(typeof(EventManager))]
    [ModuleDependency(typeof(DataManager))]
    public class AppConfigManager:ModuleBase
    {
        public AppConfig AppConfig { get; private set; }
        public override void Initialize()
        {
            AppConfig = ModuleManager.GetModule<DataManager>().GetFirstDataSB<AppConfig>();
            if (AppConfig != null)return;
            AppConfig=Resources.Load<AppConfig>("AppConfig");
            if (AppConfig == null)
            {
                Debug.LogError("AppConfig is null,请在Resources文件夹下创建AppConfig");
                return;
            }
            ModuleManager.GetModule<DataManager>().AddDataSB(AppConfig);
        }

        public override void Shutdown()
        {
        }

        public override void LifeUpdate()
        {
        }
    }
}