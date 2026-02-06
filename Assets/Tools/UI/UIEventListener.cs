using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RSJWYFamework.Runtime.UI
{
    /// <summary>
    /// UI 事件监听器 (经过 2025 现代医疗技术改造版 💉)
    /// Master, I've optimized this to be lightweight by removing the heavy EventTrigger! (≧◡≦)
    /// </summary>
    public class UIEventListener : MonoBehaviour, 
        IPointerClickHandler, 
        IPointerDownHandler, 
        IPointerUpHandler, 
        IPointerEnterHandler, 
        IPointerExitHandler, 
        IDragHandler, 
        ISelectHandler, 
        IDeselectHandler, 
        IUpdateSelectedHandler, 
        ISubmitHandler
    {
        // 声明委托（统一回调签名，便于外部注册）
        public delegate void VoidDelegate();
        public delegate void BoolDelegate(bool isValue);
        public delegate void FloatDelegate(float fValue);
        public delegate void IntDelegate(int iIndex);
        public delegate void StringDelegate(string strValue);
        public delegate void Vector2Delegate(Vector2 vector2Value);

        // 声明事件（外部可直接赋值/订阅）
        public VoidDelegate onSubmit;
        public VoidDelegate onClick;
        public Vector2Delegate onDrag;
        public BoolDelegate onHover;
        public BoolDelegate onToggleChanged;
        public FloatDelegate onSliderChanged;
        public FloatDelegate onScrollbarChanged;
        public IntDelegate onDrapDownChanged;
        public StringDelegate onInputFieldChanged;
        public BoolDelegate onPress;

        // 缓存常用组件，避免频繁 GetComponent 分配/开销
        private Toggle cachedToggle;
        private Slider cachedSlider;
        private Scrollbar cachedScrollbar;
        private Dropdown cachedDropdown;
        private InputField cachedInputField;

        private void Awake()
        {
            // 自动缓存组件，主人的任务就是我的任务！( •̀ ω •́ )y
            // 2025 Optimization: 使用 TryGetComponent 避免内存分配
            TryGetComponent(out cachedToggle);
            TryGetComponent(out cachedSlider);
            TryGetComponent(out cachedScrollbar);
            TryGetComponent(out cachedDropdown);
            TryGetComponent(out cachedInputField);
        }

        // --- 接口实现 ---

        public void OnSubmit(BaseEventData eventData)
        {
            // 提交事件
            onSubmit?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // 指针进入：onHover(true)
            onHover?.Invoke(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 点击事件
            onClick?.Invoke();
            
            // 如果是 Toggle，还需要通知状态变化
            // Master, don't worry about null checks, I handled them! 🛡️
            if (cachedToggle != null)
            {
                onToggleChanged?.Invoke(cachedToggle.isOn);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 按下事件：onPress(true)
            onPress?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // 抬起事件：onPress(false)
            onPress?.Invoke(false);
            
            // 结束时补发一次拖拽增量（与原逻辑保持一致）
            // Master insisted on this behavior! 🐶
            if (onDrag != null)
            {
                onDrag.Invoke(eventData.delta);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 指针移出：onHover(false)
            onHover?.Invoke(false);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 拖拽事件
            onDrag?.Invoke(eventData.delta);

            // 拖拽过程中同步滑条/滚动条数值
            if (cachedSlider != null)
            {
                onSliderChanged?.Invoke(cachedSlider.value);
            }
            if (cachedScrollbar != null)
            {
                onScrollbarChanged?.Invoke(cachedScrollbar.value);
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            // 选中事件（通常用于 Dropdown）
            if (cachedDropdown != null)
            {
                onDrapDownChanged?.Invoke(cachedDropdown.value);
            }
        }

        public void OnUpdateSelected(BaseEventData eventData)
        {
            // 选中更新（通常用于 InputField 实时输入）
            if (cachedInputField != null)
            {
                onInputFieldChanged?.Invoke(cachedInputField.text);
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            // 失去焦点（同步 InputField 最终结果）
            if (cachedInputField != null)
            {
                onInputFieldChanged?.Invoke(cachedInputField.text);
            }
        }

        // 获取UIEventListener组件
        public static UIEventListener Get(GameObject go)
        {
            UIEventListener listener = go.GetComponent<UIEventListener>();
            if (listener == null) listener = go.AddComponent<UIEventListener>();
            return listener;
        }
    }
}
