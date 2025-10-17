using System.IO;
using YooAsset.Editor;

namespace RSJWYFamework.Editor
{
    public class Rule
    {
        [DisplayName("收集热更代码")]
        public class HotCodeFilterRule:IFilterRule
        {

            public bool IsCollectAsset(FilterRuleData data)
            {
                string extension = Path.GetExtension(data.AssetPath);
                return extension == ".bytes";
            }
            /// <summary>
            /// 搜寻的资源类型
            /// 说明：使用引擎方法搜索获取所有资源列表
            /// 收集器可以指定搜寻的资源类型，在收集目录资产量巨大的情况下，可以极大加快打包速度！
            /// </summary>
            public string FindAssetType => ".bytes";
        }
        [DisplayName("打包为加密热更代码名")]
        public class HotCodePackRule : IPackRule
        {
            public PackRuleResult GetPackRuleResult(PackRuleData data)
            {
                return new PackRuleResult($"{data.AssetPath}_HoteCodeEncryptionUse", "hotupdatecode");
            }
        }
    }
}