using UnityEngine;
using RSJWYFamework.Runtime;

public class UITestLauncher : MonoBehaviour
{
    void Update()
    {
        // 按 Q 打开主菜单
        if (Input.GetKeyDown(KeyCode.Q))
        {
            AppLogger.Log("测试：按下 Q，打开主菜单");
            ModuleManager.GetModule<UIPageManager>().Push("DemoMainMenuPage");
        }

        // 按 W 打开设置页（传递参数）
        if (Input.GetKeyDown(KeyCode.W))
        {
            AppLogger.Log("测试：按下 W，打开设置页");
            ModuleManager.GetModule<UIPageManager>().Push("DemoSettingsPage", "Hello from Test!");
        }

        // 按 E 关闭当前页
        if (Input.GetKeyDown(KeyCode.E))
        {
            AppLogger.Log("测试：按下 E，关闭当前页");
            ModuleManager.GetModule<UIPageManager>().Pop();
        }
        
        // 按 R 替换当前页
        if (Input.GetKeyDown(KeyCode.R))
        {
             AppLogger.Log("测试：按下 R，用主菜单替换当前页");
             ModuleManager.GetModule<UIPageManager>().Replace("DemoMainMenuPage");
        }
    }
}
