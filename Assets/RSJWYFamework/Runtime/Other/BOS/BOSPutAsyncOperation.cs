using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace RSJWYFamework.Runtime
{
    public class BOSAsync
    {
        /// <summary>
        /// Unity环境下通过UniTask+UnityWebRequest上传文件到BOS
        /// </summary>
        /// <param name="accessKeyId">AK</param>
        /// <param name="secretAccessKey">SK</param>
        /// <param name="bucketName">存储桶名</param>
        /// <param name="region">区域（如bj、gz）</param>
        /// <param name="objectKey">BOS对象键（如"images/test.png"）</param>
        /// <param name="localFilePath">本地文件路径（支持Unity路径格式，如"file:///C:/test.png"或Application.persistentDataPath下的路径）</param>
        /// <param name="storageClass">存储类型（默认STANDARD）</param>
        /// <returns>上传结果（成功/失败信息）</returns>
        public static async UniTask<UploadResp> UploadFileAsync(
            string accessKeyId, string secretAccessKey, string bucketName,
            string region, string objectKey, string localFilePath,
            string storageClass = "STANDARD")
        {
            // 1. 校验本地文件是否存在
            if (!File.Exists(localFilePath))
                return new UploadResp()
                {
                    success = false,
                    message = $"错误：本地文件不存在 → {localFilePath}"
                };

            try
            {
                // 2. 读取文件字节（Unity中处理文件需注意路径格式）
                byte[] fileData = File.ReadAllBytes(localFilePath);

                return await UploadBytesAsync(accessKeyId, secretAccessKey, bucketName, region, objectKey, fileData,
                    storageClass);
            }
            catch (Exception ex)
            {
                return new UploadResp()
                {
                    success = false,
                    message = $"上传异常！\n{ex.Message}\n堆栈: {ex.StackTrace}"
                };
            }
        }

        /// <summary>
        /// Unity环境下通过UniTask+UnityWebRequest上传字节数据到BOS
        /// </summary>
        /// <param name="accessKeyId">AK</param>
        /// <param name="secretAccessKey">SK</param>
        /// <param name="bucketName">存储桶名</param>
        /// <param name="region">区域（如bj、gz）</param>
        /// <param name="objectKey">BOS对象键（如"images/test.png"）</param>
        /// <param name="fileData">字节数据</param>
        /// <param name="storageClass">存储类型（默认STANDARD）</param>
        /// <returns>上传结果（成功/失败信息）</returns>
        public static async UniTask<UploadResp> UploadBytesAsync(
            string accessKeyId, string secretAccessKey, string bucketName,
            string region, string objectKey, byte[] fileData,
            string storageClass = "STANDARD")
        {
            try
            {
                long contentLength = fileData.LongLength;

                // 3. 基础参数配置
                string httpMethod = "PUT";
                string service = "bos";
                string host = $"{bucketName}.{region}.bcebos.com";
                string uriPath = $"/{objectKey}";
                string url = $"https://{host}{uriPath}";

                // 4. 日期参数（UTC时间，Unity中DateTime.UtcNow兼容各平台）
                string xBceDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                // 5. 构建请求头
                var headers = new Dictionary<string, string>
                {
                    { "Host", host },
                    { "x-bce-date", xBceDate },
                    { "Content-Length", contentLength.ToString() },
                    { "Content-Type", Utility.BOS.GetContentType(objectKey) },
                    { "x-bce-storage-class", storageClass }
                };

                // 可选：添加Content-MD5校验（防止文件损坏）
                string contentMd5 = Utility.BOS.CalculateMd5(fileData);
                if (!string.IsNullOrEmpty(contentMd5))
                    headers.Add("Content-MD5", contentMd5);

                // 6. 生成V1签名
                string authString = Utility.BOS.GenerateAuthString(
                    accessKeyId, secretAccessKey, httpMethod, uriPath,
                    new Dictionary<string, string>(), headers, xBceDate);

                // 7. 配置UnityWebRequest（PUT方法上传）
                using (UnityWebRequest webRequest = UnityWebRequest.Put(url, fileData))
                {
                    // 设置请求头（必须在Send前设置）
                    webRequest.SetRequestHeader("Authorization", authString);
                    foreach (var headersKV in headers)
                    {
                        // Unity会自动管理Host和Content-Length，手动设置会报警告，且可能导致未定义行为，因此跳过
                        if (headersKV.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                            headersKV.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        webRequest.SetRequestHeader(headersKV.Key, headersKV.Value);
                    }

                    // 8. 发送请求并等待完成（使用UniTask适配Unity异步）
                    // 注意：需传入CancellationToken，可绑定到MonoBehaviour生命周期
                    var asyncOp = webRequest.SendWebRequest();
                    await asyncOp.ToUniTask();

                    // 9. 处理响应结果
                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        string eTag = webRequest.GetResponseHeader("ETag") ?? "";
                        string versionId = webRequest.GetResponseHeader("x-bce-version-id") ?? "";
                        return new UploadResp()
                        {
                            success = true,
                            message = $"上传成功！\nETag: {eTag}\n版本号: {versionId}\nBOS路径: bos://{bucketName}/{objectKey}"
                        };
                    }
                    else
                    {
                        return new UploadResp()
                        {
                            success = false,
                            message = $"上传失败！\n错误: {webRequest.error}\n状态码: {webRequest.responseCode}\n响应: {webRequest.downloadHandler.text}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new UploadResp()
                {
                    success = false,
                    message =$"上传异常！\n{ex.Message}\n堆栈: {ex.StackTrace}"
                };
            }
        }
    }

    public struct UploadResp
    {
        public bool success;
        public string message;
    }
}