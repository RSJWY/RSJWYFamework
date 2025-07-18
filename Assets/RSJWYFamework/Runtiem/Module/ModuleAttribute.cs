using System;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 模块参数
    /// <remarks>添加此属性会在程序运行时自动注册模块，目前没有配置一些特别的点</remarks>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ModuleAttribute : Attribute
    {
        public ModuleAttribute(int priority = 0)
        {
            
        }
    }
}