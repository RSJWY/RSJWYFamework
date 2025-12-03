using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

public class ScreenResolutionManager : MonoBehaviour
{
    
    private void Awake()
    {
        ParseXML();
    }
    protected virtual void ParseXML()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "Config.xml");
        XmlDocument xml = new XmlDocument();
        xml.Load(configPath);
        var node = xml.SelectSingleNode("Config");
        var sqlConfig = node.SelectSingleNode("ScreenResolution");
        var width = sqlConfig.SelectSingleNode("Width");
        var height = sqlConfig.SelectSingleNode("Height");
        var fullScr = sqlConfig.SelectSingleNode("FullScreen");
        int wid = int.Parse(width.InnerText);
        int hei = int.Parse(height.InnerText);
        FullScreenMode value = (FullScreenMode)Enum.Parse(typeof(FullScreenMode), fullScr.InnerText);
        Screen.SetResolution(wid, hei, value);
    }
}
