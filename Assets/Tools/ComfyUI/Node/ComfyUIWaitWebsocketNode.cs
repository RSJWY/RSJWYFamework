using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TouchSocket.Core;
using TouchSocket.Http.WebSockets;

namespace RSJWYFamework.Runtime.Node
{
    public class ComfyUIWaitWebsocketNode: StateNodeBase<ComfyUITaskAsyncOperation>
    {
        int waitTime=0;
        
        CancellationTokenSource cancellationTokenSource;
        
        public override void OnInit()
        {
            waitTime = 60;
        }

        public override void OnClose()
        {
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            waitTime = 60;
            //onnectComfyUI().Forget();
        }
        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
           
        }

        public override void OnUpdate()
        {
            // base.OnUpdate(); // StateNodeBase<T> might not have base logic for OnUpdate, usually abstract or virtual empty.
            // Timeout logic was commented out in OnUpdateSecond.
        }
    }
}