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
            
            public static bool AppendTimeTicks  { get; private set; } = false;
            
            public static  int DownloadingMaxNum { get; private set; }= 10;
            public static int FailedTryAgainNum { get; private set; } = 3;

            public static void Setting(
                string hostServerIP = "测试工程", string projectName = "测试软件",
                string appName = "测试软件", string appVersion = "v1.0",  
                int timeout = 30, bool appendTimeTicks = false, int downloadingMaxNum = 10, int failedTryAgainNum = 3)
            {
                ProjectName = projectName;
                AppName = appName;
                HostServerIP = hostServerIP;
                AppVersion = appVersion;
                Timeout = timeout;
                AppendTimeTicks = appendTimeTicks;
                FailedTryAgainNum = failedTryAgainNum;
                DownloadingMaxNum = downloadingMaxNum;
            }

            /// <summary>
            /// 获取资源服务器地址
            /// </summary>
            /// <param name="packageName">包名</param>
            /// <returns></returns>
            public static string GetHostServerURL(string packageName)
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
                    return $"{HostServerIP}/{ProjectName}/{AppName}/{AppVersion}/Android/{packageName}";
                else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
                    return $"{HostServerIP}/{ProjectName}/{AppName}/{AppVersion}/IPhone/{packageName}";
                else
                    return $"{HostServerIP}/{ProjectName}/{AppName}/{AppVersion}/PC/{packageName}";
#else
        if (Application.platform == RuntimePlatform.Android)
            return $"{HostServerIP}/{ProjectName}/{AppName}/{AppVersion}/Android/{packageName}";
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
            return $"{HostServerIP}/{ProjectName}/{AppName}/{AppVersion}/IPhone/{packageName}";
        else
            return $"{HostServerIP}/{ProjectName}/{AppName}/{AppVersion}/PC/{packageName}";
#endif
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