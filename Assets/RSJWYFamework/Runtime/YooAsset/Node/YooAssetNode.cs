using System;
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    public class YooAssetNode : StateNodeBase
    { 
        public override void OnInit()
        {
        }

        public override void OnClose()
        {
        }

        protected virtual void AsyncExceptionStopStateMache(Exception exception)
        {
            _sm.Stop(500, $"Async Exception: {exception.Message}");
        }
    }
}