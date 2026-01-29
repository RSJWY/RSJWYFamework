
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 加载热更代码流程结束
    /// </summary>
    public class LoadHotCodeDoneNode:StateNodeBase
    {
        public override void OnInit()
        {
            
        }

        public override void OnClose()
        {
        }

        public override async UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
            AppLogger.Log($"加载热更代码流程结束");
        }

        public override void OnUpdate()
        {
        }

        public override void OnUpdateSecond()
        {
        }
    }
}