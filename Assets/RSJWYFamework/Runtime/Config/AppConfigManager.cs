using System;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RSJWYFamework.Runtime
{
    [Module]
    [ModuleDependency(typeof(EventManager))]
    [ModuleDependency(typeof(DataManager))]
    public class AppConfigManager:ModuleBase
    {
        public AppConfig AppConfig { get; private set; }

        public readonly string JsonConfigPath = Application.streamingAssetsPath;
        public ConcurrentDictionary<string,JObject> JsonConfigDict { get; private set; } = new ConcurrentDictionary<string, JObject>();

        /// <summary>
        /// 获取配置文件
        /// </summary>
        /// <param name="name">配置文件名</param>
        /// <returns>配置文件内容，为JObject类型</returns>
        public JObject GetConfig(string name)
        {
            JObject _output = null;
            if (JsonConfigDict.TryGetValue(name, out _output))
            {
                return _output;
            }
            AppLogger.Warning($"AppConfigManager GetConfig {name} is not found,try to load from {JsonConfigPath}");
            if (LoadConfig(name))
            {
                if (JsonConfigDict.TryGetValue(name, out _output))
                {
                    return _output;
                }
                throw new Exception($"AppConfigManager LoadConfig {name} is not found！！path is {JsonConfigPath},这是个不可能发生的错误");
            }

            AppLogger.Error($"AppConfigManager LoadConfig {name} is failed,path is {JsonConfigPath}");
            return null;
        }
        /// <summary>
        /// 设置配置文件
        /// </summary>
        /// <remarks>
        /// 该方法会覆盖原有配置文件，并写入文件
        /// </remarks>
        /// <param name="name">配置文件名</param>
        /// <param name="config">配置文件内容，为JObject类型</param>
        public void SetConfig(string name, JObject config)
        {
            //覆盖原有配置文件
            JsonConfigDict.AddOrUpdate(
                name, config, 
                (key, oldValue) => config
            );
            //写入文件
            string path = $"{JsonConfigPath}/{name}.json";
            try
            {
                Utility.FileAndFolder.EnsureDirectoryExists(path);
                File.WriteAllText(path, config.ToString());
            }
            catch (Exception ex)
            {
                AppLogger.Error($"AppConfigManager SetConfig {name} to file is failed,path is {path},error is {ex.Message}");
            }
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <param name="name">配置文件名</param>
        /// <returns>是否加载成功</returns>
        public bool LoadConfig(string name)
        {
            string path = $"{JsonConfigPath}/{name}.json";
            if (!File.Exists(path))
            {
                Debug.LogError($"AppConfigManager LoadConfig {name} is not found,path is {path}");
                return false;
            }
            try
            {
                string json = File.ReadAllText(path);
                JObject jsonObject = JObject.Parse(json); // 解析失败会抛出异常
                JsonConfigDict[name] = jsonObject;
                return true;
            }
            catch (Exception ex) // 捕获解析异常（如格式错误、非对象结构等）
            {
                Debug.LogError($"解析JSON失败，路径：{path}，错误：{ex.Message}");
                return false;
            }
        }
        
        
        
        public override void Initialize()
        {
            AppConfig = ModuleManager.GetModule<DataManager>().GetFirstData<AppConfig>();
            if (AppConfig != null)return;
            AppConfig=Resources.Load<AppConfig>("AppConfig");
            if (AppConfig == null)
            {
                Debug.LogError("AppConfig is null,请在Resources文件夹下创建AppConfig");
                return;
            }
            ModuleManager.GetModule<DataManager>().AddData(AppConfig);

            // =========================================================
            // 小码酱添加：尝试加载外部 AppConfig.json 覆盖默认配置
            // =========================================================
            try
            {
                string overridePath = Path.Combine(JsonConfigPath, "AppConfig.json");
                if (File.Exists(overridePath))
                {
                    Debug.Log($"[AppConfigManager] 检测到外部配置文件，正在应用覆盖：{overridePath}");
                    string json = File.ReadAllText(overridePath);
                    // 使用 PopulateObject 将 JSON 数据注入到现有的 ScriptableObject 实例中
                    JsonConvert.PopulateObject(json, AppConfig);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppConfigManager] 应用外部 AppConfig.json 失败：{ex.Message}");
            }
            // =========================================================

            try
            {
                if (Directory.Exists(JsonConfigPath))
                {
                    var files = Directory.GetFiles(JsonConfigPath, "*.json", SearchOption.TopDirectoryOnly);
                    foreach (var path in files)
                    {
                        try
                        {
                            var json = File.ReadAllText(path);
                            var obj = JObject.Parse(json);
                            var name = Path.GetFileNameWithoutExtension(path);
                            JsonConfigDict[name] = obj;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"解析JSON失败，路径：{path}，错误：{ex.Message}");
                        }
                    }
                }
                else
                {
                    AppLogger.Warning($"StreamingAssets路径不存在：{JsonConfigPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"遍历StreamingAssets失败，路径：{JsonConfigPath}，错误：{ex.Message}");
            }
        }

        public override void Shutdown()
        {
        }

        public override void LifeUpdate()
        {
        }
    }
}
