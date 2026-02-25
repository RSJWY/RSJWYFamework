using UnityEngine;
using UnityEngine.Events;

namespace RSJWYFamework.Runtime.Utilitiy
{
    /// <summary>
    /// FakeProgress 的 MonoBehaviour 包装器，方便在 Inspector 中配置和使用
    /// </summary>
    public class FakeProgressMono : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("初始进度值")]
        public float StartValue = 0f;
        
        [Tooltip("虚假增长速度 (每秒)")]
        public float FakeSpeed = 0.1f;
        
        [Tooltip("追赶真实进度速度 (每秒)")]
        public float CatchUpSpeed = 1.0f;
        
        [Tooltip("虚假上限 (0-1)")]
        [Range(0f, 1f)]
        public float FakeTarget = 0.9f;

        [Header("Events")]
        public UnityEvent<float> OnProgressChanged;
        public UnityEvent OnComplete;

        public FakeProgress Logic { get; private set; }

        private void Awake()
        {
            Logic = new FakeProgress(StartValue);
            SyncSettings();

            Logic.OnProgressChanged += (v) => OnProgressChanged?.Invoke(v);
            Logic.OnComplete += () => OnComplete?.Invoke();
        }

        private void Update()
        {
            if (Logic == null) return;
            
            // 允许运行时在 Inspector 调整参数
            SyncSettings();
            Logic.Update(Time.deltaTime);
        }

        private void SyncSettings()
        {
            if (Logic == null) return;
            Logic.FakeSpeed = FakeSpeed;
            Logic.CatchUpSpeed = CatchUpSpeed;
            Logic.FakeTarget = FakeTarget;
        }

        /// <summary>
        /// 设置真实目标进度
        /// </summary>
        public void SetTarget(float value)
        {
            Logic?.SetTarget(value);
        }

        /// <summary>
        /// 立即完成
        /// </summary>
        public void Finish()
        {
            Logic?.Finish();
        }

        /// <summary>
        /// 重置进度
        /// </summary>
        public void ResetProgress(float value = 0f)
        {
            Logic?.Reset(value);
        }
    }
}
