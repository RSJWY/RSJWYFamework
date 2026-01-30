

using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 下载更新文件完成
    /// </summary>
    public class DownloadPackageOverNode:StateNodeBase
    {
        public override void OnInit()
        {
        }

        public override void OnClose()
        {
        }

        public override UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
            var packageName=(string)_sm.GetBlackboardValue("PackageName");
            AppLogger.Log($"下载包{packageName}新资源完成");
            _sm.SwitchNode<ClearPackageCacheNode>();
            return UniTask.CompletedTask;
        }

    }
}