using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 更新包版本
    /// </summary>
    public class UpdatePackageVersionNode:StateNodeBase
    {
        public override void OnInit()
        {
            
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
            
            AppLogger.Log($"更新包{packageName}版本");
            
            var operation = package.RequestPackageVersionAsync(false);
            await operation.ToUniTask();
            if (operation.Status != EOperationStatus.Succeed)
            {
                AppLogger.Error($"更新包{packageName}版本失败！Error：{operation.Error}");
                _sm.SetBlackboardValue("NetworkNormal", false);
            }
            else
            {
                _sm.SetBlackboardValue("PackageVersion", operation.PackageVersion);
                _sm.SetBlackboardValue("NetworkNormal", true);
                AppLogger.Log($"包{packageName}请求到包版本为：{operation.PackageVersion}");
                _sm.SwitchNode<UpdatePackageManifestNode>();
            }
        }

        public override void OnLeave(StateNodeBase nextProcedureBase)
        {
        }
    }
}