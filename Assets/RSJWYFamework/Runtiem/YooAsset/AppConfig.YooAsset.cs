using System.Collections.Generic;
using Sirenix.OdinInspector;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    public partial class AppConfig : DataBaseSB
    {
        [FoldoutGroup("YooAssetInfo")] 
        public List<YooAssetPackageData> YooAssetPackageData=new List<YooAssetPackageData>();
        
        [FoldoutGroup("YooAssetInfo")] [Required] [LabelText("运行模式")]
        public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

        [FoldoutGroup("YooAssetInfo")]
        [BoxGroup("Host配置")]
        [LabelText("资源根地址")]
        [ShowIf("PlayMode", EPlayMode.HostPlayMode)]
        public string hostServerIP = "http://127.0.0.1";
    }
}