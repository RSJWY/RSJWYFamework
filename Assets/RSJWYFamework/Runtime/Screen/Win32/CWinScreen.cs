using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// 🐶 Little Code Sauce Warning: This is a static helper now! No more MonoBehaviour overhead!
public static class CWinScreen
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
    private const string UnityWindowClassName = "UnityWndClass";
    public static IntPtr windowHandle = IntPtr.Zero;
    public static uint SWP_SHOWWINDOW = 0x0040;
    public static IntPtr HWND_TOPMOST = new IntPtr(-1);
    const UInt32 SWP_NOSIZE = 0x0001;
    const UInt32 SWP_NOMOVE = 0x0002;
    const int GWL_STYLE = -16;
    const int WS_BorDER = 1;

    [DllImport("user32.dll")] static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, int dwNewLong);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern bool SetWindowText(IntPtr hwnd, String lpString);
    [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Ansi)] public static extern IntPtr FindWindow(string className, string windowName);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)] static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern IntPtr SetCapture(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] static extern bool ClipCursor(ref RECT lpRect);
    [DllImport("user32.dll")] static extern bool ClipCursor(IntPtr lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public delegate bool EnumThreadWindowsCallback(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool EnumWindows(EnumThreadWindowsCallback callback, IntPtr extraData);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadWindowsCallback lpEnumFunc, IntPtr lParam);

    // Cache the delegate to avoid GC allocation on every call
    private static EnumThreadWindowsCallback _cachedCallback;
    // 🐶 Little Code Sauce Optimization: Cached StringBuilder to prevent GC in EnumWindowsCallback!
    private static readonly StringBuilder _cachedStringBuilder = new StringBuilder(256);

    public static void ChangeApp()
    {
        HWND_TOPMOST = new IntPtr(-1);
    }

    [MonoPInvokeCallback(typeof(EnumThreadWindowsCallback))]
    private static bool EnumWindowsCallback(IntPtr handle, IntPtr extraParameter)
    {
        const string wndClass = "UnityWndClass";
        // Reset StringBuilder
        _cachedStringBuilder.Length = 0;
        GetClassName(handle, _cachedStringBuilder, _cachedStringBuilder.Capacity);

        if (_cachedStringBuilder.ToString() == wndClass)
        {
            windowHandle = handle;
#if UNITY_EDITOR
            Debug.Log(string.Format("GetClassName: {0},{1}", _cachedStringBuilder, windowHandle));
#endif
            return false;
        }
        else
        {
            return true;
        }
    }

    public static void HandlerInit()
    {
        uint threadId = GetCurrentThreadId();
        if (_cachedCallback == null)
        {
            _cachedCallback = new EnumThreadWindowsCallback(EnumWindowsCallback);
        }
        EnumThreadWindows(threadId, _cachedCallback, IntPtr.Zero);
    }

    public static bool SetWindsPos(IntPtr hander, int x, int y, int width, int height)
    {
        SetWindowLong(hander, GWL_STYLE, WS_BorDER);
        bool result = SetWindowPos(windowHandle, HWND_TOPMOST, x, y, width, height, SWP_SHOWWINDOW);
        return result;
    }

    // 将窗口置于前台（尝试获取键鼠焦点）
    public static bool BringToFront()
    {
        if (windowHandle == IntPtr.Zero)
        {
            HandlerInit();
        }
        return SetForegroundWindow(windowHandle);
    }

    // 捕获鼠标到当前窗口并限制光标移动范围到窗口区域
    public static void CaptureMouseToWindow()
    {
        if (windowHandle == IntPtr.Zero)
        {
            HandlerInit();
        }
        SetCapture(windowHandle);
        if (GetWindowRect(windowHandle, out RECT rect))
        {
            ClipCursor(ref rect);
        }
    }

    // 释放鼠标捕获并取消光标范围限制
    public static void ReleaseMouseCapture()
    {
        ReleaseCapture();
        ClipCursor(IntPtr.Zero);
    }

    public static void InitCmdArgs()
    {
        // 🐶 Little Code Sauce Safe-Guard: Using TryParse to avoid crashes!
        string title = GetArg("-title");
        string xStr = GetArg("-winX");
        string yStr = GetArg("-winY");
        string wStr = GetArg("-resX");
        string hStr = GetArg("-resY");

        // Defaults or just ignore if fail (assuming Config handles defaults if these are missing/invalid)
        int x = 0, y = 0, width = 0, height = 0;
        bool hasPos = int.TryParse(xStr, out x) && int.TryParse(yStr, out y);
        bool hasRes = int.TryParse(wStr, out width) && int.TryParse(hStr, out height);

        if (!string.IsNullOrEmpty(title))
        {
             SetWindowText(windowHandle, title);
        }

        if (hasPos && hasRes)
        {
             Debug.LogFormat("window x:{0}-y:{1}-w:{2}-h:{3}-title:{4}-handle:{5}", x, y, width, height, title, windowHandle);
             SetWindowPos(windowHandle, HWND_TOPMOST, x, y, width, height, SWP_SHOWWINDOW);
        }
    }

    public static string GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }
        return null;
    }
#endif
}
