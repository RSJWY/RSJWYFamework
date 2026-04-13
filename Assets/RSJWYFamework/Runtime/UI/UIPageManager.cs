using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        /// 页面栈，管理页面层级
        /// </summary>
        private readonly Stack<UIPageBase> _pageStack = new Stack<UIPageBase>();

        /// <summary>
        /// UI 根节点
        /// </summary>
        private Transform _uiRoot;

        /// <summary>
        /// 缓存已加载的页面，避免重复实例化 (PageName -> Instance)
        /// </summary>
        private readonly Dictionary<string, UIPageBase> _pageCache = new Dictionary<string, UIPageBase>();

        /// <summary>
        /// UI 资源加载路径前缀 (Resources/UI/...)
        /// </summary>
        private const string UI_RESOURCE_PATH = "UIPrefab/";

        public override void Initialize()
        {
            // 1. 初始化容器
            _pageStack.Clear();
            _pageCache.Clear();

            // 2. 查找或创建 UI Root
            // 主人，这里我们假设场景里有一个 Canvas 叫做 "UIRoot"
            // 如果没有，小码酱就帮您创建一个临时的，但最好还是主人自己在场景里放一个哦！
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

            AppLogger.Log("UIPageManager 初始化完成！等待指令~ (≧▽≦)");
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
            AppLogger.Log("UIPageManager 已停止工作，晚安~");
        }

        /// <summary>
        /// 打开新页面（压栈）
        /// </summary>
        /// <param name="pageName">页面资源名称（不带路径，例如 "MainMenu"）</param>
        /// <param name="data">传递参数</param>
        public void Push(string pageName, object data = null)
        {
            // 1. 检查栈顶页面，暂停它
            if (_pageStack.Count > 0)
            {
                var topPage = _pageStack.Peek();
                topPage.OnPause();
            }

            // 2. 获取页面实例
            UIPageBase page = GetPageInstance(pageName);
            if (page == null)
            {
                AppLogger.Error($"无法打开页面: {pageName}，加载失败了呜呜呜...");
                return;
            }

            // 3. 入栈并进入
            _pageStack.Push(page);
            page.PageName = pageName;
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
            var currentPage = _pageStack.Pop();
            currentPage.OnExit();

            // 3. 恢复上一个页面
            if (_pageStack.Count > 0)
            {
                var previousPage = _pageStack.Peek();
                previousPage.OnResume();
            }
        }

        /// <summary>
        /// 替换当前页面 (Pop then Push)
        /// </summary>
        public void Replace(string pageName, object data = null)
        {
            if (_pageStack.Count > 0)
            {
                var currentPage = _pageStack.Pop();
                currentPage.OnExit();
            }
            
            Push(pageName, data);
        }

        /// <summary>
        /// 获取当前页面
        /// </summary>
        public UIPageBase GetCurrentPage()
        {
            if (_pageStack.Count > 0)
            {
                return _pageStack.Peek();
            }
            return null;
        }

        /// <summary>
        /// 预加载页面（加载并实例化，但不显示）
        /// </summary>
        /// <param name="pageName">页面名称</param>
        public void Preload(string pageName)
        {
            if (_pageCache.ContainsKey(pageName))
            {
                AppLogger.Log($"[UI] 页面 {pageName} 已经在缓存里啦，不用重复预加载哦！");
                return;
            }

            var page = GetPageInstance(pageName);
            if (page != null)
            {
                AppLogger.Log($"[UI] 页面 {pageName} 预加载成功！随时待命！( •̀ ω •́ )y");
            }
        }

        /// <summary>
        /// 获取页面实例（如果未加载会自动加载）
        /// </summary>
        /// <typeparam name="T">具体的页面类型</typeparam>
        /// <param name="pageName">页面名称</param>
        /// <returns>页面组件</returns>
        public T GetPage<T>(string pageName) where T : UIPageBase
        {
            var page = GetPageInstance(pageName);
            return page as T;
        }

        /// <summary>
        /// 获取页面实例（如果未加载会自动加载）
        /// </summary>
        /// <param name="pageName">页面名称</param>
        /// <returns>页面组件</returns>
        public UIPageBase GetPage(string pageName)
        {
            return GetPageInstance(pageName);
        }

        /// <summary>
        /// 内部方法：获取或加载页面实例
        /// </summary>
        private UIPageBase GetPageInstance(string pageName)
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

            // 2. 加载资源 (Resources)
            // TODO: 如果主人以后用了 Addressables，记得来这里改一下哦！
            string path = UI_RESOURCE_PATH + pageName;
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                // 尝试直接加载（不带前缀）
                prefab = Resources.Load<GameObject>(pageName);
            }

            if (prefab == null)
            {
                AppLogger.Error($"Resources 目录下找不到 UI Prefab: {path}");
                return null;
            }

            // 3. 实例化
            var go = Instantiate(prefab, _uiRoot);
            go.name = pageName; // 去掉 (Clone) 后缀
            var pageComp = go.GetComponent<UIPageBase>();

            if (pageComp == null)
            {
                AppLogger.Error($"Prefab {pageName} 上没有挂载 UIPageBase 脚本！主人快去挂脚本！(>_<)");
                Destroy(go);
                return null;
            }

            // 4. 存入缓存
            _pageCache[pageName] = pageComp;
            
            // 默认隐藏，由 OnEnter 控制显示
            go.SetActive(false); 
            
            return pageComp;
        }
    }
}
