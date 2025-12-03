using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    private int x, y;
    private int wid;
    private int hei;
    // 是否在获得应用焦点时自动捕获并锁定鼠标
    public bool autoCaptureMouse = true;
    // 当前鼠标是否处于捕获锁定状态
    public bool isMouseCaptured = false;
    private void Awake()
    {
        ParseXML();
        
        CWinScreen.HandlerInit();
        CWinScreen.SetWindsPos(CWinScreen.windowHandle, x, y, wid, hei);
        if (autoCaptureMouse)
        {
            CaptureMouse();
        }
    }
    protected virtual void ParseXML()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "Config.xml");
        XmlDocument xml = new XmlDocument();
        xml.Load(configPath);
        var node = xml.SelectSingleNode("Config");
        var sqlConfig = node.SelectSingleNode("ScreenResolution");
        var xini = sqlConfig.SelectSingleNode("x");
        var yini = sqlConfig.SelectSingleNode("y");
        var width = sqlConfig.SelectSingleNode("Width");
        var height = sqlConfig.SelectSingleNode("Height");
        x = int.Parse(xini.InnerText);
        y = int.Parse(yini.InnerText);
        wid = int.Parse(width.InnerText);
        hei = int.Parse(height.InnerText);
        Debug.Log(string.Format("{0}  {1}  {2}  {3} ", x, y, wid, hei));
    }

    // 在获得/失去应用焦点时自动处理鼠标捕获
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!autoCaptureMouse) return;
        if (hasFocus)
        {
            CaptureMouse();
        }
        else
        {
            ReleaseMouse();
        }
    }

    // 捕获并锁定鼠标使用权（隐藏光标，锁定到窗口中心）
    public void CaptureMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isMouseCaptured = true;
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR
        CWinScreen.BringToFront();
        CWinScreen.CaptureMouseToWindow();
        #endif
    }

    // 释放鼠标使用权（显示光标，取消锁定）
    public void ReleaseMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isMouseCaptured = false;
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR
        CWinScreen.ReleaseMouseCapture();
        #endif
    }

    // 手动设置是否捕获鼠标（供外部脚本调用）
    public void SetMouseCapture(bool capture)
    {
        if (capture)
        {
            CaptureMouse();
        }
        else
        {
            ReleaseMouse();
        }
    }
}
