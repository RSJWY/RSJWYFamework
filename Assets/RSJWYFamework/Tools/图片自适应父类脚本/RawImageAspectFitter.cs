using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HSY.UI
{
    /// <summary>
    /// 挂载在父物体上，控制子物体 RawImage 撑满父物体框（保持比例，无畸变）
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class RawImageAspectFitter : UIBehaviour
    {
        public enum AspectMode
        {
            /// <summary>
            /// 覆盖模式：图片完全覆盖父物体，可能会有裁剪（撑满）
            /// </summary>
            EnvelopeParent,
            /// <summary>
            /// 适应模式：图片完全显示在父物体内，可能会有留白
            /// </summary>
            FitInParent
        }

        [Header("设置")]
        [Tooltip("需要控制的子物体 RawImage，为空则自动查找")]
        public RawImage targetRawImage;

        [Tooltip("适配模式")]
        public AspectMode aspectMode = AspectMode.EnvelopeParent;

        [Tooltip("是否在 Update 中实时刷新")]
        public bool updateEveryFrame = false;

        private RectTransform _parentRect;

        protected override void Awake()
        {
            base.Awake();
            Refresh();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh();
        }

        void Update()
        {
            if (updateEveryFrame || !Application.isPlaying)
            {
                Refresh();
            }
        }

        /// <summary>
        /// 当 RectTransform 尺寸变化时调用
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (IsActive())
            {
                Refresh();
            }
        }

        /// <summary>
        /// 设置图片并自动刷新布局
        /// </summary>
        /// <param name="texture">要显示的图片</param>
        public void SetTexture(Texture texture)
        {
            if (targetRawImage == null)
            {
                targetRawImage = GetComponentInChildren<RawImage>();
            }

            if (targetRawImage != null)
            {
                targetRawImage.texture = texture;
                Refresh();
            }
        }

        /// <summary>
        /// 对外公开的刷新方法
        /// </summary>
        [ContextMenu("立即刷新布局 (Refresh Layout)")]
        public void Refresh()
        {
            FitImage();
        }

        private void FitImage()
        {
            if (targetRawImage == null)
            {
                targetRawImage = GetComponentInChildren<RawImage>();
            }

            if (targetRawImage == null || targetRawImage.texture == null)
            {
                return;
            }

            if (_parentRect == null) _parentRect = GetComponent<RectTransform>();
            if (_parentRect == null) return;

            RectTransform childRect = targetRawImage.rectTransform;

            // 1. 获取尺寸
            float parentWidth = _parentRect.rect.width;
            float parentHeight = _parentRect.rect.height;
            float texWidth = targetRawImage.texture.width;
            float texHeight = targetRawImage.texture.height;

            if (texHeight <= 0 || parentHeight <= 0 || texWidth <= 0 || parentWidth <= 0) return;

            // 2. 计算宽高比
            float parentAspect = parentWidth / parentHeight;
            float texAspect = texWidth / texHeight;

            // 3. 计算缩放逻辑
            float finalWidth, finalHeight;

            bool fitWidth = false;

            switch (aspectMode)
            {
                case AspectMode.EnvelopeParent:
                    // 撑满：谁短谁撑满，另一边溢出
                    // 如果图片更宽 (texAspect > parentAspect)，则以高度为基准撑满 (fit height)，宽度溢出
                    // 如果图片更瘦 (texAspect < parentAspect)，则以宽度为基准撑满 (fit width)，高度溢出
                    if (texAspect > parentAspect)
                    {
                        fitWidth = false; // Fit Height
                    }
                    else
                    {
                        fitWidth = true; // Fit Width
                    }
                    break;

                case AspectMode.FitInParent:
                    // 适应：谁长谁撑满，另一边留白
                    // 如果图片更宽 (texAspect > parentAspect)，则以宽度为基准撑满 (fit width)，高度留白
                    // 如果图片更瘦 (texAspect < parentAspect)，则以高度为基准撑满 (fit height)，宽度留白
                    if (texAspect > parentAspect)
                    {
                        fitWidth = true; // Fit Width
                    }
                    else
                    {
                        fitWidth = false; // Fit Height
                    }
                    break;
            }

            if (fitWidth)
            {
                finalWidth = parentWidth;
                finalHeight = finalWidth / texAspect;
            }
            else
            {
                finalHeight = parentHeight;
                finalWidth = finalHeight * texAspect;
            }

            // 4. 应用尺寸和居中
            childRect.sizeDelta = new Vector2(finalWidth, finalHeight);
            childRect.anchorMin = new Vector2(0.5f, 0.5f);
            childRect.anchorMax = new Vector2(0.5f, 0.5f);
            childRect.pivot = new Vector2(0.5f, 0.5f);
            childRect.anchoredPosition = Vector2.zero;
        }
    }
}
