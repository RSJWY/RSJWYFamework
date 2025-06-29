using YooAsset.Editor;

namespace RSJWYFamework.Editor
{
    public static partial class UtilityEditor
    {
        public static class YooAsset
        {
            /// <summary>
            /// 内置着色器资源包名称
            /// 注意：和自动收集的着色器资源包名保持一致！
            /// </summary>
            public static string GetBuiltinShaderBundleName(string packageName)
            {
                var uniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;
                var packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
                return packRuleResult.GetBundleName(packageName, uniqueBundleName);
            }
        }
    }
}