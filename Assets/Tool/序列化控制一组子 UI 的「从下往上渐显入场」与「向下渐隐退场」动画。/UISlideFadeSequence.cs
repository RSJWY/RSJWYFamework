using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 序列化控制一组子 UI 的「从下往上渐显入场」与「向下渐隐退场」动画。
/// - 支持 RawImage、Image、CanvasGroup 的透明度控制，自动自适配；若都不存在会自动添加 CanvasGroup
/// - 使用 List 承载每一条子项，按设定间隔依次出现/退出
/// - 入场：位置从下方滑入 + 透明度从 0→1；退场：位置向下滑出 + 透明度从 1→0
/// - 提供同步接口与异步接口（返回 UniTask，方便流程串联）
/// 依赖：DOTween、UniTask
/// </summary>
public class UISlideFadeSequence : MonoBehaviour
{
    /// <summary>需要参与动画的子项列表（每条为其 RectTransform）</summary>
    public List<RectTransform> Items = new List<RectTransform>();
    /// <summary>相邻子项播放的时间间隔（秒）</summary>
    public float Interval = 0.08f;
    /// <summary>上下滑动的距离（像素，正值表示往下/右偏移）</summary>
    public float MoveDistance = 80f;

    /// <summary>
    /// 动画移动方向
    /// </summary>
    public enum SlideDirection
    {
        Vertical,   // 垂直方向（默认）：入场从下往上
        Horizontal  // 水平方向：入场从右往左（MoveDistance > 0 时）
    }

    /// <summary>移动方向配置</summary>
    public SlideDirection Direction = SlideDirection.Vertical;

    /// <summary>位移动画时长（秒）</summary>
    public float MoveDuration = 0.4f;
    /// <summary>淡入淡出动画时长（秒）</summary>
    public float FadeDuration = 0.3f;
    /// <summary>位移动画缓动</summary>
    public Ease MoveEase = Ease.OutCubic;
    /// <summary>透明度动画缓动</summary>
    public Ease FadeEase = Ease.Linear;
    /// <summary>是否使用不受 Time.timeScale 影响的时间</summary>
    public bool UseUnscaledTime = true;

    /// <summary>
    /// 可选：目标位置容器。若赋值，PlayEnter/PlayExit 时可选择从该容器子物体同步目标位置（基准位置）。
    /// </summary>
    public RectTransform TargetContainer;

    /// <summary>
    /// 播放入场前是否自动从 TargetContainer 同步基准位置
    /// </summary>
    public bool AutoSyncTargetsFromContainer = false;

    readonly List<Tween> _runningTweens = new List<Tween>();
    readonly List<Vector2> _baselinePositions = new List<Vector2>();

    // ... FadeHandle struct ...
    struct FadeHandle
    {
        public CanvasGroup cg;
        public Graphic g;
        /// <summary>直接设置透明度数值</summary>
        public void SetAlpha(float a)
        {
            if (cg != null) cg.alpha = a;
            else if (g != null)
            {
                var c = g.color;
                c.a = a;
                g.color = c;
            }
        }
        /// <summary>以 Tween 方式改变透明度</summary>
        /// <param name="target">目标透明度</param>
        /// <param name="duration">时长</param>
        /// <param name="unscaled">是否使用不受缩放影响的时间</param>
        /// <param name="ease">缓动函数</param>
        public Tween DOFade(float target, float duration, bool unscaled, Ease ease)
        {
            Tween t;
            if (cg != null) t = cg.DOFade(target, duration);
            else t = g != null ? g.DOFade(target, duration) : null;
            if (t != null)
            {
                t.SetEase(ease);
                t.SetUpdate(unscaled);
            }
            return t;
        }
    }

