using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RSJWYFamework.Runtime.UI
{
    /// <summary>
    /// UI 资源加载器接口
    /// 为了未来无缝切换 YooAsset/Addressables 而生的抽象层！( •̀ ω •́ )y
    /// </summary>
    public interface IUIResLoader
    {
        /// <summary>
        /// 异步加载 UI 预制体
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>加载到的 GameObject 预制体</returns>
        UniTask<GameObject> LoadWindowAsync(string path);

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="path">资源路径</param>
        void UnloadWindow(string path);
    }
}
