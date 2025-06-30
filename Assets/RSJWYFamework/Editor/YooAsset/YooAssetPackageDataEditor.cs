using RSJWYFamework.Runtime;
using Sirenix.OdinInspector;
using YooAsset.Editor;

namespace RSJWYFamework.Editor
{
    public class YooAssetPackageDataEditor:YooAssetPackageData
    {
        [LabelText("构建管线")]
        [Required("必须选择构建管线，程序不做检测")] 
        public EBuildPipeline BuildPipeline;
    }
}