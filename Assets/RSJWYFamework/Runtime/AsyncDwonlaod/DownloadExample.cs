using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// UnityWebRequestDownloader使用示例
    /// </summary>
    public class DownloadExample : MonoBehaviour
    {
        [Header("下载设置")]
        public string downloadUrl = "https://example.com/file.txt";
        public string saveFilePath = "D:/Downloads/downloaded_file.txt";
        
        private void Start()
        {
            // 示例：下载文本到内存
            DownloadTextExample();
            
            // 示例：下载文件到磁盘
            DownloadFileExample();
            
            // 示例：使用async/await方式下载
            DownloadWithAsyncAwait().Forget();
        }

        /// <summary>
        /// 示例：下载文本到内存
        /// </summary>
        private void DownloadTextExample()
        {
            Debug.Log("开始下载文本示例...");
            
            // 创建文本下载器
            var downloader = UnityWebRequestDownloader.CreateTextDownloader(downloadUrl);
            
            // 设置回调
            downloader.OnProgressUpdate = (progress) =>
            {
                Debug.Log($"下载进度: {progress * 100:F1}%");
            };
            
            downloader.OnDownloadComplete = (completedDownloader) =>
            {
                Debug.Log("文本下载完成!");
                Debug.Log($"下载的文本内容: {completedDownloader.DownloadText}");
                Debug.Log($"下载的数据大小: {completedDownloader.DownloadData?.Length ?? 0} bytes");
                
                // 记得释放资源
                completedDownloader.Dispose();
            };
            
            downloader.OnDownloadError = (error) =>
            {
                Debug.LogError($"下载失败: {error}");
            };
            
            // 添加到异步操作系统并开始下载
            downloader.StartAddAppAsyncOperationSystem("TextDownload");
        }

        /// <summary>
        /// 示例：下载文件到磁盘
        /// </summary>
        private void DownloadFileExample()
        {
            Debug.Log("开始下载文件示例...");
            
            // 创建文件下载器
            var downloader = UnityWebRequestDownloader.CreateFileDownloader(downloadUrl, saveFilePath);
            
            // 设置回调
            downloader.OnProgressUpdate = (progress) =>
            {
                Debug.Log($"文件下载进度: {progress * 100:F1}%");
            };
            
            downloader.OnDownloadComplete = (completedDownloader) =>
            {
                Debug.Log($"文件下载完成! 保存到: {saveFilePath}");
                
                // 记得释放资源
                completedDownloader.Dispose();
            };
            
            downloader.OnDownloadError = (error) =>
            {
                Debug.LogError($"文件下载失败: {error}");
            };
            
            // 添加到异步操作系统并开始下载
            downloader.StartAddAppAsyncOperationSystem("FileDownload");
        }

        /// <summary>
        /// 示例：使用async/await方式下载
        /// </summary>
        private async UniTask DownloadWithAsyncAwait()
        {
            Debug.Log("开始async/await下载示例...");
            
            try
            {
                // 创建下载器
                var downloader = UnityWebRequestDownloader.CreateTextDownloader(downloadUrl);
                
                // 设置进度回调
                downloader.OnProgressUpdate = (progress) =>
                {
                    Debug.Log($"Async下载进度: {progress * 100:F1}%");
                };
                
                // 添加到异步操作系统
                downloader.StartAddAppAsyncOperationSystem("AsyncDownload");
                
                // 等待下载完成
                await downloader;
                
                if (downloader.Status == AppAsyncOperationStatus.Succeed)
                {
                    Debug.Log("Async下载成功!");
                    Debug.Log($"下载的文本: {downloader.DownloadText}");
                }
                else
                {
                    Debug.LogError($"Async下载失败: {downloader.Error}");
                }
                
                // 释放资源
                downloader.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Async下载异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例：下载纹理
        /// </summary>
        public async UniTask DownloadTextureExample(string textureUrl)
        {
            Debug.Log("开始下载纹理示例...");
            
            try
            {
                // 创建纹理下载器
                var downloader = UnityWebRequestDownloader.CreateTextureDownloader(textureUrl);
                
                // 设置进度回调
                downloader.OnProgressUpdate = (progress) =>
                {
                    Debug.Log($"纹理下载进度: {progress * 100:F1}%");
                };
                
                // 添加到异步操作系统
                downloader.StartAddAppAsyncOperationSystem("TextureDownload");
                
                // 等待下载完成
                await downloader;
                
                if (downloader.Status == AppAsyncOperationStatus.Succeed)
                {
                    Debug.Log("纹理下载成功!");
                    
                    // 使用下载的纹理
                    if (downloader.DownloadTexture != null)
                    {
                        // 可以将纹理应用到材质或UI等
                        Debug.Log($"纹理尺寸: {downloader.DownloadTexture.width}x{downloader.DownloadTexture.height}");
                    }
                }
                else
                {
                    Debug.LogError($"纹理下载失败: {downloader.Error}");
                }
                
                // 释放资源
                downloader.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"纹理下载异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例：批量下载
        /// </summary>
        public async UniTask BatchDownloadExample(string[] urls)
        {
            Debug.Log("开始批量下载示例...");
            
            try
            {
                var downloaders = new UnityWebRequestDownloader[urls.Length];
                
                // 创建所有下载器
                for (int i = 0; i < urls.Length; i++)
                {
                    downloaders[i] = UnityWebRequestDownloader.CreateTextDownloader(urls[i]);
                    downloaders[i].StartAddAppAsyncOperationSystem($"BatchDownload_{i}");
                }
                
                // 等待所有下载完成
                await UniTask.WhenAll(downloaders);
                
                // 处理结果
                for (int i = 0; i < downloaders.Length; i++)
                {
                    var downloader = downloaders[i];
                    if (downloader.Status == AppAsyncOperationStatus.Succeed)
                    {
                        Debug.Log($"批量下载 {i} 成功: {downloader.DownloadText?.Length ?? 0} 字符");
                    }
                    else
                    {
                        Debug.LogError($"批量下载 {i} 失败: {downloader.Error}");
                    }
                    
                    // 释放资源
                    downloader.Dispose();
                }
                
                Debug.Log("批量下载完成!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"批量下载异常: {ex.Message}");
            }
        }
    }
}