    /// <summary>
    /// 获取（或构建）可用于该 RectTransform 的透明度控制句柄。
    /// 优先 CanvasGroup → 其次 Graphic（Image/RawImage）→ 若都无则添加 CanvasGroup。
    /// </summary>
    FadeHandle GetFadeHandle(RectTransform rt)
    {
        var fh = new FadeHandle();
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            var g = rt.GetComponent<Graphic>();
            if (g == null)
            {
                var raw = rt.GetComponent<RawImage>();
                var img = rt.GetComponent<Image>();
                if (raw != null) g = raw;
                else if (img != null) g = img;
            }
            if (g == null)
            {
                cg = rt.gameObject.AddComponent<CanvasGroup>();
            }
            else
            {
                fh.g = g;
            }
        }
        if (cg != null) fh.cg = cg;
        return fh;
    }

    /// <summary>
    /// 终止并清理当前正在执行的所有 Tween，避免与新动画冲突。
    /// </summary>
    void KillRunning()
    {
        foreach (var t in _runningTweens)
        {
            if (t != null && t.IsActive()) t.Kill();
        }
        _runningTweens.Clear();
    }

    void EnsureBaselineCapacity()
    {
        if (_baselinePositions.Count != Items.Count)
        {
            _baselinePositions.Clear();
            for (int i = 0; i < Items.Count; i++)
            {
                var rt = Items[i];
                _baselinePositions.Add(rt != null ? rt.anchoredPosition : Vector2.zero);
            }
        }
    }

    [ContextMenu("测试/刷新基准位置")]
    public void RefreshBaselinesFromItems()
    {
        _baselinePositions.Clear();
        for (int i = 0; i < Items.Count; i++)
        {
            var rt = Items[i];
            _baselinePositions.Add(rt != null ? rt.anchoredPosition : Vector2.zero);
        }
    }

    /// <summary>
    /// 更新指定索引项的基准位置（目标位置）
    /// </summary>
    /// <param name="index">索引</param>
    /// <param name="position">新的 AnchoredPosition</param>
    public void UpdateTargetPosition(int index, Vector2 position)
    {
        EnsureBaselineCapacity();
        if (index >= 0 && index < _baselinePositions.Count)
        {
            _baselinePositions[index] = position;
        }
    }

    /// <summary>
    /// 从 TargetContainer（或其他容器）同步目标位置。
    /// 会取容器子物体的 anchoredPosition 依次赋值给 Items 的基准位置。
    /// </summary>
    /// <param name="container">若为空，则尝试使用 TargetContainer 字段</param>
    public void SyncTargetsFromContainer(RectTransform container = null)
    {
        var targetC = container != null ? container : TargetContainer;
        if (targetC == null) return;

        EnsureBaselineCapacity();
        int count = Mathf.Min(Items.Count, targetC.childCount);
        for (int i = 0; i < count; i++)
        {
            var child = targetC.GetChild(i) as RectTransform;
            if (child != null)
            {
                _baselinePositions[i] = child.anchoredPosition;
            }
        }
    }

    Vector2 GetOffsetVector()
    {
        if (Direction == SlideDirection.Horizontal)
        {
            return new Vector2(MoveDistance, 0f);
        }
        else
        {
            return new Vector2(0f, -MoveDistance);
        }
    }

    /// <summary>
    /// 播放入场：每个子项先被放置在原位置的偏移处（MoveDistance），透明度置 0，然后按间隔依次滑入并淡入至 1。
    /// </summary>
    public void PlayEnter()
    {
        KillRunning();
        EnsureBaselineCapacity();
        if (AutoSyncTargetsFromContainer) SyncTargetsFromContainer();

        var offset = GetOffsetVector();

        for (int i = 0; i < Items.Count; i++)
        {
            var rt = Items[i];
            if (rt == null) continue;
            var basePos = _baselinePositions[i];
            var start = basePos + offset;
            var fh = GetFadeHandle(rt);
            rt.anchoredPosition = start;
            fh.SetAlpha(0f);
            var delay = Interval * i;
            var m = rt.DOAnchorPos(basePos, MoveDuration).SetEase(MoveEase).SetUpdate(UseUnscaledTime).SetDelay(delay);
            var f = fh.DOFade(1f, FadeDuration, UseUnscaledTime, FadeEase);
            if (f != null) f.SetDelay(delay);
            _runningTweens.Add(m);
            if (f != null) _runningTweens.Add(f);
        }
    }

    /// <summary>
    /// 播放退场：每个子项按间隔依次滑出（MoveDistance）并淡出至 0。
    /// 顺序：从最后一个开始往前播放（倒序）。
    /// </summary>
    public void PlayExit()
    {
        KillRunning();
        EnsureBaselineCapacity();
        if (AutoSyncTargetsFromContainer) SyncTargetsFromContainer();

        var offset = GetOffsetVector();

        for (int i = 0; i < Items.Count; i++)
        {
            var rt = Items[i];
            if (rt == null) continue;
            var basePos = _baselinePositions[i];
            var target = basePos + offset;
            var fh = GetFadeHandle(rt);
            
            // 倒序计算延迟：最后一个元素延迟为 0，第一个元素延迟最大
            var delay = Interval * (Items.Count - 1 - i);
            
            var m = rt.DOAnchorPos(target, MoveDuration).SetEase(MoveEase).SetUpdate(UseUnscaledTime).SetDelay(delay);
            var f = fh.DOFade(0f, FadeDuration, UseUnscaledTime, FadeEase);
            if (f != null) f.SetDelay(delay);
            _runningTweens.Add(m);
            if (f != null) _runningTweens.Add(f);
        }
    }

    /// <summary>
    /// 异步播放入场并等待整体完成（便于流程串联）。
    /// </summary>
    public async UniTask PlayEnterAsync()
    {
        PlayEnter();
        var total = Interval * (Items.Count - 1) + Mathf.Max(MoveDuration, FadeDuration);
        await UniTask.WaitForSeconds(total, UseUnscaledTime);
    }

    /// <summary>
    /// 异步播放退场并等待整体完成（便于流程串联）。
    /// </summary>
    public async UniTask PlayExitAsync()
    {
        PlayExit();
        var total = Interval * (Items.Count - 1) + Mathf.Max(MoveDuration, FadeDuration);
        await UniTask.WaitForSeconds(total, UseUnscaledTime);
    }

    /// <summary>
    /// 立即设置为退场状态（不播放动画）：位置在下方，透明度为 0。
    /// 用于初始化或重置状态。
    /// </summary>
    [ContextMenu("测试/立即设为退场状态")]
    public void SetExitStateImmediate()
    {
        KillRunning();
        EnsureBaselineCapacity();
        if (AutoSyncTargetsFromContainer) SyncTargetsFromContainer();

        var offset = GetOffsetVector();

        for (int i = 0; i < Items.Count; i++)
        {
            var rt = Items[i];
            if (rt == null) continue;
            var basePos = _baselinePositions[i];
            var target = basePos + offset;
            var fh = GetFadeHandle(rt);
            
            rt.anchoredPosition = target;
            fh.SetAlpha(0f);
        }
    }

    [ContextMenu("测试/播放入场")]
    public void EditorPlayEnter()
    {
        PlayEnter();
    }

    [ContextMenu("测试/播放退场")]
    public void EditorPlayExit()
    {
        PlayExit();
    }

    [ContextMenu("测试/从子物体填充 Items")]
    public void PopulateItemsFromChildren()
    {
        Items.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            var rt = transform.GetChild(i) as RectTransform;
            if (rt != null) Items.Add(rt);
        }
        RefreshBaselinesFromItems();
    }
}
