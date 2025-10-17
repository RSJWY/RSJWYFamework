using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 更新资源清单
    /// </summary>
    public class UpdatePackageManifestNode:StateNodeBase
    {
        public override void OnInit()
        {
           
        }

        public  override void OnClose()
        {
        }

        public  override void OnEnter(StateNodeBase lastProcedureBase)
        {
            UpdateManifest().Forget();
        }
        private async UniTask UpdateManifest() 
        {
            await UniTask.WaitForSeconds(0.5f);
            var packageName = (string)_sm.GetBlackboardValue("PackageName");
            var packageVersion = (string)_sm.GetBlackboardValue("PackageVersion");
            var package = YooAssets.GetPackage(packageName);
            
            AppLogger.Log($"更新包{packageName}资源清单");
            var operation = package.UpdatePackageManifestAsync(packageVersion);
            await operation.ToUniTask();

            if (operation.Status != EOperationStatus.Succeed)
            {
                AppLogger.Error($"更新包{packageName}清单失败！Error：{operation.Error}");
            }
            else
            {
                AppLogger.Log($"更新包{packageName}清单成功");
                _sm.SwitchNode<CreatePackageDownloaderNode>();
            }
        }

        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
        }

    }
}