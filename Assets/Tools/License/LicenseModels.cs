using System;

namespace License
{
    /// <summary>
    /// License 文件的外层包装结构
    /// 对应 Python 脚本生成的最终 JSON 结构
    /// </summary>
    [Serializable]
    public class LicenseWrapper
    {
        /// <summary>
        /// 原始数据的 Base64 编码字符串（包含机器码、时间戳等）
        /// </summary>
        public string data;
        
        /// <summary>
        /// 针对 data 字段生成的 RSA 签名（Base64 编码）
        /// </summary>
        public string signature;
    }

    /// <summary>
    /// License 的核心数据负载
    /// 解码 data 字段后得到的 JSON 对象
    /// </summary>
    [Serializable]
    public class LicensePayload
    {
        /// <summary>
        /// 授权的机器码
        /// </summary>
        public string machine_code;
        
        /// <summary>
        /// 授权开始时间戳（Unix 时间戳，秒）
        /// </summary>
        public long start_timestamp;
        
        /// <summary>
        /// 授权过期时间戳（Unix 时间戳，秒）
        /// </summary>
        public long expire_timestamp;
    }
}
