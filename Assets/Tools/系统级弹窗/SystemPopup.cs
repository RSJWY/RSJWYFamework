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
        public static void Show(string content, string title = "提示", Action onConfirm = null)
        {
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
            // type: 0 表示 MB_OK (只有一个确定按钮)
            MessageBox(IntPtr.Zero, content, title, 0);
            
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
