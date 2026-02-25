using System;
using UnityEngine;

namespace RSJWYFamework.Runtime.Utilitiy
{
    /// <summary>
    /// 虚假进度条工具，用于处理进度条的平滑过渡与自动增涨。
    /// <para>核心逻辑：在真实进度未到达100%时，模拟缓慢增长直到虚假上限；一旦真实进度完成，立即快速追赶至100%。</para>
    /// </summary>
    public class FakeProgress
    {
        /// <summary>
        /// 当前用于显示的进度值 (0.0 - 1.0)
        /// </summary>
        public float VisualValue { get; private set; }

        /// <summary>
        /// 真实的进度目标值 (0.0 - 1.0)
        /// </summary>
        public float TargetValue { get; private set; }

        /// <summary>
        /// 虚假进度的增长速度（每秒增加的进度值），默认0.1f/s
        /// </summary>
        public float FakeSpeed { get; set; } = 0.1f;

        /// <summary>
        /// 追赶真实进度的速度（每秒增加的进度值），默认1.0f/s
        /// </summary>
        public float CatchUpSpeed { get; set; } = 1.0f;

        /// <summary>
        /// 虚假进度的上限，在真实进度未完成前，虚假进度不会超过此值，默认0.9f
        /// </summary>
        public float FakeTarget { get; set; } = 0.9f;

        /// <summary>
        /// 当进度值发生变化时的回调
        /// </summary>
        public Action<float> OnProgressChanged;

        /// <summary>
        /// 当进度达到1.0时的回调
        /// </summary>
        public Action OnComplete;

        private bool _isComplete = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="startValue">初始进度值</param>
        public FakeProgress(float startValue = 0f)
        {
            VisualValue = Mathf.Clamp01(startValue);
            TargetValue = VisualValue;
        }

        /// <summary>
        /// 设置真实的进度目标
        /// </summary>
        /// <param name="value">目标进度值 (0.0 - 1.0)</param>
        public void SetTarget(float value)
        {
            TargetValue = Mathf.Clamp01(value);
            // 如果目标被重置为小于1的值，重置完成状态
            if (TargetValue < 1.0f && _isComplete)
            {
                _isComplete = false;
            }
        }

        /// <summary>
        /// 立即完成进度条（跳至1.0）
        /// </summary>
        public void Finish()
        {
            SetTarget(1.0f);
        }

        /// <summary>
        /// 更新进度逻辑，建议在 Update 中调用
        /// </summary>
        /// <param name="deltaTime">时间增量，默认使用 Time.deltaTime</param>
        public void Update(float deltaTime = -1f)
        {
            if (_isComplete) return;

            if (deltaTime < 0) deltaTime = Time.deltaTime;

            float startValue = VisualValue;

            // 1. 如果真实目标已经达到或超过 1.0，全力追赶直到完成
            if (TargetValue >= 1.0f)
            {
                VisualValue = Mathf.MoveTowards(VisualValue, 1.0f, CatchUpSpeed * deltaTime);
                if (Mathf.Approximately(VisualValue, 1.0f))
                {
                    VisualValue = 1.0f;
                    _isComplete = true;
                    OnProgressChanged?.Invoke(VisualValue);
                    OnComplete?.Invoke();
                    return;
                }
            }
            else
            {
                // 2. 如果真实目标未完成
                // 如果当前显示值落后于真实目标，快速追赶
                if (VisualValue < TargetValue)
                {
                    VisualValue = Mathf.MoveTowards(VisualValue, TargetValue, CatchUpSpeed * deltaTime);
                }
                // 3. 如果已经赶上真实目标，但还未达到虚假上限，则缓慢模拟增长
                else if (VisualValue < FakeTarget)
                {
                    VisualValue += FakeSpeed * deltaTime;
                    // 确保不超过虚假上限
                    if (VisualValue > FakeTarget)
                    {
                        VisualValue = FakeTarget;
                    }
                }
            }

            // 只有当数值发生变化时才触发回调
            if (!Mathf.Approximately(VisualValue, startValue))
            {
                OnProgressChanged?.Invoke(VisualValue);
            }
        }

        /// <summary>
        /// 重置进度条状态
        /// </summary>
        /// <param name="value">重置后的初始值</param>
        public void Reset(float value = 0f)
        {
            VisualValue = Mathf.Clamp01(value);
            TargetValue = VisualValue;
            _isComplete = false;
            OnProgressChanged?.Invoke(VisualValue);
        }
    }
}
