using RenderHeads.Media.AVProVideo;
using UnityEngine;

namespace Logger
{
    public class AVProLoopController: MonoBehaviour
    {
        [Tooltip("播放范围开始时间（秒）")]
        public float startTime = 0f;
    
        [Tooltip("播放范围结束时间（秒）")]
        public float endTime = 10f;
    
        [Tooltip("到达结束时间后是否循环回开始时间")]
        public bool loop = true;

        private MediaPlayer _mediaPlayer;

        private void Awake()
        {
            _mediaPlayer = GetComponent<MediaPlayer>();
            if (_mediaPlayer == null)
            {
                Debug.LogError("缺少MediaPlayer组件");
                enabled = false;
            }
        }

        private void Update()
        {
            // 仅在视频播放时检查范围
            if (_mediaPlayer.Control.IsPlaying() && _mediaPlayer.Control != null)
            {
                CheckPlayRange();
            }
        }

        // 核心逻辑：检查并限制播放范围
        private void CheckPlayRange()
        {
            float currentTime =(float) _mediaPlayer.Control.GetCurrentTime();
            float duration = (float)_mediaPlayer.Info.GetDuration();

            // 确保结束时间不超过视频总时长
            float validEndTime = Mathf.Min(endTime, duration);
        
            // 情况1：当前时间超过结束时间
            if (currentTime >= validEndTime)
            {
                if (loop)
                {
                    // 循环：跳回开始时间
                    _mediaPlayer.Control.Seek(startTime);
                }
                else
                {
                    // 不循环：停在结束时间
                    _mediaPlayer.Pause();
                    _mediaPlayer.Control.Seek(validEndTime);
                }
            }
            // 情况2：当前时间早于开始时间（可能手动拖动导致）
            else if (currentTime < startTime)
            {
                _mediaPlayer.Control.Seek(startTime);
            }
        }
    }
}