
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 完成更新流程
    /// </summary>
    public class UpdaterDoneNode:StateNodeBase
    {
        public override void OnInit()
        {
           
        }

        public override void OnClose()
        {
            
        }

        public override UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
            var packageName = (string)_sm.GetBlackboardValue("PackageName");
            AppLogger.Log($"完成包{packageName}更新流程");
            _sm.Stop(0,$"完成包{packageName}更新流程");
            return UniTask.CompletedTask;
        }
        
    }
}