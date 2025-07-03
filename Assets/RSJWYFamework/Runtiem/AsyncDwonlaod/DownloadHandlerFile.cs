using System.IO;
using UnityEngine.Networking;

namespace RSJWYFamework.Runtime
{
    public class DownloadHandlerFile : DownloadHandlerScript
    {
        /// <summary>
        /// 存储的文件
        /// </summary>
        private string filePath;
        /// <summary>
        /// 文件流
        /// </summary>
        private FileStream fileStream;
        
        DownloadFileAsyncOperation _downloadFileAsyncOperation;

        public DownloadHandlerFile(string path, DownloadFileAsyncOperation fileAsyncOperation) : base()
        {
            this.filePath = path;
            _downloadFileAsyncOperation=fileAsyncOperation;
        
            // 确保目录存在
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 打开文件流
            fileStream = new FileStream(
                path, 
                FileMode.Create, 
                FileAccess.Write, 
                FileShare.Read
            );
        }

        public void Close()
        {
            fileStream.Close();
            fileStream.Dispose();
        }

        /// <summary>
        /// 接收下载到的数据
        /// </summary>
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || data.Length == 0)
                return false;//终止下载器

            fileStream.Write(data, 0, dataLength);
            _downloadFileAsyncOperation.downloadedBytes += dataLength;
            return true;//继续下载
        }

        /// <summary>
        /// 当下载完成时被调用，可以在这里进行清理或最终处理。
        /// </summary>
        protected override void CompleteContent()
        {
            Close();
        }
    }
}