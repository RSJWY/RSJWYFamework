using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 创建文件下载器
    /// </summary>
    public class CreatePackageDownloaderNode:StateNodeBase
    {
        public override  void OnInit()
        {
        }

        public override  void OnClose()
        {
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            var packageName = (string)_sm.GetBlackboardValue("PackageName");
            var package = YooAssets.GetPackage(packageName);
            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
            _sm.SetBlackboardValue("Downloader", downloader);

            if (downloader.TotalDownloadCount == 0)
            {
                AppLogger.Log($"包{packageName}没找到任何下载文件！");
                _sm.SwitchNode<UpdaterDoneNode>();
            }
            else
            {
                // 发现新更新文件后，挂起流程系统
                // 注意：开发者需要在下载前检测磁盘空间不足
                AppLogger.Log($"包{packageName}发现新文件！下载的文件总量：{downloader.TotalDownloadCount}，总大小：{downloader.TotalDownloadBytes}");
                _sm.SwitchNode<DownloadPackageFilesNode>();
            }
        }

        public  override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
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