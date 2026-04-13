using UnityEngine;

namespace RSJWYFamework.Runtime.UI.Examples
{
    /// <summary>
    /// 示例页面：设置页
    /// </summary>
    public class DemoSettingsPage : UIPageBase
    {
        public override void OnEnter(object data)
        {
            base.OnEnter(data);
            AppLogger.Log(">>> 设置页：打开中...");
            if (data is string msg)
            {
                AppLogger.Log($"收到参数: {msg}");
            }
        }

        public void OnClickClose()
        {
            // 关闭自己
            ModuleManager.GetModule<UIPageManager>().Pop();
        }
    }
}
