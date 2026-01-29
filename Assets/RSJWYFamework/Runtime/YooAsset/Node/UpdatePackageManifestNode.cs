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

        public  override async UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
           await UpdateManifest();
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

                AppLogger.Error($"更新包{packageName}清单失败！Error：{operation.Error} ");
                
                _sm.SetBlackboardValue("NetworkNormal", false);
                //_sm.SwitchNode<UpdatePackageManifestNode>();
                _sm.Stop(500,$"更新包{packageName}清单文件失败");
            }
            else
            {
                AppLogger.Log($"更新包{packageName}清单成功");
                _sm.SwitchNode<CreatePackageDownloaderNode>();
            }
        }

    }
}