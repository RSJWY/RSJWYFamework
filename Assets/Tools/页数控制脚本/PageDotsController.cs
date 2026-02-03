using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RSJWYFamework.Runtime.UI;
using System;

/// <summary>
/// 页面点控制器：用于显示和管理分页指示点，支持以下能力：
/// 1) 根据总页数动态生成/同步点位，并显示对应页码数字；
/// 2) 通过点击任意点位，直接跳转到该页（触发 OnPageChanged 事件，外部控制器监听并处理）；
/// 3) 提供上一页/下一页按钮引用，便于统一交由该控制器管理（更通用可复用）；
/// 4) 在运行时重建布局，保证指示器位置与数字更新正确；
/// 用法：外部设置 totalPages 后调用 RefreshTotalPages，再调用 UpdateCurrentPage 同步当前页即可。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("UI/PageDotsController")]
public class PageDotsController : MonoBehaviour
{
    /// <summary>
    /// 页面点容器：用于承载所有页码点；建议为水平 GridLayoutGroup。
    /// </summary>
    public GridLayoutGroup grid;
    /// <summary>
    /// 当前选中高亮指示器：显示当前页的数字与位置（跟随对应点）。
    /// </summary>
    public PageNumber_Item indicator;
    /// <summary>
    /// 页面点预制体：用于生成可点击的页码点，每个点上需挂载 UIEventListener。
    /// </summary>
    public PageNumber_Item dotPrefab;
    /// <summary>
    /// 是否忽略 inactive 与 LayoutElement.ignoreLayout 的子节点（用于编辑器下清理无效点）。
    /// </summary>
    public bool ignoreInactiveAndIgnored = true;
    
    /// <summary>
    /// 最大可见点数量：当总页数超过该值时，指示窗口滑动显示部分页码。
    /// </summary>
    public int maxVisibleDots = 5;

    /// <summary>
    /// 页面跳转事件回调（供外部控制器监听）：参数为目标页索引，从 0 开始。
    /// </summary>
    public event Action<int> OnPageChanged;

    /// <summary>
    /// 上一页按钮：引用外部的 UIEventListener，以便统一交由该控制器管理。
    /// </summary>
    public UIEventListener prevPageBtn;

    /// <summary>
    /// 下一页按钮：引用外部的 UIEventListener，以便统一交由该控制器管理。
    /// </summary>
    public UIEventListener nextPageBtn;

    /// <summary>
    /// 当前有效的点列表（运行时按世界坐标 X 排序，保证与视觉顺序一致）。
    /// </summary>
    readonly List<RectTransform> dots = new List<RectTransform>();
    /// <summary>
    /// 总页数。
    /// </summary>
    private int m_TotalPages;
    /// <summary>
    /// 当前页索引（0-based）。
    /// </summary>
    private int m_CurrentPageIndex;
    /// <summary>
    /// 当前窗口起始页，用于将窗口内第 0 个点映射到实际页码。
    /// 例如在总页数很多时，窗口显示 [startPage..startPage+dots.Count-1]。
    /// </summary>
    private int m_StartPage;

    /// <summary>
    /// 其他需要同步的 PageDotsController 实例（子实例）
    /// 子实例将跟随主实例的状态变化，并可触发翻页操作
    /// </summary>
    public List<PageDotsController> syncInstances = new List<PageDotsController>();

    /// <summary>
    /// 是否为单页显示模式（仅显示一页，不支持点位跳转，仅支持左右翻页）
    /// </summary>
    public bool isSinglePageMode = false;

