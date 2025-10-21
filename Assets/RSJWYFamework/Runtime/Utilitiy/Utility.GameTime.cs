using UnityEngine;

namespace RSJWYFamework.Runtime
{
    //// <summary>
    /// 游戏时间
    /// 引用自TEngine框架，并进行修改
    /// </summary>
    public static partial class Utility
    {
        public static class GameTime
        {
            /// <summary>
            /// 此帧开始时的时间（只读）。
            /// </summary>
            public static float time { get; private set; }

            /// <summary>
            /// 从上一帧到当前帧的间隔（秒）（只读）。
            /// </summary>
            public static float deltaTime { get; private set; }

            /// <summary>
            /// timeScale从上一帧到当前帧的独立时间间隔（以秒为单位）（只读）。
            /// </summary>
            public static float unscaledDeltaTime { get; private set; }

            /// <summary>
            /// 执行物理和其他固定帧速率更新的时间间隔（以秒为单位）。
            /// <remarks>如MonoBehavior的MonoBehaviour.FixedUpdate。</remarks>
            /// </summary>
            public static float fixedDeltaTime { get; private set; }

            /// <summary>
            /// 自游戏开始以来的总帧数（只读）。
            /// </summary>
            public static float frameCount { get; private set; }

            /// <summary>
            /// timeScale此帧的独立时间（只读）。这是自游戏开始以来的时间（以秒为单位）。
            /// </summary>
            public static float unscaledTime { get; private set; }

            /// <summary>
            /// 采样一帧的时间。
            /// </summary>
            public static void StartFrame()
            {
                time = Time.time;
                deltaTime = Time.deltaTime;
                unscaledDeltaTime = Time.unscaledDeltaTime;
                fixedDeltaTime = Time.fixedDeltaTime;
                frameCount = Time.frameCount;
                unscaledTime = Time.unscaledTime;
            }

            /// <summary>
            /// 采样一帧的时间。
            /// </summary>
            public static ReGameTime GetStartFrame()
            {
                var _time = new ReGameTime()
                {
                    time = Time.time,
                    deltaTime = Time.deltaTime,
                    unscaledDeltaTime = Time.unscaledDeltaTime,
                    fixedDeltaTime = Time.fixedDeltaTime,
                    frameCount = Time.frameCount,
                    unscaledTime = Time.unscaledTime
                };
                return _time;
            }
        }

        /// <summary>
        /// 记录当前的游戏时间
        /// </summary>
        public record ReGameTime
        {
            /// <summary>
            /// 此帧开始时的时间（只读）。
            /// </summary>
            public float time;

            /// <summary>
            /// 从上一帧到当前帧的间隔（秒）（只读）。
            /// </summary>
            public float deltaTime;

            /// <summary>
            /// timeScale从上一帧到当前帧的独立时间间隔（以秒为单位）（只读）。
            /// </summary>
            public float unscaledDeltaTime;

            /// <summary>
            /// 执行物理和其他固定帧速率更新的时间间隔（以秒为单位）。
            /// <remarks>如MonoBehavior的MonoBehaviour.FixedUpdate。</remarks>
            /// </summary>
            public float fixedDeltaTime;

            /// <summary>
            /// 自游戏开始以来的总帧数（只读）。
            /// </summary>
            public float frameCount;

            /// <summary>
            /// timeScale此帧的独立时间（只读）。这是自游戏开始以来的时间（以秒为单位）。
            /// </summary>
            public float unscaledTime;
        }
    }
}