using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace RSJWYFamework.Editor
{
    /// <summary>
    /// 热更新配置表
    /// </summary>
    [CreateAssetMenu(fileName = "HCLRToolSettingConfig", menuName = "RSJWYFamework/创建代码热更设置参数", order = 0)]
    public class HCLRToolSettingConfig:ScriptableObject
    {
        [FolderPath][LabelText("构建热更代码DLL到：")]
        public string BuildHotCodeDllToPatch="Assets/HotUpdateAssets/HotCode/HotCode";
        [FolderPath][LabelText("构建补充选数据DLL到：")]
        public string BuildMetadataForAOTAssembliesDllToPatch="Assets/HotUpdateAssets/HotCode/MetadataForAOTAssemblies";
        [Sirenix.OdinInspector.FilePath][LabelText("构建热更列表文件路径：")]
        public string GeneratedHotUpdateDLLJsonToFile="Assets/HotUpdateAssets/HotCode/List/HotCodeDLL.json"; 
        [LabelText("构建目标平台")]
        public BuildTarget BuildTarget=BuildTarget.StandaloneWindows64; 
    }
}