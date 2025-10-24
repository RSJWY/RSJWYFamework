using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    [Module()]
    [ModuleDependency(typeof(StateMachineManager))][ModuleDependency(typeof(AppAsyncOperationSystem))]
    [ModuleDependency(typeof(DataManager))][ModuleDependency(typeof(EventManager))]
    [ModuleDependency(typeof(EventManager))]
    public class YooAssetManager:ModuleBase
    {
        /// <summary>
        /// 获取包
        /// </summary>
        public ResourcePackage GetPackage(string packageName)
        {
            return YooAssets.GetPackage(packageName);
        }
        
        public async UniTask LoadPackage()
        {
            //获取数据并存入数据
            var projectConfig = ModuleManager.GetModule<DataManager>().GetFirstDataSB<AppConfig>();
            Utility.YooAsset.Setting(projectConfig.hostServerIP, projectConfig.ProjectName, projectConfig.APPName, projectConfig.Version, projectConfig.UpdatePackageVersionNumberOfRetries);
            UniTask[] taskArr=new UniTask[projectConfig.YooAssetPackageData.Count];
            for (int i = 0; i < projectConfig.YooAssetPackageData.Count; i++)
            {
                //配置异步任务
                LoadPackagesAsyncOperation operationR = 
                    new LoadPackagesAsyncOperation(this,projectConfig.YooAssetPackageData[i].PackageName, projectConfig.PlayMode);
                
                taskArr[i]=operationR.ToUniTask();
            }
            //等待完成
            await UniTask.WhenAll(taskArr);
        }
        public override void Initialize()
        {
            YooAssets.Initialize();
        }

        public override void Shutdown()
        {
        }

        public override void LifeUpdate()
        {
        }
    }
}