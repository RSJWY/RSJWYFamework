using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace RSJWYFamework.Tools.UI触摸滑动旋转
{
    public class SwipeInputHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("滑动开关")]
        public bool allowHorizontalRotation = true;
        public bool allowVerticalRotation = true; 

        [Header("灵敏度设置 (双轴独立)")]
        public float speedHorizontal = 0.5f; // 水平滑动灵敏度
        public float speedVertical = 0.3f;   // 垂直滑动灵敏度 (通常比水平低)

        [Header("翻转设置")]
        public bool invertHorizontal = false;
        public bool invertVertical = false;

        [Header("回调事件")]
        public UnityEvent<Vector2> onRotateInput;

        private int currentPointerId = int.MinValue;

        /// <summary>
        /// 供配置系统动态更新参数
        /// </summary>
        public void UpdateConfiguration(float speedH, float speedV, bool allowH, bool allowV, bool invH, bool invV)
        {
            speedHorizontal = speedH;
            speedVertical = speedV;
            allowHorizontalRotation = allowH;
            allowVerticalRotation = allowV;
            invertHorizontal = invH;
            invertVertical = invV;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (currentPointerId == int.MinValue)
            {
                currentPointerId = eventData.pointerId;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == currentPointerId)
            {
                Vector2 delta = eventData.delta;
                Vector2 rotationDelta = Vector2.zero;

                // 水平滑动计算
                if (allowHorizontalRotation && Mathf.Abs(delta.x) > 0.1f)
                {
                    float directionH = invertHorizontal ? 1f : -1f;
                    // 使用 speedHorizontal
                    rotationDelta.x = delta.x * speedHorizontal * directionH; 
                }

                // 垂直滑动计算
                if (allowVerticalRotation && Mathf.Abs(delta.y) > 0.1f)
                {
                    float directionV = invertVertical ? -1f : 1f;
                    // 使用 speedVertical
                    rotationDelta.y = delta.y * speedVertical * directionV;
                }

                if (rotationDelta != Vector2.zero)
                {
                    onRotateInput?.Invoke(rotationDelta);
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == currentPointerId)
            {
                currentPointerId = int.MinValue;
            }
        }
    }
}