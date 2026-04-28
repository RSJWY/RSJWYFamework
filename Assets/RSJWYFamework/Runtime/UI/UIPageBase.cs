using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RSJWYFamework.Runtime
{
    public class UIEventListener : MonoBehaviour,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        // ================= 事件 =================

        public event Action onClick;
        public event Action onDoubleClick;
        public event Action onLongPress;

        public event Action<bool> onPress;
        public event Action<bool> onHover;

        public event Action onBeginDrag;
        public event Action<Vector2> onDrag;
        public event Action onEndDrag;

        public event Action<bool> onToggleChanged;
        public event Action<float> onSliderChanged;
        public event Action<float> onScrollbarChanged;
        public event Action<int> onDropdownChanged;
        public event Action<string> onInputChanged;
        public event Action<string> onInputEndEdit;

        // ================= 配置 =================

        [Header("拖拽是否传递给父级（ScrollRect等）")]
        public bool passDragToParent = true;

        [Header("手势配置")]
        public float longPressTime = 0.5f;
        public float doubleClickTime = 0.3f;
        public float dragThreshold = 10f;

        // ================= 状态 =================

        private bool isPointerDown;
        private bool isDragging;
        private bool longPressTriggered;

        private float pressTime;
        private float lastClickTime;

        private Vector2 pressPos;

        // ================= 缓存组件 =================

        private Toggle toggle;
        private Slider slider;
        private Scrollbar scrollbar;
        private Dropdown dropdown;
        private InputField input;

        // ================= 生命周期 =================

        private void Awake()
        {
            TryGetComponent(out toggle);
            TryGetComponent(out slider);
            TryGetComponent(out scrollbar);
            TryGetComponent(out dropdown);
            TryGetComponent(out input);

            // ✅ 官方事件绑定（稳定）
            if (toggle != null)
                toggle.onValueChanged.AddListener(v => onToggleChanged?.Invoke(v));

            if (slider != null)
                slider.onValueChanged.AddListener(v => onSliderChanged?.Invoke(v));

            if (scrollbar != null)
                scrollbar.onValueChanged.AddListener(v => onScrollbarChanged?.Invoke(v));

            if (dropdown != null)
                dropdown.onValueChanged.AddListener(v => onDropdownChanged?.Invoke(v));

            if (input != null)
            {
                input.onValueChanged.AddListener(v => onInputChanged?.Invoke(v));
                input.onEndEdit.AddListener(v => onInputEndEdit?.Invoke(v));
            }
        }

        private void Update()
        {
            // 长按检测
            if (isPointerDown && !isDragging && !longPressTriggered)
            {
                if (Time.unscaledTime - pressTime >= longPressTime)
                {
                    longPressTriggered = true;
                    onLongPress?.Invoke();
                }
            }
        }

        // ================= Pointer =================

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            isDragging = false;
            longPressTriggered = false;

            pressTime = Time.unscaledTime;
            pressPos = eventData.position;

            onPress?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onPress?.Invoke(false);
            isPointerDown = false;

            // ❌ 拖拽 or 长按 → 不触发点击
            if (isDragging || longPressTriggered)
                return;

            float time = Time.unscaledTime;

            // 双击判断
            if (time - lastClickTime <= doubleClickTime)
            {
                onDoubleClick?.Invoke();
                lastClickTime = 0;
            }
            else
            {
                lastClickTime = time;
                onClick?.Invoke();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // ❗ 已在 PointerUp 里处理，这里留空避免重复
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            onHover?.Invoke(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onHover?.Invoke(false);
        }

        // ================= Drag =================

        public void OnBeginDrag(PointerEventData eventData)
        {
            onBeginDrag?.Invoke();

            if (passDragToParent)
                PassToParent<IBeginDragHandler>(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 判断拖拽阈值
            if (!isDragging)
            {
                float dis = Vector2.Distance(pressPos, eventData.position);
                if (dis > dragThreshold)
                {
                    isDragging = true;
                }
            }

            onDrag?.Invoke(eventData.delta);

            if (passDragToParent)
                PassToParent<IDragHandler>(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            onEndDrag?.Invoke();

            if (passDragToParent)
                PassToParent<IEndDragHandler>(eventData);
        }

        // ================= 父级透传 =================

        private void PassToParent<T>(PointerEventData data) where T : IEventSystemHandler
        {
            Transform parent = transform.parent;

            while (parent != null)
            {
                ExecuteEvents.Execute(parent.gameObject, data, ExecuteEvents.GetEventHandler<T>());
                parent = parent.parent;
            }
        }

        // ================= 工具方法 =================

        public static UIEventListener Get(GameObject go)
        {
            if (!go.TryGetComponent(out UIEventListener listener))
            {
                listener = go.AddComponent<UIEventListener>();
            }
            return listener;
        }
    }
}