using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 更新包版本
    /// </summary>
    public class UpdatePackageVersionNode:YooAssetNode
    {
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
            UpdatePackageVersion().Forget();
        }
        
        /// <summary>
        /// 更新包版本
        /// </summary>
        private async UniTask UpdatePackageVersion()
        {
            await UniTask.WaitForSeconds(0.5f);

            var packageName = (string)_sm.GetBlackboardValue("PackageName");
            var modle = (EPlayMode)_sm.GetBlackboardValue("PlayMode");
            var package = YooAssets.GetPackage(packageName);
            
            AppLogger.Log($"更新包{packageName}版本 (尝试次数: {_retryCount + 1})");
            
            var operation = package.RequestPackageVersionAsync(false);
            await operation.ToUniTask();
            
            if (operation.Status != EOperationStatus.Succeed)
            {
                _retryCount++;
                var maxRetries = Utility.YooAsset.UpdatePackageVersionNumberOfRetries;
                
                AppLogger.Error($"更新包{packageName}版本失败！Error：{operation.Error} (重试次数: {_retryCount})");
                
                // 检查是否需要重试
                if (ShouldRetry(maxRetries))
                {
                    AppLogger.Warning($"将在1秒后重试更新包{packageName}版本 (剩余重试次数: {GetRemainingRetries(maxRetries)})");
                    await UniTask.WaitForSeconds(1.0f);
                    
                    // 使用状态机重启功能重新执行当前节点
                    RestartStateMachine<UpdatePackageVersionNode>($"重试更新包版本，第{_retryCount}次重试",400);
                    return;
                }
                else
                {
                    SetBlackboardValue("NetworkNormal", false);
                    //_sm.SwitchNode<UpdatePackageManifestNode>();
                    //StopStateMachine($"更新包{packageName}版本失败，已达到最大重试次数({maxRetries})，停止重试");
                    // 重试次数用完，设置网络异常状态
                    AppLogger.Error($"更新包{packageName}版本失败，已达到最大重试次数({maxRetries})，停止重试，将读取上一次缓存的版本");
                    
                    SwitchToNode<CheckLocalAssetsVersionNode>();
                }
            }
            else
            {
                // 成功时重置重试计数器
                _retryCount = 0;
                SetBlackboardValue("PackageVersion", operation.PackageVersion);
                SetBlackboardValue("NetworkNormal", true);
                AppLogger.Log($"包{packageName}请求到包版本为：{operation.PackageVersion}");
                SwitchToNode<UpdatePackageManifestNode>();
            }
        }
        
        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
            base.OnLeave(nextProcedureBase, isRestarting);
        }
    }
}