using UnityEngine;
using Utils;

public class PopupExample : MonoBehaviour
{
    // 在 Unity 编辑器中可以修改这个测试内容
    public string testContent = "这是一个系统弹窗测试！";
    public string testTitle = "测试标题";

    // 可以将此函数绑定到 UI 按钮的 OnClick 事件
    public void ShowTestPopup()
    {
        SystemPopup.Show(testContent, testTitle, () =>
        {
            Debug.Log("用户点击了确定按钮！");
            // 在这里添加点击确定后的逻辑
        });
    }

    void Start()
    {
        // 示例：游戏开始时弹窗 (如果需要测试可以取消注释)
        // SystemPopup.Show("游戏启动了！");
    }
}
