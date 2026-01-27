using RSJWYFamework.Runtime;
using UnityEngine;
using Utils;

namespace License
{
    public static class LicenseManager
    {
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void CheckLicense()
        {
            if (!LicenseVerifier.Instance.IsLicenseValid())
            {
                var content = $"授权错误！请联系金东数创处理！\n机器码：{LicenseVerifier.Instance.GetMachineCode()}\n{LicenseVerifier.Instance.statusMessage}";
                SystemPopup.Show(content, "授权错误！", () =>
                {
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #else
                    Application.Quit();
                    #endif
                }, true); // 启用自动复制
            }
            else
            {
                //var start = DateTimeOffset.FromUnixTimeSeconds(LicenseVerifier.Instance.currentLicense.start_timestamp).ToLocalTime();
                //var expire = DateTimeOffset.FromUnixTimeSeconds(LicenseVerifier.Instance.currentLicense.expire_timestamp).ToLocalTime();
                AppLogger.Log($"检查通过！");
            }
        }
    }
}