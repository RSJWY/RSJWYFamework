using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RSJWYFamework.Runtime.UI;
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// UI 页面管理器
    /// 小码酱为主人准备好了栈结构，逻辑就拜托主人啦！(U •́ .̫ •̀ U)
    /// </summary>
    [Module]
    [ModuleDependency(typeof(EventManager))]
    [ModuleDependency(typeof(AppAsyncOperationSystem))]
    [ModuleDependency(typeof(AppConfigManager))]
    [ModuleDependency(typeof(StateMachineManager))]
    public class UIPageManager : ModuleBase
    {
        /// <summary>
        /// 页面栈 (使用 List 模拟栈，以便支持移除中间的页面)
        /// </summary>
        private readonly List<UIPageBase> _pageStack = new List<UIPageBase>();

        /// <summary>
        /// UI 根节点
        /// </summary>
        private Transform _uiRoot;
        
        /// <summary>
        /// UI 层级容器 (Layer -> Transform)
        /// </summary>
        private readonly Dictionary<UILayer, Transform> _layerRoots = new Dictionary<UILayer, Transform>();

        /// <summary>
        /// 缓存已加载的页面，避免重复实例化 (PageName -> Instance)
        /// </summary>
        private readonly Dictionary<string, UIPageBase> _pageCache = new Dictionary<string, UIPageBase>();

        /// <summary>
        /// 资源加载器
        /// </summary>
        private IUIResLoader _resLoader;

        public override void Initialize()
        {
            // 1. 初始化容器
            _pageStack.Clear();
            _pageCache.Clear();
            _layerRoots.Clear();
            
            // 2. 初始化资源加载器
            // Master, switching this line allows you to use YooAsset/Addressables later! 
            _resLoader = new ResourcesUIResLoader();

            // 3. 查找或创建 UI Root
            var rootObj = GameObject.Find("UIRoot");
            if (rootObj == null)
            {
                var canvasObj = new GameObject("UIRoot");
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                _uiRoot = canvasObj.transform;
                DontDestroyOnLoad(canvasObj); // 保持跨场景存在
                AppLogger.Warning("没找到 UIRoot，小码酱自动创建了一个！请主人检查场景设置哦~");
            }
            else
            {
                _uiRoot = rootObj.transform;
                DontDestroyOnLoad(rootObj);
            }
            
            // 4. 初始化层级节点
            InitLayers();

            AppLogger.Log("UIPageManager 初始化完成！等待指令~ (≧▽≦)");
        }
        
        private void InitLayers()
        {
            // 为每个层级创建一个空节点
            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                var layerObj = new GameObject(layer.ToString());
                var trans = layerObj.transform;
                trans.SetParent(_uiRoot, false);
                
                // 设置 RectTransform 撑满
                var rect = layerObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                
                _layerRoots[layer] = trans;
            }
        }

        public override void Shutdown()
        {
            // 清理缓存
            foreach (var page in _pageCache.Values)
            {
                if (page != null && page.gameObject != null)
                {
                    Destroy(page.gameObject);
                }
            }
            _pageCache.Clear();
            _pageStack.Clear();
            _layerRoots.Clear();
            
            // 卸载资源（如果加载器支持）
            // _resLoader.UnloadAll(); 
            
            AppLogger.Log("UIPageManager 已停止工作，晚安~");
        }

        /// <summary>
        /// 打开新页面（压栈）- 同步调用异步 (Fire and Forget)
        /// </summary>
        public void Push(string pageName, object data = null)
        {
            PushAsync(pageName, data).Forget();
        }
        
        /// <summary>
        /// 打开新页面（压栈）- 异步等待
        /// </summary>
        public async UniTask PushAsync(string pageName, object data = null)
        {
            // 2. 获取页面实例
            UIPageBase page = await GetPageInstanceAsync(pageName);
            if (page == null)
            {
                AppLogger.Error($"无法打开页面: {pageName}，加载失败了呜呜呜...");
                return;
            }

            // 1. 检查栈顶页面
            // 只有当新页面是【全屏】的，才需要暂停原来的页面
            // 否则（比如打开一个弹窗），原来的页面应该保持运行（可见），只是可能被遮挡
            if (_pageStack.Count > 0)
            {
                var topPage = _pageStack[_pageStack.Count - 1];
                
                if (page.WindowConfig.IsFullScreen)
                {
                    topPage.OnPause();
                }
                else
                {
                    // 如果是弹窗，虽然不 Pause，但可能需要失去焦点？
                    // 目前暂不处理，依赖弹窗自己的模态遮罩
                }
            }

            // 3. 入栈并进入
            _pageStack.Add(page);
            page.PageName = pageName;
            
            // 4. 确保层级正确 (SetAsLastSibling 在 Layer 容器内)
            if (_layerRoots.TryGetValue(page.WindowConfig.Layer, out var layerRoot))
            {
                page.transform.SetParent(layerRoot, false);
            }
            page.transform.SetAsLastSibling();
            
            page.OnEnter(data);
        }

        /// <summary>
        /// 关闭当前页面（出栈）
        /// </summary>
        public void Pop()
        {
            // 1. 检查栈是否为空
            if (_pageStack.Count == 0)
            {
                AppLogger.Warning("栈已经是空的啦，没有页面可以关闭咯！");
                return;
            }

            // 2. 退出当前页面
            var currentPage = _pageStack[_pageStack.Count - 1];
            _pageStack.RemoveAt(_pageStack.Count - 1);
            
            currentPage.OnExit();
            
            // 如果配置为不缓存，则销毁
            if (!currentPage.WindowConfig.KeepCached)
            {
                _pageCache.Remove(currentPage.PageName);
                Destroy(currentPage.gameObject);
                _resLoader.UnloadWindow(currentPage.PageName);
            }

            // 3. 恢复上一个页面
            // 只有当当前关闭的页面是【全屏】的，才需要恢复上一个页面
            // 因为只有全屏页面会导致上一个页面 Pause
            if (_pageStack.Count > 0 && currentPage.WindowConfig.IsFullScreen)
            {
                var previousPage = _pageStack[_pageStack.Count - 1];
                previousPage.OnResume();
            }
        }

        /// <summary>
        /// 关闭指定页面 (无论它在栈的哪里)
        /// </summary>
        public void Close(string pageName)
        {
            var page = _pageStack.Find(p => p.PageName == pageName);
            if (page == null)
            {
                AppLogger.Warning($"试图关闭页面 {pageName}，但在栈里没找到它！可能已经关掉啦？");
                return;
            }

            // 如果关闭的是栈顶页面，直接用 Pop (逻辑更完整)
            if (_pageStack.Count > 0 && _pageStack[_pageStack.Count - 1] == page)
            {
                Pop();
                return;
            }

            // 如果关闭的是中间的页面
            _pageStack.Remove(page);
            page.OnExit();

            if (!page.WindowConfig.KeepCached)
            {
                _pageCache.Remove(page.PageName);
                Destroy(page.gameObject);
                _resLoader.UnloadWindow(page.PageName);
            }
            
            AppLogger.Log($"[UI] 已移除中间层级页面: {pageName}");
        }

        /// <summary>
        /// 替换当前页面 (Pop then Push)
        /// </summary>
        public void Replace(string pageName, object data = null)
        {
            ReplaceAsync(pageName, data).Forget();
        }

        public async UniTask ReplaceAsync(string pageName, object data = null)
        {
             if (_pageStack.Count > 0)
            {
                // 手动 Pop
                var currentPage = _pageStack[_pageStack.Count - 1];
                _pageStack.RemoveAt(_pageStack.Count - 1);
                currentPage.OnExit();
                
                 if (!currentPage.WindowConfig.KeepCached)
                {
                    _pageCache.Remove(currentPage.PageName);
                    Destroy(currentPage.gameObject);
                    _resLoader.UnloadWindow(currentPage.PageName);
                }
            }
            
            await PushAsync(pageName, data);
        }

        /// <summary>
        /// 获取当前页面
        /// </summary>
        public UIPageBase GetCurrentPage()
        {
            if (_pageStack.Count > 0)
            {
                return _pageStack[_pageStack.Count - 1];
            }
            return null;
        }

        /// <summary>
        /// 预加载页面（加载并实例化，但不显示）
        /// </summary>
        public void Preload(string pageName)
        {
            PreloadAsync(pageName).Forget();
        }
        
        public async UniTask PreloadAsync(string pageName)
        {
            if (_pageCache.ContainsKey(pageName))
            {
                return;
            }
            await GetPageInstanceAsync(pageName);
        }

        /// <summary>
        /// 获取页面实例 (异步)
        /// </summary>
        public async UniTask<T> GetPageAsync<T>(string pageName) where T : UIPageBase
        {
            var page = await GetPageInstanceAsync(pageName);
            return page as T;
        }

        /// <summary>
        /// 内部方法：获取或加载页面实例
        /// </summary>
        private async UniTask<UIPageBase> GetPageInstanceAsync(string pageName)
        {
            // 1. 检查缓存
            if (_pageCache.TryGetValue(pageName, out var cachedPage))
            {
                if (cachedPage != null)
                {
                    return cachedPage;
                }
                _pageCache.Remove(pageName); // 引用丢失，移除
            }

            // 2. 加载资源 (使用加载器接口)
            // 优先检查 Attribute 里的 Path，如果没有就用 pageName
            // 但这里我们还没拿到 Class Type，所以只能先加载 Prefab 拿到 Component 再确认
            // 或者约定 pageName 就是 Path (简单起见)
            
            var go = await _resLoader.LoadWindowAsync(pageName);
            if (go == null)
            {
                AppLogger.Error($"加载 UI 失败: {pageName}");
                return null;
            }

            // 3. 实例化
            // 先暂时挂在 UIRoot 下，稍后 Push 时会调整到 Layer
            var instance = Instantiate(go, _uiRoot);
            instance.name = pageName; 
            var pageComp = instance.GetComponent<UIPageBase>();

            if (pageComp == null)
            {
                AppLogger.Error($"Prefab {pageName} 上没有挂载 UIPageBase 脚本！主人快去挂脚本！(>_<)");
                Destroy(instance);
                return null;
            }

            // 4. 存入缓存
            _pageCache[pageName] = pageComp;
            
            // 默认隐藏，由 OnEnter 控制显示
            instance.SetActive(false); 
            
            return pageComp;
        }
    }
}
