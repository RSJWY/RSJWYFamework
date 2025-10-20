using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 更新资源清单
    /// </summary>
    public class UpdatePackageManifestNode:YooAssetNode
    {
    
        public override void OnInit()
        {
           base.OnInit();
        }

        public  override void OnClose()
        {
            base.OnClose();
        }

        public  override void OnEnter(StateNodeBase lastProcedureBase)
        {
            base.OnEnter(lastProcedureBase);
            UpdateManifest().Forget();
        }
        private async UniTask UpdateManifest() 
        {
            await UniTask.WaitForSeconds(0.5f);
            var packageName = (string)_sm.GetBlackboardValue("PackageName");
            var packageVersion = (string)_sm.GetBlackboardValue("PackageVersion");
            var package = YooAssets.GetPackage(packageName);
            
            AppLogger.Log($"更新包{packageName}资源清单");
            var operation = package.UpdatePackageManifestAsync(packageVersion,Utility.YooAsset.Timeout);
            await operation.ToUniTask();

            if (operation.Status != EOperationStatus.Succeed)
            {
                
                _retryCount++;
                var maxRetries = Utility.YooAsset.UpdatePackageVersionNumberOfRetries;

                AppLogger.Error($"更新包{packageName}清单失败！Error：{operation.Error} (重试次数: {_retryCount})");
                
                // 检查是否需要重试
                if (ShouldRetry(maxRetries))
                {
                    
                    AppLogger.Warning($"将在1秒后重试更新包{packageName}清单文件 (剩余重试次数: {GetRemainingRetries(maxRetries)})");
                    await UniTask.WaitForSeconds(1.0f);
                    
                    // 使用状态机重启功能重新执行当前节点
                    RestartStateMachine<UpdatePackageManifestNode>($"重试更新包清单文件，第{_retryCount}次重试",400);
                    return;
                }
                else
                {
                    SetBlackboardValue("NetworkNormal", false);
                    //_sm.SwitchNode<UpdatePackageManifestNode>();
                    StopStateMachine($"更新包{packageName}清单文件失败，已达到最大重试次数({maxRetries})，停止重试",500);
                    // 重试次数用完，设置网络异常状态
                    AppLogger.Error($"更新包{packageName}清单文件失败，已达到最大重试次数({maxRetries})，停止重试");
                }
            }
            else
            {
                AppLogger.Log($"更新包{packageName}清单成功");
                SwitchToNode<CreatePackageDownloaderNode>();
            }
        }
        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
            base.OnLeave(nextProcedureBase, isRestarting);
        }

    }
}