using System.IO;
using System.Text;
using UnityEngine;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 实现YooAssets接口
    /// </summary>
    public class IYooAssets
    {
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
                byte[] fileData = Utility.AESTool.AESDecrypt(AESFileData, "");
                decryptResult.Result = AssetBundle.LoadFromMemory(fileData);
                return decryptResult;
            }

            public DecryptResult LoadAssetBundleAsync(DecryptFileInfo fileInfo)
            {
                AppLogger.Log($"解密文件：{fileInfo.BundleName}");
                DecryptResult decryptResult = new DecryptResult();
                byte[] AESFileData = File.ReadAllBytes(fileInfo.FileLoadPath);
                byte[] fileData = Utility.AESTool.AESDecrypt(AESFileData, "");
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
                return Utility.AESTool.AESEncrypt(fileData, "");
            }

            /// <summary>
            /// 获取加密过的Text
            /// </summary>
            public string ReadFileText(DecryptFileInfo fileInfo)
            {
                byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
                var DData = Utility.AESTool.AESEncrypt(fileData, "");
                return Encoding.UTF8.GetString(DData);
            }
        }

        /// <summary>
        /// 资源清单数据处理
        /// </summary>
        public class AppHotPackgaeManifestProcessServices : IManifestProcessServices
        {
            public byte[] ProcessManifest(byte[] fileData)
            {
                return fileData;
            }
        }

        /// <summary>
        /// 资源清单数据处理
        /// </summary>
        public class AppHotPackgaeManifestRestoreServices : IManifestRestoreServices
        {
            public byte[] RestoreManifest(byte[] fileData)
            {
                return fileData;
            }
        }
    }
}