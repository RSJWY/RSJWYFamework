using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace RSJWYFamework.Runtiem
{
    [Module]
    public class AsyncDownloaderManager : ModuleBase
    {

        private List<DownloadTask> activeTasks = new List<DownloadTask>();

        /// <summary>
        /// 创建下载任务，并且立即启动下载
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="onProgress">进度回调</param>
        /// <param name="onCompleted">完成回调</param>
        /// <returns></returns>
        public DownloadTask DownloadFile(string url, string savePath, 
            Action<DownloadTask> onProgress = null, 
            Action<DownloadTask> onCompleted = null)
        {
            // 检查是否已有相同任务
            var existingTask = activeTasks.Find(t => t.url == url && t.savePath == savePath);
            if (existingTask != null)
            {
                if (existingTask.isPaused)
                {
                    ResumeDownload(existingTask);
                }
                return existingTask;
            }

            var task = new DownloadTask()
            {
                url = url,
                savePath = savePath,
                onProgress = onProgress,
                onCompleted = onCompleted,
                isPaused = false,
                isDone = false
            };

            activeTasks.Add(task);
            StartCoroutine(DownloadCoroutine(task));
            return task;
        }

        /// <summary>
        /// 异步执行的下载逻辑函数
        /// </summary>
        /// <param name="task">任务</param>
        /// <returns></returns>
        private IEnumerator DownloadCoroutine(DownloadTask task)
        {
            // 检查已下载文件大小（断点续传），基于已下载文件大小信息
            FileInfo fileInfo = new FileInfo(task.savePath);
            //判断是否需要徐
            bool resumeDownload = fileInfo.Exists && fileInfo.Length > 0;
        
            // 创建下载请求
            if (resumeDownload)
            {
                //如果断点续传，则需要设置Rangeq请求头，设置请求的数据范围
                task.request = UnityWebRequest.Get(task.url);
                task.request.SetRequestHeader("Range", $"bytes={fileInfo.Length}-");
            }
            else
            {
                //直接下载
                task.request = UnityWebRequest.Get(task.url);
            }

            var downlaodFileHandle=new DownloadHandlerFile(task.savePath, resumeDownload,task);
            // 设置下载处理器
            task.request.downloadHandler = downlaodFileHandle;
            task.downloadHandlerFile=downlaodFileHandle;
            // 开始下载
            task.request.SendWebRequest();
            
            

            // 更新进度
            // 在本循环内，每一帧检查是否请求暂停
            // 其实暂停就行进行了取消
            while (!task.request.isDone)
            {
                //如果总大小没有记录，则获取
                if (task.totalBytes<=0)
                {
                    //这两种状态时，获取长度信息头
                    if (task.request.result == UnityWebRequest.Result.InProgress||task.request.result==UnityWebRequest.Result.Success )
                    {
                        //获取，保证不为空和能正常把string转为long值
                        string lengthHeader = task.request.GetResponseHeader("Content-Length");
                        if (!string.IsNullOrEmpty(lengthHeader) && long.TryParse(lengthHeader, out long size))
                        {
                            //存储
                            task.totalBytes = size;
                        }
                    }
                }
                //Debug.Log($"长度：{task.request.GetResponseHeader("Content-Length")}+{task.request.result}+{task.totalBytes}");
                if (task.isPaused)
                {
                    task.request.Abort();
                    task.downloadHandlerFile.Pause();
                    yield break;
                }

                if (task.onProgress != null)
                {
                    task.onProgress.Invoke(task);
                }
                yield return null;
            }

            // 处理完成状态
            if (task.request.result == UnityWebRequest.Result.Success)
            {
                task.isDone = true;
            }
            else
            {
                task.isError = true;
                task.errorMessage = task.request.error;
            }

            // 触发完成回调
            if (task.onCompleted != null)
            {
                task.onCompleted.Invoke(task);
            }

            // 清理
            activeTasks.Remove(task);
            task.request.Dispose();
        }

        /// <summary>
        /// 暂停下载
        /// </summary>
        /// <param name="task">下载任务</param>
        public void PauseDownload(DownloadTask task)
        {
            if (task != null && !task.isPaused && !task.isDone)
            {
                task.isPaused = true;
                if (task.request != null)
                {
                    task.request.Abort();
                }
            }
        }

        /// <summary>
        /// 重启下载
        /// </summary>
        /// <param name="task"></param>
        public void ResumeDownload(DownloadTask task)
        {
            if (task != null && task.isPaused && !task.isDone)
            {
                task.isPaused = false;
                StartCoroutine(DownloadCoroutine(task));
            }
        }

        /// <summary>
        /// 取消下砸
        /// </summary>
        /// <param name="task"></param>
        public void CancelDownload(DownloadTask task)
        {
            if (task != null && !task.isDone)
            {
                task.isPaused = true;
                if (task.request != null)
                {
                    task.request.Abort();
                }
                activeTasks.Remove(task);
                if (File.Exists(task.savePath))
                {
                    File.Delete(task.savePath);
                }
            }
        }

        /// <summary>
        /// 根据任务获取下载进度
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static float GetProgress(DownloadTask task)
        {
            if (task == null || task.totalBytes <= 0) return 0;
            return (float)task.downloadedBytes / task.totalBytes;
        }

        public override void Initialize()
        {
            
        }

        public override void Shutdown()
        {
        }

        public override void ModuleUpdate()
        {
        }

    }
}