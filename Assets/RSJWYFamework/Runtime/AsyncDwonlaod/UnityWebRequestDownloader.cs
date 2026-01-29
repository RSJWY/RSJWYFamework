using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 基于UnityWebRequest的异步下载器
    /// </summary>
    public class UnityWebRequestDownloader : AppAsyncOperationBase
    {
        private UnityWebRequest _webRequest;
        private string _url;
        private string _savePath;
        private bool _isDownloadToFile;
        
        /// <summary>
        /// 下载的数据（当下载到内存时）
        /// </summary>
        public byte[] DownloadData { get; private set; }
        
        /// <summary>
        /// 下载的文本数据
        /// </summary>
        public string DownloadText { get; private set; }
        
        /// <summary>
        /// 下载的纹理（当下载纹理时）
        /// </summary>
        public Texture2D DownloadTexture { get; private set; }
        
        /// <summary>
        /// 下载进度更新回调
        /// </summary>
        public Action<float> OnProgressUpdate;
        
        /// <summary>
        /// 下载完成回调
        /// </summary>
        public Action<UnityWebRequestDownloader> OnDownloadComplete;
        
        /// <summary>
        /// 下载失败回调
        /// </summary>
        public Action<string> OnDownloadError;

        /// <summary>
        /// 构造函数 - 下载到内存
        /// </summary>
        /// <param name="url">下载URL</param>
        public UnityWebRequestDownloader(string url)
        {
            _url = url;
            _isDownloadToFile = false;
        }
        
        /// <summary>
        /// 构造函数 - 下载到文件
        /// </summary>
        /// <param name="url">下载URL</param>
        /// <param name="savePath">保存路径</param>
        public UnityWebRequestDownloader(string url, string savePath)
        {
            _url = url;
            _savePath = savePath;
            _isDownloadToFile = true;
        }

        /// <summary>
        /// 创建下载文本的下载器
        /// </summary>
        /// <param name="url">下载URL</param>
        /// <returns>下载器实例</returns>
        public static UnityWebRequestDownloader CreateTextDownloader(string url)
        {
            return new UnityWebRequestDownloader(url);
        }
        
        /// <summary>
        /// 创建下载纹理的下载器
        /// </summary>
        /// <param name="url">下载URL</param>
        /// <returns>下载器实例</returns>
        public static UnityWebRequestDownloader CreateTextureDownloader(string url)
        {
            return new UnityWebRequestDownloader(url);
        }
        
        /// <summary>
        /// 创建下载文件的下载器
        /// </summary>
        /// <param name="url">下载URL</param>
        /// <param name="savePath">保存路径</param>
        /// <returns>下载器实例</returns>
        public static UnityWebRequestDownloader CreateFileDownloader(string url, string savePath)
        {
            return new UnityWebRequestDownloader(url, savePath);
        }

        internal override void InternalStart()
        {
            if (string.IsNullOrEmpty(_url))
            {
                Status = AppAsyncOperationStatus.Failed;
                Error = "下载URL不能为空";
                OnDownloadError?.Invoke(Error);
                return;
            }

            try
            {
                // 根据下载类型创建不同的WebRequest
                if (_isDownloadToFile)
                {
                    _webRequest = UnityWebRequest.Get(_url);
                    _webRequest.downloadHandler = new DownloadHandlerFile(_savePath);
                }
                else
                {
                    _webRequest = UnityWebRequest.Get(_url);
                }
                
                // 设置超时时间
                _webRequest.timeout = 30;
                
                // 开始下载
                _webRequest.SendWebRequest();
                
                Status = AppAsyncOperationStatus.Processing;
            }
            catch (Exception ex)
            {
                Status = AppAsyncOperationStatus.Failed;
                Error = $"开始下载时发生错误: {ex.Message}";
                OnDownloadError?.Invoke(Error);
            }
        }

        internal override void InternalUpdate()
        {
            if (_webRequest == null || Status != AppAsyncOperationStatus.Processing)
                return;

            // 更新进度
            Progress = _webRequest.downloadProgress;
            OnProgressUpdate?.Invoke(Progress);

            // 检查是否完成
            if (_webRequest.isDone)
            {
                if (_webRequest.result == UnityWebRequest.Result.Success)
                {
                    // 下载成功
                    Status = AppAsyncOperationStatus.Succeed;
                    Progress = 1.0f;
                    
                    // 根据下载类型处理数据
                    if (!_isDownloadToFile)
                    {
                        DownloadData = _webRequest.downloadHandler.data;
                        DownloadText = _webRequest.downloadHandler.text;
                        
                        // 如果是纹理下载，尝试创建纹理
                        if (_webRequest.downloadHandler is DownloadHandlerTexture textureHandler)
                        {
                            DownloadTexture = textureHandler.texture;
                        }
                    }
                    
                    OnDownloadComplete?.Invoke(this);
                }
                else
                {
                    // 下载失败
                    Status = AppAsyncOperationStatus.Failed;
                    Error = $"下载失败: {_webRequest.error}";
                    OnDownloadError?.Invoke(Error);
                }
            }
        }

        internal override void InternalSecondUpdate()
        {
            
        }

        internal override void InternalSecondUnScaleTimeUpdate()
        {
        }

        internal override void InternalAbort()
        {
            if (_webRequest != null && !_webRequest.isDone)
            {
                _webRequest.Abort();
                Status = AppAsyncOperationStatus.Failed;
                Error = "下载被用户取消";
            }
        }

        /// <summary>
        /// 获取操作描述
        /// </summary>
        /// <returns>操作描述</returns>
        internal override string InternalGetDesc()
        {
            return $"下载文件: {_url}";
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_webRequest != null)
            {
                _webRequest.Dispose();
                _webRequest = null;
            }
            
            DownloadData = null;
            DownloadText = null;
            
            if (DownloadTexture != null)
            {
                UnityEngine.Object.Destroy(DownloadTexture);
                DownloadTexture = null;
            }
        }
    }
}