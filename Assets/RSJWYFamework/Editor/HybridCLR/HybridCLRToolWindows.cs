using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor.Commands;
using Newtonsoft.Json;
using RSJWYFamework.Runtime;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace RSJWYFamework.Editor
{
    /// <summary>
    /// 
    /// </summary>
    public class HybridCLRToolWindows : OdinEditorWindow
    {
        [InlineEditor(InlineEditorModes.FullEditor)] 
        [LabelText("配置文件")]
        [ShowInInspector]
        [ReadOnly]
        public HCLRToolSettingConfig SettingData;


        [Button("加载热更新DLL列表", ButtonSizes.Gigantic)]
        [BoxGroup("DLL列表")]
        public void GetHotCodeConfig()
        {
            HotUpdateDll=UtilityEditor.HybridCLR.GetHotCodeDllConfig();
        }
        
        [BoxGroup("DLL列表")] 
        public HotCodeDLL HotUpdateDll;
        
        [Button("生成Json数据", ButtonSizes.Gigantic)]
        public void GenerateJson()
        {
            BuildHotUpdateDllJson();
        }


        [ButtonGroup("需要编辑器重编译")]
        [Button("构建所有（不包含生成补充元数据列表）",ButtonSizes.Gigantic)]
        private void BuildAllDLL()
        {
            BuildMetadataForAOTAssemblies();
            BuildHotCode();
            BuildHotUpdateDllJson();
        }
        [ButtonGroup("需要编辑器重编译")]
        [Button("构建补充元数据",ButtonSizes.Gigantic)]
        private void BuildMetadataForAOTAssemblies()
        {
            //清空目录
            Utility.FileAndFolder.ClearDirectory($"{UtilityEditor.GetProjectPath()}/{SettingData.BuildMetadataForAOTAssembliesDllToPatch}");
            AssetDatabase.Refresh();
            UtilityEditor.HybridCLR.AddMetadataForAOTAssembliesToHCLRSetArr();
            //构建补充元数据到资源目录
            UtilityEditor.HybridCLR.BuildMetadataForAOTAssemblies(
                $"{UtilityEditor.GetProjectPath()}/{SettingData.BuildMetadataForAOTAssembliesDllToPatch}",SettingData.BuildTarget);
        }

       
        [Button("获取补充元数据列表（手动执行两次）",ButtonSizes.Gigantic)]
        [ButtonGroup("获取信息")]
        private void CompileAndGenerateAOTGenericReference()
        {
            //生成补充元数据表
            AOTReferenceGeneratorCommand.CompileAndGenerateAOTGenericReference();
            AssetDatabase.Refresh();
            UtilityEditor.HybridCLR.AddMetadataForAOTAssembliesToHCLRSetArr();
        }
        [Button("创建热更dll列表",ButtonSizes.Gigantic)]
        [ButtonGroup("获取信息")]
        private void BuildHotUpdateDllJson()
        {
            UtilityEditor.HybridCLR.GenerateDLLJson(HotUpdateDll,$"{UtilityEditor.GetProjectPath()}/{SettingData.GeneratedHotUpdateDLLJsonToFile}");
        }
        [Button("构建热更代码",ButtonSizes.Gigantic)]
        private void BuildHotCode()
        {
            Utility.FileAndFolder.ClearDirectory($"{UtilityEditor.GetProjectPath()}/{SettingData.BuildHotCodeDllToPatch}");
            AssetDatabase.Refresh();
            UtilityEditor.HybridCLR.BuildHotCode($"{UtilityEditor.GetProjectPath()}/{SettingData.BuildHotCodeDllToPatch}",SettingData.BuildTarget);
        }

        
        


        protected override void OnEnable()
        {
            base.OnEnable();
            var appSB = UtilityEditor.GetSettingConfigList<HCLRToolSettingConfig>();
            if (appSB.Count!=1)
            {
                AppLogger.Warning($"App配置文件不存在或者不唯一！数量：{appSB.Count}，请手动设置配置文件");
            }
            else
            {
                SettingData = appSB[0];
            }
            //加载热更dll列表
            //UpdateHotDLLJson();
        }

        void UpdateHotDLLJson()
        {
            var hotcodedllJson = $"{UtilityEditor.GetProjectPath()}/{SettingData.GeneratedHotUpdateDLLJsonToFile}";
            HotUpdateDll = JsonConvert.DeserializeObject<HotCodeDLL>(File.ReadAllText(hotcodedllJson));
        }
        
        [MenuItem("RSJWYFamework/打开热更新系统工具")]
        public static void OpenHybridCLRToolWindows()
        {
            var _windows = OdinEditorWindow.GetWindow<HybridCLRToolWindows>();
            _windows.Show();
            _windows.titleContent = new GUIContent("热更新系统工具");
        }
    }
}