using UnityEngine;
using UnityEngine.UI;
using RenderHeads.Media.AVProVideo;

[RequireComponent(typeof(Graphic))]
public class AVProTransparentShaderHelper : MonoBehaviour
{
    [Header("Blending")]
    public UnityEngine.Rendering.BlendMode srcBlend = UnityEngine.Rendering.BlendMode.One;
    public UnityEngine.Rendering.BlendMode dstBlend = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

    [Header("Alpha Adjustment")]
    [Range(0, 1)]
    public float alphaSmoothMin = 0.0f;
    [Range(0, 1)]
    public float alphaSmoothMax = 1.0f;

    [Header("Edge Dilation (Gap Filling)")]
    [Tooltip("If enabled, edgeDilation will be driven by video progress.")]
    public bool autoAdjustEdgeDilation = false;
    public AnimationCurve dilationCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.5f, 0), new Keyframe(1, 1));

    [Range(0, 5)]
    public float edgeDilation = 0.0f;
    
    /// <summary>
    /// 靠近极限时的值 (用于曲线的峰值乘数)
    /// </summary>
    [Range(0, 5)]
    public float edgeDilationBoundaryValue = 1.0f;
    /// <summary>
    /// 中间值 (用于曲线的谷底基数)
    /// </summary>
    [Range(0, 5)]
    public float edgeDilationMedianValue = 0f;

    private Material _material;
    private Graphic _graphic;
    private MediaPlayer _mediaPlayer;

    private MaterialPropertyBlock _propBlock;

    private static readonly int PropSrcBlend = Shader.PropertyToID("_SrcBlend");
    private static readonly int PropDstBlend = Shader.PropertyToID("_DstBlend");
    private static readonly int PropAlphaSmoothMin = Shader.PropertyToID("_AlphaSmoothMin");
    private static readonly int PropAlphaSmoothMax = Shader.PropertyToID("_AlphaSmoothMax");
    private static readonly int PropEdgeDilation = Shader.PropertyToID("_EdgeDilation");

    void Start()
    {
        _graphic = GetComponent<Graphic>();
        
        // Try to find MediaPlayer on the same object or children (common for DisplayUGUI)
        if (_graphic is DisplayUGUI displayUGUI)
        {
            _mediaPlayer = displayUGUI.CurrentMediaPlayer;
        }
        else
        {
            _mediaPlayer = GetComponent<MediaPlayer>();
        }

        UpdateMaterial();
    }

    void Update()
    {
        // Continuously update in editor/runtime to allow real-time tuning
        if (Application.isPlaying)
        {
            if (autoAdjustEdgeDilation && _mediaPlayer != null && _mediaPlayer.Control != null && _mediaPlayer.Info != null)
            {
                float duration = (float)_mediaPlayer.Info.GetDuration();
                if (duration > 0)
                {
                    float progress = (float)(_mediaPlayer.Control.GetCurrentTime() / duration);
                    // Use curve to interpolate between Median and Boundary
                    // Assuming curve goes from 0 to 1. 
                    // value = Median + (Boundary - Median) * curveValue
                    float curveValue = dilationCurve.Evaluate(progress);
                    edgeDilation = Mathf.Lerp(edgeDilationMedianValue, edgeDilationBoundaryValue, curveValue);
                }
            }

            // Only update material properties if values changed
            UpdateMaterial();
        }
    }

    void OnValidate()
    {
        // Allow updating in editor mode if material is available
        UpdateMaterial();
    }

    public void UpdateMaterial()
    {
        if (_graphic == null) _graphic = GetComponent<Graphic>();
        if (_graphic == null) return;

        // Note: Graphic components do not support MaterialPropertyBlock directly like Renderers do.
        // We must access the material instance. Accessing .material creates a clone if it's not already one.
        Material mat = _graphic.material;

        if (mat != null)
        {
            // Optimization: SetFloat is fast, removed HasProperty checks for performance
            mat.SetFloat(PropSrcBlend, (float)srcBlend);
            mat.SetFloat(PropDstBlend, (float)dstBlend);
            mat.SetFloat(PropAlphaSmoothMin, alphaSmoothMin);
            mat.SetFloat(PropAlphaSmoothMax, alphaSmoothMax);
            mat.SetFloat(PropEdgeDilation, edgeDilation);
        }
    }
}
