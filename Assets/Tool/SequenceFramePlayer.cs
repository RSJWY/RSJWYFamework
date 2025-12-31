using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 单个序列帧配置：包含序列名称、帧数组以及该序列的独立帧率
/// </summary>
[System.Serializable]
public class SequenceFrameData
{
    /// <summary>序列名称（例如："huxi"、"saomiao"）</summary>
    public string sequenceName;
    /// <summary>该序列的所有帧</summary>
    public Texture2D[] frames;
    /// <summary>该序列的独立帧率（FPS）</summary>
    public float fps = 12f;

    /// <summary>该序列的总帧数</summary>
    public int skipCount = 5;
    
    public bool isUsed = false;
}
/// <summary>
/// 支持多序列轮播的序列帧播放器：
/// - 每个序列拥有独立帧率
/// - 按配置循环播放，达到循环次数后可自动切换到下一个序列
/// - 对外接口（Speed、Position 等）始终作用于“当前序列”
/// </summary>
public class SequenceFramePlayer : MonoBehaviour
{
    Texture2D[] frames;

    /// <summary>所有可播放序列的列表（按顺序切换）</summary>
    public List<SequenceFrameData> sequenceFrames = new List<SequenceFrameData>();

    /// <summary>是否在达到循环次数后自动切到下一序列</summary>
    public bool autoGroupCarousel = true;
    /// <summary>每个序列循环多少次后再切换</summary>
    public int loopsPerGroup = 1;
    /// <summary>是否循环播放当前序列</summary>
    public bool loop = true;
    /// <summary>启用时是否自动开始播放</summary>
    public bool playOnEnable = true;

    RawImage image;
    int current;
    float accumulator;
    bool playing;
    int completedLoops;
    int currentSeqIndex;

    /// <summary>若用于 UI，返回关联的 Image 组件</summary>
    public RawImage imageUI => image;
    /// <summary>当前序列的帧数组（只读引用）</summary>
    public Texture2D[] CurrentFrames => frames;
    public int CurrentSkipCount => (sequenceFrames != null && sequenceFrames.Count > 0) ? (sequenceFrames[currentSeqIndex] != null ? sequenceFrames[currentSeqIndex].skipCount : 0) : 0;

    void Awake()
    {
        image = GetComponent<RawImage>();
        if (sequenceFrames != null && sequenceFrames.Count > 0)
        {
            currentSeqIndex = FindFirstUsableIndex();
            if (currentSeqIndex >= 0)
            {
                frames = sequenceFrames[currentSeqIndex].frames;
                current = 0;
                completedLoops = 0;
            }
            else
            {
                playing = false;
            }
        }
        
        ApplyFrame();


    }
    

    void OnEnable()
    {
        playing = playOnEnable;
        ApplyFrame();
    }

    void OnDisable()
    {
        playing = false;
    }

