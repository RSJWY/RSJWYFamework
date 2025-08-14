using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoRegisterModules()
        {
            if (!_initialized)
            {
                var _all= GameObject.FindObjectsOfType<ModuleManager>();
                if( _all.Length!=0)
                {
                    throw new AppException("场景中存在多个管理器！！初始化终止");
                }
                var _manager = new GameObject("[ModuleManager]");
                _manager.AddComponent<ModuleManager>();
                DontDestroyOnLoad( _manager );
                
                // 扫描所有程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
                // 模块注册
                var moduleTypes = assemblies
                    .SelectMany(a => a.GetTypes())
                    .Where(t=> t.IsDefined(typeof(ModuleAttribute), false) &&
                                  typeof(IModule).IsAssignableFrom(t) &&
                                  !t.IsAbstract &&
                                  !t.IsInterface &&
                                  !t.IsGenericType); 
                foreach (var type in moduleTypes)
                {
                    IModule moduleInstance;
                    // 检查是否为MonoBehaviour
                    if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        var managerObj = GameObject.Find("[ModuleManager]");
                        if (managerObj == null)
                        {
                            throw new AppException("找不到 ModuleManager 对象");
                        }
                        var moduleGO = new GameObject($"[Module]{type.Name}");
                        moduleGO.transform.parent = managerObj.transform;
                        moduleInstance = moduleGO.AddComponent(type) as IModule;
                    }
                    else
                    {
                        moduleInstance = Activator.CreateInstance(type) as IModule;
                    }
                    AddModule(moduleInstance,type);
                }
                
                _initialized=true;
                AppLogger.Log($"初始化模块管理器完成！模块数量为：{Modules.Count}，生命周期已存在数量为：{Lifes.Count}，生命周期等待添加数量：{_pendingLifeAdds.Count}");
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
                var managerObj = GameObject.Find("[ModuleManager]");
                if (managerObj == null)
                {
                    throw new AppException("找不到 ModuleManager 对象");
                }
                var moduleGO = new GameObject($"[Module]{type.Name}");
                moduleGO.transform.parent = managerObj.transform;
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
            if (Modules.Remove(type))
            {
                AppLogger.Log($"移除模块：{type.Name}");
                RemoveLife(Modules[type]);
                Modules.Remove(type);
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
            }
            AppLogger.Log($"添加生命周期对象：{life.GetType().Name}");
        }
        
        /// <summary>
        /// 移除生命周期对象
        /// </summary>
        /// <param name="life"></param>
        public static void RemoveLife([NotNull]ILife life)
        {
            if (Lifes.Contains(life))
            {
                lock (_lifeLock)
                {
                    //Lifes.Remove(life);
                    _pendingLifeAdds.Enqueue(life);
                }
                AppLogger.Log($"移除生命周期对象：{life.GetType().Name}");
            }
            else
            {
                AppLogger.Warning($"生命周期对象 {life.GetType().Name} 不存在，无法移除。");
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

        private void Update()
        {
            timer += Time.deltaTime;
            timerUnscaleTime+= Time.unscaledDeltaTime
                ;
            SyncToActiveList();
            foreach (var life in Lifes)
            {
                life.LifeUpdate();
            }
            if (timer >= 1f)
            {
                foreach (var life in Lifes)
                {
                    life.LifePerSecondUpdate();
                }
                timer -= 1f; // 减去1秒，保留余数
            }
            
        }

        private void FixedUpdate()
        {
            SyncToActiveList();
            foreach (var life in Lifes)
            {
                life.LifeFixedUpdate();
            }
        }
        
        private void LateUpdate()
        {
            SyncToActiveList();
            foreach (var life in Lifes)
            {
                life.LifeLateUpdate();
            }
        }

        private void OnApplicationQuit()
        {
            foreach (var module in Modules)
            {
                module.Value.Shutdown();
            }
        }

        #endregion
    }
}
