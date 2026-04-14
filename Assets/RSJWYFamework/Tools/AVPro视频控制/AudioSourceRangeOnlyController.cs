using UnityEngine;
using System.Collections;

[AddComponentMenu("Audio/Helpers/AudioSource Range Only Controller")]
public class AudioSourceRangeOnlyController : MonoBehaviour
{
    private enum PlaybackMode
    {
        None,
        PlayToPause,
        LoopRange
    }

    [SerializeField]
    private AudioSource _audioSource;

    private Coroutine _controlCoroutine;
    private PlaybackMode _currentMode = PlaybackMode.None;

    private double _targetPauseTime;
    private double _loopStartTime;
    private double _loopEndTime;

    private bool _seekOnStart;
    private double _seekTargetTime;

    public AudioSource audioSource => _audioSource;

    public void PlayToAndPauseFromCurrent(double targetTimeInSeconds)
    {
        if (!ValidateAudioSource()) return;
        double currentTime = _audioSource.time;
        if (currentTime >= targetTimeInSeconds)
        {
            Debug.LogWarning($"无法执行 PlayToAndPauseFromCurrent：当前时间 ({currentTime}s) 已超过目标时间 ({targetTimeInSeconds}s)。");
            return;
        }
        StopControlCoroutine();
        _currentMode = PlaybackMode.PlayToPause;
        _targetPauseTime = targetTimeInSeconds;
        _seekOnStart = false;
        _controlCoroutine = StartCoroutine(ControlRoutine());
    }

    public void PlayLoopingRangeFromCurrent(double startTimeInSeconds, double endTimeInSeconds)
    {
        if (!ValidateAudioSource() || !ValidateLoopTimes(startTimeInSeconds, endTimeInSeconds)) return;
        StopControlCoroutine();
        _currentMode = PlaybackMode.LoopRange;
        _loopStartTime = startTimeInSeconds;
        _loopEndTime = endTimeInSeconds;
        double currentTime = _audioSource.time;
        if (currentTime >= _loopEndTime)
        {
            _seekOnStart = true;
            _seekTargetTime = _loopStartTime;
            Debug.Log($"当前时间 ({currentTime}s) 已超过循环结束点 ({_loopEndTime}s)，将跳转到循环开始点 ({_loopStartTime}s)。");
        }
        else
        {
            _seekOnStart = false;
        }
        _controlCoroutine = StartCoroutine(ControlRoutine());
    }

    public void PlayToAndPause(double targetTimeInSeconds)
    {
        if (!ValidateAudioSource()) return;
        StopControlCoroutine();
        _currentMode = PlaybackMode.PlayToPause;
        _targetPauseTime = targetTimeInSeconds;
        _seekOnStart = true;
        _seekTargetTime = 0;
        _controlCoroutine = StartCoroutine(ControlRoutine());
    }

    public void PlayLoopingRange(double startTimeInSeconds, double endTimeInSeconds)
    {
        if (!ValidateAudioSource() || !ValidateLoopTimes(startTimeInSeconds, endTimeInSeconds)) return;
        StopControlCoroutine();
        _currentMode = PlaybackMode.LoopRange;
        _loopStartTime = startTimeInSeconds;
        _loopEndTime = endTimeInSeconds;
        _seekOnStart = true;
        _seekTargetTime = _loopStartTime;
        _controlCoroutine = StartCoroutine(ControlRoutine());
    }

    public void StopAndPause()
    {
        if (!ValidateAudioSource()) return;
        StopControlCoroutine();
        _currentMode = PlaybackMode.None;
        if (_audioSource.isPlaying)
        {
            _audioSource.Pause();
        }
    }

    public void StopAndRewind()
    {
        if (!ValidateAudioSource()) return;
        StopControlCoroutine();
        _currentMode = PlaybackMode.None;
        _audioSource.Pause();
        _audioSource.time = 0f;
        Debug.Log("播放已停止并返回到音频开头。");
    }

    public void SetAudioSource(AudioSource src)
    {
        _audioSource = src;
    }

    private IEnumerator ControlRoutine()
    {
        if (_seekOnStart)
        {
            _audioSource.time = (float)_seekTargetTime;
        }
        _audioSource.Play();
        yield return new WaitUntil(() => _audioSource.isPlaying && _audioSource.clip != null && _audioSource.clip.length > 0f);
        switch (_currentMode)
        {
            case PlaybackMode.PlayToPause:
                yield return HandlePlayToPause();
                break;
            case PlaybackMode.LoopRange:
                yield return HandleLoopRange();
                break;
        }
        _currentMode = PlaybackMode.None;
        _controlCoroutine = null;
    }

    private IEnumerator HandlePlayToPause()
    {
        while (_audioSource.isPlaying && _audioSource.time < (float)_targetPauseTime)
        {
            yield return null;
        }
        if (_audioSource.isPlaying)
        {
            _audioSource.Pause();
            Debug.Log($"音频已在目标时间点 {_targetPauseTime}s 附近暂停。");
        }
    }

    private IEnumerator HandleLoopRange()
    {
        while (_currentMode == PlaybackMode.LoopRange)
        {
            double currentTime = _audioSource.time;
            if (currentTime < _loopStartTime || currentTime >= _loopEndTime || !_audioSource.isPlaying)
            {
                if (currentTime >= _loopEndTime || !_audioSource.isPlaying)
                {
                    _audioSource.time = (float)_loopStartTime;
                    if (!_audioSource.isPlaying)
                    {
                        _audioSource.Play();
                    }
                }
            }
            yield return null;
        }
    }

    private void StopControlCoroutine()
    {
        if (_controlCoroutine != null)
        {
            StopCoroutine(_controlCoroutine);
            _controlCoroutine = null;
        }
    }

    private bool ValidateAudioSource()
    {
        if (_audioSource == null)
        {
            Debug.LogError("AudioSource 未在 Inspector 中指定，无法执行操作。");
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
}

