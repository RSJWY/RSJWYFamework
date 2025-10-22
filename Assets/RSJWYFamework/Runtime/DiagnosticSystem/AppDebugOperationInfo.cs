using System;
using System.Collections;
using System.Collections.Generic;

namespace RSJWYFamework.Runtime
{
    [Serializable]
    internal struct AppDebugOperationInfo : IComparer<AppDebugOperationInfo>, IComparable<AppDebugOperationInfo>
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string OperationName;

        /// <summary>
        /// 任务说明
        /// </summary>
        public string OperationDesc;

        /// <summary>
        /// 优先级
        /// </summary>
        public uint Priority;
        
        /// <summary>
        /// 任务进度
        /// </summary>
        public float Progress;

        /// <summary>
        /// 任务开始的时间
        /// </summary>
        public string BeginTime;

        /// <summary>
        /// 处理耗时（单位：毫秒）
        /// </summary>
        public long ProcessTime;

        /// <summary>
        /// 任务状态
        /// </summary>
        public string Status;

        /// <summary>
        /// 子任务列表
        /// TODO : Serialization depth limit 10 exceeded
        /// </summary>
        public List<AppDebugOperationInfo> Childs;

        public int CompareTo(AppDebugOperationInfo other)
        {
            return Compare(this, other);
        }
        public int Compare(AppDebugOperationInfo a, AppDebugOperationInfo b)
        {
            return string.CompareOrdinal(a.OperationName, b.OperationName);
        }
    }
}