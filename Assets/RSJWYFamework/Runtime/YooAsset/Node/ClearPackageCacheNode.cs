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

            ClearCacheFilesOperation operation = null;
            // 根据配置选择不同的清理模式
            switch (Utility.YooAsset.FileClearMode)
            {
                case EFileClearMode.ClearBundleFilesByLocations:
                    var locations = Utility.YooAsset.ClearLocations.ToArray();
                    AppLogger.Log($"清理指定路径缓存: {string.Join(", ", locations)}");
                    operation = package.ClearCacheFilesAsync(Utility.YooAsset.FileClearMode, locations);
                    break;
                case EFileClearMode.ClearBundleFilesByTags:
                    var tags = Utility.YooAsset.ClearTags.ToArray();
                    AppLogger.Log($"清理指定标签缓存: {string.Join(", ", tags)}");
                    operation = package.ClearCacheFilesAsync(Utility.YooAsset.FileClearMode, tags);
                    break;
                default:
                    AppLogger.Log($"清理缓存模式: {Utility.YooAsset.FileClearMode}");
                    operation = package.ClearCacheFilesAsync(Utility.YooAsset.FileClearMode);
                    break;
            }
            
            operation.Completed += Operation_Completed;
            await operation.ToUniTask();
            _sm.SwitchNode<UpdaterDoneNode>();
        }
        private void Operation_Completed(YooAsset.AsyncOperationBase obj)
        {
           Owner.OnClearCacheFiles(obj);
        }
        
    }
}