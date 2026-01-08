using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 自动调整GridLayoutGroup内容宽度
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("UI/AutoResizeToGridContentWidth")]
public class AutoResizeToGridContentWidth : MonoBehaviour
{
    public GridLayoutGroup grid;
    public bool includePadding = true;
    public bool ignoreInactiveAndIgnored = true;

    RectTransform rt;
    bool pendingApply;

    void OnEnable()
    {
        rt = GetComponent<RectTransform>();
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        pendingApply = true;
    }

    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        if (rt == null) rt = GetComponent<RectTransform>();
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        pendingApply = true;
    }

    void Update()
    {
        if (!Application.isPlaying && UnityEngine.UI.CanvasUpdateRegistry.IsRebuildingLayout()) return;
        Apply();
    }

    void OnTransformChildrenChanged()
    {
        pendingApply = true;
    }

    int GetEffectiveChildCount()
    {
        int count = 0;
        if (grid == null) return count;
        var grt = grid.transform as RectTransform;
        if (grt == null) return count;
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
            count++;
        }
        return count;
    }

    void Apply()
    {
        if (grid == null || rt == null) return;
        int childCount = GetEffectiveChildCount();
        float cellWidth = grid.cellSize.x;
        float spacingX = grid.spacing.x;
        int columns = 0;
        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            columns = Mathf.Max(1, grid.constraintCount);
        }
        else if (grid.constraint == GridLayoutGroup.Constraint.FixedRowCount)
        {
            columns = Mathf.CeilToInt(childCount / (float)Mathf.Max(1, grid.constraintCount));
        }
        else
        {
            columns = childCount;
        }
        int rowsCount = 0;
        if (columns > 0) rowsCount = Mathf.CeilToInt(childCount / (float)columns);
        float maxRowWidth = 0f;
        for (int r = 0; r < rowsCount; r++)
        {
            int rowCount = Mathf.Min(columns, childCount - r * columns);
            float rowWidth = rowCount * cellWidth + Mathf.Max(0, rowCount - 1) * spacingX;
            if (rowWidth > maxRowWidth) maxRowWidth = rowWidth;
        }
        float width = maxRowWidth + (includePadding ? grid.padding.horizontal : 0f);
        if (width < 0f) width = 0f;
        float current = rt.rect.width;
        if (Mathf.Abs(current - width) > 0.5f)
        {
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
        pendingApply = false;
    }
}
