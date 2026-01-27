using System;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utils
{
    /// <summary>
    /// 提供系统级弹窗功能的工具类
    /// </summary>
    public static class SystemPopup
    {
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // 导入 Windows User32.dll 中的 MessageBox 函数
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
        #endif

        /// <summary>
        /// 显示一个系统弹窗
        /// </summary>
        /// <param name="content">弹窗显示的内容</param>
        /// <param name="title">弹窗标题，默认为"提示"</param>
        /// <param name="onConfirm">点击确定按钮后的回调</param>
        /// <param name="copyToClipboard">是否自动将内容复制到剪贴板</param>
        public static void Show(string content, string title = "提示", Action onConfirm = null, bool copyToClipboard = false)
        {
            if (copyToClipboard)
            {
                GUIUtility.systemCopyBuffer = content;
                content += "\n\n(内容已自动复制到剪贴板)";
            }

            #if UNITY_EDITOR
            // 在编辑器中使用 Unity 自带的弹窗
            // DisplayDialog 返回 true 表示点击了 ok 按钮
            if (EditorUtility.DisplayDialog(title, content, "确定"))
            {
                onConfirm?.Invoke();
            }
            #elif UNITY_STANDALONE_WIN
            // 在 Windows 平台使用系统原生弹窗
            // hWnd: 0 (IntPtr.Zero) 表示无特定父窗口
            // type: 0x00000000 (MB_OK) | 0x00040000 (MB_TOPMOST) | 0x00002000 (MB_TASKMODAL)
            // MB_TOPMOST: 确保弹窗在所有窗口之上
            // MB_TASKMODAL: 暂停当前应用程序，直到用户响应弹窗
            const uint MB_OK = 0x00000000;
            const uint MB_TOPMOST = 0x00040000;
            const uint MB_TASKMODAL = 0x00002000;
            
            MessageBox(IntPtr.Zero, content, title, MB_OK | MB_TOPMOST | MB_TASKMODAL);
            
            // MessageBox 是阻塞调用，窗口关闭后才会执行到这里
            onConfirm?.Invoke();
            #else
            // 其他平台回退到 Debug.Log
            Debug.Log($"[SystemPopup] {title}: {content}");
            onConfirm?.Invoke();
            #endif
        }
    }
}
