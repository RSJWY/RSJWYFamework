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
            
            /*// 更新窗口内每个点的显示数字（页码从 1 开始显示）
            var pageItem = dotRect.GetComponent<PageNumber_Item>();
            if (pageItem != null && pageItem.pageNumberText != null)
            {
                pageItem.pageNumberText.text = (pageNum + 1).ToString();
            }*/
            
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
            
            /*// 更新指示器上的文字
            if (indicator.pageNumberText != null)
            {
                indicator.pageNumberText.text = (pageIndex + 1).ToString();
            }*/
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
