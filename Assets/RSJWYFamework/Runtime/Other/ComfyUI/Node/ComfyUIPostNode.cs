using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TouchSocket.Http;
using UnityEngine;
using UnityEngine.Networking;
using HttpClient = System.Net.Http.HttpClient;

namespace RSJWYFamework.Runtime.Node
{
    /// <summary>
    /// Post提交任务
    /// </summary>
    public class ComfyUIPostNode : StateNodeBase
    {
        /// <summary>
        /// 客户端id
        /// </summary>
        private string _clientid;
        /// <summary>
        /// json字符串
        /// </summary>
        private JObject _json;
        
        /// <summary>
        /// ComfyUI工作任务ID
        /// </summary>
        private PromptInfo promptInfo;
        
        /// <summary>
        /// 转换后的json字符串
        /// </summary>
        private string postJson;
        
        private bool _useHttps;
        
        /// <summary>
        /// ComfyUI服务器IP地址
        /// </summary>
        private string _remoteIPHost;
        public override void OnInit()
        {
            
        }

        public override void OnClose()
        {
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            // 从Blackboard获取值
            _clientid = GetBlackboardValue<string>("CLIENTID");
            _json = GetBlackboardValue<JObject>("JSON");
             _remoteIPHost = GetBlackboardValue<string>("REMOTEIPHOST");
             _useHttps = GetBlackboardValue<bool>("USEHTTPS");
            
            // 1. 创建主JSON对象
            JObject mainJson = new JObject();
            mainJson["client_id"] =_clientid; // 字符串
            mainJson["prompt"] = _json; // 数字
            
            // 2. 转换为字符串
            postJson = mainJson.ToString();
            
            PostJson().Forget();
        }
        private async UniTaskVoid PostJson()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    // 构建请求URL
                    string requestUrl = $"{(_useHttps ? "https" : "http")}://{_remoteIPHost}/prompt";
            
                    // 创建JSON请求内容（自动设置Content-Type为application/json）
                    var content = new StringContent(postJson, Encoding.UTF8, "application/json");
            
                    // 发送POST请求
                    HttpResponseMessage response = await httpClient.PostAsync(requestUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        // 读取成功响应内容
                        string responseText = await response.Content.ReadAsStringAsync();
                        AppLogger.Log("POST成功！响应：" + responseText);
                
                        // 解析JSON响应
                        promptInfo = JsonConvert.DeserializeObject<PromptInfo>(responseText);
                        SetBlackboardValue("PROMPTINFO", promptInfo);
                        SwitchToNode<ComfyUIWaitWebsocketNode>();
                    }
                    else
                    {
                        // 处理请求失败（非2xx状态码）
                        int statusCode = (int)response.StatusCode;
                        string errorMessage = await response.Content.ReadAsStringAsync() ?? response.ReasonPhrase;
                
                        AppLogger.Error("状态码：" + statusCode);
                        AppLogger.Error("POST失败！错误：" + errorMessage);
                        TerminateStateMachine($"PostJson失败！错误：{errorMessage},状态码：{statusCode}", 500);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
        }
    }

    #region Post200

    /// <summary>
    /// 表示一个常规的提示信息响应。
    /// </summary>
    public class PromptInfo
    {
        /// <summary>
        /// 当前的任务ID
        /// </summary>
        [JsonProperty("prompt_id")]
        public string PromptId { get; set; }
        /// <summary>
        /// 在当前ComfyUI实例中这个任务序号（总的）
        /// </summary>
        [JsonProperty("number")]
        public int Number { get; set; }
        // 即使在成功的情况下node_errors也可能存在（尽管是空的），所以包含它
        [JsonProperty("node_errors")]
        public Dictionary<string, NodeErrorDetail> NodeErrors { get; set; }
    }

    #endregion

    #region Post400

    
    /// <summary>
    /// 表示一个包含错误的响应。
    /// </summary>
    public class PromptError
    {
        [JsonProperty("error")]
        public TopLevelError Error { get; set; }
        [JsonProperty("node_errors")]
        public Dictionary<string, NodeErrorDetail> NodeErrors { get; set; }
    }
    /// <summary>
    /// 顶层错误的详细信息。
    /// </summary>
    public class TopLevelError
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("details")]
        public string Details { get; set; }
        // extra_info 在示例中是空对象{}，可能包含动态键值对，
        // 使用 JObject 可以灵活处理。
        [JsonProperty("extra_info")]
        public JObject ExtraInfo { get; set; }
    }
    /// <summary>
    /// 特定节点的错误详情。
    /// </summary>
    public class NodeErrorDetail
    {
        [JsonProperty("errors")]
        public List<ErrorDetail> Errors { get; set; }
        [JsonProperty("dependent_outputs")]
        public List<string> DependentOutputs { get; set; }
        [JsonProperty("class_type")]
        public string ClassType { get; set; }
    }
    /// <summary>
    /// 单个错误的具体信息。
    /// </summary>
    public class ErrorDetail
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("details")]
        public string Details { get; set; }
        [JsonProperty("extra_info")]
        public ExtraInfo ExtraInfo { get; set; }
    }
    /// <summary>
    /// 错误的额外补充信息。
    /// </summary>
    public class ExtraInfo
    {
        [JsonProperty("input_name")]
        public string InputName { get; set; }
        // input_config 是一个混合类型的数组, [["..."], {"..."}],
        // 使用 JToken (或 JArray) 是处理这种结构的最佳实践。
        [JsonProperty("input_config")]
        public JToken InputConfig { get; set; }
        [JsonProperty("received_value")]
        public string ReceivedValue { get; set; }
    }

    #endregion
}