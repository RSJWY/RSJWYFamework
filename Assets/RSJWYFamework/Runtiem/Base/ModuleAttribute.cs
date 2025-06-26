using System;
using System.Collections;
using UnityEngine;

namespace Assets.RSJWYFamework.Runtiem.Base
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ModuleAttribute : Attribute
    {
        /// <summary>
        /// 模块名
        /// </summary>
        public string ModuleName { get; }
        /// <summary>
        /// 模块优先级
        /// </summary>
        public int Priority { get; }

        public ModuleAttribute(string name = null, int priority = 0, bool autoInit = true)
        {
            ModuleName = name;
            Priority = priority;
        }
    }
}