using UnityEngine;
using DG.Tweening; // 引入 DOTween
using System.Collections.Generic;

public class BoundedUIFly : MonoBehaviour
{
    public RectTransform flyingItem; // 飞行物体
    public RectTransform targetItem; // 目标物体
    public Canvas parentCanvas;      // 飞行物体所在的Canvas
    public float flyDuration = 1.0f; // 持续时间
    
    [Header("安全边距（像素）")]
    public float topPadding = 50f;   // 距离屏幕顶部的最小距离

    private RectTransform canvasRect;

    private void Start()
    {
        // 获取Canvas的RectTransform以确定屏幕边界
        canvasRect = parentCanvas.GetComponent<RectTransform>();
    }

    [ContextMenu("开始飞行测试")] // 在编辑器组件右键菜单中测试
    public void StartFlyAnimation()
    {
        if (canvasRect == null) canvasRect = parentCanvas.GetComponent<RectTransform>();
        
        flyingItem.gameObject.SetActive(true);

        // 1. 获取起点和终点的世界坐标
        Vector3 startPos = flyingItem.position;
        Vector3 endPos = targetItem.position;

        // 2. 计算安全的控制点
        Vector3[] path = CalculateSafeBezierPath(startPos, endPos);

        // 3. 使用 DOTween 的 DOPath 沿计算出的路径飞行
        // PathType.CubicBezier 表示使用三次贝塞尔曲线
        flyingItem.DOPath(path, flyDuration, PathType.CubicBezier)
                  .SetEase(Ease.OutQuad)
                  .OnComplete(() =>
                  {
                      Debug.Log("安全飞抵终点！");
                      flyingItem.gameObject.SetActive(false);
                  });
    }

    /// <summary>
    ```C#
    /// 计算一条不超出屏幕顶部的贝塞尔路径点数组
    /// 数组格式：[终点, 控制点1, 控制点2] (起点是当前位置，不需要包含)
    /// </summary>
    private Vector3[] CalculateSafeBezierPath(Vector3 startPos, Vector3 endPos)
    {
        // --- 第一步：计算屏幕顶部的世界坐标 Y 值 ---
        // 获取Canvas在Local坐标系下的顶部Y值 (通常是 height/2)
        float canvasTopLocalY = canvasRect.rect.yMax;
        
        // 将Local坐标转换为世界坐标，从而得到屏幕顶部的世界Y坐标
        Vector3 canvasTopWorldPos = canvasRect.TransformPoint(new Vector3(0, canvasTopLocalY, 0));
        float screenTopWorldY = canvasTopWorldPos.y;

        // 应用安全边距
        float safeTopWorldY = screenTopWorldY - topPadding;


        // --- 第二步：确定控制点的初始 Y 值 ---
        // 初始设想：最高点在起点和终点中较高者的基础上，再往上飞一段距离（例如100像素）
        float baseHeight = Mathf.Max(startPos.y, endPos.y);
        float desiredControlY = baseHeight + 100f; 


        // --- 第三步：核心！将控制点限制在安全范围内 ---
        // 如果期望的高度超过了安全高度，就强制压低到安全高度
        float safeControlY = Mathf.Min(desiredControlY, safeTopWorldY);
        
        // 容错：如果起点/终点本身就在安全线以上（比如由于Padding设置过大），则不向上飞，改走平直线
        safeControlY = Mathf.Max(safeControlY, baseHeight);


        // --- 第四步：构建贝塞尔控制点 ---
        // 控制点1：在起点和终点水平方向的 1/4 处
        Vector3 controlPoint1 = Vector3.Lerp(startPos, endPos, 0.25f);
        controlPoint1.y = safeControlY; // 设置计算出的安全高度

        // 控制点2：在起点和终点水平方向的 3/4 处
        Vector3 controlPoint2 = Vector3.Lerp(startPos, endPos, 0.75f);
        controlPoint2.y = safeControlY; // 设置同一安全高度

        // --- 第五步：返回路径数组 ---
        // DOPath 使用 CubicBezier 时，需要传入：[终点, 控制点1, 控制点2]
        return new Vector3[] { endPos, controlPoint1, controlPoint2 };
    }
}
/*
要实现保证不超出屏幕外的曲线飞行，最核心的思路是：不能设置固定的跳跃高度，而要根据剩余的屏幕空间动态计算一个安全的跳跃高度。
关键点解析：
Canvas 坐标转换 (TransformPoint)：
这是最关键的一步。UI是在Canvas下通过RectTransform管理的。为了知道屏幕顶部的确切“世界坐标”位置，我们需要获取 canvasRect.rect.yMax（Canvas顶部的本地Y坐标），然后使用 TransformPoint 将其转换为世界坐标。这样我们才能直接与UI元素的 .position（也是世界坐标）进行比较和计算。

Mathf.Min 限制高度：
核心逻辑：Mathf.Min(desiredControlY, safeTopWorldY)。我们取“期望的飞行高度”和“屏幕安全最高高度”中的较小值。这保证了无论起点在哪里，曲线的控制点绝对不会超过你设定的安全线。

PathType.CubicBezier：
三次贝塞尔曲线提供了一个非常平滑的“中间拱起”效果。我们通过动态设置两个控制点（controlPoint1和controlPoint2）的Y轴为计算出的 safeControlY，来精确“压低”曲线的最高点。

设置步骤：
将上述脚本添加到一个 GameObject 上。

在 Inspector 中拖拽赋值：

Flying Item: 你的金币/图标。

Target Item: 你的背包/头像。

Parent Canvas: 它们共同的 Canvas。

调整 topPadding（顶部安全边距），例如设为50像素。

运行游戏，运行 StartFlyAnimation()。无论你如何移动起点和终点的位置，飞行物体的最高点都会被“压”在屏幕顶部下方。
*/