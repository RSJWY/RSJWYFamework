using System;
using System.IO;
using UnityEngine.Networking;

namespace RSJWYFamework.Runtime
{
    public class DownloadFileAsyncOperation:AppGameAsyncOperation
    {
        private enum DownloadFileSteps
        {
            None,
            GetTotalBytes,
            Update,
            Pause,
            Abort,
            Done
        }
        
        
        /// <summary>
        /// 下载的URL
        /// </summary>
        public string url{get;private set;}
        /// <summary>
        /// 存储路径
        /// </summary>
        public string savePath{get;private set;}
        /// <summary>
        /// Unity的下载器
        /// </summary>
        private UnityWebRequest request;
        /// <summary>
        /// 文件总大小
        /// </summary>
        public long totalBytes{get;private set;}
        /// <summary>
        /// 已下载大小
        /// </summary>
        public long downloadedBytes{get;internal set;}
        /// <summary>
        /// 下载进度率刷新
        /// </summary>
        public event Action<float> OnProgress;
        /// <summary>
        /// 完成下载
        /// </summary>
        public Action<DownloadFileAsyncOperation> onCompleted;
        /// <summary>
        /// 下载文件处理器
        /// </summary>
        public DownloadHandlerFile downloadHandlerFile;
        
        private DownloadFileSteps _steps = DownloadFileSteps.None;

        /// <summary>
        /// 创建下载异步任务
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="savePath">保存路径（如果路径上有占用，则直接执行删除）</param>
        public DownloadFileAsyncOperation(string url, string savePath)
        {
            this.url = url;
            this.savePath = savePath;
        }

        /// <summary>
        /// 启动下载
        /// </summary>
        public void BeginDownload()
        {
            if (_steps==DownloadFileSteps.None)
            {
                //清理本地文件
                if (Utility.FileAndFolder.EnsureDirectoryExists(Utility.FileAndFolder.GetDirectoryPath(savePath)))
                {
                    if (File.Exists(savePath))
                    {
                        File.Delete(savePath);
                    }
                }
                AppAsyncOperationSystem.StartOperation(Utility.Timestamp.UnixTimestampMilliseconds.ToString(),this);
            }
        }
        protected override void OnStart()
        {
            //创建下载请求
            request = UnityWebRequest.Get(url);
            downloadHandlerFile = new DownloadHandlerFile(savePath, this);
            request.downloadHandler = downloadHandlerFile;
            request.SendWebRequest();
            Progress = 0;
            _steps = DownloadFileSteps.GetTotalBytes;
        }

        protected override void OnUpdate()
        {
            if (_steps is DownloadFileSteps.None or DownloadFileSteps.Done)
            {
                return;
            }
            if (_steps == DownloadFileSteps.GetTotalBytes)
            {
                if (totalBytes>0)
                {
                    _steps = DownloadFileSteps.Update;
                }
                //这两种状态时，获取长度信息头
                if (request.result == UnityWebRequest.Result.InProgress||request.result==UnityWebRequest.Result.Success )
                {
                    //获取，保证不为空和能正常把string转为long值
                    string lengthHeader = request.GetResponseHeader("Content-Length");
                    if (!string.IsNullOrEmpty(lengthHeader) && long.TryParse(lengthHeader, out long size))
                    {
                        //存储
                        totalBytes = size;
                        AppLogger.Log($"获取到要下载的文件总大小为：{totalBytes}");
                        _steps = DownloadFileSteps.Update;
                    }
                }
            }
            if (_steps == DownloadFileSteps.Update)
            {
                //更新下载进度
                Progress=Utility.AsyncDownloader.GetProgress(this);
                if (OnProgress != null)
                {
                    OnProgress.Invoke(Progress);
                }
                if (request.result == UnityWebRequest.Result.Success)
                {
                    _steps = DownloadFileSteps.Done;
                    Status = AppAsyncOperationStatus.Succeed;
                }
                else
                {
                    _steps = DownloadFileSteps.Done;
                    Status = AppAsyncOperationStatus.Succeed;
                    Error=request.error;
                }
            }
            
        }

        protected override void OnSecondUpdate()
        {
            
        }

        /// <summary>
        /// 暂停下载
        /// </summary>
        public void PauseDownload()
        {
            //因为UnityWebRequest并不支持暂停，只能基于取消，重启时再重新创建下载器
            //这里暂停并不执行异步系统里的取消操作，整体异步仍然在执行等待阶段
            if (_steps!=DownloadFileSteps.Done||_steps!=DownloadFileSteps.Abort||_steps!=DownloadFileSteps.Pause)
            {
                request?.Abort();
            }
        }
        
        /// <summary>
        /// 重启下载
        /// </summary>
        public void ResumeDownload()
        {
            if (_steps==DownloadFileSteps.Pause||_steps!=DownloadFileSteps.Done||_steps!=DownloadFileSteps.Abort)
            {
                //如果断点续传，则需要设置Rangeq请求头，设置请求的数据范围
                request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Range", $"bytes={downloadedBytes}-");
                //downloadHandlerFile = new DownloadHandlerFile(savePath, true,this);
                request.downloadHandler = downloadHandlerFile;
                _steps = DownloadFileSteps.GetTotalBytes;
                request.SendWebRequest();
            }
        }

        public void AbortDownload()
        {
            Abort();
        }
        protected override void OnAbort()
        {
            if (_steps!=DownloadFileSteps.Done||_steps!=DownloadFileSteps.Abort)
            {
                request?.Abort();
                _steps = DownloadFileSteps.Abort;
                downloadHandlerFile.Close();
            }
        }

        protected override void OnSecondUpdateUnScaleTime()
        {
        }

        protected override void OnWaitForAsyncComplete()
        {
        }
    }
}