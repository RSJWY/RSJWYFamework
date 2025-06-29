using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RSJWYFamework.Runtiem
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
                var _all= GameObject.FindObjectsOfType<ModuleManager>();
                if( _all.Length!=0)
                {
                    throw new AppException("场景中存在多个管理器！！初始化终止");
                }
                var _manager = new GameObject("[ModuleManager]");
                _manager.AddComponent<ModuleManager>();
                DontDestroyOnLoad( _manager );


                // 1. 反射获取所有实现IModule的类型
                var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()      // 获取当前应用程序域中加载的所有程序集
                    .SelectMany(a => a.GetTypes())                       // 获取每个程序集中定义的所有类型
                    .Where(t =>                                            // 筛选满足以下条件的类型：
                            t.IsDefined(typeof(ModuleAttribute)) &&  // 1. 标记了AutoRegisterModuleAttribute特性
                            typeof(IModule).IsAssignableFrom(t) &&           // 2. 实现了IModule接口或继承自IModule
                            !t.IsAbstract                                      // 3. 不是抽象类
                    );

                // 收集模块信息并排序
                var moduleInfos = new List<(Type type, ModuleAttribute attr)>();
                foreach (var type in moduleTypes)
                {
                    var attr = type.GetCustomAttribute<ModuleAttribute>();
                    moduleInfos.Add((type, attr));
                }

                // 按优先级排序
                moduleInfos = moduleInfos.OrderBy(x => x.attr.Priority).ToList();
                
                //实例化
                foreach (var moduleInfo in moduleInfos)
                {
                    IModule moduleI;
                    if (typeof(MonoBehaviour).IsAssignableFrom(moduleInfo.type))
                    {
                        // Unity模块：挂载到GameObject
                        GameObject moduleGO = new GameObject($"[Module]{moduleInfo.type.Name}");
                        moduleGO.transform.parent = _manager.transform;
                        moduleI= moduleGO.AddComponent(moduleInfo.type) as IModule;
                    }
                    else
                    {
                        moduleI = Activator.CreateInstance(moduleInfo.type) as IModule;
                    }

                    _modules[moduleInfo.type] = moduleI;
                    _orderedModules.Add(moduleI);
                    moduleI.Initialize();
                    AppLogger.Log($"初始化模块类：{moduleInfo.type.Name}");
                }
                initialized=true;
                AppLogger.Log($"初始化模块管理器完成！模块数量为：{_modules.Count}");
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
            AppLogger.Exception(new AppException($"无法获取模块{typeof(T).Name}"));
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
            timer += Time.unscaledDeltaTime;
            foreach (var module in GetAllModules())
            {
                module.ModuleUpdate();
                if (timer >= 1f)
                {
                    module.ModulePerSecondUpdate();
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
