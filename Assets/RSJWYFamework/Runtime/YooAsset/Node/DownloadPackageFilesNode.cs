using System;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 下载需要更新的文件
    /// </summary>
    public class DownloadPackageFilesNode: StateNodeBase<LoadPackagesAsyncOperation>
    {
        private string packageName;
        public override void OnInit()
        {
        }

        public override void OnClose()
        {
        } 
        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
        }

        public override async UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
           await BeginDownload();
        }
        private async UniTask  BeginDownload()
        {
            packageName=(string)_sm.GetBlackboardValue("PackageName");
            var downloader = (ResourceDownloaderOperation)_sm.GetBlackboardValue("Downloader");
            downloader.DownloadErrorCallback = OnDownloadErrorFunction;
            downloader.DownloadUpdateCallback = OnDownloadProgressUpdateFunction;
            downloader.DownloadFinishCallback = OnDownloadOverFunction;
            downloader.DownloadFileBeginCallback = OnStartDownloadFileFunction;
            downloader.BeginDownload();
            await downloader;
            

            // 检测下载结果
            if (downloader.Status != EOperationStatus.Succeed)
            {
                AppLogger.Error($"更新包{packageName}下载失败：{downloader.Error}");
                _sm.Stop(500,"资源下载失败");
            }
            else
            {
                AppLogger.Log($"包{packageName}下载新资源完成");
                _sm.SwitchNode<DownloadPackageOverNode>();
            }
        }
        
        /// <summary>
        /// 开始下载
        /// </summary>
        private void OnStartDownloadFileFunction(DownloadFileData data)
        {
            //AppLogger.Log($"包{data.PackageName}开始下载：文件名：{data.FileName}, 文件大小：{data.FileSize}");
            Owner?.OnStartDownload(data);
        }

        /// <summary>
        /// 下载完成
        /// </summary>
        private void OnDownloadOverFunction(DownloaderFinishData data)
        {
           // AppLogger.Log($"包{data.PackageName}下载：{ (data.Succeed ? "成功" : "失败")}");
           Owner?.OnDownloadOver(data);
        }

        /// <summary>
        /// 更新中
        /// </summary>
        private void OnDownloadProgressUpdateFunction(DownloadUpdateData data)
        {
            //AppLogger.Log($"包{data.PackageName}文件总数：{data.TotalDownloadCount}, 已下载文件数：{data.CurrentDownloadCount}, 下载总大小：{data.TotalDownloadBytes}, 已下载大小：{data.CurrentDownloadBytes}");
            Owner.OnDownloadProgressUpdate(data);
        }

        /// <summary>
        /// 下载出错
        /// </summary>
        private void OnDownloadErrorFunction(DownloadErrorData data)
        {
            //AppLogger.Log($"包{data.PackageName}下载出错：文件名：{data.FileName}, 错误信息：{data.ErrorInfo}");
            Owner.OnDownloadError(data);
        }

    }
}