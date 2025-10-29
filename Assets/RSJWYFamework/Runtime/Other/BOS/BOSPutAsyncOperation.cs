using System;

namespace RSJWYFamework.Runtime.BOS
{
    public class BOSPutAsyncOperation:AppGameAsyncOperation
    {

        /// <summary>
        /// PUT提交信息
        /// </summary>
        private PutInfo putInfo;




        public BOSPutAsyncOperation(PutInfo putInfo)
        {
            
        }
        
        
        
        
        
        protected override void OnStart()
        {
            
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnSecondUpdate()
        {
        }

        protected override void OnAbort()
        {
        }

        protected override void OnSecondUpdateUnScaleTime()
        {
        }

        protected override void OnWaitForAsyncComplete()
        {
        }
    }

    public struct PutInfo
    {
        /// <summary>
        /// accessKeyId
        /// </summary>
        public string accessKeyId;
        /// <summary>
        /// secretAccessKey
        /// </summary>
        public string secretAccessKey;
        /// <summary>
        /// 存储区域
        /// </summary>
        public string region ;
        /// <summary>
        /// 服务类型，固定位bos
        /// </summary>
        public const string service = "BceBos"; 
        /// <summary>
        /// Put请求
        /// </summary>
        public const string httpMethod = "PUT"; 
        /// <summary>
        /// 你的存储桶名
        /// </summary>
        public string bucketName ; 
        /// <summary>
        /// 上传后的对象键（BOS中的路径）
        /// </summary>
        public string objectKey;

        /// <summary>
        /// 规范URI路径（必须以/开头）
        /// </summary>
        public string uriPath;

        /// <summary>
        /// Host头值
        /// </summary>
        public string host;
        
        /// <summary>
        /// HTTP 1.1协议中规定的GMT时间,Wed, 06 Apr 2016 06:34:40 GMT。
        /// </summary>
        public string date;
        /// <summary>
        /// 用于x-bce-date头（ISO格式）
        /// </summary>
        public string xBceDate; 

        internal void SetInternalInfo()
        {
            uriPath = $"/{objectKey}";
            date= DateTime.UtcNow.ToString("yyyyMMdd");
            date= DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            host= $"{bucketName}.{region}.bcebos.com"; 
        }
    }
}