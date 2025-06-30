using System.Collections.Generic;
using System.IO;
using RSJWYFamework.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RSJWYFamework.Editor
{
    public static partial class UtilityEditor
    {
        /// <summary>
        /// 获取项目根路径
        /// </summary>
        /// <returns></returns>
        public static string GetProjectPath()
        {
           string prpjectPatch= Path.GetDirectoryName(Application.dataPath);
           return Utility.FileAndFolder.NormalizePath(prpjectPatch);
        }
        /// <summary>
        /// 创建ScriptableObject
        /// </summary>
        /// <param name="path">创建路径</param>
        /// <typeparam name="TScriptableObject">类型，必须继承自ScriptableObject</typeparam>
        public static void CreateScriptableObject<TScriptableObject>(string path)where TScriptableObject:ScriptableObject
        {
            if (File.Exists(path))
            {
                AppLogger.Error($"路径：{path} 已存在");
                return;
            }
            //创建数据资源文件
            //泛型是继承自ScriptableObject的类
            var asset = ScriptableObject.CreateInstance<TScriptableObject>();
            //前一步创建的资源只是存在内存中，现在要把它保存到本地
            //通过编辑器API，创建一个数据资源文件，第二个参数为资源文件在Assets目录下的路径
            AssetDatabase.CreateAsset(asset, path);
            //保存创建的资源
            AssetDatabase.SaveAssets();
            //刷新界面
            AssetDatabase.Refresh();
            AppLogger.Log($"创建成功！！路径：{path}");
        }
        
        /// <summary>
        /// 保存场景
        /// </summary>
        public static bool AutoSaveScence()
        {
            // if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            // {
            //     Debug.Log("Scene not saved");
            // }
            // else
            // {
            //     Debug.Log("Scene saved");
            // }
            AppLogger.Log("保存场景");
            bool issave = EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            return issave;
        }
        /// <summary>
        /// 加载ScriptableObject实例化数据
        /// </summary>
        /// <typeparam name="TSettingConfig">数据类型，继承自ScriptableObject</typeparam>
        /// <returns></returns>
        public static List<TSettingConfig> GetSettingConfigList<TSettingConfig>()where TSettingConfig:ScriptableObject
        {
            List<TSettingConfig> assets = new List<TSettingConfig>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(TSettingConfig).Name}");
            
            foreach(string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TSettingConfig asset = AssetDatabase.LoadAssetAtPath<TSettingConfig>(assetPath);
                if(asset != null)
                {
                    assets.Add(asset);
                }
            }
            return assets;
        }
    }
}