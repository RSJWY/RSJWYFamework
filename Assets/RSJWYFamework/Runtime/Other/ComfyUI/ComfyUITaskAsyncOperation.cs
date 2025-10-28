using System;
using Newtonsoft.Json.Linq;
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
            DownloadResult,
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
        /// 获取历史图片URL的处理函数，用户手动处理获取输出的图片URL
        /// </summary>
        private Func<JObject,GetHistoryImageURLResult> _getHistoryImageURL;
        /// <summary>
        /// ComfyUI工作任务ID
        /// </summary>
        private PromptInfo promptInfo;
        /// <summary>
        /// ComfyUI服务器地址
        /// </summary>
        private string _remoteIPHost;
        /// <summary>
        /// 是否使用wss
        /// </summary>
        private bool _useWss;
        
        /// <summary>
        /// 获取历史图片URL的函数
        /// </summary>
        /// <param name="clientid">设备id</param>
        /// <param name="json">json字符串</param>
        /// <param name="remoteIPHost">ComfyUI服务器地址</param>
        /// <param name="getHistoryImageURL">获取历史图片URL的处理函数，用户手动处理获取输出的图片URL</param>
        /// <param name="useWss">是否使用wss</param>
        /// <param name="owner">任务所属对象</param>
        public ComfyUITaskAsyncOperation(string clientid,string json,string remoteIPHost,
            Func<JObject,GetHistoryImageURLResult> getHistoryImageURL,
        bool useWss,object owner)
        {
            _smc = new StateMachine(this,$"ComfyUITask-{Guid.NewGuid()}");
            _clientid = clientid;
            _json = json;
            _remoteIPHost = remoteIPHost;
            _getHistoryImageURL = getHistoryImageURL;
            
            _smc.SetBlackboardValue("CLIENTID",_clientid);
            _smc.SetBlackboardValue("JSON",_json);
            _smc.SetBlackboardValue("REMOTEIPHOST",_remoteIPHost);
            _smc.SetBlackboardValue("USEWSS",_useWss);
            _smc.SetBlackboardValue("GETHISTORYIMAGEURL",_getHistoryImageURL);
            
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
                promptInfo=_smc.GetBlackboardValue<PromptInfo>("PROMPTINFO");
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