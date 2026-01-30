using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
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
        
        private StateMachine<ComfyUITaskAsyncOperation> _smc;
        private ComfyUITaskStatus _steps = ComfyUITaskStatus.None;
        
        /// <summary>
        /// 客户端id
        /// </summary>
        public string ClientId { get; private set; }
        /// <summary>
        /// json字符串
        /// </summary>
        public JObject Json { get; private set; }
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
        public GetHistoryImageURLHandle GetHistoryImageURL { get; private set; }
        /// <summary>
        /// ComfyUI工作任务ID
        /// </summary>
        public PromptInfo PromptInfo { get; set; }
        /// <summary>
        /// ComfyUI服务器地址
        /// </summary>
        public string RemoteIPHost { get; private set; }
        /// <summary>
        /// 是否使用wss
        /// </summary>
        public bool UseWss { get; private set; }
        public bool UseHttps => UseWss;
        
        public Texture2D DownloadedTexture { get; set; }

        public event Action<ProgressData> OnProgress;
        
        private TaskIDHandler _taskIDHandler;
        public TaskIDHandler TaskIDHandler => _taskIDHandler;
        
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
            _smc = new StateMachine<ComfyUITaskAsyncOperation>(this,$"ComfyUITask-{Guid.NewGuid()}");
            ClientId = clientid;
            Json = json;
            RemoteIPHost = remoteIPHost;
            GetHistoryImageURL = getHistoryImageURL;
            UseWss = useWss;
            
            _taskIDHandler = new TaskIDHandler();
            
            _smc.StateMachineTerminatedEvent+=OnStateMachineTerminated;
            _smc.ProcedureSwitchEvent+=OnProcedureSwitchEvent;
            
            _smc.AddNode<ComfyUIPostNode>();
            _smc.AddNode<ComfyUIWaitWebsocketNode>();
            _smc.AddNode<ComfyUIDownloadResultNode>();
            
            _taskIDHandler.OnTaskTriggered+=OnTaskTriggered;
        }

        private void OnTaskTriggered(string benchmarkID)
        {
             UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                _smc.SwitchNode<ComfyUIDownloadResultNode>();
            });
        }

        private void OnProcedureSwitchEvent(StateNodeBase last, StateNodeBase current)
        {
            if (current is ComfyUIPostNode)
            {
                _steps = ComfyUITaskStatus.Post;
            }
            else if (current is ComfyUIWaitWebsocketNode)
            {
                _steps = ComfyUITaskStatus.WebsocketWait;
            }
            else if (current is ComfyUIDownloadResultNode)
            {
                _steps = ComfyUITaskStatus.DownloadResult;
            }
        }

        /// <summary>
        /// ComfyUI任务状态机终止事件
        /// </summary>
        private void OnStateMachineTerminated(StateMachine arg1, string stopReason, int code)
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


        protected override void OnStart()
        {
            UniTask.Create(async () =>
            {
                try
                {
                    await ConnectComfyUI();
                    _smc.StartNode<ComfyUIPostNode>();
                }
                catch (Exception e)
                {
                    _smc.Stop(500,$"连接ComfyUI失败:{e.Message}");
                }
            });
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


        #region Websocket

        CancellationTokenSource cancellationTokenSource;
        private WebSocketClient ComfyUIWSClient;
        public async UniTask ConnectComfyUI()
        {
            ComfyUIWSClient?.Dispose();
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
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
                    .SetRemoteIPHost($"{(UseWss?"wss":"ws")}://{RemoteIPHost}/ws?clientId={ClientId}");
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
                AppLogger.Log($"ComfyUI websocket connected,client_id is {ClientId}");
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
                //AppLogger.Log($"ComfyUI websocket text message: \n{message}");
                var comfyui_msg_type=_json["type"]?.ToString();
                if (Enum.TryParse(comfyui_msg_type,out ComfyUIWebsocketMsgType _state))
                {
                    if (_state==ComfyUIWebsocketMsgType.execution_success)
                    {
                        var successMsg = JsonConvert.DeserializeObject<ExecutionSuccessMessage>(message);
                        if (successMsg != null)
                        {
                            AppLogger.Log($"ComfyUI websocket received execution_success message, prompt_id: {successMsg.MsgData.PromptId}");
                            _taskIDHandler.AddWSID(successMsg.MsgData.PromptId);
                        }
                    }
                    else if (_state == ComfyUIWebsocketMsgType.progress)
                    {
                        var progressMsg = JsonConvert.DeserializeObject<ProgressMessage>(message);
                        if (progressMsg != null)
                        {
                            OnProgress?.Invoke(progressMsg.MsgData);
                        }
                    }
                }
                else
                {
                    //AppLogger.Warning($"ComfyUI websocket received message type {comfyui_msg_type}  not implemented!! Raw message is :\n{message}");
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