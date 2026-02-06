using System;

namespace RSJWYFamework.Runtime.UI
{
    /// <summary>
    /// UI 窗口配置特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UIWindowAttribute : Attribute
    {
        /// <summary>
        /// 资源路径 (相对于 UI Resource Root)
        /// 如果为空，默认使用类名
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// UI 层级
        /// </summary>
        public UILayer Layer { get; set; } = UILayer.Normal;
        
        /// <summary>
        /// 是否常驻内存 (关闭时不销毁)
        /// </summary>
        public bool KeepCached { get; set; } = true;

        /// <summary>
        /// 是否是全屏窗口 (如果是全屏，打开时会覆盖下层窗口)
        /// </summary>
        public bool IsFullScreen { get; set; } = false;

        public UIWindowAttribute(string path = null)
        {
            Path = path;
        }
    }
}
