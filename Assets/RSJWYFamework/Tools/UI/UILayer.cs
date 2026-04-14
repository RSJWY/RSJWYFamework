namespace RSJWYFamework.Runtime.UI
{
    /// <summary>
    /// UI 层级枚举
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 底层 (背景、地图等)
        /// </summary>
        Bottom = 0,
        
        /// <summary>
        /// 普通层 (常规窗口、全屏界面)
        /// </summary>
        Normal = 1000,
        
        /// <summary>
        /// 顶层 (弹窗、浮窗)
        /// </summary>
        Top = 2000,
        
        /// <summary>
        /// 系统层 (Loading、断线重连、新手引导)
        /// </summary>
        System = 3000
    }
}
