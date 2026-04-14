using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RSJWYFamework.Runtime.UI
{
    /// <summary>
    /// 默认的 Resources 加载器
    /// </summary>
    public class ResourcesUIResLoader : IUIResLoader
    {
        // 资源加载路径前缀 (Resources/UIPrefab/...)
        private const string ROOT_PATH = "UIPrefab/";

        public async UniTask<GameObject> LoadWindowAsync(string path)
        {
            // 拼接完整路径
            string fullPath = ROOT_PATH + path;
            
            // 使用 ResourceRequest 异步加载
            ResourceRequest request = Resources.LoadAsync<GameObject>(fullPath);
            
            await request.ToUniTask();

            if (request.asset == null)
            {
                // 尝试直接加载 (兼容不带前缀的旧代码)
                request = Resources.LoadAsync<GameObject>(path);
                await request.ToUniTask();
            }

            return request.asset as GameObject;
        }

        public void UnloadWindow(string path)
        {
            // Resources.UnloadUnusedAssets() is heavy and global.
            // 对于 Resources 模式，单个卸载比较难，通常依赖引用计数或全局清理。
            // 这里暂时留空，或者可以手动 Destroy 实例。
            // 真正的卸载逻辑会在切换到 YooAsset 时大放异彩！
        }
    }
}
