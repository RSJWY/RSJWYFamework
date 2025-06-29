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