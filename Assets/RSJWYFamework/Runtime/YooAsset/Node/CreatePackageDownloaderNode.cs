using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 创建文件下载器
    /// </summary>
    public class CreatePackageDownloaderNode:StateNodeBase
    {
        private int _retryCount = 0;
    
        public override void OnInit()
        {
           
            // 初始化时重置重试计数器
            _retryCount = 0;
        }


        public override  void OnClose()
        {
        }

        public override UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
            var packageName = (string)_sm.GetBlackboardValue("PackageName");
            var package = YooAssets.GetPackage(packageName);
            var downloader = package.CreateResourceDownloader(Utility.YooAsset.DownloadingMaxNum, Utility.YooAsset.FailedTryAgainNum);
            _sm. SetBlackboardValue("Downloader", downloader);

            if (downloader.TotalDownloadCount == 0)
            {
                AppLogger.Log($"包{packageName}没找到任何需要下载的资源！");
                _sm.SwitchNode<UpdaterDoneNode>();
            }
            else
            {
                // 发现新更新文件后，挂起流程系统
                // 注意：开发者需要在下载前检测磁盘空间不足
                AppLogger.Log($"包{packageName}发现新文件！下载的文件总量：{downloader.TotalDownloadCount}，总大小：{downloader.TotalDownloadBytes}");
                _sm.SwitchNode<DownloadPackageFilesNode>();
            }
            return UniTask.CompletedTask;
        }

        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
        }
    }
}