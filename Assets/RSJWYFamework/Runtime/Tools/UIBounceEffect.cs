using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIBounceEffect : MonoBehaviour
{
    [Header("跳动参数")]
    public float bounceHeight = 50f;  // 初始跳动高度
    public float horizontalSway = 10f;  // 初始水平偏移
    public float bounceSpeed = 2f;  // 初始跳动速度
    public bool autoStart = true;

    [Header("衰减设置")]
    public bool useDecay = true;
    public AnimationCurve decayCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.3f);
    public float decayTime = 2f;
    [Range(0f, 1f)] public float steadyAmplitude = 0.3f;

    [Header("启动过渡")]
    [Range(0.1f, 1f)] public float startSmoothness = 0.2f;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private float elapsedTime;
    private bool isBouncing;
    private float startTransitionProgress;
    private float decayCurveEndValue;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (autoStart)
        {
            StartBounce();
        }
    }

    private void Start()
    {
        // 计算随机值范围
        RandomizeAnimationValues();
    }

    // 随机化每次动画的参数
    private void RandomizeAnimationValues()
    {
        // 随机化跳动速度和幅度
        bounceSpeed = Random.Range(1.5f, 3f);  // 可调整随机范围
        horizontalSway = Random.Range(8f, 12f);  // 可调整随机范围
        bounceHeight = Random.Range(40f, 60f);  // 可调整随机范围
        steadyAmplitude = Random.Range(0.2f, 0.4f);  // 可调整随机范围
    }

    public void StartBounce()
    {
        // 计算当前 Y 偏移，用于推回相位
        Vector2 currentOffset = rectTransform.anchoredPosition - originalAnchoredPosition;
        originalAnchoredPosition = rectTransform.anchoredPosition;
        decayCurveEndValue = decayCurve.Evaluate(1f);
        float phaseGuess = 0f;
        if (Mathf.Abs(bounceHeight) > 0.01f)
        {
            float ratio = Mathf.Clamp(currentOffset.y / bounceHeight, -1f, 1f);
            phaseGuess = Mathf.Asin(ratio); // [-π/2, π/2]
        }

        // 用推测相位恢复 elapsedTime，使跳动继续顺滑
        elapsedTime = phaseGuess / bounceSpeed;
        if (float.IsNaN(elapsedTime)) elapsedTime = 0f;

        startTransitionProgress = 0f;
        isBouncing = true;
    }

    public void StopBounce()
    {
        isBouncing = false;
        //rectTransform.anchoredPosition = originalAnchoredPosition;
    }

    private void Update()
    {
        if (!isBouncing) return;

        elapsedTime += Time.deltaTime;
        startTransitionProgress = Mathf.Min(1f, startTransitionProgress + Time.deltaTime / startSmoothness);
        float startFactor = Mathf.SmoothStep(0f, 1f, startTransitionProgress);

        // 随机生成的偏移量计算
        float verticalOffset = Mathf.Sin(elapsedTime * bounceSpeed) * bounceHeight;
        float horizontalOffset = Mathf.Cos(elapsedTime * bounceSpeed * 1.2f) * horizontalSway;

        // 衰减控制
        float decayFactor = 1f;
        if (useDecay)
        {
            if (elapsedTime < decayTime)
            {
                decayFactor = decayCurve.Evaluate(elapsedTime / decayTime);
            }
            else
            {
                float postDecayProgress = Mathf.Min(1f, (elapsedTime - decayTime) / 0.5f);
                decayFactor = Mathf.Lerp(decayCurveEndValue, steadyAmplitude, postDecayProgress);
            }
        }

        // 更新目标位置
        rectTransform.anchoredPosition = new Vector2(
            originalAnchoredPosition.x + horizontalOffset * decayFactor * startFactor,
            originalAnchoredPosition.y + verticalOffset * decayFactor * startFactor
        );
    }
}
