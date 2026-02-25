using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    public partial class AppConfig : ScriptableData
    {
        [FoldoutGroup("YooAssetInfo")] 
        public List<YooAssetPackageData> YooAssetPackageData=new List<YooAssetPackageData>();
        
        [FoldoutGroup("YooAssetInfo")] 
        [Required] 
        [LabelText("运行模式")]
        [InfoBox("本框架不支持 WebGL 或 自定义运行模式", InfoMessageType.Error, "@PlayMode == EPlayMode.WebPlayMode || PlayMode == EPlayMode.CustomPlayMode")]
        public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;
        

        [FoldoutGroup("YooAssetInfo")]
        [BoxGroup("YooAssetInfo/Host配置")]
        [LabelText("资源根地址")]
        [ShowIf("PlayMode", EPlayMode.HostPlayMode)]
        public string hostServerIP = "http://127.0.0.1";
        
        [BoxGroup("YooAssetInfo/覆盖安装")]
        [LabelText("覆盖安装清理模式")]
        [ShowIf("PlayMode", EPlayMode.HostPlayMode)]
        public EOverwriteInstallClearMode  OverwriteInstallClearMode = EOverwriteInstallClearMode.ClearAllCacheFiles;
        
        [BoxGroup("YooAssetInfo/清理配置")]
        [LabelText("文件清理模式")]
        // TODO: 这里的 EFileClearMode 需要确保引用了 YooAsset 命名空间
        public EFileClearMode FileClearMode = EFileClearMode.ClearAllBundleFiles;

        [BoxGroup("YooAssetInfo/清理配置")]
        [LabelText("指定清理路径列表")]
        [ShowIf("FileClearMode", EFileClearMode.ClearBundleFilesByLocations)]
        // TODO: 当模式为 ClearBundleFilesByLocations 时，需要在这里填写具体的资源路径
        public List<string> ClearLocations = new List<string>();

        [FoldoutGroup("YooAssetInfo")] 
        [LabelText("项目运行环境")]
        public Utility.YooAsset.BuildEnvironment buildEnv= Utility.YooAsset.BuildEnvironment.Dev;
        
        [BoxGroup("YooAssetInfo/清理配置")]
        [LabelText("指定清理标签列表")]
        [ShowIf("FileClearMode", EFileClearMode.ClearBundleFilesByTags)]
        // TODO: 当模式为 ClearBundleFilesByTags 时，需要在这里填写具体的资源标签
        public List<string> ClearTags = new List<string>();
        
    }
}