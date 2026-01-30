using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 更新包版本
    /// </summary>
    public class UpdatePackageVersionNode: StateNodeBase<LoadPackagesAsyncOperation>
    {
        public override void OnInit()
        {
        }

        public override void OnClose()
        {
        }

        public override async UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
            await base.OnEnter(lastProcedureBase);
            await UpdatePackageVersion();
        }
        
        /// <summary>
        /// 更新包版本
        /// </summary>
        private async UniTask UpdatePackageVersion()
        {
            var packageName = (string)_sm.GetBlackboardValue("PackageName");
            var modle = (EPlayMode)_sm.GetBlackboardValue("PlayMode");
            var package = YooAssets.GetPackage(packageName);
            
            AppLogger.Log($"更新包{packageName}版本");
            
            var operation = package.RequestPackageVersionAsync(Utility.YooAsset.AppendTimeTicks);
            await operation.ToUniTask();
            
            if (operation.Status != EOperationStatus.Succeed)
            {
                AppLogger.Error($"更新包{packageName}版本失败！Error：{operation.Error}，将尝试使用上一次记录的包版本");
                _sm.SetBlackboardValue("NetworkNormal", false);
                _sm.SwitchNode<CheckLocalAssetsVersionNode>();
            }
            else
            {
                _sm.SetBlackboardValue("PackageVersion", operation.PackageVersion);
                _sm.SetBlackboardValue("NetworkNormal", true);
                AppLogger.Log($"包{packageName}请求到包版本为：{operation.PackageVersion}");
                _sm.SwitchNode<UpdatePackageManifestNode>();
            }
        }
        
        public override async UniTask OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
            await base.OnLeave(nextProcedureBase, isRestarting);
        }
    }
}