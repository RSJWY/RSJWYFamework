using System;
using RenderHeads.Media.AVProVideo;
using UnityEngine;
using UnityEngine.UI;

public class AVProPlayerController : MonoBehaviour
{
    // AVPro 播放控制器：
    // - 默认暂停并显示暂停图标
    // - 点击显示区域或播放按钮：切换播放/暂停状态
    // - 播放结束：自动回到开头并显示暂停图标
    // 依赖：UIEventListener（用于拦截显示区域点击）
    public MediaPlayer mediaPlayer;
    // 暂停图标的 UI 物体（Image 等）
    public GameObject pauseIcon;
    // 承载 AVPro 显示的区域（通常挂有 DisplayUGUI）
    public DisplayUGUI avproDisplayArea;
    // 播放按钮（点击后开始播放）
    public UIEventListener playButton;
    // 待机帧时间（秒），默认 0
    public float standbyTime = 0f;
    bool prepareFromStart = false;

    public bool IsPlaying => mediaPlayer != null && mediaPlayer.Control != null && mediaPlayer.Control.IsPlaying();

    public event Action FinishedPlaying;
    public event Action<bool> PlayStateChanged;

    public string VideoURL=String.Empty;
    
    public AudioSource selectAudioSource;
    void Start()
    {
        if (VideoURL!=String.Empty)
        {
            mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, VideoURL, false);
        }
        // 初始化：统一设为暂停并显示暂停图标
        if (mediaPlayer != null)
        {
            mediaPlayer.Pause();
            mediaPlayer.Events.AddListener(OnMediaPlayerEvent);
            // 初始进入待机状态
            SetPrepareFromStart(true);
        }
        if (pauseIcon != null) pauseIcon.SetActive(true);
        
        // 绑定显示区域点击
        if (avproDisplayArea != null)
        {
            var listener = UIEventListener.Get(avproDisplayArea.gameObject);
            listener.onClick = TogglePlayPause;
        }
        // 绑定播放按钮点击
        if (playButton != null)
        {
            playButton.onClick = TogglePlayPause;
        }
    }

    void OnDestroy()
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Events.RemoveListener(OnMediaPlayerEvent);
        }
    }

    // 切换播放/暂停逻辑
    void TogglePlayPause()
    {
        if (mediaPlayer == null || mediaPlayer.Control == null) return;
        PlaySelectAudio();
        if (mediaPlayer.Control.IsPlaying())
        {
            // 播放中 -> 暂停，显示图标
            mediaPlayer.Pause();
            if (pauseIcon != null) pauseIcon.SetActive(true);
            PlayStateChanged?.Invoke(false);
        }
        else
        {
            // 未播放 -> 播放，隐藏图标
            if (prepareFromStart)
            {
                mediaPlayer.Control.SeekFast(0.0);
                prepareFromStart = false; // 消耗掉该标志，确保后续暂停再播放是继续播放
            }
            mediaPlayer.Play();
            if (pauseIcon != null) pauseIcon.SetActive(false);
            PlayStateChanged?.Invoke(true);
        }
    }

    // 监听 AVPro 事件
    void OnMediaPlayerEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
    {
        switch (et)
        {
            case MediaPlayerEvent.EventType.FinishedPlaying:
                // 播放结束：回到待机帧，显示图标，标记为下次从头播
                if (mp.Control != null)
                {
                    mp.Control.SeekFast(standbyTime);
                    // Seek 后通常会自动暂停，但为了保险可以显式调 Pause
                    mp.Pause(); 
                }
                if (pauseIcon != null) pauseIcon.SetActive(true);
                // 标记为待机状态，以便下次点击时从头播放
                prepareFromStart = true;
                PlayStateChanged?.Invoke(false);
                FinishedPlaying?.Invoke();
                break;
            case MediaPlayerEvent.EventType.MetaDataReady:
                // 确保初始待机帧正确（如果是初始待机状态）
                if (prepareFromStart && mp.Control != null && !mp.Control.IsPlaying())
                {
                    mp.Control.SeekFast(standbyTime);
                }
                break;
        }
    }

    // 保留旧函数名以兼容现有引用（重定向到 TogglePlayPause 或废弃）
    void OnDisplayClick() => TogglePlayPause();
    void OnPlayClicked() => TogglePlayPause();

    public void SetPrepareFromStart(bool value)
    {
        prepareFromStart = value;
        if (value)
        {
            if (mediaPlayer != null)
            {
                mediaPlayer.Pause();
                if (mediaPlayer.Control != null) mediaPlayer.Control.SeekFast(standbyTime);
            }
            if (pauseIcon != null) pauseIcon.SetActive(true);
            PlayStateChanged?.Invoke(false);
        }
    }

    public void PlaySelectAudio()
    {
        selectAudioSource.time = 0;
        selectAudioSource.Play();
    }
}
