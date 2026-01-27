using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using System.Diagnostics;
using RSJWYFamework.Runtime;
using Debug = UnityEngine.Debug;

namespace License
{
    /// <summary>
    /// License 验证器
    /// 负责加载 License 文件，验证 RSA 签名，并检查机器码和有效期
    /// </summary>
    public class LicenseVerifier : SingletonBaseMono<LicenseVerifier>
    {
        [Header("设置")]
        [Tooltip("License 文件名，默认查找路径为 StreamingAssets 或 PersistentDataPath")]
        public string licenseFileName = "License.lic";
        
        [TextArea(10, 20)]
        [Tooltip("在此处粘贴 XML 格式的公钥 (<RSAKeyValue>...</RSAKeyValue>)")]
        public string publicKeyXML = "";

        [Header("状态")]
        public bool isValid = false;
        public string statusMessage = "";
        public LicensePayload currentLicense;

        // 验证成功或失败的事件
        public event Action OnLicenseValid;
        public event Action<string> OnLicenseInvalid;

        private void Start()
        {
            Debug.Log($"[LicenseVerifier] 当前机器码: {GetMachineCode()}");
            Verify();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                LicenseManager.CheckLicense();
            }
        }

        /// <summary>
        /// 外部调用接口：检查 License 是否有效
        /// </summary>
        /// <returns>有效返回 true，无效返回 false</returns>
        public bool IsLicenseValid()
        {
            Verify(); // 每次调用时重新验证一次，防止运行期间状态被篡改或过期
            return isValid;
        }

