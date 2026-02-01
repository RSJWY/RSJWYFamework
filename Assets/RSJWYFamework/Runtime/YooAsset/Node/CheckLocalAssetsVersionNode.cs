using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 检查本地资源包版本是否与服务器版本一致，以应对弱联网
    /// </summary>
    public class CheckLocalAssetsVersionNode:StateNodeBase
    {
        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
        }



        public override async UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
            await LocalAssetsVersion();
        }


        private async UniTask LocalAssetsVersion()
        {
            await UniTask.WaitForSeconds(0.5f);

            var packageName = (string)_sm.GetBlackboardValue("PackageName");
            var modle = (EPlayMode)_sm.GetBlackboardValue("PlayMode");
            var package = YooAssets.GetPackage(packageName);
            
            string version = PlayerPrefs.GetString($"{packageName}_VERSION", string.Empty);
            if (string.IsNullOrEmpty(version))
            {
                AppLogger.Error($"未找到上次初始化的{packageName}的版本号");
                _sm.Stop(500,$"{packageName}初始化失败，请联网更新");
            }
            else
            {
                _sm.SetBlackboardValue("PackageVersion", version);
                AppLogger.Log($"上次初始化的{packageName}的版本号为{version}，继续后续流程");
                _sm.SwitchNode<UpdatePackageManifestNode>();
                /*var manifestOp = package.UpdatePackageManifestAsync(version);
                await manifestOp.ToUniTask();
                if (manifestOp.Status != EOperationStatus.Succeed)
                {
                    AppLogger.Error($"加载包{packageName}版本{version}清单失败！Error：{manifestOp.Error}");
                    _sm.Stop(400,$"{packageName}初始化失败，请检查网络以更新资源包");
                }
                else
                {
                    AppLogger.Log($"加载包{packageName}版本{version}本地清单成功！检查资源完整性");
                    var downloader = package.CreateResourceDownloader(Utility.YooAsset.DownloadingMaxNum, Utility.YooAsset.FailedTryAgainNum);
                    if (downloader.TotalDownloadCount > 0)   
                    {
                        AppLogger.Error($"包{packageName}版本{version}有{downloader.TotalDownloadCount}个资源上次未完成下载，本地内容不完整，请连接网络以进行完整下载");
                        _sm.Stop(500,$"{packageName}初始化失败，请检查网络以更新资源包");
                    }
                }*/
            }
        }

    }
}