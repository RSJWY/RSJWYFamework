using System;
using RSJWYFamework.Runtime;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    public class LoadPackagesAsyncOperation:AppStateMachineAsyncOperation
    { 

        /// <summary>
        /// 开始下载
        /// </summary>
        public Action<DownloadFileData> OnStartDownload;
        
        /// <summary>
        /// 下载完成
        /// </summary>
        public Action<DownloaderFinishData> OnDownloadOver;
        
        
        /// <summary>
        /// 下载进度更新
        /// </summary>
        public Action<DownloadUpdateData> OnDownloadProgressUpdate;
        
        /// <summary>
        /// 下载错误
        /// </summary>
        public Action<DownloadErrorData> OnDownloadError;
        
        
        /// <summary>
        /// 下载完成后清理缓存文件完成回调
        /// </summary>
        public Action<YooAsset.AsyncOperationBase> OnClearCacheFiles;
        
        
        public LoadPackagesAsyncOperation(YooAssetPackageData packageData, EPlayMode playMode)
        {
            var sm = new StateMachine<LoadPackagesAsyncOperation>(this,"初始化资源管理");
            // 创建状态机
            //2.2.1版本 offlinePlayMode EditorSimulateMode 需要依次调用init, request version, update manifest 三部曲
            sm.AddNode(new InitPackageNode());
            sm.AddNode(new UpdatePackageVersionNode());
            sm.AddNode(new UpdatePackageManifestNode());
            sm.AddNode(new CreatePackageDownloaderNode());
            sm.AddNode(new DownloadPackageFilesNode());
            sm.AddNode(new DownloadPackageOverNode());
            sm.AddNode(new ClearPackageCacheNode());
            sm.AddNode(new UpdaterDoneNode());
            //检查本地资源版本，弱联网将检查上次下载的版本
            sm.AddNode(new CheckLocalAssetsVersionNode());
            
            //写入数据
            sm.SetBlackboardValue("PlayMode",playMode);
            sm.SetBlackboardValue("PackageName",packageData.packageName);
            sm.SetBlackboardValue("PackageData",packageData);
            
            InitStateMachine(sm, typeof(InitPackageNode));
        }

        public IRemoteServices SetRemoteService()
        {
            string defaultHostServer =  Utility.YooAsset.GetHostServerURL("");
            string fallbackHostServer =  Utility.YooAsset.GetHostServerURL("");
            IRemoteServices remoteServices = new IYooAssets.RemoteServices(defaultHostServer, fallbackHostServer);
            return remoteServices;
        }

    }
}