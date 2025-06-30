using System;

namespace RSJWYFamework.Runtime
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ModuleAttribute : Attribute
    {
        /// <summary>
        /// 模块优先级
        /// </summary>
        public int Priority { get; }

        public ModuleAttribute(int priority = 0)
        {
            Priority = priority;
        }
    }
}