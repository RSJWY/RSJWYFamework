using System;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 初始化包
    /// </summary>
    public class InitPackageNode : StateNodeBase<LoadPackagesAsyncOperation>
    {
        public override void OnInit()
        {
        }

        public override async UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
           await InitPackage();
        }


        /// <summary>
        /// 创建初始化包
        /// </summary>
        /// <returns></returns>
        async UniTask InitPackage()
        {
            //获取游戏模式以及包名
            var playMode = (EPlayMode)_sm.GetBlackboardValue("PlayMode");
            var packageName = (string)_sm.GetBlackboardValue("PackageName");

            AppLogger.Log($"初始化包{packageName} 运行模式{playMode} ");
            if (playMode is EPlayMode.WebPlayMode or EPlayMode.CustomPlayMode)
            {
                throw new AppException($"运行模式：{playMode} 目前不支持，初始化失败");
            }
            //初始化资源解密服务
            var fileDecryption = new Utility.YooAsset.AppHotPackageFileDecryption();
            
            // 创建资源包裹类
            var package = YooAssets.TryGetPackage(packageName);
            package ??= YooAssets.CreatePackage(packageName);

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
                //无论成功与否，使用统一的远端目录
                string defaultHostServer =  Utility.YooAsset.GetHostServerURL(packageName);
                string fallbackHostServer =  Utility.YooAsset.GetHostServerURL(packageName);
                IRemoteServices remoteServices = new Utility.YooAsset.RemoteServices(defaultHostServer, fallbackHostServer);
                var createParameters = new HostPlayModeParameters();
                createParameters.BuildinFileSystemParameters = 
                    FileSystemParameters.CreateDefaultBuildinFileSystemParameters(fileDecryption);
                //拷贝内置清单到资源目录
                createParameters.BuildinFileSystemParameters.AddParameter(FileSystemParametersDefine.COPY_BUILDIN_PACKAGE_MANIFEST, true);
                createParameters.CacheFileSystemParameters = 
                    FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices,fileDecryption);
                //覆盖安装内资资源清单拷贝问题
                createParameters.CacheFileSystemParameters.AddParameter(FileSystemParametersDefine.INSTALL_CLEAR_MODE, Utility.YooAsset.InstallClearMode);
                initializationOperation = package.InitializeAsync(createParameters);
            }

            await initializationOperation.ToUniTask();
            if (initializationOperation.Status != EOperationStatus.Succeed)
            {
                AppLogger.Error($"资源包:{packageName}初始化失败！！运行模式：{playMode}");
                _sm.Stop(500,$"资源包:{packageName}初始化失败！！");
            }
            else
            {
                _sm.SwitchNode<UpdatePackageVersionNode>();
            }
        }
        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
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