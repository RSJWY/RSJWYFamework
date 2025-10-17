using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 更新包版本
    /// </summary>
    public class UpdatePackageVersionNode:StateNodeBase
    {
        private int _retryCount = 0;
        
        public override void OnInit()
        {
            // 初始化时重置重试计数器
            _retryCount = 0;
        }

        public override void OnClose()
        {
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
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
                    RestartStateMachine<UpdatePackageVersionNode>($"重试更新包版本，第{_retryCount}次重试");
                    return;
                }
                else
                {
                    _sm.SetBlackboardValue("NetworkNormal", false);
                    //_sm.SwitchNode<UpdatePackageManifestNode>();
                    StopStateMachine($"更新包{packageName}版本失败，已达到最大重试次数({maxRetries})，停止重试");
                    // 重试次数用完，设置网络异常状态
                    AppLogger.Error($"更新包{packageName}版本失败，已达到最大重试次数({maxRetries})，停止重试");
                }
            }
            else
            {
                // 成功时重置重试计数器
                _retryCount = 0;
                _sm.SetBlackboardValue("PackageVersion", operation.PackageVersion);
                _sm.SetBlackboardValue("NetworkNormal", true);
                AppLogger.Log($"包{packageName}请求到包版本为：{operation.PackageVersion}");
                SwitchToNode<UpdatePackageManifestNode>();
            }
        }
        
        /// <summary>
        /// 判断是否应该重试
        /// </summary>
        /// <param name="maxRetries">最大重试次数，-1表示无限重试</param>
        /// <returns>是否应该重试</returns>
        private bool ShouldRetry(int maxRetries)
        {
            // -1 表示无限重试
            if (maxRetries == -1)
            {
                return true;
            }
            
            // 检查是否还有重试次数
            return _retryCount < maxRetries;
        }
        
        /// <summary>
        /// 获取剩余重试次数的描述
        /// </summary>
        /// <param name="maxRetries">最大重试次数</param>
        /// <returns>剩余重试次数描述</returns>
        private string GetRemainingRetries(int maxRetries)
        {
            if (maxRetries == -1)
            {
                return "无限";
            }
            
            return (maxRetries - _retryCount).ToString();
        }

        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
        }
    }
}