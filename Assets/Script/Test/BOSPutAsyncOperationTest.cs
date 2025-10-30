using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;
using RSJWYFamework.Runtime;

namespace Script.Test
{
    /// <summary>
    /// BOS上传异步操作测试类
    /// </summary>
    public class BOSPutAsyncOperationTest : MonoBehaviour
    {
        [Header("BOS配置参数")]
        [SerializeField] private string accessKeyId = "your_access_key_id";
        [SerializeField] private string secretAccessKey = "your_secret_access_key";
        [SerializeField] private string bucketName = "your_bucket_name";
        [SerializeField] private string region = "bj"; // 区域，如bj、gz等
        
        [Header("上传配置")]
        [SerializeField] private string objectKey = "test/upload_test.txt";
        [SerializeField] private string storageClass = "STANDARD";
        
        [Header("测试控制")]
        [SerializeField] private bool autoCreateTestFile = true;
        [SerializeField] private string testFileName = "test_upload_file.txt";
        
        private string testFilePath;
        
        void Start()
        {
            // 设置测试文件路径
            testFilePath = Path.Combine(Application.persistentDataPath, testFileName);
            
            // 如果启用自动创建测试文件，则创建一个测试文件
            if (autoCreateTestFile)
            {
                CreateTestFile();
            }
        }
        
        /// <summary>
        /// 创建测试文件
        /// </summary>
        private void CreateTestFile()
        {
            try
            {
                string testContent = $"BOS上传测试文件\n创建时间: {System.DateTime.Now}\n测试内容: Hello BOS Upload Test!";
                File.WriteAllText(testFilePath, testContent);
                Debug.Log($"测试文件已创建: {testFilePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"创建测试文件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 开始上传测试 - 通过Inspector按钮调用
        /// </summary>
        [ContextMenu("开始上传测试")]
        public async void StartUploadTest()
        {
            await TestUploadFile();
        }
        
        /// <summary>
        /// 测试文件上传
        /// </summary>
        public async UniTask TestUploadFile()
        {
            Debug.Log("=== BOS上传测试开始 ===");
            
            // 验证配置参数
            if (!ValidateConfiguration())
            {
                Debug.LogError("配置参数验证失败，请检查BOS配置");
                return;
            }
            
            // 检查测试文件是否存在
            if (!File.Exists(testFilePath))
            {
                Debug.LogError($"测试文件不存在: {testFilePath}");
                if (autoCreateTestFile)
                {
                    CreateTestFile();
                    if (!File.Exists(testFilePath))
                    {
                        Debug.LogError("无法创建测试文件");
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            
            try
            {
                Debug.Log($"开始上传文件: {testFilePath}");
                Debug.Log($"目标路径: bos://{bucketName}/{objectKey}");
                
                // 调用BOS上传方法
                string result = await BOSAsync.UploadFileAsync(
                    accessKeyId, 
                    secretAccessKey, 
                    bucketName, 
                    region, 
                    objectKey, 
                    testFilePath, 
                    storageClass
                );
                
                // 输出结果
                if (result.Contains("上传成功"))
                {
                    Debug.Log($"<color=green>上传成功!</color>\n{result}");
                }
                else
                {
                    Debug.LogError($"上传失败:\n{result}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"上传过程中发生异常: {ex.Message}\n{ex.StackTrace}");
            }
            
            Debug.Log("=== BOS上传测试结束 ===");
        }
        
        /// <summary>
        /// 验证配置参数
        /// </summary>
        private bool ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(accessKeyId) || accessKeyId == "your_access_key_id")
            {
                Debug.LogError("请设置有效的Access Key ID");
                return false;
            }
            
            if (string.IsNullOrEmpty(secretAccessKey) || secretAccessKey == "your_secret_access_key")
            {
                Debug.LogError("请设置有效的Secret Access Key");
                return false;
            }
            
            if (string.IsNullOrEmpty(bucketName) || bucketName == "your_bucket_name")
            {
                Debug.LogError("请设置有效的Bucket名称");
                return false;
            }
            
            if (string.IsNullOrEmpty(region))
            {
                Debug.LogError("请设置有效的区域");
                return false;
            }
            
            if (string.IsNullOrEmpty(objectKey))
            {
                Debug.LogError("请设置有效的对象键");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 测试上传不同类型的文件
        /// </summary>
        [ContextMenu("测试上传图片文件")]
        public async void TestUploadImageFile()
        {
            // 创建一个简单的测试图片文件（实际项目中可以使用真实图片）
            string imagePath = Path.Combine(Application.persistentDataPath, "test_image.png");
            
            // 这里只是创建一个假的PNG文件用于测试
            // 实际使用时应该使用真实的图片文件
            try
            {
                byte[] fakeImageData = System.Text.Encoding.UTF8.GetBytes("fake png data for testing");
                File.WriteAllBytes(imagePath, fakeImageData);
                
                string imageObjectKey = "images/test_image.png";
                
                Debug.Log($"开始上传图片文件: {imagePath}");
                
                string result = await BOSAsync.UploadFileAsync(
                    accessKeyId, 
                    secretAccessKey, 
                    bucketName, 
                    region, 
                    imageObjectKey, 
                    imagePath, 
                    storageClass
                );
                
                if (result.Contains("上传成功"))
                {
                    Debug.Log($"<color=green>图片上传成功!</color>\n{result}");
                }
                else
                {
                    Debug.LogError($"图片上传失败:\n{result}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"图片上传测试异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清理测试文件
        /// </summary>
        [ContextMenu("清理测试文件")]
        public void CleanupTestFiles()
        {
            try
            {
                if (File.Exists(testFilePath))
                {
                    File.Delete(testFilePath);
                    Debug.Log($"已删除测试文件: {testFilePath}");
                }
                
                string imagePath = Path.Combine(Application.persistentDataPath, "test_image.png");
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    Debug.Log($"已删除测试图片: {imagePath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"清理测试文件失败: {ex.Message}");
            }
        }
        
        void OnDestroy()
        {
            // 可选：在对象销毁时清理测试文件
            // CleanupTestFiles();
        }
    }
}