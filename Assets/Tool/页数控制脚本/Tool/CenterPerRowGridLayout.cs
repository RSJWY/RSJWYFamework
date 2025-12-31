using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 每行独立居中的GridLayoutGroup（支持任意列数/行数）
/// </summary>
[AddComponentMenu("UI/CenterPerRowGridLayout")]
public class CenterPerRowGridLayout : GridLayoutGroup
{
    private System.Collections.Generic.List<int> GetCenterOutOrder(int count)
    {
        var order = new System.Collections.Generic.List<int>(count);
        if (count <= 0) return order;
        if ((count & 1) == 1)
        {
            int center = count / 2;
            order.Add(center);
            for (int d = 1; d <= center; d++)
            {
                order.Add(center - d);
                order.Add(center + d);
            }
        }
        else
        {
            int rightCenter = count / 2;
            int leftCenter = rightCenter - 1;
            order.Add(leftCenter);
            order.Add(rightCenter);
            for (int d = 1; d < rightCenter; d++)
            {
                order.Add(leftCenter - d);
                order.Add(rightCenter + d);
            }
        }
        return order;
    }

    public override void SetLayoutHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        if (rectChildren.Count == 0) return;

        float cellWidth = cellSize.x;
        float cellHeight = cellSize.y;
        float spacingX = spacing.x;
        float spacingY = spacing.y;

        int columns = 0;
        if (constraint == Constraint.FixedColumnCount)
        {
            columns = Mathf.Max(1, constraintCount);
        }
        else if (constraint == Constraint.FixedRowCount)
        {
            columns = Mathf.CeilToInt(rectChildren.Count / (float)Mathf.Max(1, constraintCount));
        }
        else
        {
            columns = rectChildren.Count;
        }

        var rows = new System.Collections.Generic.List<System.Collections.Generic.List<RectTransform>>();
        var currentRow = new System.Collections.Generic.List<RectTransform>(columns);
        for (int i = 0; i < rectChildren.Count; i++)
        {
            currentRow.Add(rectChildren[i]);
            if (currentRow.Count >= columns)
            {
                rows.Add(currentRow);
                currentRow = new System.Collections.Generic.List<RectTransform>(columns);
            }
        }
        if (currentRow.Count > 0) rows.Add(currentRow);

        float totalHeight = rows.Count * cellHeight + Mathf.Max(0, rows.Count - 1) * spacingY;
        float startY = GetStartOffset(1, totalHeight);

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            int rowCount = row.Count;
            float rowWidth = rowCount * cellWidth + Mathf.Max(0, rowCount - 1) * spacingX;
            float startX = GetStartOffset(0, rowWidth);

            for (int k = 0; k < rowCount; k++)
            {
                int colIndex = k;
                var child = row[k];
                float x = startX + colIndex * (cellWidth + spacingX);
                float y = startY + rowIndex * (cellHeight + spacingY);
                SetChildAlongAxis(child, 0, x, cellWidth);
                SetChildAlongAxis(child, 1, y, cellHeight);
            }
        }
    }

    public override void SetLayoutVertical()
    {
        // 留空：已在 SetLayoutHorizontal 中设置两轴位置
    }

    protected void OnValidate()
    {
        if (!IsActive()) return;
        SetDirty();
    }
}
