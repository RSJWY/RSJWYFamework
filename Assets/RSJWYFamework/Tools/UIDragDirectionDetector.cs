
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace RSJWYFamework.Runtime
{

public class UIDragDirectionWithEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // 定义事件类型
    [System.Serializable]
    public class DragDirectionEvent : UnityEvent { }

    // 四个方向的事件
    public DragDirectionEvent onDragUp;
    public DragDirectionEvent onDragDown;
    public DragDirectionEvent onDragLeft;
    public DragDirectionEvent onDragRight;

    // 记录开始触摸/点击的位置
    private Vector2 _startPos;
    
    // 最小拖动距离阈值，避免误操作
    public float minDragDistance = 5f;
    
    // 是否正在当前UI上按下
    private bool _isPressingOnThisUI = false;

    // 当指针（鼠标/触摸）按下在UI上时调用
    public void OnPointerDown(PointerEventData eventData)
    {
        // 记录按下位置
        _startPos = eventData.position;
        _isPressingOnThisUI = true;
    }

    // 当指针（鼠标/触摸）在UI上抬起时调用
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isPressingOnThisUI) return;
        
        // 获取结束位置
        Vector2 endPos = eventData.position;
        
        // 计算拖动距离
        float dragDistance = Vector2.Distance(_startPos, endPos);
        
        // 只有当拖动距离超过阈值时才判断方向
        if (dragDistance > minDragDistance)
        {
            DetermineDragDirection(_startPos, endPos);
        }
        
        _isPressingOnThisUI = false;
    }
    
    // 判断拖动方向
    private void DetermineDragDirection(Vector2 start, Vector2 end)
    {
        // 计算位置差值
        Vector2 direction = end - start;
        
        // 判断水平和垂直方向哪个更明显
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // 水平方向拖动
            if (direction.x > 0)
            {
                Debug.Log("向右拖动");
                onDragRight?.Invoke();
            }
            else
            {
                Debug.Log("向左拖动");
                onDragLeft?.Invoke();
            }
        }
        else
        {
            // 垂直方向拖动
            if (direction.y > 0)
            {
                Debug.Log("向上拖动");
                onDragUp?.Invoke();
            }
            else
            {
                Debug.Log("向下拖动");
                onDragDown?.Invoke();
            }
        }
    }
}

}