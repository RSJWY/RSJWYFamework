using System;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 下载需要更新的文件
    /// </summary>
    public class DownloadPackageFilesNode:YooAssetNode
    {
        private string packageName;
        public override void OnInit()
        {
            base.OnInit();
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            base.OnEnter(lastProcedureBase);
            BeginDownload().Forget();
        }
        private async UniTask  BeginDownload()
        {
            packageName=(string)GetBlackboardValue("PackageName");
            var downloader = (ResourceDownloaderOperation)GetBlackboardValue("Downloader");
            downloader.DownloadErrorCallback = OnDownloadErrorFunction;
            downloader.DownloadUpdateCallback = OnDownloadProgressUpdateFunction;
            downloader.DownloadFinishCallback = OnDownloadOverFunction;
            downloader.DownloadFileBeginCallback = OnStartDownloadFileFunction;
            downloader.BeginDownload();
            await downloader;
            

            // 检测下载结果
            if (downloader.Status != EOperationStatus.Succeed)
            {
                
                _retryCount++;
                var maxRetries = Utility.YooAsset.UpdatePackageVersionNumberOfRetries;

                AppLogger.Error($"更新包{packageName}下载失败：{downloader.Error} (重试次数: {_retryCount})");
                // 检查是否需要重试
                if (ShouldRetry(maxRetries))
                {
                    
                    AppLogger.Warning($"将在1秒后重试更新包{packageName}下载文件 (剩余重试次数: {GetRemainingRetries(maxRetries)})");
                    await UniTask.WaitForSeconds(1.0f);
                    
                    // 使用状态机重启功能重新执行当前节点
                    RestartStateMachine<DownloadPackageFilesNode>($"重试更新包{packageName}下载文件，第{_retryCount}次重试",400);
                    return;
                }
                else
                {
                    SetBlackboardValue("NetworkNormal", false);
                    //_sm.SwitchNode<UpdatePackageManifestNode>();
                    StopStateMachine($"更新包{packageName}下载文件失败，已达到最大重试次数({maxRetries})，停止重试",500);
                    // 重试次数用完，设置网络异常状态
                    AppLogger.Error($"更新包{packageName}下载文件失败，已达到最大重试次数({maxRetries})，停止重试");
                }
            }
            else
            {
                AppLogger.Log($"包{packageName}下载新资源完成");
                SwitchToNode<DownloadPackageOverNode>();
            }
        }
        
        /// <summary>
        /// 开始下载
        /// </summary>
        private void OnStartDownloadFileFunction(DownloadFileData data)
        {
            AppLogger.Log($"包{data.PackageName}开始下载：文件名：{data.FileName}, 文件大小：{data.FileSize}");
        }

        /// <summary>
        /// 下载完成
        /// </summary>
        private void OnDownloadOverFunction(DownloaderFinishData data)
        {
            AppLogger.Log($"包{data.PackageName}下载：{ (data.Succeed ? "成功" : "失败")}");
        }

        /// <summary>
        /// 更新中
        /// </summary>
        private void OnDownloadProgressUpdateFunction(DownloadUpdateData data)
        {
            AppLogger.Log($"包{data.PackageName}文件总数：{data.TotalDownloadCount}, 已下载文件数：{data.CurrentDownloadCount}, 下载总大小：{data.TotalDownloadBytes}, 已下载大小：{data.CurrentDownloadBytes}");
        }

        /// <summary>
        /// 下载出错
        /// </summary>
        private void OnDownloadErrorFunction(DownloadErrorData data)
        {
            AppLogger.Log($"包{data.PackageName}下载出错：文件名：{data.FileName}, 错误信息：{data.ErrorInfo}");
        }

        static int GetBaiFenBi(int now, int sizeBytes)
        {
            return (int)sizeBytes / now * 100;
        }

        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
            base.OnLeave(nextProcedureBase, isRestarting);
        }
    }
}