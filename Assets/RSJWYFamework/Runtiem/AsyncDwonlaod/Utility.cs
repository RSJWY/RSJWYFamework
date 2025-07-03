namespace RSJWYFamework.Runtime
{
    public static partial class Utility
    {
        public static class AsyncDownloader
        {
            /// <summary>
            /// 根据任务获取下载进度
            /// </summary>
            /// <param name="fileAsyncOperation"></param>
            /// <returns></returns>
            public static float GetProgress(DownloadFileAsyncOperation fileAsyncOperation)
            {
                if (fileAsyncOperation == null || fileAsyncOperation.totalBytes <= 0) return 0;
                return Utility.GetProgress(fileAsyncOperation.totalBytes, fileAsyncOperation.downloadedBytes);
            }
        }
    }
}