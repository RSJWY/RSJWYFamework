using System;
using RSJWYFamework.Runtime.Node;

namespace RSJWYFamework.Runtime
{
    public class ComfyUITaskAsyncOperation:AppGameAsyncOperation
    {
        enum ComfyUITaskStatus
        {
            None,
            Post,
            WebsocketWait,
            Download,
            Done,
            Error,
        }
        
        private readonly StateMachine _smc;
        private ComfyUITaskStatus _steps = ComfyUITaskStatus.None;
        /// <summary>
        /// 客户端id
        /// </summary>
        private string _clientid;
        /// <summary>
        /// json字符串
        /// </summary>
        private string _json;
        
        
        /// <summary>
        /// ComfyUI工作任务ID
        /// </summary>
        private PromptInfo promptInfo;
        
        private string _postURL;
        
        public ComfyUITaskAsyncOperation(string clientid,string json,string postURL,object owner)
        {
            _smc = new StateMachine(this,$"ComfyUITask-{Guid.NewGuid()}");
            _clientid = clientid;
            _json = json;
            _postURL = postURL;
            
            _smc.SetBlackboardValue("CLIENTID",_clientid);
            _smc.SetBlackboardValue("JSON",_json);
            _smc.SetBlackboardValue("POSTURL",_postURL);
            
            _smc.StateMachineTerminatedEvent+=OnStateMachineTerminated;
            _smc.ProcedureSwitchEvent+=OnProcedureSwitchEvent;
            
            _smc.AddNode<ComfyUIPostNode>();
            _smc.AddNode<ComfyUIWebsocketNode>();
        }

        private void OnProcedureSwitchEvent(StateNodeBase last, StateNodeBase current)
        {
            if (current is ComfyUIPostNode)
            {
                _steps = ComfyUITaskStatus.Post;
            }
            else if (current is ComfyUIWebsocketNode)
            {
                _steps = ComfyUITaskStatus.WebsocketWait;
            }
        }

        /// <summary>
        /// ComfyUI任务状态机终止事件
        /// </summary>
        private void OnStateMachineTerminated(StateMachine arg1, string stopReason, int code, bool isRestart)
        {
            if (isRestart==false)
            {
                // 非重启状态下，根据code判断任务是否成功
                if (code == 0)
                {
                    _steps = ComfyUITaskStatus.Done;
                    Status=AppAsyncOperationStatus.Succeed;
                }
                else
                {
                    _steps = ComfyUITaskStatus.Error;
                    Status=AppAsyncOperationStatus.Failed;
                    Error=stopReason;
                }
            }
        }


        protected override void OnStart()
        {
            _smc.StartNode<ComfyUIPostNode>();
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnSecondUpdate()
        {
        }

        protected override void OnAbort()
        {
        }

        protected override void OnSecondUpdateUnScaleTime()
        {
        }

        protected override void OnWaitForAsyncComplete()
        {
        }
    }
}