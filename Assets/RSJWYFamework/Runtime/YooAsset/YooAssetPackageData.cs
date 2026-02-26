using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    ///包信息参数
    /// </summary>
    [Serializable]
    public class YooAssetPackageData:DataBase
    {
        [LabelText("包名")]
        [Required("必须输入包名称，程序不做检测")] 
        public string packageName;
        [LabelText("包说明")]
        public string packageDesc;
        
        
        [BoxGroup("清理配置")]
        [LabelText("文件清理模式")]
        public EFileClearMode fileClearMode = EFileClearMode.ClearAllBundleFiles;
        
        [BoxGroup("清理配置")]
        [LabelText("指定清理标签列表")]
        [ShowIf("fileClearMode", EFileClearMode.ClearBundleFilesByTags)]
        public List<string> clearTags = new List<string>();
        
        [BoxGroup("清理配置")]
        [LabelText("指定清理路径列表")]
        [ShowIf("fileClearMode", EFileClearMode.ClearBundleFilesByLocations)]
        // TODO: 当模式为 ClearBundleFilesByLocations 时，需要在这里填写具体的资源路径
        public List<string> clearLocations = new List<string>();
    }
}