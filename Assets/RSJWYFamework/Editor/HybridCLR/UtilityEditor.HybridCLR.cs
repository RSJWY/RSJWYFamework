using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Settings;
using Newtonsoft.Json;
using RSJWYFamework.Runtiem;
using UnityEditor;
using UnityEngine;

namespace RSJWYFamework.Editor
{
    public static partial class UtilityEditor
    {
        public static class HybridCLR
        {
            /// <summary>
            /// 构建补充元数据到资源文件夹
            /// </summary>
            /// <param name="BuildMetadataForAOTAssembliesDllPatch">复制的目标目录</param>
            /// <param name="buildTarget">构建目标平台</param>
            public static void BuildMetadataForAOTAssemblies(string BuildMetadataForAOTAssembliesDllPatch,
                BuildTarget buildTarget)
            {
                if (!Editor.UtilityEditor.AutoSaveScence())
                {
                    AppLogger.Error("场景保存失败");
                    return;
                }

                AppLogger.Log($"构建生成补充元数据程序集DLL，目标平台：{buildTarget}");
                //构建补充元数据
                StripAOTDllCommand.GenerateStripedAOTDlls();
                //拷贝到资源包
                Debug.Log($"拷贝补充元数据到资源包，构建模式为{buildTarget.ToString()}");
                //获取构建DLL的路径
                var aotAssembliesSrcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(buildTarget);
                Utility.FileAndFolder.EnsureDirectoryExists(BuildMetadataForAOTAssembliesDllPatch);
                //多线程复制
                Parallel.ForEach(SettingsUtil.AOTAssemblyNames, aotDll =>
                {
                    string srcDllPath = $"{aotAssembliesSrcDir}/{aotDll}.dll";
                    string dllBytesPath = $"{BuildMetadataForAOTAssembliesDllPatch}/{aotDll}.dll.bytes";
                    Utility.FileAndFolder.CopyFile(srcDllPath, dllBytesPath, true);
                    AppLogger.Log($"[拷贝补充元数据到热更包] 拷贝 {srcDllPath} -> 到{dllBytesPath}");
                });
                AssetDatabase.Refresh();
            }

            /// <summary>
            /// 构建热更代码到资源文件夹
            /// </summary>
            /// <param name="BuildHotCodeDllPatch">目标拷贝路径</param>
            /// <param name="buildTarget">构建平台</param>
            public static void BuildHotCode(string BuildHotCodeDllPatch, BuildTarget buildTarget)
            {
                if (!AutoSaveScence())
                {
                    AppLogger.Error("场景保存失败");
                    return;
                }

                AppLogger.Log($"构建生成热更新程序集DLL，目标平台：{buildTarget}");
                //构建热更新代码
                CompileDllCommand.CompileDll(buildTarget);
                //拷贝到资源包
                AppLogger.Log($"拷贝热更新代码到资源包，构建模式为{buildTarget.ToString()}");
                //获取构建DLL的路径
                var hotfixDllSrcDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(buildTarget);
                Utility.FileAndFolder.EnsureDirectoryExists(BuildHotCodeDllPatch);
                //拷贝到资源-并行拷贝
                Parallel.ForEach(SettingsUtil.HotUpdateAssemblyNamesExcludePreserved, hotDll =>
                {
                    //拷贝热更代码
                    string startDllPath = $"{hotfixDllSrcDir}/{hotDll}.dll";
                    string endDllBytePath = $"{BuildHotCodeDllPatch}/{hotDll}.dll.bytes";
                    Utility.FileAndFolder.CopyFile(startDllPath, endDllBytePath, true);
                    AppLogger.Log($"[拷贝热更代码到热更包] 拷贝 {startDllPath} -> 到{endDllBytePath}");
                });
                Parallel.ForEach(SettingsUtil.HotUpdateAssemblyNamesExcludePreserved, hotDll =>
                {
                    //拷贝PDB
                    string startPdbPath = $"{hotfixDllSrcDir}/{hotDll}.pdb";
                    string endPdbBytePath = $"{BuildHotCodeDllPatch}/{hotDll}.pdb.bytes";
                    Utility.FileAndFolder.CopyFile(startPdbPath, endPdbBytePath, true);
                    AppLogger.Log($"[拷贝热更代码PDB到热更包] 拷贝 {startPdbPath} -> 到{endPdbBytePath}");
                });
                AssetDatabase.Refresh();
            }

            /// <summary>
            /// 构建补充元数据列表到HybridCLR设置
            /// </summary>
            public static void AddMetadataForAOTAssembliesToHCLRSetArr()
            {
                //生成补充元数据表
                var aotdlls = AOTGenericReferences.PatchedAOTAssemblyList.ToList();
                //处理信息
                var temp = new List<string>();
                foreach (string str in aotdlls)
                {
                    var _s = str.Replace(".dll", "");
                    temp.Add(_s);
                }

                //保存处理的数据
                HybridCLRSettings.Instance.patchAOTAssemblies = temp.ToArray();
                HybridCLRSettings.Save();
                AssetDatabase.Refresh();
            }

            /// <summary>
            /// 获取热更新DLL配置信息
            /// </summary>
            /// <returns></returns>
            public static HotCodeDLL GetHotCodeDllConfig()
            {
                //读取列表
                var aotAssemblies = HybridCLRSettings.Instance.patchAOTAssemblies.ToList();
                var hotDllDef = HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions.ToList();
                var preserverhotDllDef = HybridCLRSettings.Instance.preserveHotUpdateAssemblies.ToList();

                //List<string> _HotdllName = HybridCLRSettings.Instance.hotUpdateAssemblies.ToList();
                List<string> asmDefNames = hotDllDef
                    .Select(asset => asset.name) // 获取Unity资产的名称（不含扩展名）
                    .ToList();
                HotCodeDLL hotCodeDLL = new();
                hotCodeDLL.HotCode.AddRange(asmDefNames);
                hotCodeDLL.HotCode.AddRange(preserverhotDllDef);
                hotCodeDLL.MetadataForAOTAssemblies.AddRange(aotAssemblies);
                return hotCodeDLL;
            }


            /// <summary>
            /// 根据列表生成Json文件
            /// </summary>
            public static void GenerateDLLJson(HotCodeDLL hotCodeDLL, string GeneratedHotUpdateDLLJsonPath)
            {
                Utility.FileAndFolder.EnsureDirectoryExists(
                    Utility.FileAndFolder.GetDirectoryPath(GeneratedHotUpdateDLLJsonPath));
                File.WriteAllText(GeneratedHotUpdateDLLJsonPath, JsonConvert.SerializeObject(hotCodeDLL));
                AssetDatabase.Refresh();
            }
        }
    }
}