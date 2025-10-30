using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RSJWYFamework.Runtime
{
    public static partial class Utility
    {
        public static class BOS
        {
            /// <summary>
            /// 自动识别文件Content-Type
            /// </summary>
            public static string GetContentType(string objectKey)
            {
                string extension = Path.GetExtension(objectKey).ToLowerInvariant();
                return extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".txt" => "text/plain",
                    ".json" => "application/json",
                    ".pdf" => "application/pdf",
                    _ => "application/octet-stream" // 默认类型
                };
            }

            /// <summary>
            /// 计算字节数组的MD5（Base64格式，适配BOS的Content-MD5要求）
            /// </summary>
            public static string CalculateMd5(byte[] data)
            {
                using (MD5 md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(data);
                    return Convert.ToBase64String(hashBytes);
                }
            }

            /// <summary>
            /// 生成V2认证字符串（含签名）
            /// </summary>
            /// <param name="accessKeyId">Access Key ID</param>
            /// <param name="secretAccessKey">Secret Access Key</param>
            /// <param name="httpMethod">HTTP请求方法（GET/POST/PUT/DELETE/HEAD，全大写）</param>
            /// <param name="uriPath">请求URI路径（如/example/测试）</param>
            /// <param name="queryParameters">查询参数字典</param>
            /// <param name="headers">请求头字典</param>
            /// <param name="region">区域（如bj，小写）</param>
            /// <param name="service">服务名（如bos，小写）</param>
            /// <param name="date">UTC日期（格式yyyymmdd，如20250101）</param>
            /// <returns>完整V2认证字符串</returns>
            public static string GenerateAuthString(
                string accessKeyId,
                string secretAccessKey,
                string httpMethod,
                string uriPath,
                Dictionary<string, string> queryParameters,
                Dictionary<string, string> headers,
                string region,
                string service,
                string date)
            {
                // 1. 生成规范请求
                string canonicalRequest = BuildCanonicalRequest(httpMethod, uriPath, queryParameters, headers);

                // 2. 生成签名密钥
                string authStringPrefix = $"bce-auth-v2/{accessKeyId}/{date}/{region}/{service}";
                string signingKey = HmacSha256Hex(secretAccessKey, authStringPrefix);

                // 3. 生成签名
                string signature = HmacSha256Hex(signingKey, canonicalRequest);

                // 4. 生成signedHeaders（编码的头名小写+字典序+分号连接）
                string signedHeaders = GetSignedHeaders(headers);

                // 5. 拼接完整认证字符串
                return $"bce-auth-v2/{accessKeyId}/{date}/{region}/{service}/{signedHeaders}/{signature}";
            }

            /// <summary>
            /// 构建规范请求CanonicalRequest
            /// </summary>
            private static string BuildCanonicalRequest(
                string httpMethod,
                string uriPath,
                Dictionary<string, string> queryParameters,
                Dictionary<string, string> headers)
            {
                string canonicalUri = UriEncodeExceptSlash(uriPath);
                string canonicalQueryString = BuildCanonicalQueryString(queryParameters);
                string canonicalHeaders = BuildCanonicalHeaders(headers);

                // 按格式拼接，各部分用\n分隔
                return $"{httpMethod.ToUpper()}\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}";
            }

            /// <summary>
            /// UriEncodeExceptSlash：斜杠不编码，其他按RFC3986编码
            /// </summary>
            private static string UriEncodeExceptSlash(string input)
            {
                if (string.IsNullOrEmpty(input)) return "/";
                // 先执行标准Uri编码，再将%2F还原为/
                return Uri.EscapeDataString(input)
                    .Replace("%2F", "/")
                    .Replace("+", "%20") // 处理空格编码差异
                    .Replace("*", "%2A")
                    .Replace("%7E", "~");
            }

            /// <summary>
            /// 构建规范查询字符串CanonicalQueryString
            /// </summary>
            private static string BuildCanonicalQueryString(Dictionary<string, string> queryParameters)
            {
                if (queryParameters == null || queryParameters.Count == 0) return "";

                // 过滤authorization参数，编码后按ASCII字典序排序
                var encodedParams = queryParameters
                    .Where(kv => kv.Key.ToLower() != "authorization")
                    .Select(kv =>
                    {
                        string encodedKey = UriEncode(kv.Key);
                        string encodedValue = UriEncode(string.IsNullOrEmpty(kv.Value) ? "" : kv.Value);
                        return $"{encodedKey}={encodedValue}";
                    })
                    .OrderBy(param => param, StringComparer.Ordinal);

                return string.Join("&", encodedParams);
            }

            /// <summary>
            /// 构建规范请求头CanonicalHeaders
            /// </summary>
            private static string BuildCanonicalHeaders(Dictionary<string, string> headers)
            {
                if (headers == null || headers.Count == 0) return "";

                // 筛选需编码的头（必须包含Host，推荐包含Content-*和x-bce-*）
                var validHeaders = headers
                    .Where(kv => !string.IsNullOrEmpty(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                    .Select(kv => new KeyValuePair<string, string>(
                        kv.Key.ToLower().Trim(),
                        kv.Value.Trim() // 去除值首尾空白
                    ))
                    .Where(kv => !string.IsNullOrEmpty(kv.Value)) // 过滤空值
                    .OrderBy(kv => kv.Key, StringComparer.Ordinal); // 按头名字典序排序

                // 编码后用\n连接
                var encodedHeaders = validHeaders
                    .Select(kv => $"{UriEncode(kv.Key)}:{UriEncode(kv.Value)}");

                return string.Join("\n", encodedHeaders);
            }

            /// <summary>
            /// 标准UriEncode（按RFC3986）
            /// </summary>
            private static string UriEncode(string input)
            {
                if (string.IsNullOrEmpty(input)) return "";
                return Uri.EscapeDataString(input)
                    .Replace("+", "%20")
                    .Replace("*", "%2A")
                    .Replace("%7E", "~");
            }

            /// <summary>
            /// 生成signedHeaders（编码的头名小写+字典序+分号连接）
            /// </summary>
            private static string GetSignedHeaders(Dictionary<string, string> headers)
            {
                if (headers == null || headers.Count == 0) return "";

                var signedHeaderNames = headers
                    .Where(kv => !string.IsNullOrEmpty(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                    .Select(kv => kv.Key.ToLower().Trim())
                    .OrderBy(name => name, StringComparer.Ordinal);

                return string.Join(";", signedHeaderNames);
            }

            /// <summary>
            /// HMAC-SHA256哈希，返回小写十六进制字符串
            /// </summary>
            private static string HmacSha256Hex(string key, string message)
            {
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
                {
                    byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                    // 转为小写十六进制
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }
    }
}