    /// <summary>
    /// 组件启用：自动获取 Grid 并同步子节点。
    /// </summary>
    void OnEnable()
    {
        if (grid == null) grid = GetComponentInChildren<GridLayoutGroup>(true);
        
        // 如果是子实例，可能需要初始化状态
        if (isSinglePageMode)
        {
            maxVisibleDots = 1; // 强制仅显示1个点
        }
        
        // 注意：这里不要在 OnEnable 中直接调用 RefreshTotalPages，
        // 因为如果存在互相引用或者在 Awake/Start 阶段数据尚未准备好，可能会导致问题。
        // RefreshTotalPages 通常由外部管理器（如 AnnualModelWorkerManager）在初始化数据后主动调用。
        // 但为了保证编辑器下能看到效果，或者运行时有默认状态，可以保留，但要小心死循环。
        // 既然是外部控制，这里可以移除自动刷新，或者加个判空保护。
        // 另外，syncInstances 可能会导致递归调用，如果配置不当（例如 A 同步 B，B 又同步 A）。
        // 为安全起见，我们移除 OnEnable 中的 RefreshTotalPages，依赖外部调用。
        // RefreshTotalPages(m_TotalPages); 

        // 内部注册按钮事件
        if (Application.isPlaying)
        {
            if (prevPageBtn != null)
            {
                // 先移除防止重复绑定（虽然 Action 是多播委托，但为了安全）
                prevPageBtn.onClick = null;
                prevPageBtn.onClick += OnPrevPageClick;
            }
            if (nextPageBtn != null)
            {
                nextPageBtn.onClick = null;
                nextPageBtn.onClick += OnNextPageClick;
            }

            // 为子实例绑定事件
            foreach (var instance in syncInstances)
            {
                if (instance != null && instance != this) // 防止自己同步自己
                {
                    // 设置子实例模式
                    instance.isSinglePageMode = true; // 强制子实例为单页模式（根据需求描述）
                    instance.maxVisibleDots = 1;
                    
                    // 绑定子实例的翻页事件到主实例的转发逻辑
                    // 改为事件监听模式，不再直接操作子实例的按钮委托，避免被子实例自身的 OnEnable 覆盖
                    instance.OnPageChanged -= OnChildPageChanged;
                    instance.OnPageChanged += OnChildPageChanged;
                }
            }
        }
    }

    /// <summary>
    /// 组件禁用：移除事件监听
    /// </summary>
    void OnDisable()
    {
        if (Application.isPlaying)
        {
            foreach (var instance in syncInstances)
            {
                if (instance != null)
                {
                    instance.OnPageChanged -= OnChildPageChanged;
                }
            }
        }
    }

    /// <summary>
    /// 内部处理：子实例触发的翻页事件
    /// </summary>
    private void OnChildPageChanged(int pageIndex)
    {
        // 转发给主实例的监听者
        OnPageChanged?.Invoke(pageIndex);
    }

