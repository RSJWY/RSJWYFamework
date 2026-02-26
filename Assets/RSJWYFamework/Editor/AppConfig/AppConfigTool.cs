using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RSJWYFamework.Runtime;

namespace RSJWYFamework.Editor
{
    public static class AppConfigTool
    {
        
        [MenuItem("RSJWYFamework/Sync YooAsset Packages to AppConfig (同步包配置)")]
        public static void SyncYooAssetPackagesToAppConfig()
        {
            // 1. 加载 AssetBundleCollectorSetting
            // string collectorPath = "Assets/AssetBundleCollectorSetting.asset";
            // var collectorAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(collectorPath);
            
            // 改为全局搜索唯一实例化数据
            string[] guids = AssetDatabase.FindAssets("t:AssetBundleCollectorSetting");
            if (guids.Length == 0)
            {
                Debug.LogError("[AppConfigTool] 未找到 AssetBundleCollectorSetting 配置文件！请检查是否存在。");
                return;
            }
            if (guids.Length > 1)
            {
                Debug.LogError($"[AppConfigTool] 找到多个 AssetBundleCollectorSetting 配置文件，请确保全局唯一！数量：{guids.Length}");
                return;
            }

            string collectorPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var collectorAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(collectorPath);

            if (collectorAsset == null)
            {
                Debug.LogError($"[AppConfigTool] 加载 AssetBundleCollectorSetting 失败，路径：{collectorPath}");
                return;
            }

            var so = new SerializedObject(collectorAsset);
            var packagesProp = so.FindProperty("Packages");
            if (packagesProp == null || !packagesProp.isArray)
            {
                Debug.LogError("[AppConfigTool] 在 AssetBundleCollectorSetting 中未找到 'Packages' 属性");
                return;
            }

            // 2. 加载 AppConfig
            var appConfigs = UtilityEditor.GetSettingConfigList<AppConfig>();
            if (appConfigs.Count == 0)
            {
                Debug.LogError("[AppConfigTool] 未找到 AppConfig 配置。");
                return;
            }
            var appConfig = appConfigs[0];

            // 3. 同步
            bool dirty = false;
            
            // 从收集器创建包名集合以便查找
            var collectorPackages = new Dictionary<string, string>(); // Name -> Desc
            
            for (int i = 0; i < packagesProp.arraySize; i++)
            {
                var pkgProp = packagesProp.GetArrayElementAtIndex(i);
                var nameProp = pkgProp.FindPropertyRelative("PackageName");
                var descProp = pkgProp.FindPropertyRelative("PackageDesc");
                
                if (nameProp != null)
                {
                    string name = nameProp.stringValue;
                    string desc = descProp != null ? descProp.stringValue : "";
                    if (!string.IsNullOrEmpty(name))
                    {
                        collectorPackages[name] = desc;
                    }
                }
            }

            // 更新或添加
            if (appConfig.YooAssetPackageData == null)
            {
                appConfig.YooAssetPackageData = new List<YooAssetPackageData>();
            }

            foreach (var kvp in collectorPackages)
            {
                string pkgName = kvp.Key;
                string pkgDesc = kvp.Value;

                var existingPkg = appConfig.YooAssetPackageData.Find(p => p.packageName == pkgName);
                if (existingPkg != null)
                {
                    // 如果描述有变化则更新
                    if (existingPkg.packageDesc != pkgDesc)
                    {
                        existingPkg.packageDesc = pkgDesc;
                        dirty = true;
                        Debug.Log($"[AppConfigTool] 更新包描述：{pkgName}");
                    }
                }
                else
                {
                    // 添加新包
                    var newPkg = new YooAssetPackageData
                    {
                        packageName = pkgName,
                        packageDesc = pkgDesc,
                        // 如有必要初始化其他字段（通常默认值即可）
                    };
                    appConfig.YooAssetPackageData.Add(newPkg);
                    dirty = true;
                    Debug.Log($"[AppConfigTool] 添加新包：{pkgName}");
                }
            }

            // 如果有变更则保存
            if (dirty)
            {
                EditorUtility.SetDirty(appConfig);
                AssetDatabase.SaveAssets();
                Debug.Log("[AppConfigTool] AppConfig 更新成功。");
            }
            else
            {
                Debug.Log("[AppConfigTool] AppConfig 已经是最新状态。");
            }
        }
    }
}