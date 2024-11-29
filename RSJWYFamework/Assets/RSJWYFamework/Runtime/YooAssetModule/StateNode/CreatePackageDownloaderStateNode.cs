using Cysharp.Threading.Tasks;
using RSJWYFamework.Runtime.Default.Manager;
using RSJWYFamework.Runtime.Logger;
using RSJWYFamework.Runtime.Main;
using RSJWYFamework.Runtime.StateMachine;
using YooAsset;

namespace RSJWYFamework.Runtime.YooAssetModule.StateNode
{
    /// <summary>
    /// 创建文件下载器
    /// </summary>
    public class CreatePackageDownloaderStateNode:StateNodeBase
    {
        public override  void OnInit()
        {
        }

        public override  void OnClose()
        {
        }

        public override void OnEnter(StateNodeBase lastStateNodeBase)
        {
            var packageName = (string)pc.GetBlackboardValue("PackageName");
            var package = YooAssets.GetPackage(packageName);
            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
            pc.SetBlackboardValue("Downloader", downloader);

            if (downloader.TotalDownloadCount == 0)
            {
                RSJWYLogger.Log(RSJWYFameworkEnum.YooAssets,$"包{packageName}没找到任何下载文件！");
                pc.SwitchProcedure<UpdaterDoneStateNode>();
            }
            else
            {
                // 发现新更新文件后，挂起流程系统
                // 注意：开发者需要在下载前检测磁盘空间不足
                RSJWYLogger.Log(RSJWYFameworkEnum.YooAssets,$"包{packageName}发现新文件！下载的文件总量：{downloader.TotalDownloadCount}，总大小：{downloader.TotalDownloadBytes}");
                pc.SwitchProcedure<DownloadPackageFilesStateNode>();
            }  
        }

        public  override void OnLeave(StateNodeBase nextStateNodeBase)
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