    /// <summary>
    /// 在编辑器中验证数据，防止循环引用
    /// </summary>
    private void OnValidate()
    {
        // 移除空引用
        syncInstances.RemoveAll(x => x == null);

        // 1. 移除自身
        if (syncInstances.Contains(this))
        {
            Debug.LogWarning($"[PageDotsController] {name}: Cannot sync with itself. Removed from list.");
            syncInstances.Remove(this);
        }

        // 2. 检测并移除简单的循环引用 (A -> B, B -> A)
        // 注意：更复杂的循环引用（A->B->C->A）需要图遍历算法，这里只做简单的直接检测
        for (int i = syncInstances.Count - 1; i >= 0; i--)
        {
            var child = syncInstances[i];
            if (child != null && child.syncInstances.Contains(this))
            {
                Debug.LogWarning($"[PageDotsController] {name}: Circular reference detected with {child.name}. Removed from list to prevent infinite loop.");
                syncInstances.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 内部处理：上一页按钮点击
    /// </summary>
    private void OnPrevPageClick()
    {
        if (m_TotalPages <= 0) return;
        
        // 计算目标页码（支持循环）
        int targetPage = m_CurrentPageIndex - 1;
        if (targetPage < 0) targetPage = m_TotalPages - 1; // Loop to last
        
        // 触发跳转事件
        if (targetPage != m_CurrentPageIndex)
        {
            OnPageChanged?.Invoke(targetPage);
        }
    }

    /// <summary>
    /// 内部处理：下一页按钮点击
    /// </summary>
    private void OnNextPageClick()
    {
        if (m_TotalPages <= 0) return;

        // 计算目标页码（支持循环）
        int targetPage = m_CurrentPageIndex + 1;
        if (targetPage >= m_TotalPages) targetPage = 0; // Loop to first

        // 触发跳转事件
        if (targetPage != m_CurrentPageIndex)
        {
            OnPageChanged?.Invoke(targetPage);
        }
    }

    /// <summary>
    /// 监听子节点变化（编辑器/运行时均可触发），保持 dots 列表与场景一致。
    /// </summary>
    void OnTransformChildrenChanged()
    {
        SyncDotsFromChildren();
    }

    /// <summary>
    /// 点位点击回调：通过闭包捕获具体 GameObject，进而映射到窗口内索引，再结合 m_StartPage 得到实际页码。
    /// </summary>
    /// <param name="go">被点击的页码点对象</param>
    private void OnDotClick(GameObject go)
    {
        // 找到点击的 dot 在列表中的索引
        RectTransform rt = go.transform as RectTransform;
        int index = dots.IndexOf(rt);
        
        if (index >= 0)
        {
            
            // 计算实际的目标页码
            int targetPage = m_StartPage + index;
            if (targetPage >= 0 && targetPage < m_TotalPages)
            {
                // 如果点击的不是当前页，则触发回调
                if (targetPage != m_CurrentPageIndex)
                {
                    OnPageChanged?.Invoke(targetPage);
                }
            }
            
        }
    }

    /// <summary>
    /// 刷新总页数：动态生成/销毁点位，绑定点击事件，并重建布局与当前页显示。
    /// </summary>
    public void RefreshTotalPages(int totalPages)
    {
        if (grid == null) return;
        if (totalPages < 0) totalPages = 0;
        
        m_TotalPages = totalPages;
        
        // 计算实际需要显示的点的数量（不超过最大限制）
        // 如果是单页模式，强制为1（如果总页数>0）
        int visibleCount = Mathf.Min(totalPages, maxVisibleDots);
        if (isSinglePageMode && totalPages > 0) visibleCount = 1;
        
        SyncDotsFromChildren();
        
        while (dots.Count < visibleCount)
        {
            if (dotPrefab == null) break;
            var go = Application.isPlaying ? UnityEngine.Object.Instantiate(dotPrefab.gameObject, grid.transform) : InstantiateEditor(dotPrefab.gameObject, grid.transform);
            var rt = go.transform as RectTransform;
            go.gameObject.SetActive(true);
            if (rt != null)
            {
                dots.Add(rt);
                // 绑定点击事件：UIEventListener.onClick 为无参委托，使用闭包捕获 go。
                if (Application.isPlaying)
                {
                    var listener = UIEventListener.Get(go);
                    // 单页模式下点位不可点击跳转（因为只显示当前页，点自己没意义，或者需求不允许跳）
                    // 但保留回调以防万一，或者根据需求屏蔽
                    if (!isSinglePageMode)
                    {
                        listener.onClick = () => OnDotClick(go);
                    }
                    else
                    {
                        listener.onClick = null; // 移除点击事件
                    }
                }
            }
        }
        while (dots.Count > visibleCount)
        {
            var last = dots[dots.Count - 1];
            if (last != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(last.gameObject); else UnityEngine.Object.DestroyImmediate(last.gameObject);
            }
            dots.RemoveAt(dots.Count - 1);
        }
        RebuildLayout();
        
        // 刷新当前页显示
        UpdateCurrentPage(m_CurrentPageIndex);

        // 同步刷新所有子实例
        foreach (var instance in syncInstances)
        {
            if (instance != null && instance != this)
            {
                instance.RefreshTotalPages(totalPages);
            }
        }
    }

    /// <summary>
    /// 刷新当前页：计算可视窗口起点，使当前页尽量居中；同步指示器位置与数字。
    /// </summary>
    public void UpdateCurrentPage(int pageIndex)
    {
        if (grid == null || indicator == null) return;
        
        // 确保页码在有效范围内
        if (pageIndex < 0) pageIndex = 0;
        if (m_TotalPages > 0 && pageIndex >= m_TotalPages) pageIndex = m_TotalPages - 1;
        
        // 如果页码没有变化且不需要强制刷新，可以跳过（可选优化）
        // 但为了保证 UI 状态正确，通常不跳过，或者需要更复杂的脏标记
        // 这里主要解决递归问题，下面已经加了 instance != this 的判断
        
        m_CurrentPageIndex = pageIndex;
        
        SyncDotsFromChildren();
        if (dots.Count == 0) 
        {
             indicator.gameObject.SetActive(false);
             // 如果子实例也要隐藏
             // 避免无限递归：只有主实例（syncInstances有值）才去驱动子实例
             if (syncInstances != null && syncInstances.Count > 0)
             {
                 foreach (var instance in syncInstances)
                 {
                     if(instance != null && instance != this) instance.UpdateCurrentPage(pageIndex);
                 }
             }
             return;
        }

        // 计算显示的起始页码（窗口起点），尝试让当前页居中
        int halfWindow = maxVisibleDots / 2;
        int startPage = pageIndex - halfWindow;
        
        // 单页模式下，起始页就是当前页
        if (isSinglePageMode)
        {
            startPage = pageIndex;
        }
        else
        {
            // 边界处理
            if (startPage < 0) startPage = 0;
            if (startPage + dots.Count > m_TotalPages) 
            {
                startPage = m_TotalPages - dots.Count;
            }
            if (startPage < 0) startPage = 0;
        }

        m_StartPage = startPage;

        RectTransform targetDot = null;
        
        for (int i = 0; i < dots.Count; i++)
        {
            int pageNum = startPage + i;
            var dotRect = dots[i];
            
            // 更新窗口内每个点的显示数字（页码从 1 开始显示）
            var pageItem = dotRect.GetComponent<PageNumber_Item>();
            if (pageItem != null && pageItem.pageNumberText != null)
            {
                pageItem.pageNumberText.text = (pageNum + 1).ToString();
            }
            
            // 找到当前页对应的点
            // 在单页模式下，唯一的那个点（i=0）就对应当前页
            if (isSinglePageMode)
            {
                if (i == 0) targetDot = dotRect;
            }
            else
            {
                if (pageNum == pageIndex)
                {
                    targetDot = dotRect;
                }
            }
        }
        
        if (targetDot != null)
        {
            indicator.gameObject.SetActive(true);
            // 依赖布局系统的结果：已在运行时调用 RebuildLayout，且 dots 已按世界坐标排序。
            indicator.transform.position = targetDot.position;
            
            // 更新指示器上的文字
            if (indicator.pageNumberText != null)
            {
                indicator.pageNumberText.text = (pageIndex + 1).ToString();
            }
        }
        else
        {
            // 如果总页数为 0 或出现异常，隐藏指示器
             indicator.gameObject.SetActive(false);
        }

        // 同步更新所有子实例
        // 避免无限递归：只有主实例（syncInstances有值）才去驱动子实例
        // 子实例通常 syncInstances 为空
        if (syncInstances != null && syncInstances.Count > 0)
        {
            foreach (var instance in syncInstances)
            {
                if (instance != null && instance != this)
                {
                    instance.UpdateCurrentPage(pageIndex);
                }
            }
        }
    }

    /// <summary>
    /// 同步子节点为点列表：支持忽略 inactive/ignoreLayout，并在运行时为每个点绑定点击事件。
    /// </summary>
    void SyncDotsFromChildren()
    {
        dots.Clear();
        if (grid == null) return;
        var grt = grid.transform as RectTransform;
        if (grt == null) return;
        for (int i = 0; i < grt.childCount; i++)
        {
            var child = grt.GetChild(i) as RectTransform;
            if (child == null) continue;
            if (ignoreInactiveAndIgnored)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                var le = child.GetComponent<LayoutElement>();
                if (le != null && le.ignoreLayout) continue;
            }
            dots.Add(child);
            
            // 绑定点击事件：UIEventListener.onClick 为无参委托，使用闭包捕获 child.gameObject。
            if (Application.isPlaying)
            {
                var listener = UIEventListener.Get(child.gameObject);
                listener.onClick = () => OnDotClick(child.gameObject);
            }
        }

        // 依据世界坐标 X 排序，确保列表顺序与视觉顺序一致
        //dots.Sort((a, b) => a.position.x.CompareTo(b.position.x));
    }

    /// <summary>
    /// 强制重建布局：确保在增删点后位置与尺寸及时更新。
    /// </summary>
    void RebuildLayout()
    {
        if (grid == null) return;
        var grt = grid.transform as RectTransform;
        if (grt == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(grt);
    }

    /// <summary>
    /// 编辑器环境下安全实例化：避免使用 SetParent 默认保持世界坐标带来的偏移。
    /// </summary>
    GameObject InstantiateEditor(GameObject prefab, Transform parent)
    {
        var go = UnityEngine.Object.Instantiate(prefab);
        go.transform.SetParent(parent, false);
        return go;
    }
}
/*
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 页面点控制器：用于显示和管理分页指示点，支持以下能力：
/// 1) 根据总页数动态生成/同步点位，并显示对应页码数字；
/// 2) 通过点击任意点位，直接跳转到该页（触发 OnPageChanged 事件，外部控制器监听并处理）；
/// 3) 提供上一页/下一页按钮引用，便于统一交由该控制器管理（更通用可复用）；
/// 4) 在运行时重建布局，保证指示器位置与数字更新正确；
/// 用法：外部设置 totalPages 后调用 RefreshTotalPages，再调用 UpdateCurrentPage 同步当前页即可。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("UI/PageDotsController")]
public class PageDotsController : MonoBehaviour
{
    /// <summary>
    /// 页面点容器：用于承载所有页码点；建议为水平 GridLayoutGroup。
    /// </summary>
    public GridLayoutGroup grid;
    /// <summary>
    /// 当前选中高亮指示器：显示当前页的数字与位置（跟随对应点）。
    /// </summary>
    public GameObject indicator;
    /// <summary>
    /// 页面点预制体：用于生成可点击的页码点，每个点上需挂载 UIEventListener。
    /// </summary>
    public GameObject dotPrefab;
    /// <summary>
    /// 是否忽略 inactive 与 LayoutElement.ignoreLayout 的子节点（用于编辑器下清理无效点）。
    /// </summary>
    public bool ignoreInactiveAndIgnored = true;
    
    /// <summary>
    /// 最大可见点数量：当总页数超过该值时，指示窗口滑动显示部分页码。
    /// </summary>
    public int maxVisibleDots = 5;

    /// <summary>
    /// 页面跳转事件回调（供外部控制器监听）：参数为目标页索引，从 0 开始。
    /// </summary>
    public event Action<int> OnPageChanged;

    /// <summary>
    /// 上一页按钮：引用外部的 UIEventListener，以便统一交由该控制器管理。
    /// </summary>
    public UIEventListener prevPageBtn;

    /// <summary>
    /// 下一页按钮：引用外部的 UIEventListener，以便统一交由该控制器管理。
    /// </summary>
    public UIEventListener nextPageBtn;

    /// <summary>
    /// 当前有效的点列表（运行时按世界坐标 X 排序，保证与视觉顺序一致）。
    /// </summary>
    readonly List<RectTransform> dots = new List<RectTransform>();
    /// <summary>
    /// 总页数。
    /// </summary>
    private int m_TotalPages;
    /// <summary>
    /// 当前页索引（0-based）。
    /// </summary>
    private int m_CurrentPageIndex;
    /// <summary>
    /// 当前窗口起始页，用于将窗口内第 0 个点映射到实际页码。
    /// 例如在总页数很多时，窗口显示 [startPage..startPage+dots.Count-1]。
    /// </summary>
    private int m_StartPage;

    /// <summary>
    /// 组件启用：自动获取 Grid 并同步子节点。
    /// </summary>
    void OnEnable()
    {
        if (grid == null) grid = GetComponentInChildren<GridLayoutGroup>(true);
        RefreshTotalPages(m_TotalPages);

        if (Application.isPlaying)
        {
            if (prevPageBtn != null)
            {
                prevPageBtn.onClick = OnPrevPageClick;
            }
            if (nextPageBtn != null)
            {
                nextPageBtn.onClick = OnNextPageClick;
            }
        }
    }

    private void OnPrevPageClick()
    {
        if (m_TotalPages <= 0) return;
        int targetPage = m_CurrentPageIndex - 1;
        if (targetPage < 0) targetPage = 0;
        
        if (targetPage != m_CurrentPageIndex)
        {
            OnPageChanged?.Invoke(targetPage);
        }
    }

    private void OnNextPageClick()
    {
        if (m_TotalPages <= 0) return;
        int targetPage = m_CurrentPageIndex + 1;
        if (targetPage >= m_TotalPages) targetPage = m_TotalPages - 1;

        if (targetPage != m_CurrentPageIndex)
        {
            OnPageChanged?.Invoke(targetPage);
        }
    }

    /// <summary>
    /// 监听子节点变化（编辑器/运行时均可触发），保持 dots 列表与场景一致。
    /// </summary>
    void OnTransformChildrenChanged()
    {
        SyncDotsFromChildren();
    }

    /// <summary>
    /// 点位点击回调：通过闭包捕获具体 GameObject，进而映射到窗口内索引，再结合 m_StartPage 得到实际页码。
    /// </summary>
    /// <param name="go">被点击的页码点对象</param>
    private void OnDotClick(GameObject go)
    {
        // 找到点击的 dot 在列表中的索引
        RectTransform rt = go.transform as RectTransform;
        int index = dots.IndexOf(rt);
        
        if (index >= 0)
        {
            // 计算实际的目标页码
            int targetPage = m_StartPage + index;
            if (targetPage >= 0 && targetPage < m_TotalPages)
            {
                // 如果点击的不是当前页，则触发回调
                if (targetPage != m_CurrentPageIndex)
                {
                    OnPageChanged?.Invoke(targetPage);
                }
            }
        }
    }

    /// <summary>
    /// 刷新总页数：动态生成/销毁点位，绑定点击事件，并重建布局与当前页显示。
    /// </summary>
    public void RefreshTotalPages(int totalPages)
    {
        if (grid == null) return;
        if (totalPages < 0) totalPages = 0;
        
        m_TotalPages = totalPages;
        
        // 计算实际需要显示的点的数量（不超过最大限制）
        int visibleCount = Mathf.Min(totalPages, maxVisibleDots);
        
        SyncDotsFromChildren();
        
        while (dots.Count < visibleCount)
        {
            if (dotPrefab == null) break;
            var go = Application.isPlaying ? UnityEngine.Object.Instantiate(dotPrefab.gameObject, grid.transform) : InstantiateEditor(dotPrefab.gameObject, grid.transform);
            var rt = go.transform as RectTransform;
            go.gameObject.SetActive(true);
            if (rt != null)
            {
                dots.Add(rt);
                // 绑定点击事件：UIEventListener.onClick 为无参委托，使用闭包捕获 go。
                if (Application.isPlaying)
                {
                    var listener = UIEventListener.Get(go);
                    listener.onClick += () => OnDotClick(go);
                }
            }
        }
        while (dots.Count > visibleCount)
        {
            var last = dots[dots.Count - 1];
            if (last != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(last.gameObject); else UnityEngine.Object.DestroyImmediate(last.gameObject);
            }
            dots.RemoveAt(dots.Count - 1);
        }
        RebuildLayout();
        
        // 刷新当前页显示
        UpdateCurrentPage(m_CurrentPageIndex);
    }

    /// <summary>
    /// 刷新当前页：计算可视窗口起点，使当前页尽量居中；同步指示器位置与数字。
    /// </summary>
    public void UpdateCurrentPage(int pageIndex)
    {
        if (grid == null || indicator == null) return;
        
        // 确保页码在有效范围内
        if (pageIndex < 0) pageIndex = 0;
        if (m_TotalPages > 0 && pageIndex >= m_TotalPages) pageIndex = m_TotalPages - 1;
        
        m_CurrentPageIndex = pageIndex;
        
        SyncDotsFromChildren();
        if (dots.Count == 0) 
        {
             indicator.gameObject.SetActive(false);
             return;
        }

        // 计算显示的起始页码（窗口起点），尝试让当前页居中
        int halfWindow = maxVisibleDots / 2;
        int startPage = pageIndex - halfWindow;
        
        // 边界处理
        if (startPage < 0) startPage = 0;
        if (startPage + dots.Count > m_TotalPages) 
        {
            startPage = m_TotalPages - dots.Count;
        }
        if (startPage < 0) startPage = 0;

        m_StartPage = startPage;

        RectTransform targetDot = null;
        
        for (int i = 0; i < dots.Count; i++)
        {
            int pageNum = startPage + i;
            var dotRect = dots[i];
            
            /* // 更新窗口内每个点的显示数字（页码从 1 开始显示）
            var pageItem = dotRect.GetComponent<PageNumber_Item>();
            if (pageItem != null && pageItem.pageNumberText != null)
            {
                pageItem.pageNumberText.text = (pageNum + 1).ToString();
            }* /
            
            // 找到当前页对应的点
            if (pageNum == pageIndex)
            {
                targetDot = dotRect;
            }
        }
        
        if (targetDot != null)
        {
            indicator.gameObject.SetActive(true);
            // 依赖布局系统的结果：已在运行时调用 RebuildLayout，且 dots 已按世界坐标排序。
            indicator.transform.position = targetDot.position;
            
            /* // 更新指示器上的文字
            if (indicator.pageNumberText != null)
            {
                indicator.pageNumberText.text = (pageIndex + 1).ToString();
            }* /
        }
        else
        {
            // 如果总页数为 0 或出现异常，隐藏指示器
             indicator.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 同步子节点为点列表：支持忽略 inactive/ignoreLayout，并在运行时为每个点绑定点击事件。
    /// </summary>
    void SyncDotsFromChildren()
    {
        dots.Clear();
        if (grid == null) return;
        var grt = grid.transform as RectTransform;
        if (grt == null) return;
        for (int i = 0; i < grt.childCount; i++)
        {
            var child = grt.GetChild(i) as RectTransform;
            if (child == null) continue;
            if (ignoreInactiveAndIgnored)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                var le = child.GetComponent<LayoutElement>();
                if (le != null && le.ignoreLayout) continue;
            }
            dots.Add(child);
            
            // 绑定点击事件：UIEventListener.onClick 为无参委托，使用闭包捕获 child.gameObject。
            if (Application.isPlaying)
            {
                var listener = UIEventListener.Get(child.gameObject);
                listener.onClick = () => OnDotClick(child.gameObject);
            }
        }

        // 依据世界坐标 X 排序，确保列表顺序与视觉顺序一致
        //dots.Sort((a, b) => a.position.x.CompareTo(b.position.x));
    }

    /// <summary>
    /// 强制重建布局：确保在增删点后位置与尺寸及时更新。
    /// </summary>
    void RebuildLayout()
    {
        if (grid == null) return;
        var grt = grid.transform as RectTransform;
        if (grt == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(grt);
    }

    /// <summary>
    /// 编辑器环境下安全实例化：避免使用 SetParent 默认保持世界坐标带来的偏移。
    /// </summary>
    GameObject InstantiateEditor(GameObject prefab, Transform parent)
    {
        var go = UnityEngine.Object.Instantiate(prefab);
        go.transform.SetParent(parent, false);
        return go;
    }
}

*/