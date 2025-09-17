
using UnityEngine;
namespace RSJWYFamework.Runtime
{

    [RequireComponent(typeof(CanvasGroup))]
    public class FadePopup : MonoBehaviour
    {
        [Header("动画设置")] [Tooltip("淡入动画持续时间(秒)")] [SerializeField]
        private float fadeInDuration = 0.3f;

        [Tooltip("淡出动画持续时间(秒)")] [SerializeField]
        private float fadeOutDuration = 0.3f;

        [Tooltip("提示显示时间(秒)")] [SerializeField]
        private float displayDuration = 2f;

        public AudioSource audioSource;

        private CanvasGroup canvasGroup;
        private Tween currentTween;

        public bool isShow { get; private set; }

        private void Awake()
        {
            // 获取CanvasGroup组件
            canvasGroup = GetComponent<CanvasGroup>();

            // 初始化状态
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // 默认隐藏
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // 确保销毁时停止所有动画
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }
        }

        /// <summary>
        /// 显示提示框
        /// </summary>
        /// <param name="message">要显示的消息</param>
        public void Show()
        {

            audioSource.time = 0;
            audioSource.Play();
            // 如果有正在播放的动画，先停止
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }

            // 激活游戏对象
            gameObject.SetActive(true);

            // 重置透明度
            canvasGroup.alpha = 0;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            isShow = true;
            // 执行淡入动画
            currentTween = canvasGroup.DOFade(1, fadeInDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // 淡入完成后，延迟指定时间后执行淡出
                    currentTween = DOVirtual.DelayedCall(displayDuration, () => { Hide(); });
                });
        }

        /// <summary>
        /// 隐藏提示框
        /// </summary>
        public void Hide()
        {
            audioSource.Stop();
            // 如果有正在播放的动画，先停止
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }

            isShow = false;
            // 执行淡出动画
            currentTween = canvasGroup.DOFade(0, fadeOutDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                    gameObject.SetActive(false);
                });
        }
    }

}