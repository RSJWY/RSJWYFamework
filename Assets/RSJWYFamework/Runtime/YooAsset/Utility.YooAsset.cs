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

            public static void Setting(
                string hostServerIP = "测试工程", string projectName = "测试软件",
                string appName = "测试软件", string appVersion = "v1.0",  
                EOverwriteInstallClearMode installClearMode = EOverwriteInstallClearMode.None,
                int timeout = 30, bool appendTimeTicks = false, 
                int downloadingMaxNum = 10, int failedTryAgainNum = 3,
                EFileClearMode fileClearMode = EFileClearMode.ClearAllBundleFiles,
                List<string> clearLocations = null, List<string> clearTags = null)
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

            /// <summary>
            /// 远端资源地址查询服务类
            /// </summary>
            public class RemoteServices : IRemoteServices
            {
                private readonly string _defaultHostServer;
                private readonly string _fallbackHostServer;

                public RemoteServices(string defaultHostServer, string fallbackHostServer)
                {
                    _defaultHostServer = defaultHostServer;
                    _fallbackHostServer = fallbackHostServer;
                }

                string IRemoteServices.GetRemoteMainURL(string fileName)
                {
                    return $"{_defaultHostServer}/{fileName}";
                }

                string IRemoteServices.GetRemoteFallbackURL(string fileName)
                {
                    return $"{_fallbackHostServer}/{fileName}";
                }
            }

            /// <summary>
            /// 资源文件流加载解密类
            /// </summary>
            public class AppHotPackageFileDecryption : IDecryptionServices
            {

                //TODO:需要对这里加解密深度优化一下
                public DecryptResult LoadAssetBundle(DecryptFileInfo fileInfo)
                {
                    AppLogger.Log($"解密文件：{fileInfo.BundleName}");
                    DecryptResult decryptResult = new DecryptResult();
                    byte[] AESFileData = File.ReadAllBytes(fileInfo.FileLoadPath);
                    byte[] fileData = AESTool.AESDecrypt(AESFileData,"");
                    decryptResult.Result = AssetBundle.LoadFromMemory(fileData);
                    return decryptResult;
                }

                public DecryptResult LoadAssetBundleAsync(DecryptFileInfo fileInfo)
                {
                    AppLogger.Log($"解密文件：{fileInfo.BundleName}");
                    DecryptResult decryptResult = new DecryptResult();
                    byte[] AESFileData = File.ReadAllBytes(fileInfo.FileLoadPath);
                    byte[] fileData = AESTool.AESDecrypt(AESFileData, "");
                    decryptResult.Result = AssetBundle.LoadFromMemory(fileData);
                    return decryptResult;
                }

                public DecryptResult LoadAssetBundleFallback(DecryptFileInfo fileInfo)
                {
                    return new DecryptResult();
                }

                /// <summary>
                /// 获取加密过的Data
                /// </summary>
                public byte[] ReadFileData(DecryptFileInfo fileInfo)
                {
                    AppLogger.Log($"解密文件{fileInfo.BundleName}");
                    byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
                    return AESTool.AESEncrypt(fileData,"");
                }

                /// <summary>
                /// 获取加密过的Text
                /// </summary>
                public string ReadFileText(DecryptFileInfo fileInfo)
                {
                    byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
                    var DData = AESTool.AESEncrypt(fileData,"");
                    return Encoding.UTF8.GetString(DData);
                }
            }
            /// <summary>
            /// 资源清单数据处理
            /// </summary>
            public class AppHotPackgaeManifestProcessServices:IManifestProcessServices
            {
                public byte[] ProcessManifest(byte[] fileData)
                {
                    return fileData;
                }
            }
            /// <summary>
            /// 资源清单数据处理
            /// </summary>
            public class AppHotPackgaeManifestRestoreServices:IManifestRestoreServices
            {

                public byte[] RestoreManifest(byte[] fileData)
                {
                    return fileData;
                }
            }
            
            #if UNITY_EDITOR
            /// <summary>
            /// 加密资源包-原生资源
            /// </summary>
            public class EncryptRF : IEncryptionServices
            {
                private string aeskey;

                public EncryptRF() : base()
                {
                    aeskey = Resources.Load<AppConfig>("AppConfig").AESKey;
                }

                public EncryptResult Encrypt(EncryptFileInfo fileInfo)
                {
                    // 注意：针对特定规则加密
                    if (fileInfo.BundleName.Contains("_HoteCodeEncryptionUse"))
                    {
                        AppLogger.Log($"加密文件{fileInfo.BundleName}");
                        byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
                        var edata =AESTool.AESEncrypt(fileData, aeskey);
                        EncryptResult result = new EncryptResult
                        {
                            Encrypted = true,
                            EncryptedData = edata
                        };
                        return result;
                    }
                    else
                    {
                        return new EncryptResult
                        {
                            Encrypted = false
                        };
                    }
                }
            }

            /// <summary>
            /// 加密资源包-资源文件
            /// </summary>
            public class EncryptPF : IEncryptionServices
            {
                private string aeskey;

                public EncryptPF() : base()
                {
                    aeskey = Resources.Load<AppConfig>("AppConfig").AESKey;
                }

                public EncryptResult Encrypt(EncryptFileInfo fileInfo)
                {
                    // 注意：针对特定规则加密
                    AppLogger.Log($"加密文件{fileInfo.BundleName}");
                    byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);

                    var edata = AESTool.AESEncrypt(fileData, aeskey);

                    return new EncryptResult
                    {
                        Encrypted = true,
                        EncryptedData = edata
                    };
                }
            }
            #endif
        }
    }
}