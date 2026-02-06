using UnityEngine;
using UnityEngine.UI;

namespace RSJWYFamework.Runtime.UI.Examples
{
    /// <summary>
    /// 示例页面：主菜单
    /// </summary>
    public class DemoMainMenuPage : UIPageBase
    {
        // 假设这里有一些 UI 组件
        // public Button startButton;

        public override void OnEnter(object data)
        {
            base.OnEnter(data);
            AppLogger.Log(">>> 主菜单：我显示啦！可以播放入场动画~");
        }

        public override void OnExit()
        {
            base.OnExit();
            AppLogger.Log("<<< 主菜单：我隐藏啦！");
        }

        public override void OnPause()
        {
            base.OnPause();
            AppLogger.Log("=== 主菜单：被盖住啦，暂停背景音乐...");
        }

        public override void OnResume()
        {
            base.OnResume();
            AppLogger.Log("=== 主菜单：又回来啦，继续播放音乐！");
        }
        
        // 供按钮绑定的方法
        public void OnClickOpenSettings()
        {
            // 打开设置页
            ModuleManager.GetModule<UIPageManager>().Push("DemoSettingsPage");
        }
    }
}
