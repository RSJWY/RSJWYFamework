using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 模块管理器
    /// </summary>
    public  class ModuleManager : MonoBehaviour
    {
        private static bool _initialized = false;
        /// <summary>
        /// 模块字典
        /// </summary>
        private static readonly Dictionary<Type, IModule> Modules = new ();
        /// <summary>
        /// 生命周期列表，允许重复添加
        /// </summary>
        private static readonly List<ILife> Lifes = new ();
        /// <summary>
        /// 新添加的生命周期列表，等待被unity调用时进行添加
        /// </summary>
        private static Queue<ILife> _pendingLifeAdds = new Queue<ILife>();
        /// <summary>
        /// 生命周期添加移除线程锁
        /// </summary>
        private static readonly object _lifeLock = new object();

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public static bool EnablePerformanceMonitoring { get; set; } = 
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public static bool EnableVerboseLogging { get; set; } = 
#if UNITY_EDITOR
            true;
#else
            false;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoRegisterModules()
        {
            if (!_initialized)
            {
                var managers = GameObject.FindObjectsOfType<ModuleManager>();
                if (managers.Length > 1)
                {
                    throw new AppException("场景中存在多个管理器！！初始化终止");
                }
                GameObject managerObj;
                if (managers.Length == 1)
                {
                    managerObj = managers[0].gameObject;
                }
                else
                {
                    managerObj = new GameObject("[ModuleManager]");
                    managerObj.AddComponent<ModuleManager>();
                }
                DontDestroyOnLoad(managerObj);
                SceneManager.sceneLoaded += OnSceneLoaded;
                
                // 初始化依赖解析器
                ModuleDependencyResolver.Initialize();
                
                // 验证依赖关系
                if (!ModuleDependencyResolver.ValidateDependencies())
                {
                    throw new AppException("检测到模块循环依赖，初始化终止！");
                }
                
                // 扫描所有程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
                // 获取所有模块类型
                var allModuleTypes = assemblies
                    .SelectMany(a => a.GetTypes())
                    .Where(t=> t.IsDefined(typeof(ModuleAttribute), false) &&
                                  typeof(IModule).IsAssignableFrom(t) &&
                                  !t.IsAbstract &&
                                  !t.IsInterface &&
                                  !t.IsGenericType)
                    .ToList();
                
                // 按依赖顺序获取模块类型
                var orderedModuleTypes = ModuleDependencyResolver.GetOrderedModuleTypes()
                    .Where(t => allModuleTypes.Contains(t))
                    .ToList();
                
                // 添加没有依赖关系的模块
                var remainingModules = allModuleTypes.Except(orderedModuleTypes).ToList();
                orderedModuleTypes.AddRange(remainingModules);
                
                AppLogger.Log($"模块初始化顺序：{string.Join(" -> ", orderedModuleTypes.Select(t => t.Name))}");
                
                // 按正确顺序注册模块
                var preplaced = new List<string>();
                var autoAdded = new List<string>();
                var removedDuplicates = new List<string>();
                foreach (var type in orderedModuleTypes)
                {
                    if (Modules.ContainsKey(type))
                    {
                        continue;
                    }
                    IModule moduleInstance = null;
                    if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        var existing = UnityEngine.Object.FindObjectsOfType(type);
                        if (existing != null && existing.Length > 0)
                        {
                            moduleInstance = existing[0] as IModule;
                            var comp = existing[0] as MonoBehaviour;
                            if (comp != null && comp.transform.parent != managerObj.transform)
                            {
                                comp.transform.SetParent(managerObj.transform, false);
                                comp.gameObject.name = $"[Module]{type.Name}";
                            }
                            preplaced.Add(type.Name);
                            if (existing.Length > 1)
                            {
                                for (int i = 1; i < existing.Length; i++)
                                {
                                    var dup = existing[i] as MonoBehaviour;
                                    if (dup != null)
                                    {
                                        UnityEngine.Object.Destroy(dup.gameObject);
                                    }
                                }
                                removedDuplicates.Add(type.Name);
                            }
                        }
                        else
                        {
                            var moduleGO = new GameObject($"[Module]{type.Name}");
                            moduleGO.transform.parent = managerObj.transform;
                            moduleInstance = moduleGO.AddComponent(type) as IModule;
                            autoAdded.Add(type.Name);
                        }
                    }
                    else
                    {
                        moduleInstance = Activator.CreateInstance(type) as IModule;
                        autoAdded.Add(type.Name);
                    }
                    AddModule(moduleInstance, type);
                }
                
                _initialized=true;
                AppLogger.Log($"初始化模块管理器完成！模块数量为：{Modules.Count}，生命周期已存在数量为：{Lifes.Count}，生命周期等待添加数量：{_pendingLifeAdds.Count}");
                if (preplaced.Count > 0)
                {
                    AppLogger.Log($"场景预置模块：{string.Join(", ", preplaced)}");
                }
                if (autoAdded.Count > 0)
                {
                    AppLogger.Log($"自动添加模块：{string.Join(", ", autoAdded)}");
                }
                if (removedDuplicates.Count > 0)
                {
                    AppLogger.Warning($"检测到重复预置并已移除：{string.Join(", ", removedDuplicates)}");
                }
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var allModuleTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t=> t.IsDefined(typeof(ModuleAttribute), false) &&
                              typeof(IModule).IsAssignableFrom(t) &&
                              !t.IsAbstract &&
                              !t.IsInterface &&
                              !t.IsGenericType)
                .ToList();
            var manager = UnityEngine.Object.FindObjectOfType<ModuleManager>();
            if (manager == null) return;
            var preplacedInScene = new List<string>();
            var autoAddedInScene = new List<string>();
            var removedDupInScene = new List<string>();
            foreach (var type in allModuleTypes)
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    var existing = UnityEngine.Object.FindObjectsOfType(type);
                    var inScene = existing
                        .Cast<MonoBehaviour>()
                        .Where(m => m.gameObject.scene == scene)
                        .ToArray();
                    if (inScene.Length > 0)
                    {
                        if (Modules.ContainsKey(type))
                        {
                            for (int i = 0; i < inScene.Length; i++)
                            {
                                var dup = inScene[i];
                                UnityEngine.Object.Destroy(dup.gameObject);
                            }
                            removedDupInScene.Add(type.Name);
                        }
                        else
                        {
                            var comp = inScene[0];
                            var moduleInstance = comp as IModule;
                            if (moduleInstance != null)
                            {
                                comp.transform.SetParent(manager.transform, false);
                                comp.gameObject.name = $"[Module]{type.Name}";
                                AddModule(moduleInstance, type);
                                preplacedInScene.Add(type.Name);
                                for (int i = 1; i < inScene.Length; i++)
                                {
                                    var dup = inScene[i];
                                    UnityEngine.Object.Destroy(dup.gameObject);
                                }
                                if (inScene.Length > 1)
                                {
                                    removedDupInScene.Add(type.Name);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!Modules.ContainsKey(type))
                        {
                            var moduleGO = new GameObject($"[Module]{type.Name}");
                            moduleGO.transform.parent = manager.transform;
                            var moduleInstance = moduleGO.AddComponent(type) as IModule;
                            AddModule(moduleInstance, type);
                            autoAddedInScene.Add(type.Name);
                        }
                    }
                }
            }
            if (preplacedInScene.Count > 0)
            {
                AppLogger.Log($"场景[{scene.name}]预置模块：{string.Join(", ", preplacedInScene)}");
            }
            if (autoAddedInScene.Count > 0)
            {
                AppLogger.Log($"场景[{scene.name}]自动添加模块：{string.Join(", ", autoAddedInScene)}");
            }
            if (removedDupInScene.Count > 0)
            {
                AppLogger.Warning($"场景[{scene.name}]检测到重复预置并已移除：{string.Join(", ", removedDupInScene)}");
            }
        }
        
        /// <summary>
        /// 获取模块
        /// </summary>
        public static T GetModule<T>() where T : class, IModule
        {
            if (Modules.TryGetValue(typeof(T), out var module))
            {
                return module as T;
            }
            AppLogger.Exception(new AppException($"无法获取模块{typeof(T).Name}"));
            return null;
        }
        /// <summary>
        /// 手动添加模块
        /// </summary>
        /// <remarks>因为涉及自动实例化到场景中，必须有无参构造函数，否则将会异常</remarks>
        public static T AddModule<T>() where T : class, IModule,new()
        {
            Type type = typeof(T);

            if (Modules.ContainsKey(type))
            {
                AppLogger.Warning($"模块 {type.Name} 已存在，跳过添加。");
                return Modules[type] as T;
            }

            IModule moduleInstance;

            // 检查是否为MonoBehaviour
            if (typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                var manager = UnityEngine.Object.FindObjectOfType<ModuleManager>();
                if (manager == null)
                {
                    throw new AppException("找不到 ModuleManager 对象");
                }
                var moduleGO = new GameObject($"[Module]{type.Name}");
                moduleGO.transform.parent = manager.transform;
                moduleInstance = moduleGO.AddComponent(type) as IModule;
            }
            else
            {
                moduleInstance = Activator.CreateInstance(type) as IModule;
            }
            AddModule(moduleInstance,type);

            return moduleInstance as T;
        }


        /// <summary>
        /// 手动添加模块
        /// </summary>
        public static void AddModule([NotNull]IModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            AddModule(module,module.GetType());
        }
        /// <summary>
        /// 手动添加模块
        /// </summary>
        public static void AddModule([NotNull]IModule module,[NotNull]Type type)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (type == null) throw new ArgumentNullException(nameof(type));
            //检查是否存在
            if (Modules.ContainsKey(type))
            {
                AppLogger.Warning($"模块 {type.Name} 已存在，跳过添加。");
                return;
            }
            var mono = module as MonoBehaviour;
            if (mono != null)
            {
                var manager = UnityEngine.Object.FindObjectOfType<ModuleManager>();
                if (manager == null)
                {
                    throw new AppException("找不到 ModuleManager 对象");
                }
                if (mono.transform.parent != manager.transform)
                {
                    mono.transform.SetParent(manager.transform, false);
                    mono.gameObject.name = $"[Module]{type.Name}";
                }
            }
            AddLife(module);
            //添加
            Modules[type] = module;
            module.Initialize();
            AppLogger.Log($"添加模块：{type.Name}");
        }
        
        /// <summary>
        /// 手动移除模块
        /// </summary>
        public static void RemoveModule<T>()where  T : class, IModule
        {
            RemoveModule(typeof(T));
        }

        /// <summary>
        /// 手动移除模块
        /// </summary>
        public static void RemoveModule(Type type)
        {
            if (Modules.TryGetValue(type, out var module))
            {
                // 先调用模块的Shutdown方法
                module.Shutdown();
                // 从生命周期中移除
                RemoveLife(module);
                // 从模块字典中移除
                Modules.Remove(type);
                AppLogger.Log($"移除模块：{type.Name}");
            }
            else
            {
                AppLogger.Warning($"模块 {type.Name} 不存在，无法移除。");
            }
        }
        
        /// <summary>
        /// 通过泛型添加生命
        /// </summary>
        /// <remarks>可以理解为帮您实例化一个生命周期对象</remarks>
        /// <remarks>加入到待添加队列，在下一次unity生命周期调用</remarks>
        /// <typeparam name="T">继承自ILife接口，同时必须有无参构造函数</typeparam>
        public static T AddLife<T>()where T : class,ILife,new()
        {
            var life = new T();
            AddLife(life);
            return life;
        }
        
        /// <summary>
        /// 添加生命周期对象
        /// </summary>
        /// <remarks>生命周期不会寻找唯一性，允许存在多实例</remarks>
        /// <remarks>加入到待添加队列，在下一次unity生命周期调用</remarks>
        public static void AddLife([NotNull]ILife life)
        {
            if (life == null) throw new ArgumentNullException(nameof(life));
            lock (_lifeLock)
            {
                //Lifes.Add(life);
                _pendingLifeAdds.Enqueue(life);
                _needSync = true; // 标记需要同步
            }
            AppLogger.Log($"添加生命周期对象：{life.GetType().Name}");
        }
        
        /// <summary>
        /// 移除生命周期对象
        /// </summary>
        /// <param name="life"></param>
        public static void RemoveLife([NotNull]ILife life)
        {
            if (life == null) throw new ArgumentNullException(nameof(life));
            
            lock (_lifeLock)
            {
                if (Lifes.Remove(life))
                {
                    _needSync = true; // 标记需要同步
                    AppLogger.Log($"移除生命周期对象：{life.GetType().Name}");
                }
                else
                {
                    AppLogger.Warning($"生命周期对象 {life.GetType().Name} 不存在，无法移除。");
                }
            }
        }
        /// <summary>
        /// 同步待添加队列到生命周期列表
        /// <remarks>这里才是最终添加到生命周期列表的地方</remarks>
        /// </summary>
        public static void SyncToActiveList()
        {
            lock (_lifeLock)
            {
                while (_pendingLifeAdds.Count > 0)
                {
                    Lifes.Add(_pendingLifeAdds.Dequeue());
                }
                Lifes.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
        }
        
        #region 生命周期
        private float timer = 0f;
        private float timerUnscaleTime = 0f;
        private static bool _needSync = false;

        private void Update()
        {
            timer += Time.deltaTime;
            timerUnscaleTime += Time.unscaledDeltaTime;
            
            // 只在需要时同步，减少锁竞争
            if (_needSync)
            {
                SyncToActiveList();
                _needSync = false;
            }
            
            // 创建生命周期对象的快照，确保线程安全
            ILife[] lifeSnapshot;
            lock (_lifeLock)
            {
                lifeSnapshot = Lifes.ToArray();
            }
            
            // 安全执行生命周期回调
            ExecuteLifeUpdate(lifeSnapshot);
            
            if (timer >= 1f)
            {
                ExecuteLifePerSecondUpdate(lifeSnapshot);
                timer -= 1f; // 减去1秒，保留余数
            }
            
            // 处理不受时间缩放影响的每秒更新
            if (timerUnscaleTime >= 1f)
            {
                ExecuteLifePerSecondUpdateUnScaleTime(lifeSnapshot);
                timerUnscaleTime -= 1f;
            }
        }
        
        /// <summary>
        /// 安全执行LifeUpdate方法
        /// </summary>
        private static void ExecuteLifeUpdate(ILife[] lifeSnapshot)
        {
            for (int i = 0; i < lifeSnapshot.Length; i++)
            {
                var life = lifeSnapshot[i];
                try
                {
                    if (EnablePerformanceMonitoring)
                    {
                        var key = $"{life.GetType().Name}.LifeUpdate";
                        ModulePerformanceMonitor.StartTimer(key);
                        life.LifeUpdate();
                        ModulePerformanceMonitor.EndTimer(key);
                    }
                    else
                    {
                        life.LifeUpdate();
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Exception(new AppException($"模块 {life.GetType().Name} 在执行 LifeUpdate 时发生异常", ex));
                }
            }
        }
        
        /// <summary>
        /// 安全执行LifePerSecondUpdate方法
        /// </summary>
        private static void ExecuteLifePerSecondUpdate(ILife[] lifeSnapshot)
        {
            for (int i = 0; i < lifeSnapshot.Length; i++)
            {
                var life = lifeSnapshot[i];
                try
                {
                    if (EnablePerformanceMonitoring)
                    {
                        var key = $"{life.GetType().Name}.LifePerSecondUpdate";
                        ModulePerformanceMonitor.StartTimer(key);
                        life.LifePerSecondUpdate();
                        ModulePerformanceMonitor.EndTimer(key);
                    }
                    else
                    {
                        life.LifePerSecondUpdate();
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Exception(new AppException($"模块 {life.GetType().Name} 在执行 LifePerSecondUpdate 时发生异常", ex));
                }
            }
        }
        
        /// <summary>
        /// 安全执行LifePerSecondUpdateUnScaleTime方法
        /// </summary>
        private static void ExecuteLifePerSecondUpdateUnScaleTime(ILife[] lifeSnapshot)
        {
            for (int i = 0; i < lifeSnapshot.Length; i++)
            {
                var life = lifeSnapshot[i];
                try
                {
                    if (EnablePerformanceMonitoring)
                    {
                        var key = $"{life.GetType().Name}.LifePerSecondUpdateUnScaleTime";
                        ModulePerformanceMonitor.StartTimer(key);
                        life.LifePerSecondUpdateUnScaleTime();
                        ModulePerformanceMonitor.EndTimer(key);
                    }
                    else
                    {
                        life.LifePerSecondUpdateUnScaleTime();
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Exception(new AppException($"模块 {life.GetType().Name} 在执行 LifePerSecondUpdateUnScaleTime 时发生异常", ex));
                }
            }
        }
        
        /// <summary>
        /// 安全执行LifeFixedUpdate方法
        /// </summary>
        private static void ExecuteLifeFixedUpdate(ILife[] lifeSnapshot)
        {
            for (int i = 0; i < lifeSnapshot.Length; i++)
            {
                var life = lifeSnapshot[i];
                try
                {
                    if (EnablePerformanceMonitoring)
                    {
                        var key = $"{life.GetType().Name}.LifeFixedUpdate";
                        ModulePerformanceMonitor.StartTimer(key);
                        life.LifeFixedUpdate();
                        ModulePerformanceMonitor.EndTimer(key);
                    }
                    else
                    {
                        life.LifeFixedUpdate();
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Exception(new AppException($"模块 {life.GetType().Name} 在执行 LifeFixedUpdate 时发生异常", ex));
                }
            }
        }
        
        /// <summary>
        /// 安全执行LifeLateUpdate方法
        /// </summary>
        private static void ExecuteLifeLateUpdate(ILife[] lifeSnapshot)
        {
            for (int i = 0; i < lifeSnapshot.Length; i++)
            {
                var life = lifeSnapshot[i];
                try
                {
                    if (EnablePerformanceMonitoring)
                    {
                        var key = $"{life.GetType().Name}.LifeLateUpdate";
                        ModulePerformanceMonitor.StartTimer(key);
                        life.LifeLateUpdate();
                        ModulePerformanceMonitor.EndTimer(key);
                    }
                    else
                    {
                        life.LifeLateUpdate();
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Exception(new AppException($"模块 {life.GetType().Name} 在执行 LifeLateUpdate 时发生异常", ex));
                }
            }
        }

        private void FixedUpdate()
        {
            // 只在需要时同步，减少锁竞争
            if (_needSync)
            {
                SyncToActiveList();
                _needSync = false;
            }
            
            // 创建生命周期对象的快照，确保线程安全
            ILife[] lifeSnapshot;
            lock (_lifeLock)
            {
                lifeSnapshot = Lifes.ToArray();
            }
            
            ExecuteLifeFixedUpdate(lifeSnapshot);
        }
        
        private void LateUpdate()
        {
            // 只在需要时同步，减少锁竞争
            if (_needSync)
            {
                SyncToActiveList();
                _needSync = false;
            }
            
            // 创建生命周期对象的快照，确保线程安全
            ILife[] lifeSnapshot;
            lock (_lifeLock)
            {
                lifeSnapshot = Lifes.ToArray();
            }
            
            ExecuteLifeLateUpdate(lifeSnapshot);
        }

        private void OnApplicationQuit()
        {
            // 按照优先级倒序关闭模块，确保依赖关系正确
            var moduleList = Modules.Values.ToList();
            moduleList.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            foreach (var module in moduleList)
            {
                try
                {
                    module.Shutdown();
                }
                catch (Exception ex)
                {
                    AppLogger.Exception(new AppException($"模块 {module.GetType().Name} 关闭时发生异常", ex));
                }
            }
        }

        #endregion
    }
}
