using UnityEngine;
using System.Collections;
using RenderHeads.Media.AVProVideo;

/// <summary>
/// 一个高级控制器，用于管理 AVPro MediaPlayer 的播放行为。
/// 支持模式：播放到暂停、循环片段、停止并返回开头。
/// V2: 增加了从当前位置开始播放的智能逻辑。
/// </summary>
[AddComponentMenu("AVPro Video/Helpers/AVPro Range Only Controller")]
public class AVProRangeOnlyController : MonoBehaviour
{
    private enum PlaybackMode
    {
        None,
        PlayToPause,
        LoopRange
    }

    [Tooltip("要控制的 AVPro MediaPlayer 组件。")]
    [SerializeField]
    private MediaPlayer _mediaPlayer;
    [SerializeField]
    private DisplayUGUI displayUGUI;
    private Coroutine _controlCoroutine;
    private PlaybackMode _currentMode = PlaybackMode.None;

    private double _targetPauseTime;
    private double _loopStartTime;
    private double _loopEndTime;

    // 新增标志，用于决定协程启动时是否需要Seek
    private bool _seekOnStart;
    private double _seekTargetTime;
    
    public MediaPlayer mediaPlayer => _mediaPlayer;
    
    public DisplayUGUI DisplayUGUI => displayUGUI;


    #region Public API (New & Improved)

    /// <summary>
    /// 从当前位置开始播放，并在到达指定时间点后暂停。
    /// </summary>
    /// <param name="targetTimeInSeconds">希望视频暂停的目标时间（秒）。</param>
    public void PlayToAndPauseFromCurrent(double targetTimeInSeconds)
    {
        if (!ValidateMediaPlayer()) return;

        double currentTime = _mediaPlayer.Control.GetCurrentTime();
        if (currentTime >= targetTimeInSeconds)
        {
            //Debug.LogWarning($"无法执行 PlayToAndPauseFromCurrent：当前时间 ({currentTime}s) 已超过目标时间 ({targetTimeInSeconds}s)。");
            return;
        }

        StopControlCoroutine();
        _currentMode = PlaybackMode.PlayToPause;
        _targetPauseTime = targetTimeInSeconds;
        _seekOnStart = false; // 明确指示不要Seek，从当前位置播放

        _controlCoroutine = StartCoroutine(ControlRoutine());
    }

    /// <summary>
    /// 从当前位置开始，并进入指定的循环播放范围。
    /// - 如果当前时间在范围后，则跳到范围开始处。
    /// - 否则，从当前位置继续播放。
    /// </summary>
    /// <param name="startTimeInSeconds">循环片段的开始时间（秒）。</param>
    /// <param name="endTimeInSeconds">循环片段的结束时间（秒）。</param>
    public void PlayLoopingRangeFromCurrent(double startTimeInSeconds, double endTimeInSeconds)
    {
        if (!ValidateMediaPlayer() || !ValidateLoopTimes(startTimeInSeconds, endTimeInSeconds)) return;
        
        StopControlCoroutine();
        _currentMode = PlaybackMode.LoopRange;
        _loopStartTime = startTimeInSeconds;
        _loopEndTime = endTimeInSeconds;

        // 核心逻辑：根据当前时间决定是否需要Seek
        double currentTime = _mediaPlayer.Control.GetCurrentTime();
        if (currentTime >= _loopEndTime)
        {
            _seekOnStart = true; // 需要跳转
            _seekTargetTime = _loopStartTime;
            //Debug.Log($"当前时间 ({currentTime}s) 已超过循环结束点 ({_loopEndTime}s)，将跳转到循环开始点 ({_loopStartTime}s)。");
        }
        else
        {
            _seekOnStart = false; // 不需要跳转，从当前位置继续
        }

        _controlCoroutine = StartCoroutine(ControlRoutine());
    }

    #endregion

    #region Public API (Legacy - for backward compatibility)

    /// <summary>
    /// [旧版] 从头播放，并在到达指定时间点后暂停。
    /// </summary>
    public void PlayToAndPause(double targetTimeInSeconds)
    {
        if (!ValidateMediaPlayer()) return;
        StopControlCoroutine();
        _currentMode = PlaybackMode.PlayToPause;
        _targetPauseTime = targetTimeInSeconds;
        _seekOnStart = true; // 总是从头开始
        _seekTargetTime = 0;
        _controlCoroutine = StartCoroutine(ControlRoutine());
    }

