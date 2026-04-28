using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NestedScrollRect : ScrollRect
{
    [Header("手动指定父 ScrollRect（必须）")]
    public ScrollRect parentScroll;

    private bool routeToParent = false;
    private Vector2 startDragPos;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (parentScroll == this)
        {
            Debug.LogError("parentScroll 指向自己，会导致死循环！");
            return;
        }

        startDragPos = eventData.position;
        routeToParent = false;
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (parentScroll == null)
        {
            base.OnDrag(eventData);
            return;
        }

        Vector2 delta = eventData.position - startDragPos;

        // 方向判断
        if (!routeToParent)
        {
            if (horizontal && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                base.OnDrag(eventData);
            }
            else
            {
                routeToParent = true;

                // ⚠️ 防止递归
                if (parentScroll != this)
                    parentScroll.OnBeginDrag(eventData);
            }
        }

        if (routeToParent)
        {
            if (parentScroll != this)
                parentScroll.OnDrag(eventData);
        }
        else
        {
            base.OnDrag(eventData);
        }
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (routeToParent)
        {
            if (parentScroll != this)
                parentScroll.OnEndDrag(eventData);
        }
        else
        {
            base.OnEndDrag(eventData);
        }

        routeToParent = false;
    }
}