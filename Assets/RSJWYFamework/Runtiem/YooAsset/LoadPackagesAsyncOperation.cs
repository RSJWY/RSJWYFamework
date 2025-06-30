using RSJWYFamework.Runtime;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    public class LoadPackagesAsyncOperation:AppGameAsyncOperation
    { 
        enum RSteps
        {
            None,
            Update,
            Done
        }
        private readonly StateMachine _smc;
        private RSteps _steps = RSteps.None;
        
        
        public LoadPackagesAsyncOperation(string packageName, EPlayMode playMode)
        {
            _smc = new StateMachine(this,"初始化资源管理");
            // 创建状态机
            //2.2.1版本 offlinePlayMode EditorSimulateMode 需要依次调用init, request version, update manifest 三部曲
            _smc.AddNode(new InitPackageNode());
            _smc.AddNode(new UpdatePackageVersionNode());
            _smc.AddNode(new UpdatePackageManifestNode());
            _smc.AddNode(new CreatePackageDownloaderNode());
            _smc.AddNode(new DownloadPackageFilesNode());
            _smc.AddNode(new DownloadPackageOverNode());
            _smc.AddNode(new ClearPackageCacheNode());
            _smc.AddNode(new UpdaterDoneNode());
            //写入数据
            _smc.SetBlackboardValue("PlayMode",playMode);
            _smc.SetBlackboardValue("PackageName",packageName);
            //开始异步任务
            AppAsyncOperationSystem.StartOperation(typeof(LoadPackagesAsyncOperation).FullName, this);
        }

        protected override void OnStart()
        {
            _steps = RSteps.Update;
            _smc.StartNode<InitPackageNode>();
        }

        protected override void OnUpdate()
        {
            switch (_steps)
            {
                case RSteps.None:
                case RSteps.Done:
                    return;
                case RSteps.Update:
                {
                    _smc.OnUpdate();
                    if(_smc.GetNowNode() == typeof(UpdaterDoneNode))
                    {
                        Status = AppAsyncOperationStatus.Succeed;
                        _steps = RSteps.Done;
                    }

                    break;
                }
            }
        }

        protected override void OnAbort()
        {
        }
    }
}