    /// <summary>
    /// [旧版] 跳转到循环开始位置，并开始在指定范围内循环播放。
    /// </summary>
    public void PlayLoopingRange(double startTimeInSeconds, double endTimeInSeconds)
    {
        if (!ValidateMediaPlayer() || !ValidateLoopTimes(startTimeInSeconds, endTimeInSeconds)) return;
        StopControlCoroutine();
        _currentMode = PlaybackMode.LoopRange;
        _loopStartTime = startTimeInSeconds;
        _loopEndTime = endTimeInSeconds;
        _seekOnStart = true; // 总是从循环起点开始
        _seekTargetTime = _loopStartTime;
        _controlCoroutine = StartCoroutine(ControlRoutine());
    }
    
    /// <summary>
    /// 停止所有特殊播放行为，并暂停视频在当前位置。
    /// </summary>
    public void StopAndPause()
    {
        if (!ValidateMediaPlayer()) return;
        StopControlCoroutine();
        _currentMode = PlaybackMode.None;
        if (_mediaPlayer.Control.IsPlaying())
        {
            _mediaPlayer.Control.Pause();
        }
    }

    /// <summary>
    /// 停止所有特殊播放行为，并将播放头返回到视频的开头。
    /// </summary>
    public void StopAndRewind()
    {
        if (!ValidateMediaPlayer()) return;
        StopControlCoroutine();
        _currentMode = PlaybackMode.None;
        _mediaPlayer.Control.Pause();
        _mediaPlayer.Control.Seek(0.0f);
        //Debug.Log("播放已停止并返回到视频开头。");
    }
    
    /// <summary>
    /// 允许从外部代码动态设置 MediaPlayer 实例。
    /// </summary>
    public void SetMediaPlayer(MediaPlayer mp,DisplayUGUI du)
    {
        _mediaPlayer = mp;
        displayUGUI = du;
    }

    #endregion

    #region Coroutine Logic

    private IEnumerator ControlRoutine()
    {
        // 1. 根据 _seekOnStart 标志决定是否执行跳转
        if (_seekOnStart)
        {
            _mediaPlayer.Control.Seek(_seekTargetTime);
        }

        // 2. 确保视频开始播放
        _mediaPlayer.Control.Play();

        // 3. 等待视频真正开始播放
        yield return new WaitUntil(() => _mediaPlayer.Control.IsPlaying() && _mediaPlayer.Info.GetDuration() > 0);

        //Debug.Log($"[{_currentMode}] 模式已启动。");

        // 4. 根据模式进入不同的处理循环
        switch (_currentMode)
        {
            case PlaybackMode.PlayToPause:
                yield return HandlePlayToPause();
                break;
            case PlaybackMode.LoopRange:
                yield return HandleLoopRange();
                break;
        }
        
        //Debug.Log($"[{_currentMode}] 模式已结束。");
        _currentMode = PlaybackMode.None;
        _controlCoroutine = null;
    }

    private IEnumerator HandlePlayToPause()
    {
        while (_mediaPlayer.Control.IsPlaying() && _mediaPlayer.Control.GetCurrentTime() < _targetPauseTime)
        {
            yield return null;
        }
        if (_mediaPlayer.Control.IsPlaying())
        {
            _mediaPlayer.Control.Pause();
            //Debug.Log($"视频已在目标时间点 {_targetPauseTime}s 附近暂停。");
        }
    }

    private IEnumerator HandleLoopRange()
    {
        while (_currentMode == PlaybackMode.LoopRange)
        {
            // 如果当前播放时间不在循环范围内 (处理在开始点之前进入的情况)
            // 或者超过了结束点，就跳回循环起点
            double currentTime = _mediaPlayer.Control.GetCurrentTime();
            if ((currentTime < _loopStartTime && _mediaPlayer.Control.IsFinished() == false) || currentTime >= _loopEndTime || _mediaPlayer.Control.IsFinished())
            {
                 // 检查时间是否真的越界，避免在seek后立即再次seek
                if (currentTime >= _loopEndTime || _mediaPlayer.Control.IsFinished())
                {
                    _mediaPlayer.Control.Seek(_loopStartTime);
                    if (!_mediaPlayer.Control.IsPlaying())
                    {
                        _mediaPlayer.Control.Play();
                    }
                }
            }
            yield return null;
        }
    }

    #endregion

    #region Helper Methods

    private void StopControlCoroutine()
    {
        if (_controlCoroutine != null)
        {
            StopCoroutine(_controlCoroutine);
            _controlCoroutine = null;
        }
    }

    private bool ValidateMediaPlayer()
    {
        if (_mediaPlayer == null)
        {
            Debug.LogError("MediaPlayer 未在 Inspector 中指定，无法执行操作。");
            return false;
        }
        return true;
    }

    private bool ValidateLoopTimes(double start, double end)
    {
        if (start >= end)
        {
            Debug.LogError("循环播放的开始时间必须小于结束时间。");
            return false;
        }
        return true;
    }

    #endregion
}
