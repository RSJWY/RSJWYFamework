using Assets.RSJWYFamework.Runtiem.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Assets.RSJWYFamework.Runtiem.Boot
{
    /// <summary>
    /// 模块管理器
    /// </summary>
    public class ModuleManager : MonoBehaviour
    {
        private static bool initialized = false;
        /// <summary>
        /// 模块字典
        /// </summary>
        private static readonly Dictionary<Type, IModule> _modules = new Dictionary<Type, IModule>();
        /// <summary>
        /// 模块有序列表
        /// </summary>
        private static readonly List<IModule> _orderedModules = new List<IModule>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoRegisterModules()
        {

            if (!initialized)
            {
                // 获取所有带有ModuleAttribute的类型
                var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => type.IsDefined(typeof(ModuleAttribute)) &&
                                  typeof(IModule).IsAssignableFrom(type));

                // 收集模块信息并排序
                var moduleInfos = new List<(Type type, ModuleAttribute attr)>();
                foreach (var type in moduleTypes)
                {
                    var attr = type.GetCustomAttribute<ModuleAttribute>();
                    moduleInfos.Add((type, attr));
                }

                // 按优先级排序
                moduleInfos = moduleInfos.OrderBy(x => x.attr.Priority).ToList();

                // 实例化并注册模块
                foreach (var (type, attr) in moduleInfos)
                {
                    GameObject moduleGO = new GameObject(
                        string.IsNullOrEmpty(attr.ModuleName) ? type.Name : attr.ModuleName);

                    DontDestroyOnLoad(moduleGO);

                    IModule module = (IModule)moduleGO.AddComponent(type);
                    _modules[type] = module;
                    _orderedModules.Add(module);

                    module.Initialize();
                }
            }
        }
        /// <summary>
        /// 获取模块
        /// </summary>
        public static T GetModule<T>() where T : class, IModule
        {
            if (_modules.TryGetValue(typeof(T), out var module))
            {
                return module as T;
            }
            return null;
        }

        public static IEnumerable<IModule> GetAllModules()
        {
            return _orderedModules.AsReadOnly();
        }
        #region 生命周期
        private float timer = 0f;

        private void Update()
        {
            timer += Time.deltaTime;
            foreach (var module in GetAllModules())
            {
                module.ModuleUpdate();
                if (timer >= 1f)
                {
                    module.ModuleSecondUpdate();
                }
            }
            timer -= 1f; // 减去1秒，保留余数
        }

        private void FixedUpdate()
        {
            foreach (var module in GetAllModules())
            {
                module.ModuleFixedUpdate();
            }
        }

        private void LateUpdate()
        {
            foreach (var module in GetAllModules())
            {
                module.ModuleLateUpdate();
            }
        }

        #endregion
    }
}
