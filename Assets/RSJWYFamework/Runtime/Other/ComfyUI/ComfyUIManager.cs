using System;
using Cysharp.Threading.Tasks;
using TouchSocket.Core;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;
using Newtonsoft.Json;

namespace RSJWYFamework.Runtime
{
    public class ComfyUIManager:ModuleBase
    {
        public string ClientID;
        public string ComfyUIRemote="127.0.0.1:8188";
        private WebSocketClient ComfyUIWSClient;
        public bool UseWss = false;
        public override void Initialize()
        {
            UpdateGuid();
            ConnectComfyUI().Forget();
        }

        public override void Shutdown()
        {
            ComfyUIWSClient?.Dispose();
        }
        /// <summary>
        /// 连接ComfyUI websocket
        /// </summary>
        /// <returns>是否连接成功</returns>
        public async UniTask<bool> ConnectComfyUI()
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
                    .SetRemoteIPHost($"{(UseWss?"wss":"ws")}://{ComfyUIRemote}/ws?client_id={ClientID}");
                ComfyUIWSClient = new WebSocketClient();
                await ComfyUIWSClient.SetupAsync(_wsConfig);
                await ComfyUIWSClient.ConnectAsync();
                AppLogger.Log("ComfyUI websocket connected");
                return true;
            }
            catch (Exception e)
            {
                AppLogger.Error($"ComfyUI websocket connect failed,error is {e.Message}");
                return false;
            }
            
        }
        public void UpdateGuid(string clientID="")
        {
            if (string.IsNullOrEmpty(clientID))
            {
                ClientID = Guid.NewGuid().ToString();
            }
            else
            {
                ClientID = clientID;
            }
        }
        
    }
    
    
    
}