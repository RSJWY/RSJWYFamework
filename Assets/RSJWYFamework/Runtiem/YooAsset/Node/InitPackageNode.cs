using System;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 初始化包
    /// </summary>
    public class InitPackageNode : StateNodeBase
    {
        public override void OnInit()
        {
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            InitPackage().Forget();
        }

        public override void OnLeave(StateNodeBase nextProcedureBase)
        {
        }

        /// <summary>
        /// 创建初始化包
        /// </summary>
        /// <returns></returns>
        async UniTask InitPackage()
        {

            var playMode = (EPlayMode)_sm.GetBlackboardValue("PlayMode");
            var packageName = (string)_sm.GetBlackboardValue("PackageName");

            if (playMode is EPlayMode.WebPlayMode or EPlayMode.CustomPlayMode)
            {
                throw new AppException($"运行模式：{playMode} 目前不支持，初始化失败");
            }
            
            var fileDecryption = new Utility.YooAsset.AppHotPackageFileDecryption();
            
            AppLogger.Log($"初始化包{packageName} 运行模式{playMode} ");
            // 创建资源包裹类
            var package = YooAssets.TryGetPackage(packageName);
            if (package == null)
                package = YooAssets.CreatePackage(packageName);

            InitializationOperation initializationOperation = null;
            if (playMode == EPlayMode.EditorSimulateMode)
            {
                var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
                var packageRoot = buildResult.PackageRootDirectory;
                var createParameters = new EditorSimulateModeParameters();
                createParameters.EditorFileSystemParameters =
                    FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                initializationOperation = package.InitializeAsync(createParameters);
            }

            // 单机运行模式
            if (playMode == EPlayMode.OfflinePlayMode)
            {
                var createParameters = new OfflinePlayModeParameters();
                createParameters.BuildinFileSystemParameters =
                    FileSystemParameters.CreateDefaultBuildinFileSystemParameters(fileDecryption);
                initializationOperation = package.InitializeAsync(createParameters);
            }

            // 联机运行模式
            if (playMode == EPlayMode.HostPlayMode)
            {
                string defaultHostServer =  Utility.YooAsset.GetHostServerURL(packageName);
                string fallbackHostServer =  Utility.YooAsset.GetHostServerURL(packageName);
                IRemoteServices remoteServices = new Utility.YooAsset.RemoteServices(defaultHostServer, fallbackHostServer);
                var createParameters = new HostPlayModeParameters();
                createParameters.BuildinFileSystemParameters =
                    FileSystemParameters.CreateDefaultBuildinFileSystemParameters(fileDecryption);
                createParameters.CacheFileSystemParameters =
                    FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices,fileDecryption);
                initializationOperation = package.InitializeAsync(createParameters);
            }

            await initializationOperation.ToUniTask();
            if (initializationOperation.Status != EOperationStatus.Succeed)
            {
                throw new AppException($"资源包:{packageName}初始化失败！！");
            }
            else
            {
                _sm.SwitchNode<UpdatePackageVersionNode>();
            }
        }

        public override void OnClose()
        {
        }

        public override void OnUpdate()
        {
        }

        public override void OnUpdateSecond()
        {
        }
    }
}