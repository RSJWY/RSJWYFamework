using Cysharp.Threading.Tasks;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 清理未使用的缓存文件
    /// </summary>
    public class ClearPackageCacheNode:StateNodeBase
    {
        public override void OnInit()
        {
        }

        public override  void OnClose()
        {
        }


        public override  void OnEnter(StateNodeBase nextProcedureBase)
        {
            UniTask.Create(async () =>
            {
                AppLogger.Log($"清理未使用的缓存文件");
                var packageName = (string)_sm.GetBlackboardValue("PackageName");
                var package = YooAssets.GetPackage(packageName);
                var operation = package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
                operation.Completed += Operation_Completed;
                await operation.ToUniTask();
                _sm.SwitchNode(typeof(UpdaterDoneNode));
            });
        }
        private void Operation_Completed(YooAsset.AsyncOperationBase obj)
        {
           
        }
        public override  void OnLeave(StateNodeBase lastProcedureBase, bool isRestarting = false)
        {;
        }

        public override  void OnUpdate()
        {
        }

        public override  void OnUpdateSecond()
        {
        }
        
    }
}