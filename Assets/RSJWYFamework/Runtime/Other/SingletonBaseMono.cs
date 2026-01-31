using UnityEngine;

namespace RSJWYFamework.Runtime
{
    [DisallowMultipleComponent]
    public abstract class SingletonBaseMono<T> : MonoBehaviour where T : SingletonBaseMono<T>
    {
        private static T _instance;
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// 是否为全局单例（默认为 true）
        /// 如果为 true，则会自动调用 DontDestroyOnLoad
        /// </summary>
        protected virtual bool IsGlobal => true;

        /// <summary>
        /// 初始化方法，在 Awake 中初始化单例后调用
        /// 请在子类重写此方法替代 Awake 进行初始化
        /// </summary>
        protected virtual void OnInitialize() { }

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    // 避免在退出时创建新的单例导致报错
                    return null;
                }

                if (_instance == null)
                {
                    // 优先在场景中查找现有实例 (Unity 2023+ 推荐 API)
                    _instance = FindFirstObjectByType<T>();
                    
                    if (_instance == null)
                    {
                        // 如果场景中不存在，则自动创建一个新的
                        GameObject singletonObject = new GameObject(typeof(T).ToString() + " [Singleton]");
                        _instance = singletonObject.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // 如果已经存在实例，销毁当前的重复对象，保证单例唯一性
                Destroy(gameObject);
                return;
            }
            
            _instance = (T)this;
            
            // 如果是全局单例且是根节点，则设置为不销毁
            if (IsGlobal && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            // 执行子类初始化逻辑
            OnInitialize();
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
