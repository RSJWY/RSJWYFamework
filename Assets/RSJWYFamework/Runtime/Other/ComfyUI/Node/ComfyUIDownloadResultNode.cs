using System;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RSJWYFamework.Runtime.Node
{
    public class ComfyUIDownloadResultNode: StateNodeBase
    {
        private PromptInfo promptInfo;
        private string _remoteIPHost;
        private Texture2D _texture;
        /// <summary>
        /// 获取历史图片URL的处理函数，用户手动处理获取输出的图片URL
        /// </summary>
        private ComfyUITaskAsyncOperation.GetHistoryImageURLHandle _getHistoryImageURL;
        private bool _useHttps;
        public override void OnInit()
        {
            
        }

        public override void OnClose()
        {
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            promptInfo=GetBlackboardValue<PromptInfo>("PROMPTINFO");
            _useHttps=GetBlackboardValue<bool>("USEHTTPS");
            _remoteIPHost=GetBlackboardValue<string>("REMOTEIPHOST");
            _getHistoryImageURL=GetBlackboardValue<ComfyUITaskAsyncOperation.GetHistoryImageURLHandle>("GETHISTORYIMAGEURL");
            DownloadImageURL().Forget();
        }

        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
            
        }

        private async UniTask DownloadImageURL()
        {
            var result=await GetHistoryImageURL();
            if(result.Success)
            {
                SetBlackboardValue("IMAGEURL", result.ImageURL);
            }
            else
            {
                TerminateStateMachine($"获取历史图片URL失败！错误：{result.Error ?? "未知错误"}",500);
            }
            _texture=await DownloadImage(result.ImageURL);
            if(_texture==null)
            {
                TerminateStateMachine($"下载图片失败！",500);
            }
            else
            {
                SetBlackboardValue("TEXTURE", _texture);
                TerminateStateMachine("下载图片成功！",0);
            }
        }
        private async UniTask<Texture2D> DownloadImage(string imageURL)
        {
            var URL=$"{(_useHttps ? "https" : "http")}://{_remoteIPHost}/view{imageURL}";
            AppLogger.Log($"下载图片URL：{URL}");
            using (var httpClient = new HttpClient())
            {
                try
                {
                    // 发送GET请求获取图片字节流（图片下载默认用GET）
                    byte[] imageData = await httpClient.GetByteArrayAsync(URL);

                    // 将字节流转换为Texture2D
                    Texture2D texture = new Texture2D(2, 2); // 初始尺寸不影响，LoadImage会自动调整
                    bool isLoaded = texture.LoadImage(imageData); // 支持PNG、JPG等常见格式

                    if (isLoaded)
                    {
                        AppLogger.Log("图片下载成功！");
                        return texture;
                    }
                    else
                    {
                        AppLogger.Error("图片解析失败（字节流不是有效的图片格式）");
                        TerminateStateMachine("下载图片失败：字节流无法解析为图片", 500);
                        return null;
                    }
                }
                catch (HttpRequestException ex)
                {
                    // 处理网络连接错误（如无网络、DNS失败、超时等）
                    AppLogger.Error("网络请求错误：" + ex.Message);
                    TerminateStateMachine($"下载图片失败：网络错误 - {ex.Message}", 500);
                    return null;
                }
                catch (Exception ex)
                {
                    // 处理其他未知错误
                    AppLogger.Error("下载图片时发生未知错误：" + ex.Message);
                    TerminateStateMachine($"下载图片失败：未知错误 - {ex.Message}", 500);
                    return null;
                }
            }
        }
        private async UniTask<GetHistoryImageURLResult> GetHistoryImageURL()
        {
            var _prompt_id=promptInfo.PromptId;
            var imageUrl = $"{(_useHttps ? "https" : "http")}://{_remoteIPHost}/history/{_prompt_id}";
            AppLogger.Log($"获取历史图片URL：{imageUrl}");
            using (var httpClient = new HttpClient())
            {
                try
                {
                    // 发送GET请求并获取响应文本
                    string responseText = await httpClient.GetStringAsync(imageUrl);

                    // 解析响应为JObject
                    JObject historyInfo = JObject.Parse(responseText);
                    AppLogger.Log("GET成功！响应：" + responseText);

                    // 调用处理方法并返回结果
                    return _getHistoryImageURL(historyInfo, _prompt_id);
                }
                catch (HttpRequestException ex)
                {
                    // 网络层面错误（无网络、连接超时、DNS失败等，无HTTP状态码）
                    AppLogger.Error("网络请求失败：" + ex.Message);
                    TerminateStateMachine($"Get请求失败：网络错误 - {ex.Message}", 500);

                    return new GetHistoryImageURLResult()
                    {
                        ImageURL = string.Empty,
                        Success = false
                    };
                }
                catch (Exception ex)
                {
                    // 处理其他未知错误（如JSON解析失败等）
                    AppLogger.Error("GET请求发生未知错误：" + ex.Message);
                    TerminateStateMachine($"Get请求失败！未知错误：{ex.Message}", 500);

                    return new GetHistoryImageURLResult()
                    {
                        ImageURL = string.Empty,
                        Success = false
                    };
                }
            }
        }
    }
    
    public struct GetHistoryImageURLResult
    {
        /// <summary>
        /// 使用/view接口，所以返回的图片URL需要拼接完整路径
        /// ?filename=ComfyUI_00702_.png&type=output
        /// 可使用本方法提供的静态函数ComfyUITaskAsyncOperation.GetFullImageURL
        /// </summary>
        public string ImageURL;
        public bool Success;
        public string Error;
        
        public static string GetFullImageURL(string filename,string type)
        {
            return $"?filename={filename}&type={type}";
        }
    }
}