    void Update()
    {
        if (!playing) return;
        if (frames == null || frames.Length == 0) return;
        // 使用当前序列的独立帧率推进
        float currentFps = GetCurrentFps();
        if (currentFps <= 0f) return;
        accumulator += Time.deltaTime;
        float step = 1f / currentFps; // 单帧间隔
        if (accumulator >= step)
        {
            int advance = Mathf.FloorToInt(accumulator / step);
            accumulator -= advance * step;
            int before = current;
            Advance(advance);
            if (loop && advance > 0 && frames != null && frames.Length > 0)
            {
                if (current < before)
                {
                    // 发生回绕，视为完成一次循环
                    completedLoops++;
                    if (autoGroupCarousel && loopsPerGroup > 0 && completedLoops >= loopsPerGroup)
                    {
                        SwitchSequence();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 推进帧位置，支持循环或在非循环模式下停在最后一帧
    /// </summary>
    void Advance(int steps)
    {
        if (!loop)
        {
            int next = current + steps;
            if (next >= frames.Length)
            {
                next = frames.Length - 1;
                playing = false;
                // 非循环模式不在此自动切到下一序列，避免破坏外部等待逻辑
            }
            current = next;
        }
        else
        {
            int len = frames.Length;
            current = ((current + steps) % len + len) % len;
        }
        ApplyFrame();
    }

    /// <summary>
    /// 将当前帧应用到 Image 或 SpriteRenderer
    /// </summary>
    void ApplyFrame()
    {
        Texture2D s = GetSpriteAt(current);
        if (image != null)image.texture =s;
    }

    /// <summary>
    /// 安全获取指定索引的帧
    /// </summary>
    Texture2D GetSpriteAt(int index)
    {
        if (frames == null || frames.Length == 0) return null;
        int i = Mathf.Clamp(index, 0, frames.Length - 1);
        return frames[i];
    }

    /// <summary>开始播放当前序列</summary>
    public void Play() { playing = true; }
    /// <summary>暂停播放</summary>
    public void Pause() { playing = false; }
    /// <summary>停止播放并回到第 0 帧</summary>
    public void Stop() { playing = false; accumulator = 0f; SetPosition(0); }

    /// <summary>
    /// 读取/设置当前序列的帧率（映射到当前 SequenceFrameData.fps）
    /// </summary>
    public float Speed
    {
        get { return GetCurrentFps(); }
        set { SetCurrentFps(Mathf.Max(0f, value)); }
    }

    /// <summary>读取/设置当前帧索引</summary>
    public int Position
    {
        get { return current; }
        set { SetPosition(value); }
    }

    /// <summary>设置到指定帧索引并立即显示</summary>
    public void SetPosition(int index)
    {
        int i = Mathf.Clamp(index, 0, frames != null && frames.Length > 0 ? frames.Length - 1 : 0);
        current = i;
        ApplyFrame();
    }

    /// <summary>
    /// 当前帧的归一化位置（0..1），按当前序列长度计算
    /// </summary>
    public float NormalizedPosition
    {
        get
        {
            if (frames == null || frames.Length == 0) return 0f;
            if (frames.Length == 1) return 0f;
            return (float)current / (frames.Length - 1);
        }
        set
        {
            if (frames == null || frames.Length == 0) return;
            int len = frames.Length;
            int i = Mathf.RoundToInt(Mathf.Clamp01(value) * (len - 1));
            SetPosition(i);
        }
    }

    /// <summary>
    /// 切换到下一个序列（按列表顺序），并重置循环计数与帧位置
    /// </summary>
    void SwitchSequence()
    {
        if (sequenceFrames == null || sequenceFrames.Count == 0) return;
        int nextIndex = FindNextUsableIndex(currentSeqIndex);
        if (nextIndex < 0) return;
        currentSeqIndex = nextIndex;
        var next = sequenceFrames[currentSeqIndex];
        if (next != null && next.frames != null && next.frames.Length > 0)
        {
            frames = next.frames;
            completedLoops = 0;
            current = 0;
            ApplyFrame();
        }
    }

    int FindFirstUsableIndex()
    {
        if (sequenceFrames == null) return -1;
        for (int i = 0; i < sequenceFrames.Count; i++)
        {
            var s = sequenceFrames[i];
            if (s != null && s.isUsed && s.frames != null && s.frames.Length > 0)
            {
                return i;
            }
        }
        return -1;
    }

    int FindNextUsableIndex(int currentIndex)
    {
        if (sequenceFrames == null || sequenceFrames.Count == 0) return -1;
        for (int step = 1; step <= sequenceFrames.Count; step++)
        {
            int idx = (currentIndex + step) % sequenceFrames.Count;
            var s = sequenceFrames[idx];
            if (s != null && s.isUsed && s.frames != null && s.frames.Length > 0)
            {
                return idx;
            }
        }
        return -1;
    }

    /// <summary>获取当前序列的独立帧率</summary>
    float GetCurrentFps()
    {
        if (sequenceFrames == null || sequenceFrames.Count == 0) return 0f;
        return Mathf.Max(0f, sequenceFrames[currentSeqIndex].fps);
    }

    /// <summary>设置当前序列的帧率</summary>
    void SetCurrentFps(float value)
    {
        if (sequenceFrames == null || sequenceFrames.Count == 0) return;
        sequenceFrames[currentSeqIndex].fps = value;
    }
}

public class SequenceData
{
    public FrameData[] Frames;
    public int Loops;
    public float Scale;
}
public class FrameData
{
    public string SequenceName;
    public int FPS;
    public int SkipCount;
    public bool isUsed;
}