        /// <summary>
        /// 执行验证流程
        /// </summary>
        [ContextMenu("验证 License")]
        public void Verify()
        {
            isValid = false;
            statusMessage = "正在检查...";

            // 1. 确定 License 文件路径
            // 优先检查 StreamingAssets
            string path = Path.Combine(Application.streamingAssetsPath, licenseFileName);
            if (!File.Exists(path))
            {
                // 如果没有，检查 PersistentDataPath (通常用于热更新或手动放置)
                path = Path.Combine(Application.persistentDataPath, licenseFileName);
                if (!File.Exists(path))
                {
                    // 最后检查是否是绝对路径
                    if (File.Exists(licenseFileName))
                    {
                        path = licenseFileName;
                    }
                    else
                    {
                        statusMessage = $"未找到 License 文件，路径: {path} 或 {licenseFileName}";
                        Debug.LogError(statusMessage);
                        OnLicenseInvalid?.Invoke(statusMessage);
                        return;
                    }
                }
            }

            try
            {
                // 2. 读取并解析文件
                string fileContent = File.ReadAllText(path);
                
                // Python 脚本生成的是 Base64 编码的 Blob，所以需要先解码一次
                string finalJson = Encoding.UTF8.GetString(Convert.FromBase64String(fileContent));
                
                // 反序列化为外层包装结构 (包含 data 和 signature)
                LicenseWrapper wrapper = JsonConvert.DeserializeObject<LicenseWrapper>(finalJson);
                
                if (wrapper == null)
                {
                    statusMessage = "License 格式无效。";
                    Debug.LogError(statusMessage);
                    OnLicenseInvalid?.Invoke(statusMessage);
                    return;
                }

                // 3. 验证签名
                if (VerifySignature(wrapper.data, wrapper.signature))
                {
                    // 签名验证通过，解码内部的数据负载
                    string payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(wrapper.data));
                    currentLicense = JsonConvert.DeserializeObject<LicensePayload>(payloadJson);
                    
                    // 4. 验证业务逻辑 (机器码、有效期)
                    if (ValidatePayload(currentLicense))
                    {
                        isValid = true;
                        statusMessage = "License 有效。";
                        Debug.Log($"[LicenseVerifier] {statusMessage}");
                        OnLicenseValid?.Invoke();
                    }
                    else
                    {
                        // 业务逻辑验证失败 (机器码不匹配或过期)
                        OnLicenseInvalid?.Invoke(statusMessage);
                    }
                }
                else
                {
                    statusMessage = "签名无效，文件可能已被篡改。";
                    Debug.LogError(statusMessage);
                    OnLicenseInvalid?.Invoke(statusMessage);
                }
            }
            catch (Exception e)
            {
                statusMessage = $"验证 License 时出错: {e.Message}";
                Debug.LogError(statusMessage);
                OnLicenseInvalid?.Invoke(statusMessage);
            }
        }

        /// <summary>
        /// 验证 RSA 签名
        /// </summary>
        private bool VerifySignature(string dataBase64, string signatureBase64)
        {
            if (string.IsNullOrEmpty(publicKeyXML))
            {
                Debug.LogError("LicenseVerifier 中缺少公钥 (XML 格式)。");
                return false;
            }

            // 简单的格式检查
            if (!publicKeyXML.Trim().StartsWith("<RSAKeyValue>"))
            {
                Debug.LogError("公钥格式错误：请使用 XML 格式 (<RSAKeyValue>...)。请运行 Python 脚本的 '3. 显示公钥' 选项获取转换后的 XML。");
                return false;
            }

            try
            {
                byte[] payloadBytes = Convert.FromBase64String(dataBase64);
                byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

                // 使用 RSACryptoServiceProvider 并导入 XML，这是 Unity 兼容性最好的方式
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(publicKeyXML);
                    
                    // 注意：Python Crypto 库的 pkcs1_15.sign 使用的是 SHA256 算法
                    return rsa.VerifyData(payloadBytes, SHA256.Create(), signatureBytes);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"签名验证异常: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 验证具体的数据负载 (机器码和有效期)
        /// </summary>
        private bool ValidatePayload(LicensePayload payload)
        {
            // 检查机器码
            string currentMachineCode = GetMachineCode();
            if (payload.machine_code != currentMachineCode)
            {
                statusMessage = $"机器码不匹配。License: {payload.machine_code}, 当前机器: {currentMachineCode}";
                Debug.LogError(statusMessage);
                return false;
            }

            // 检查日期
            DateTime now = DateTime.UtcNow;
            DateTime start = DateTimeOffset.FromUnixTimeSeconds(payload.start_timestamp).UtcDateTime;
            DateTime expire = DateTimeOffset.FromUnixTimeSeconds(payload.expire_timestamp).UtcDateTime;

            if (now < start)
            {
                statusMessage = $"License 尚未生效。开始时间: {start} (UTC)";
                Debug.LogError(statusMessage);
                return false;
            }

            if (now > expire)
            {
                statusMessage = $"License 已过期。过期时间: {expire} (UTC)";
                Debug.LogError(statusMessage);
                return false;
            }
            
            Debug.Log($"License 有效期至: {expire} (UTC)");
            return true;
        }

        private string _cachedMachineCode;
        
        /// <summary>
        /// 获取当前设备的机器码
        /// (改造版：在 Windows 下结合硬件信息生成高复杂度指纹)
        /// </summary>
        public string GetMachineCode()
        {
            if (!string.IsNullOrEmpty(_cachedMachineCode))
            {
                return _cachedMachineCode;
            }

            string rawId = SystemInfo.deviceUniqueIdentifier;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // 在 Windows 平台上，尝试获取更具体的硬件信息混合
            try 
            {
                string cpuId = GetWmicOutput("cpu get processorid");
                string biosId = GetWmicOutput("bios get serialnumber");
                string diskId = GetWmicOutput("diskdrive get serialnumber");
                
                if (!string.IsNullOrEmpty(cpuId)) rawId += $"_CPU:{cpuId}";
                if (!string.IsNullOrEmpty(biosId)) rawId += $"_BIOS:{biosId}";
                if (!string.IsNullOrEmpty(diskId)) rawId += $"_DISK:{diskId}";
            }
            catch (Exception e)
            {
                Debug.LogWarning($"获取硬件信息失败，回退到标准 ID: {e.Message}");
            }
#endif

            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawId));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                _cachedMachineCode = builder.ToString();
                return _cachedMachineCode;
            }
        }

        /// <summary>
        /// 通过 wmic 命令行获取硬件信息 (无需额外 DLL)
        /// </summary>
        private string GetWmicOutput(string arguments)
        {
            try
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "wmic";
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    string[] lines = output.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 1)
                    {
                        return lines[1].Trim();
                    }
                }
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LicenseVerifier] 无法获取硬件信息 ({arguments}): {e.Message}");
            }
            return string.Empty;
        }

        /// <summary>
        /// 辅助功能：将机器码复制到剪贴板 (右键组件菜单)
        /// </summary>
        [ContextMenu("复制机器码到剪贴板")]
        public void CopyMachineCode()
        {
            string code = GetMachineCode();
            GUIUtility.systemCopyBuffer = code;
            Debug.Log($"机器码已复制: {code}");
        }
    }
}
