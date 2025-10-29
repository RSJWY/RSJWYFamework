using System;
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
        private Func<JObject,GetHistoryImageURLResult> _getHistoryImageURL;
        public override void OnInit()
        {
            
        }

        public override void OnClose()
        {
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            promptInfo=GetBlackboardValue<PromptInfo>("PROMPTINFO");
            _remoteIPHost=GetBlackboardValue<string>("REMOTEIPHOST");
            _getHistoryImageURL=GetBlackboardValue<Func<JObject,GetHistoryImageURLResult>>("GETHISTORYIMAGEURL");
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
                StopStateMachine($"获取历史图片URL失败！错误：{result.Error ?? "未知错误"}",500);
            }
            _texture=await DownloadImage(result.ImageURL);
            if(_texture==null)
            {
                StopStateMachine($"下载图片失败！",500);
            }
            else
            {
                SetBlackboardValue("TEXTURE", _texture);
                StopStateMachine("下载图片成功！",0);
            }
        }
        private async UniTask<Texture2D> DownloadImage(string imageURL)
        {
            var URL=$"{_remoteIPHost}/{imageURL}";
            using ( UnityWebRequest request = UnityWebRequestTexture.GetTexture(URL))
            {
                await request.SendWebRequest().ToUniTask();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    AppLogger.Log("POST成功！响应：" + request.downloadHandler.text);
                    // 解析响应（如需要）
                    // var response = JsonUtility.FromJson<ResponseData>(webRequest.downloadHandler.text);
                    Texture2D texture =DownloadHandlerTexture.GetContent(request);
                    return texture;
                }
                else
                {
                    AppLogger.Error("状态码：" + request.responseCode);
                    AppLogger.Error("POST失败！错误：" + request.error);
                    StopStateMachine($"下载图片失败！错误：{request.error},状态码：{request.responseCode}",500);
                    return null;
                }
            }  
        }
        private async UniTask<GetHistoryImageURLResult> GetHistoryImageURL()
        {
            var _prompt_id=promptInfo.PromptId;
            var imageUrl = $"{_remoteIPHost}/history/{_prompt_id}";
            using ( UnityWebRequest request = new UnityWebRequest(imageUrl, "GET"))
            {
                await request.SendWebRequest().ToUniTask();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    AppLogger.Log("POST成功！响应：" + request.downloadHandler.text);
                    // 解析响应（如需要）
                    // var response = JsonUtility.FromJson<ResponseData>(webRequest.downloadHandler.text);
                    JObject historyInfo = JObject.Parse(request.downloadHandler.text);
                    return _getHistoryImageURL(historyInfo);
                     
                }
                else
                {
                    AppLogger.Error("状态码：" + request.responseCode);
                    AppLogger.Error("POST失败！错误：" + request.error);
                    StopStateMachine($"PostJson失败！错误：{request.error},状态码：{request.responseCode}",500);
                    return new GetHistoryImageURLResult()
                    {
                        ImageURL=string.Empty,
                        Success=false
                    };
                }
            }  
        }
    }
    
    public struct GetHistoryImageURLResult
    {
        
        public string ImageURL;
        public bool Success;
        public string Error;
    }
}