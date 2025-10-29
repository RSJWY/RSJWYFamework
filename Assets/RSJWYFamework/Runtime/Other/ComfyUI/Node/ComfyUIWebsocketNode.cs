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
            ConnectComfyUI().Forget();
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
                    .SetRemoteIPHost($"{(_useWss?"wss":"ws")}://{_remoteIPHost}/ws?client_id={_clientid}");
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
                AppLogger.Log("ComfyUI websocket connected");
            }
            catch (Exception e)
            {
                AppLogger.Error($"ComfyUI websocket connect failed,error is {e.Message}");
                StopStateMachine($"ComfyUI websocket connect failed,error is {e.Message}",500);
            }
            
        }
        
        private void HandleTextMessage(string message)
        {
            try
            {
                var _json=JObject.Parse(message);
                var comfyui_msg_type=_json["type"]?.ToString();
                if (Enum.TryParse(comfyui_msg_type,out ComfyUIWebsocketMsgType _state))
                {
                    if (_state==ComfyUIWebsocketMsgType.execution_success)
                    {
                        AppLogger.Log($"ComfyUI websocket received execution_success message");
                        SwitchToNode<ComfyUIDownloadResultNode>();
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

       
    }
    
     /// <summary>
    /// ComfyUI websocket 消息转换器
    /// </summary>
    public class ComfyWebsocketMessageConverter : JsonConverter
    {
        // 消息类型字符串 → 对应的 Message 类型
        private static readonly Dictionary<string, Type> TypeMap = new()
        {
            [nameof(ComfyUIWebsocketMsgType.status)] = typeof(StatusMessage),
            [nameof(ComfyUIWebsocketMsgType.execution_start)] = typeof(ExecutionStartMessage),
            [nameof(ComfyUIWebsocketMsgType.executing)] = typeof(ExecutingMessage),
            [nameof(ComfyUIWebsocketMsgType.progress)] = typeof(ProgressMessage),
            [nameof(ComfyUIWebsocketMsgType.execution_success)] = typeof(ExecutionSuccessMessage),
            [nameof(ComfyUIWebsocketMsgType.execution_cached)] = typeof(ExecutionCachedMessage),
            // 以后再加
        };

        /// <summary>
        /// 判断是否可以转换为指定的消息类型
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType) =>
            typeof(ComfyUIWebsocketBaseMessage<BaseComfyUImanager>).IsAssignableFrom(objectType);

        /// <summary>
        /// 从 JSON 读取消息
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);

            // 取 type 字段
            var typeToken = jo["type"];
            if (typeToken == null)
                throw new JsonException("Missing 'type' field.");

            var typeStr = typeToken.Value<string>()!;
            if (!TypeMap.TryGetValue(typeStr, out var targetType))
                throw new JsonException($"Unknown message type: {typeStr}");

            // 让 JsonSerializer 继续反序列化到具体子类
            return jo.ToObject(targetType, serializer);
        }

        /// <summary>
        /// 写入 JSON 数据
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer)
        {
            // 如果用不到序列化，直接抛异常即可
            throw new NotImplementedException("Only deserialization is supported.");
        }
    }
    public enum ComfyUIWebsocketMsgType
    {
        status,
        execution_start,
        executing,
        progress,
        execution_success,
        execution_cached,
    }

    public abstract class ComfyUIWebsocketBaseMessage<TData> where TData : BaseComfyUImanager,new() // 约束TData必须有默认构造函数，方便反序列化
    {
        [JsonProperty("type")]
        public abstract string MsgType { get; } // 抽象属性，由子类指定具体type值

        [JsonProperty("data")]
        public TData MsgData { get; set; } = new TData(); // 泛型Data，默认初始化避免空引用
    }

    public class BaseComfyUImanager
    {
    }
    /// <summary>
    /// 状态消息
    /// </summary>
    public class StatusMessage : ComfyUIWebsocketBaseMessage<StatusData>
    {
        public override string MsgType => "status"; // 固定type值
    }
    /// <summary>
    /// 任务开始消息，携带返回你提交的任务ID，请判断任务ID是否与你提交的任务ID一致
    /// </summary>
    public class ExecutionStartMessage : ComfyUIWebsocketBaseMessage<ExecutionStartData>
    {
        public override string MsgType => "execution_start"; // 固定type值
    }
    
    /// <summary>
    /// 任务执行中消息，携带返回你提交的任务ID，请判断任务ID是否与你提交的任务ID一致
    /// </summary>
    public class ExecutingMessage : ComfyUIWebsocketBaseMessage<ExecutingData>
    {
        public override string MsgType => "executing"; // 固定type值
    }
   
    /// <summary>
    /// 任务步数数据，哪个节点的步数执行进度
    /// </summary>
    public class ProgressMessage : ComfyUIWebsocketBaseMessage<ProgressData>
    {
        public override string MsgType => "progress"; // 固定type值
    }
    
    /// <summary>
    /// 任务成功消息，携带返回你提交的任务ID，请判断任务ID是否与你提交的任务ID一致
    /// </summary>
    public class ExecutionSuccessMessage : ComfyUIWebsocketBaseMessage<ExecutionSuccessData>
    {
        public override string MsgType => "execution_success"; // 固定type值
    }
    /// <summary>
    /// 某些节点因为输出已存在于缓存中，本次执行被跳过。
    /// 缓存的节点列表，这些节点将在本次运行中跳过（上次执行后参数未变，直接使用上次缓存的结果）
    /// </summary>
    public class ExecutionCachedMessage : ComfyUIWebsocketBaseMessage<ExecutionCachedData>
    {
        public override string MsgType => "execution_cached"; // 固定type值
    }

    #region status
    public class StatusData : BaseComfyUImanager
    {
        [JsonProperty("status")]
        public StatusInfo Status { get; set; } = new StatusInfo();
        /// <summary>
        /// 会话ID
        /// </summary>
        /// <remarks>这是ComfyUI的会话ID，用于唯一标识一个会话，即你WS请求的ClientID</remarks>
        [JsonProperty("sid")]
        public string SID { get; set; } = string.Empty;
    }

    public class StatusInfo
    {
        [JsonProperty("exec_info")]
        public ExecInfo ExecInfo { get; set; } = new ExecInfo();
    }

    public class ExecInfo
    {
        /// <summary>
        /// 队列剩余数量
        /// </summary>
        /// <remarks>这是本ComfyUI的全局队列剩余数量，详细队列信息请请求GET /queue</remarks>
        [JsonProperty("queue_remaining")]
        public int QueueRemaining { get; set; }
    }


    #endregion
    #region execution_start
    
    public class ExecutionStartData : BaseComfyUImanager
    {
        /// <summary>
        /// 提交的图片的任务ID
        /// </summary>
        [JsonProperty("prompt_id")]
        public string PromptId { get; set; } = string.Empty;
        /// <summary>
        /// 消息时间戳，本消息发送时间的时间戳，单位毫秒
        /// </summary>
        [JsonProperty("prompt")]
        public long TimeStamp { get; set; }
    }

    #endregion

    #region executing
    public class ExecutingData : BaseComfyUImanager
    {
        /// <summary>
        /// 提交的图片的任务ID
        /// </summary>
        [JsonProperty("prompt_id")]
        public string PromptId { get; set; } = string.Empty;
        /// <summary>
        /// 当前节点执行到的节点，当为null时，同时代表任务执行完成
        /// </summary>
        [JsonProperty("node")]
        public string Node { get; set; }
        /// <summary>
        /// 当前节点执行到的节点的显示名称？
        /// 不知道，看发来信息是有这条数据，设置为可null，不需要管这个参数
        /// </summary>
        [JsonProperty("display_node")] 
        public string DisplayNode { get; set; }
    }

    #endregion
    #region execution_cached
    /// <summary>
    /// 任务缓存数据
    /// </summary>
    public class ExecutionCachedData : BaseComfyUImanager
    {
        /// <summary>
        /// 缓存的节点列表，这些节点将在本次运行中跳过（上次执行后参数未变，直接使用上次缓存的结果）
        /// </summary>
        [JsonProperty("nodes")]
        public string[] Nodes { get; set; }
        /// <summary>
        /// 提交的图片的任务ID
        /// </summary>
        [JsonProperty("prompt_id")]
        public string PromptId { get; set; } = string.Empty;
        /// <summary>
        /// 消息时间戳，本消息发送时间的时间戳，单位毫秒
        /// </summary>
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }
    }
    
    #endregion
    

    #region progress
    public class ProgressData : BaseComfyUImanager
    {
        /// <summary>
        /// 当前节点执行的步数
        /// </summary>
        [JsonProperty("value")]
        public int Value { get; set; }
        
        /// <summary>
        /// 最大执行步数
        /// </summary>
        [JsonProperty("max")]
        public int Max { get; set; }
        
        /// <summary>
        /// 任务ID
        /// </summary>
        [JsonProperty("prompt_id")]
        public string PromptId { get; set; }
        
        
        /// <summary>
        /// 当前节点执行到的节点
        /// </summary>
        [JsonProperty("node")]
        public string Node { get; set; }
    }

    #endregion
    
    #region execution_success
    /// <summary>
    /// 任务成功数据
    /// </summary>
    public class ExecutionSuccessData : BaseComfyUImanager
    {
        /// <summary>
        /// 提交的图片的任务ID
        /// </summary>
        [JsonProperty("prompt_id")]
        public string PromptId { get; set; } = string.Empty;
        /// <summary>
        /// 消息时间戳，本消息发送时间的时间戳，单位毫秒
        /// </summary>
        [JsonProperty("prompt")]
        public long TimeStamp { get; set; }
    }
    #endregion
}