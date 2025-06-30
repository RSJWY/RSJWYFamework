using System.Collections.Generic;
using Newtonsoft.Json;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 热更代码json加载
    /// </summary>
    public class HotCodeDLL
    {
        /// <summary>
        /// 补充元数据
        /// </summary>
        [JsonProperty("MetadataForAOTAssemblies")]
        public List<string> MetadataForAOTAssemblies=new();
        /// <summary>
        /// 热更代码
        /// </summary>
        [JsonProperty("HotCode")]
        public List<string> HotCode=new();
    }
    /// <summary>
    /// 热更代码DLL&PDB对
    /// </summary>
    public class HotCodeBytes
    {
        /// <summary>
        /// 热更代码
        /// </summary>
        public byte[] dllBytes;
        /// <summary>
        /// pdb字符文件
        /// </summary>
        public byte[] pdbBytes;
    }
}