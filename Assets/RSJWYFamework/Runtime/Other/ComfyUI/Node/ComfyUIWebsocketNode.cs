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
    public class ComfyUIWebsocketNode: StateNodeBase
    {
        /// <summary>
        /// ComfyUI工作任务ID
        /// </summary>
        private PromptInfo promptInfo;
        private WebSocketClient ComfyUIWSClient;
        private string _remoteIPHost;
        private bool _useWss;
        
        /// <summary>
        /// 客户端id
        /// </summary>
        private string _clientid;
        
        CancellationTokenSource cancellationTokenSource;
        
        public override void OnInit()
        {
        }

        public override void OnClose()
        {
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            _clientid=GetBlackboardValue<string>("CLIENTID");
            promptInfo=GetBlackboardValue<PromptInfo>("PROMPTINFO");
            _remoteIPHost=GetBlackboardValue<string>("REMOTEIPHOST");
            _useWss=GetBlackboardValue<bool>("USEWSS");
            cancellationTokenSource=new CancellationTokenSource();
            //onnectComfyUI().Forget();
        }
        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
            cancellationTokenSource.Cancel();
            UniTask.Create(async () =>
            {
                await ComfyUIWSClient.CloseAsync("正常关闭");
                ComfyUIWSClient?.Dispose();
            }).Forget();
        }

       

       
    }
}