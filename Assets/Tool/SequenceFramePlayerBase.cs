using UnityEngine;
using UnityEngine.UI;
namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 序列帧目标组件类型
    /// </summary>
    public enum SequenceTargetType
    {
        /// <summary>自动检测绑定组件类型</summary>
        Auto,
        /// <summary>用于 2D/渲染器的 SpriteRenderer</summary>
        SpriteRenderer,
        /// <summary>用于 UGUI 的 Image（Sprite）</summary>
        Image,
        /// <summary>用于 UGUI 的 RawImage（Texture）</summary>
        RawImage
    }

    /// <summary>
    /// 兼容 Unity 的高性能序列帧播放器。
    /// 支持：开场序列（仅播放一次）+ 循环序列（无限循环）；
    /// 兼容组件：SpriteRenderer / Image / RawImage；
    /// 采用时间累加器按帧率推进，避免协程分配与过多 GC。
    /// </summary>
    public class SequenceFramePlayerBase : MonoBehaviour
    {
        [Header("播放设置")]
        [Tooltip("目标组件类型，Auto 将自动检测绑定的渲染组件")]
        [SerializeField] private SequenceTargetType targetType = SequenceTargetType.Auto;

        [Tooltip("播放帧率（FPS）")]
        [SerializeField] private float frameRate = 24f;

        [Tooltip("启用后自动播放")]
        [SerializeField] private bool playOnAwake = true;

        [Tooltip("使用不受 Time.timeScale 影响的时间源")]
        [SerializeField] private bool useUnscaledTime = false;

        [Header("序列数据（Sprite）")]
        [Tooltip("开场序列（Sprite），播放一次后进入循环序列")]
        [SerializeField] private Sprite[] introSprites;
        [Tooltip("循环序列（Sprite），会一直循环播放")]
        [SerializeField] private Sprite[] loopSprites;

        [Header("序列数据（Texture）")]
        [Tooltip("开场序列（Texture），播放一次后进入循环序列")]
        [SerializeField] private Texture[] introTextures;
        [Tooltip("循环序列（Texture），会一直循环播放")]
        [SerializeField] private Texture[] loopTextures;

        // 解析后的目标类型与组件缓存，避免运行时反复 GetComponent
        private SequenceTargetType resolvedType;
        private SpriteRenderer spriteRenderer;
        private Image image;
        private RawImage rawImage;

        // 播放状态
        private bool playing;
        private bool paused;
        private bool inIntro;
        private int index;
        private float accumulator;

        private void Awake()
        {
            // 缓存目标渲染组件
            ResolveTarget();
            // 按需自动播放
            if (playOnAwake) Play();
        }

        private void Update()
        {
            if (!playing || paused) return;
            // 时间累加器，按帧率推进到下一帧，避免低帧率漏帧
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            accumulator += dt;
            float step = 1f / Mathf.Max(1e-6f, frameRate);
            while (accumulator >= step)
            {
                accumulator -= step;
                StepFrame();
            }
        }

        /// <summary>
        /// 开始播放：若有开场序列则先播放一次，随后进入循环序列
        /// </summary>
        public void Play()
        {
            playing = true;
            paused = false;
            accumulator = 0f;
            index = 0;
            inIntro = HasIntro();
            ApplyFrame(0);
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            playing = false;
            paused = false;
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            paused = true;
        }

        /// <summary>
        /// 恢复播放
        /// </summary>
        public void Resume()
        {
            paused = false;
        }

        /// <summary>
        /// 设置播放帧率（下限为 1 FPS）
        /// </summary>
        public void SetFrameRate(float fps)
        {
            frameRate = Mathf.Max(1f, fps);
        }

        /// <summary>
        /// 设置 Sprite 序列（开场 + 循环）
        /// </summary>
        public void SetSprites(Sprite[] intro, Sprite[] loop)
        {
            introSprites = intro;
            loopSprites = loop;
        }

        /// <summary>
        /// 设置 Texture 序列（开场 + 循环）
        /// </summary>
        public void SetTextures(Texture[] intro, Texture[] loop)
        {
            introTextures = intro;
            loopTextures = loop;
        }

        /// <summary>
        /// 帧推进逻辑：优先播放开场序列，结束后进入循环序列
        /// </summary>
        private void StepFrame()
        {
            // RawImage 使用 Texture 序列
            if (resolvedType == SequenceTargetType.RawImage)
            {
                if (inIntro)
                {
                    int len = introTextures != null ? introTextures.Length : 0;
                    if (len == 0)
                    {
                        inIntro = false;
                        index = 0;
                    }
                    else
                    {
                        index++;
                        if (index >= len)
                        {
                            // 开场播放完成，进入循环
                            inIntro = false;
                            index = 0;
                        }
                        else
                        {
                            ApplyFrame(index);
                            return;
                        }
                    }
                }
                int loopLen = loopTextures != null ? loopTextures.Length : 0;
                if (loopLen == 0)
                {
                    // 无循环资源，停止播放
                    playing = false;
                    return;
                }
                index = (index + 1) % loopLen;
                ApplyFrame(index);
                return;
            }

            // SpriteRenderer/Image 使用 Sprite 序列
            if (inIntro)
            {
                int len = introSprites != null ? introSprites.Length : 0;
                if (len == 0)
                {
                    inIntro = false;
                    index = 0;
                }
                else
                {
                    index++;
                    if (index >= len)
                    {
                        // 开场播放完成，进入循环
                        inIntro = false;
                        index = 0;
                    }
                    else
                    {
                        ApplyFrame(index);
                        return;
                    }
                }
            }
            int loopLenSprites = loopSprites != null ? loopSprites.Length : 0;
            if (loopLenSprites == 0)
            {
                // 无循环资源，停止播放
                playing = false;
                return;
            }
            index = (index + 1) % loopLenSprites;
            ApplyFrame(index);
        }

        /// <summary>
        /// 是否存在开场序列
        /// </summary>
        private bool HasIntro()
        {
            if (resolvedType == SequenceTargetType.RawImage)
                return introTextures != null && introTextures.Length > 0;
            return introSprites != null && introSprites.Length > 0;
        }

        /// <summary>
        /// 将第 i 帧应用到目标组件
        /// </summary>
        private void ApplyFrame(int i)
        {
            if (resolvedType == SequenceTargetType.RawImage)
            {
                Texture t = inIntro ? GetTexture(introTextures, i) : GetTexture(loopTextures, i);
                if (rawImage != null) rawImage.texture = t;
                return;
            }
            Sprite s = inIntro ? GetSprite(introSprites, i) : GetSprite(loopSprites, i);
            if (spriteRenderer != null) spriteRenderer.sprite = s;
            if (image != null) image.sprite = s;
        }

        /// <summary>
        /// 安全获取数组中的第 i 个 Sprite
        /// </summary>
        private static Sprite GetSprite(Sprite[] arr, int i)
        {
            int len = arr != null ? arr.Length : 0;
            if (len == 0) return null;
            if (i < 0) i = 0;
            if (i >= len) i = len - 1;
            return arr[i];
        }

        /// <summary>
        /// 安全获取数组中的第 i 个 Texture
        /// </summary>
        private static Texture GetTexture(Texture[] arr, int i)
        {
            int len = arr != null ? arr.Length : 0;
            if (len == 0) return null;
            if (i < 0) i = 0;
            if (i >= len) i = len - 1;
            return arr[i];
        }

        /// <summary>
        /// 自动检测并缓存目标渲染组件，减少运行时开销
        /// </summary>
        private void ResolveTarget()
        {
            resolvedType = targetType;
            if (resolvedType == SequenceTargetType.Auto)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                image = GetComponent<Image>();
                rawImage = GetComponent<RawImage>();
                if (spriteRenderer != null) resolvedType = SequenceTargetType.SpriteRenderer;
                else if (image != null) resolvedType = SequenceTargetType.Image;
                else if (rawImage != null) resolvedType = SequenceTargetType.RawImage;
                else resolvedType = SequenceTargetType.SpriteRenderer;
            }
            else
            {
                if (resolvedType == SequenceTargetType.SpriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
                if (resolvedType == SequenceTargetType.Image) image = GetComponent<Image>();
                if (resolvedType == SequenceTargetType.RawImage) rawImage = GetComponent<RawImage>();
            }
        }
    }
}
