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
    public class ComfyUIWaitWebsocketNode: StateNodeBase
    {
        int waitTime=0;
        /// <summary>
        /// 客户端id
        /// </summary>
        private string _clientid;
        
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

        public override void OnUpdateSecond()
        {
            base.OnUpdateSecond();
            waitTime--;
            if(waitTime<=0)
            {
                TerminateStateMachine("超时",400);
            }
        }
    }
}