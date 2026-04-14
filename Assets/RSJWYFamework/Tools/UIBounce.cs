
using UnityEngine;
using UnityEngine.UI;
namespace RSJWYFamework.Runtime
{

    [RequireComponent(typeof(RectTransform))]
    public class UIBounce : MonoBehaviour
    {
        [Header("跳动设置")] [Tooltip("跳动的高度")] public float bounceHeight = 50f;

        [Tooltip("跳动的速度")] public float bounceSpeed = 2f;

        [Tooltip("是否在启用时自动开始跳动")] public bool autoStart = true;

        private RectTransform rectTransform;
        private Vector2 originalPosition;
        private bool isBouncing = false;

        private void Awake()
        {
            // 获取UI元素的RectTransform组件
            rectTransform = GetComponent<RectTransform>();
            // 记录初始位置
            originalPosition = rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            // 如果设置了自动开始，则在启用时开始跳动
            if (autoStart)
            {
                StartBounce();
            }
        }

        private void Update()
        {
            if (isBouncing)
            {
                // 使用正弦函数计算Y轴位置，实现平滑的上下运动
                float newY = originalPosition.y + Mathf.Sin(Time.time * bounceSpeed) * bounceHeight / 2;
                rectTransform.anchoredPosition = new Vector2(originalPosition.x, newY);
            }
        }

        /// <summary>
        /// 开始跳动动画
        /// </summary>
        public void StartBounce()
        {
            isBouncing = true;
        }

        /// <summary>
        /// 停止跳动动画并回到初始位置
        /// </summary>
        public void StopBounce()
        {
            isBouncing = false;
            rectTransform.anchoredPosition = originalPosition;
        }

        /// <summary>
        /// 切换跳动状态（开始/停止）
        /// </summary>
        public void ToggleBounce()
        {
            if (isBouncing)
            {
                StopBounce();
            }
            else
            {
                StartBounce();
            }
        }
    }
}