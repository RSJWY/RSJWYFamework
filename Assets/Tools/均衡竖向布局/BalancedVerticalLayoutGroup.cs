using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HSY.UI
{
    /// <summary>
    /// 均衡竖向布局组件
    /// <para>优先竖向排列，当竖向排列装不下时，新增一列。</para>
    /// <para>特点：会自动平衡各列的数量，避免出现“一列很长、一列很短”的情况。</para>
    /// </summary>
    [AddComponentMenu("Layout/Balanced Vertical Layout Group")]
    public class BalancedVerticalLayoutGroup : GridLayoutGroup
    {
        protected BalancedVerticalLayoutGroup()
        { }

        /// <summary>
        /// 重写水平布局计算
        /// </summary>
        public override void SetLayoutHorizontal()
        {
            // 核心逻辑在这里实现
            // 因为我们需要同时计算行和列来确定位置，所以大部分逻辑集中在这里
            // SetLayoutVertical 也会被调用，但我们可以在这里一次性计算好或者分两步
            
            // 为了避免重复计算，我们在这里进行主要的布局计算
            UpdateLayout();
        }

        /// <summary>
        /// 重写垂直布局计算
        /// </summary>
        public override void SetLayoutVertical()
        {
            // 同样调用 UpdateLayout，确保垂直方向也被正确设置
            UpdateLayout();
        }

        /// <summary>
        /// 执行布局逻辑
        /// </summary>
        [ContextMenu("立即刷新布局 (Update Layout)")]
        private void UpdateLayout()
        {
            // 1. 获取所有活跃的子物体
            List<RectTransform> rectChildren = new List<RectTransform>();
            for (int i = 0; i < rectTransform.childCount; i++)
            {
                var child = rectTransform.GetChild(i) as RectTransform;
                if (child != null && child.gameObject.activeInHierarchy)
                {
                    rectChildren.Add(child);
                }
            }

            int count = rectChildren.Count;
            if (count == 0) return;

            // 2. 获取布局参数
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            
            // 扣除 Padding 后的可用高度
            float availableHeight = height - padding.vertical;

            // 计算每一行需要的高度 (Cell + Spacing)
            // 注意：最后一行不需要 Spacing，但为了计算方便，我们通常假设每个单元格都带一个 Spacing
            // 或者用 (cellY + spacingY) * rows - spacingY <= availableHeight
            float cellHeight = cellSize.y;
            float spacingY = spacing.y;
            
            // 计算单列最大能容纳多少行
            // 公式: rows * cellHeight + (rows - 1) * spacingY <= availableHeight
            // rows * (cellHeight + spacingY) - spacingY <= availableHeight
            // rows * (cellHeight + spacingY) <= availableHeight + spacingY
            int maxRowsPerCol = Mathf.FloorToInt((availableHeight + spacingY) / (cellHeight + spacingY));
            
            // 至少能放1行，防止除以0等问题
            if (maxRowsPerCol < 1) maxRowsPerCol = 1;

            // 3. 计算需要多少列
            // 总数 / 单列最大行数，向上取整
            int colCount = Mathf.CeilToInt((float)count / maxRowsPerCol);
            if (colCount < 1) colCount = 1;

            // 4. 核心：平衡各列的行数
            // 我们希望将 count 个元素均匀分配到 colCount 列中
            // 比如 10 个元素，3 列。 10 / 3 = 3 余 1。
            // 那么分布应该是：4, 3, 3
            int baseRows = count / colCount;
            int remainder = count % colCount;

            // 5. 开始遍历并设置位置
            int currentChildIndex = 0;

            // 计算整个内容块所需的宽和高
            // 宽度：列数 * 宽 + (列数-1) * 间距
            float requiredWidth = colCount * cellSize.x + (colCount - 1) * spacing.x;
            
            // 高度：最大行数 * 高 + (最大行数-1) * 间距
            // 注意：因为各列行数不同，我们以最长的一列（maxRowsInThisLayout）作为内容高度基准，
            // 这样能保证整体布局在容器中对齐。
            // baseRows + (remainder > 0 ? 1 : 0) 即为最大行数
            int maxRowsInLayout = baseRows + (remainder > 0 ? 1 : 0);
            float requiredHeight = maxRowsInLayout * cellSize.y + (maxRowsInLayout - 1) * spacing.y;

            // 利用 LayoutGroup 提供的 GetStartOffset 方法，根据 ChildAlignment 计算起始偏移
            // 0 是水平轴，1 是垂直轴
            float startOffsetX = GetStartOffset(0, requiredWidth);
            float startOffsetY = GetStartOffset(1, requiredHeight);

            for (int c = 0; c < colCount; c++)
            {
                // 当前列应该有多少行
                // 如果当前列索引 < 余数，则该列多分配 1 个（分摊余数）
                int rowsInThisCol = baseRows + (c < remainder ? 1 : 0);

                for (int r = 0; r < rowsInThisCol; r++)
                {
                    if (currentChildIndex >= count) break;

                    RectTransform child = rectChildren[currentChildIndex];

                    // XPos = StartOffset + ColIndex * (CellX + SpacingX)
                    float xPos = startOffsetX + c * (cellSize.x + spacing.x);
                    
                    // YPos = StartOffset + RowIndex * (CellY + SpacingY)
                    float yOffset = startOffsetY + r * (cellHeight + spacingY);

                    // 设置 X (Axis 0)
                    SetChildAlongAxis(child, 0, xPos, cellSize.x);
                    
                    // 设置 Y (Axis 1)
                    SetChildAlongAxis(child, 1, yOffset, cellSize.y);

                    currentChildIndex++;
                }
            }
        }
        
        // 必须重写 CalculateLayoutInputHorizontal/Vertical 以确保 ContentSizeFitter 能正确工作
        // 如果不重写，ContentSizeFitter 可能无法获取正确的大小
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            
            // 计算所需的总宽度
            // 逻辑类似 UpdateLayout，但我们只需要知道 colCount
            
            int count = 0;
            for (int i = 0; i < rectTransform.childCount; i++)
            {
                if (rectTransform.GetChild(i).gameObject.activeInHierarchy) count++;
            }
            if (count == 0) return;

            float height = rectTransform.rect.height;
            float availableHeight = height - padding.vertical;
            float cellHeight = cellSize.y;
            float spacingY = spacing.y;
            int maxRowsPerCol = Mathf.FloorToInt((availableHeight + spacingY) / (cellHeight + spacingY));
            if (maxRowsPerCol < 1) maxRowsPerCol = 1;

            int colCount = Mathf.CeilToInt((float)count / maxRowsPerCol);
            
            float minWidth = padding.horizontal + colCount * cellSize.x + (colCount - 1) * spacing.x;
            
            SetLayoutInputForAxis(minWidth, minWidth, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            // 垂直方向通常由父物体决定（因为是 "竖向装不下再换列"），
            // 所以我们不需要像 ContentSizeFitter 那样去撑大高度，
            // 除非是 Height 也是 Flexible 的。
            // 但根据需求 "竖向装不下"，隐含高度是受限的。
            // 这里我们保留 base 的计算，或者简单设定 minHeight
            // 如果我们也想支持 "内容很少时收缩高度"，可以计算实际行数。
            // 但为了简单和符合 "Fill Vertical" 逻辑，我们假设高度是给定的。
            
            // 不过，为了兼容性，如果父物体是 ContentSizeFitter (Vertical Fit = Preferred)，
            // 我们需要给出一个 Preferred Height。
            // 在这种情况下，maxRowsPerCol 可能会变得无穷大（因为 height 无限），导致只排1列。
            // 这是一个悖论：如果不限高，就永远不会 "装不下"。
            // 所以用户使用此组件时，RectTransform 必须有固定高度，或者受限高度。
            
            base.CalculateLayoutInputVertical();
        }
    }
}
