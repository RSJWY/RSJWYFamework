using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NestedScrollRectUltimately : ScrollRect
{
    [Header("必须手动指定父 ScrollRect")]
    public ScrollRect parentScroll;

    private Vector2 startPointerPos;
    private bool routeToParent;

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        base.OnInitializePotentialDrag(eventData);

        startPointerPos = eventData.position;
        routeToParent = false;
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        // 不让 Unity 默认锁死拖拽对象
        // 我们自己决定交给谁
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (parentScroll == null)
        {
            base.OnDrag(eventData);
            return;
        }

        Vector2 delta = eventData.position - startPointerPos;

        // ====== 方向判定（核心）======
        if (!routeToParent)
        {
            bool isHorizontalDrag = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);

            if (horizontal && isHorizontalDrag)
            {
                // 内层处理
                base.OnDrag(eventData);
                return;
            }

            if (vertical && !isHorizontalDrag)
            {
                // 内层处理
                base.OnDrag(eventData);
                return;
            }

            // ====== 交给父级 ======
            routeToParent = true;

            parentScroll.OnInitializePotentialDrag(eventData);
            parentScroll.OnBeginDrag(eventData);
        }

        // ====== 交给父 ScrollRect ======
        if (routeToParent)
        {
            parentScroll.OnDrag(eventData);
        }
        else
        {
            base.OnDrag(eventData);
        }
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (parentScroll != null && routeToParent)
        {
            parentScroll.OnEndDrag(eventData);
        }
        else
        {
            base.OnEndDrag(eventData);
        }

        routeToParent = false;
    }
}