using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 清理未使用的缓存文件
    /// </summary>
    public class ClearPackageCacheNode:StateNodeBase<LoadPackagesAsyncOperation>
    {
        public override void OnInit()
        {
        }

        public override  void OnClose()
        {
        }

        public override  async UniTask OnEnter(StateNodeBase nextProcedureBase)
        {
            AppLogger.Log($"清理未使用的缓存文件");
            var packageName = (string)_sm.GetBlackboardValue("PackageName");
            var package = YooAssets.GetPackage(packageName);
            var packageData = (YooAssetPackageData)_sm.GetBlackboardValue("PackageData");

            ClearCacheFilesOperation operation = null;
            // 根据配置选择不同的清理模式
            switch (packageData.fileClearMode)
            {
                case EFileClearMode.ClearBundleFilesByLocations:
                    AppLogger.Log($"包：{packageName}清理-指定路径缓存");
                    var locations = packageData.clearLocations.ToArray();
                    operation = package.ClearCacheFilesAsync(packageData.fileClearMode, locations);
                    break;
                case EFileClearMode.ClearBundleFilesByTags:
                    AppLogger.Log($"包：{packageName}清理-指定标签缓存");
                    var tags = packageData.clearTags.ToArray();
                    operation = package.ClearCacheFilesAsync(packageData.fileClearMode, tags);
                    break;
                default:
                    AppLogger.Log($"包：{packageName}清理缓存模式: {packageData.fileClearMode}");
                    operation = package.ClearCacheFilesAsync(packageData.fileClearMode);
                    break;
            }
            
            operation.Completed += Operation_Completed;
            await operation.ToUniTask();
            _sm.SwitchNode<UpdaterDoneNode>();
        }

        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
        }

        private void Operation_Completed(YooAsset.AsyncOperationBase obj)
        {
           Owner.OnClearCacheFiles(obj);
        }
        
    }
}