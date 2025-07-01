using System;
using System.Diagnostics;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    public static  partial class Utility
    {
        public static class Timestamp
        {
            private static Stopwatch _stopwatch;

            [RuntimeInitializeOnLoadMethod]
            private static void InitializeTimestamp()
            {
                _stopwatch = Stopwatch.StartNew();
            }

            #region 系统时间相关

            /// <summary>
            /// 获取当前Unix时间戳(秒)
            /// </summary>
            public static long UnixTimestampSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            /// <summary>
            /// 获取当前Unix时间戳(毫秒)
            /// </summary>
            public static long UnixTimestampMilliseconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            /// <summary>
            /// 获取系统计时周期数(100纳秒间隔)
            /// </summary>
            public static long SystemTicks => DateTime.Now.Ticks;

            /// <summary>
            /// 获取格式化的本地时间字符串
            /// </summary>
            /// <param name="format">格式字符串(默认:yyyy-MM-dd HH:mm:ss.fff)</param>
            public static string FormattedLocalTime(string format = "yyyy-MM-dd HH:mm:ss.fff")
            {
                return DateTime.Now.ToString(format);
            }

            #endregion

            #region Unity游戏时间相关

            /// <summary>
            /// 获取游戏运行时间(秒，受TimeScale影响)
            /// </summary>
            public static float GameTime => Time.time;

            /// <summary>
            /// 获取游戏运行时间(秒，不受TimeScale影响)
            /// </summary>
            public static float GameTimeUnscaled => Time.unscaledTime;

            /// <summary>
            /// 获取从游戏启动开始的帧数
            /// </summary>
            public static int FrameCount => Time.frameCount;

            #endregion

            #region 高精度计时

            /// <summary>
            /// 获取高精度计时器周期数
            /// </summary>
            public static long HighPrecisionTicks => _stopwatch.ElapsedTicks;

            /// <summary>
            /// 获取高精度计时毫秒数
            /// </summary>
            public static long HighPrecisionMilliseconds => _stopwatch.ElapsedMilliseconds;

            /// <summary>
            /// 将计时周期数转换为秒
            /// </summary>
            public static double TicksToSeconds(long ticks)
            {
                return ticks / (double)Stopwatch.Frequency;
            }

            #endregion

            #region 实用方法

            /// <summary>
            /// 生成基于时间戳的唯一ID
            /// </summary>
            public static string GenerateTimeBasedID()
            {
                return $"{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            }

            /// <summary>
            /// 将Unix时间戳转换为DateTime
            /// </summary>
            public static DateTime UnixToDateTime(long unixTimestamp, bool isMilliseconds = false)
            {
                return isMilliseconds
                    ? DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).DateTime
                    : DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
            }

            #endregion
        }
    }
}