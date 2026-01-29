using RSJWYFamework.Runtime;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    public class LoadPackagesAsyncOperation:AppGameAsyncOperation
    { 
        enum LoadPackageSteps
        {
            None,
            Update,
            Done
        }
        private readonly StateMachine<LoadPackagesAsyncOperation> _smc;
        private LoadPackageSteps _steps = LoadPackageSteps.None;
        
        
        public LoadPackagesAsyncOperation(string packageName, EPlayMode playMode)
        {
            _smc = new StateMachine<LoadPackagesAsyncOperation>(this,"初始化资源管理");
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
            //检查本地资源版本，弱联网将检查上次下载的版本
            _smc.AddNode(new CheckLocalAssetsVersionNode());
            
            //写入数据
            _smc.SetBlackboardValue("PlayMode",playMode);
            _smc.SetBlackboardValue("PackageName",packageName);
            
            //绑定事件
            _smc.StateMachineTerminatedEvent+=OnStateMachineTerminatedEvent;
            //开始异步任务
            AppAsyncOperationSystem.StartOperation(typeof(LoadPackagesAsyncOperation).FullName, this);
        }
        /// <summary>
        /// 状态机终止事件
        /// </summary>
        /// <param name="stateMachine">状态机</param>
        /// <param name="TerminationReason">终止原因</param>
        /// <param name="StatusCode">状态码</param>
        /// <param name="isRestarting">是否重新启动</param>
        private void OnStateMachineTerminatedEvent(StateMachine stateMachine, string TerminationReason, int StatusCode, bool isRestarting)
        {
            if (isRestarting)return;
            
            if(stateMachine == _smc)
            {
                if(StatusCode==0)
                {
                    Status = AppAsyncOperationStatus.Succeed;
                    _steps = LoadPackageSteps.Done;
                }
                else
                {
                    Status = AppAsyncOperationStatus.Failed;
                    _steps = LoadPackageSteps.Done;
                }
            }
        }

        protected override void OnStart()
        {
            _steps = LoadPackageSteps.Update;
            _smc.StartNode<InitPackageNode>();
        }

        protected override void OnUpdate()
        {
            /*switch (_steps)
            {
                case LoadPackageSteps.None:
                case LoadPackageSteps.Done:
                    return;
                case LoadPackageSteps.Update:
                {
                    _smc.OnUpdate();
                    if(_smc.GetNowNode() == typeof(UpdaterDoneNode))
                    {
                        Status = AppAsyncOperationStatus.Succeed;
                        _steps = LoadPackageSteps.Done;
                    }
                    else
                    {
                        Status = AppAsyncOperationStatus.Succeed;
                    }

                    break;
                }
            }*/
        }

        protected override void OnSecondUpdate()
        {
            
        }

        protected override void OnAbort()
        {
        }

        protected override void OnSecondUpdateUnScaleTime()
        {
        }

    }
}