using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using RSJWYFamework.Runtime.Node;
using TouchSocket.Core;
using TouchSocket.Http.WebSockets;
using UnityEngine;

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
        private JObject _json;
        /// <summary>
        /// 获取历史图片URL的处理函数委托
        /// </summary>
        /// <param name="json">ComfyUI历史响应json字符串</param>
        /// <param name="promptID">ComfyUI工作任务ID</param>
        /// <returns>获取历史图片URL的结果</returns>
        public delegate GetHistoryImageURLResult GetHistoryImageURLHandle(JObject json,string promptID);
        /// <summary>
        /// 获取历史图片URL的处理函数，用户手动处理获取输出的图片URL
        /// </summary>
        private GetHistoryImageURLHandle _getHistoryImageURL;
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
        
        public Texture2D DownloadedTexture { get; private set; }
        
        /// <summary>
        /// 获取历史图片URL的函数
        /// </summary>
        /// <param name="clientid">设备id</param>
        /// <param name="json">json字符串</param>
        /// <param name="remoteIPHost">ComfyUI服务器地址</param>
        /// <param name="getHistoryImageURL">获取历史图片URL的处理函数，用户手动处理获取输出的图片URL</param>
        /// <param name="useWss">是否使用wss</param>
        /// <param name="owner">任务所属对象</param>
        public ComfyUITaskAsyncOperation(string clientid,[NotNull]JObject json,string remoteIPHost,
            GetHistoryImageURLHandle getHistoryImageURL,
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
            _smc.SetBlackboardValue("USEHTTPS",useWss);
            
            _smc.StateMachineTerminatedEvent+=OnStateMachineTerminated;
            _smc.ProcedureSwitchEvent+=OnProcedureSwitchEvent;
            
            _smc.AddNode<ComfyUIPostNode>();
            _smc.AddNode<ComfyUIWebsocketNode>();
            _smc.AddNode<ComfyUIDownloadResultNode>();
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
            else if (current is ComfyUIDownloadResultNode)
            {
                _steps = ComfyUITaskStatus.DownloadResult;
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
                    DownloadedTexture=_smc.GetBlackboardValue<Texture2D>("TEXTURE");
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

        #region Websocket

        CancellationTokenSource cancellationTokenSource;
        private WebSocketClient ComfyUIWSClient;
         public async UniTask ConnectComfyUI()
        {
            ComfyUIWSClient?.Dispose();
            try
            {
                var _wsConfig = new TouchSocketConfig()
                    .ConfigureContainer(a =>
                    {
                        a.AddLogger(TouchSocketContainerUnityDebugLogger.Default);
                    })
                    .ConfigurePlugins(a =>
                    {
                       
                    })
                    .SetRemoteIPHost($"{(_useWss?"wss":"ws")}://{_remoteIPHost}/ws?clientId={_clientid}");
                ComfyUIWSClient = new WebSocketClient();
                ComfyUIWSClient.Received = (c, e) =>
                {
                    switch (e.DataFrame.Opcode)
                    {
                        case WSDataType.Cont:
                            //处理中继包
                            break;
                        case WSDataType.Text:
                            //处理文本包
                            HandleTextMessage(e.DataFrame.ToText());
                            break;
                        case WSDataType.Binary:
                            //处理二进制包
                            //var data = dataFrame.PayloadData;
                            //Console.WriteLine($"收到二进制数据，长度：{data.Length}");
                            break;
                        case WSDataType.Close:
                            //处理关闭包
                            break;
                        case WSDataType.Ping:
                            //处理Ping包
                            break;
                        case WSDataType.Pong:
                            //处理Pong包
                            break;
                        default:
                            //处理其他包
                            break;
                    }
                    return EasyTask.CompletedTask;
                };
                await ComfyUIWSClient.SetupAsync(_wsConfig);
                await ComfyUIWSClient.ConnectAsync(cancellationTokenSource.Token);
                AppLogger.Log($"ComfyUI websocket connected,client_id is {_clientid}");
            }
            catch (Exception e)
            {
                AppLogger.Error($"ComfyUI websocket connect failed,error is {e.Message}");
                //TerminateStateMachine($"ComfyUI websocket connect failed,error is {e.Message}",500);
            }
            
        }
        
        private void HandleTextMessage(string message)
        {
            try
            {
                var _json=JObject.Parse(message);
                AppLogger.Log($"ComfyUI websocket text message: \n{message}");
                var comfyui_msg_type=_json["type"]?.ToString();
                if (Enum.TryParse(comfyui_msg_type,out ComfyUIWebsocketMsgType _state))
                {
                    if (_state==ComfyUIWebsocketMsgType.execution_success)
                    {
                        AppLogger.Log($"ComfyUI websocket received execution_success message");
                        UniTask.Create(async () =>
                        {
                            await UniTask.SwitchToMainThread();
                            //SwitchToNode<ComfyUIDownloadResultNode>();
                        });
                    }
                }
                else
                {
                    AppLogger.Warning($"ComfyUI websocket received message type {comfyui_msg_type}  not implemented!! Raw message is :\n{message}");
                }
            }
            catch (Exception exception)
            {
                AppLogger.Error($"ComfyUI websocket received message parse failed,error is {exception.Message}");
            }
        }

        #endregion
    }
}