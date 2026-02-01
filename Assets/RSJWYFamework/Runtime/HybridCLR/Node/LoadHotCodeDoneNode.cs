
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 加载热更代码流程结束
    /// </summary>
    public class LoadHotCodeDoneNode:StateNodeBase<LoadHotCodeAsyncOperation>
    {
        public override async UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
            AppLogger.Log($"加载热更代码流程结束");
            _sm.ClearBlackboard();
            Machine.Stop(0, "Done");
        }

        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
        }

        public override void OnUpdate()
        {
        }

        public override void OnUpdateSecond()
        {
        }
    }
}