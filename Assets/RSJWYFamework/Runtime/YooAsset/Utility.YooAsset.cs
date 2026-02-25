using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    public static partial class Utility
    {
        public static class YooAsset
        {
            public static string ProjectName { get; private set; } = "测试工程";
            public static string AppName { get; private set; } = "测试软件";
            public static string HostServerIP  { get; private set; }= "http://127.0.0.1";
            public static string AppVersion { get; private set; } = "v1.0";
            
            public static int Timeout { get; private set; } = 30;
            /// <summary>
            /// 附加时间戳
            /// </summary>
            public static bool AppendTimeTicks  { get; private set; } = false;
            /// <summary>
            /// 最大同时下载数量
            /// </summary>
            public static  int DownloadingMaxNum { get; private set; }= 10;
            /// <summary>
            /// 重试次数
            /// </summary>
            public static int FailedTryAgainNum { get; private set; } = 3;
            /// <summary>
            /// 覆盖安装模式
            /// </summary>
            public static EOverwriteInstallClearMode InstallClearMode { get; private set; } = EOverwriteInstallClearMode.None;
            /// <summary>
            /// 文件清理模式
            /// </summary>
            public static EFileClearMode FileClearMode { get; private set; } = EFileClearMode.ClearAllBundleFiles;
            /// <summary>
            /// 指定清理路径列表
            /// </summary>
            public static List<string> ClearLocations { get; private set; } = new List<string>();
            /// <summary>
            /// 指定清理标签列表
            /// </summary>
            public static List<string> ClearTags { get; private set; } = new List<string>();

            /// <summary>
            /// 运行时使用的包
            /// </summary>
            public static BuildEnvironment BuildEnv;

            public static void Setting(
                string hostServerIP = "测试工程", string projectName = "测试软件",
                string appName = "测试软件", string appVersion = "v1.0",  
                EOverwriteInstallClearMode installClearMode = EOverwriteInstallClearMode.None,
                int timeout = 30, bool appendTimeTicks = false, 
                int downloadingMaxNum = 10, int failedTryAgainNum = 3,
                EFileClearMode fileClearMode = EFileClearMode.ClearAllBundleFiles,
                List<string> clearLocations = null, List<string> clearTags = null,
                BuildEnvironment buildEnv=BuildEnvironment.Dev)
            {
                ProjectName = projectName;
                AppName = appName;
                HostServerIP = hostServerIP;
                AppVersion = appVersion;
                Timeout = timeout;
                AppendTimeTicks = appendTimeTicks;
                FailedTryAgainNum = failedTryAgainNum;
                DownloadingMaxNum = downloadingMaxNum;
                InstallClearMode = installClearMode;
                FileClearMode = fileClearMode;
                ClearLocations = clearLocations ?? new List<string>();
                ClearTags = clearTags ?? new List<string>();
                BuildEnv= buildEnv;
            }

            /// <summary>
            /// 获取资源服务器地址
            /// </summary>
            /// <param name="packageName">包名</param>
            /// <returns></returns>
            public static string GetHostServerURL(string packageName)
            {
                return $"{HostServerIP}/{ProjectName}/{AppName}/{AppVersion}/{packageName}";
            }
            
            
            
            public enum BuildEnvironment
            {
                /// <summary>
                /// 开发版
                /// </summary>
                Dev,
                /// <summary>
                /// 测试版
                /// </summary>
                Test,
                /// <summary>
                /// 发行版
                /// </summary>
                Prod
            }

        }
    }
}