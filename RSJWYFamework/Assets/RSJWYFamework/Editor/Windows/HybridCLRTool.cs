using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using Newtonsoft.Json;
using RSJWYFamework.Editor.Tool;
using RSJWYFamework.Runtime.HybridCLR;
using RSJWYFamework.Runtime.Logger;
using RSJWYFamework.Runtime.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace RSJWYFamework.Editor.Windows
{
    public class HybridCLRTool:OdinEditorWindow
    {
        [FolderPath][LabelText("构建热更代码DLL到：")]
        public string BuildHotCodeDllPatch="Assets/HotUpdateAssets/HotCode";
        [FolderPath][LabelText("构建补充选数据DLL到：")]
        public string BuildMetadataForAOTAssembliesDllPatch="Assets/HotUpdateAssets/MetadataForAOTAssemblies";
        
        public string GeneratedHotUpdateDLLJson="Assets/HotUpdateAssets/Config"; 
        [ReadOnly]
        public HotCodeDLL HotUpdateDll;
        
        
        
        [Button("构建所有")][ButtonGroup]
        private void BuildAllDLL()
        {
            BuildMetadataForAOTAssemblies();
            BuildHotCode();
        }
        [Button("构建补充元数据")][ButtonGroup]
        private void BuildMetadataForAOTAssemblies()
        {
            UtilityEditor.UtilityEditor.HybrildCLR.AddMetadataForAOTAssembliesToHCLRSetArr();
            UtilityEditor.UtilityEditor.HybrildCLR.BuildMetadataForAOTAssemblies(BuildMetadataForAOTAssembliesDllPatch);
        }
        [Button("构建热更代码")][ButtonGroup]
        private void BuildHotCode()
        {
            UtilityEditor.UtilityEditor.HybrildCLR.BuildHotCode(BuildHotCodeDllPatch);
        }
        [Button("创建热更dll列表")]
        private void BuildHotUpdateDllJson()
        {
            UtilityEditor.UtilityEditor.HybrildCLR.AddMetadataForAOTAssembliesToHCLRSetArr();
            UtilityEditor.UtilityEditor.HybrildCLR.BuildDLLJson(GeneratedHotUpdateDLLJson);
            UpdateHotDLLJson();
        }
        
        

        protected override void OnEnable()
        {
            //加载热更dll列表
            base.OnEnable();
            UpdateHotDLLJson();
        }

        void UpdateHotDLLJson()
        {
            var hotcodedllJson = $"{UtilityEditor.UtilityEditor.GetProjectPath()}/Assets/HotUpdateAssets/Config/HotCodeDLL.json";
            HotUpdateDll = JsonConvert.DeserializeObject<HotCodeDLL>(File.ReadAllText(hotcodedllJson));
            
        }
    }
}