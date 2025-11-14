using UnityEngine;

namespace RSJWYFamework.Runtime
{
    
    public abstract class SingletonBaseMono<T>:MonoBehaviour where T : SingletonBaseMono<T>
    {
        private static T _instance;
        private static bool _applicationIsQuitting = false;
        private static readonly object _lock = new object();
        
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[{typeof(T)}] 实例在应用程序退出时被访问，返回null");
                    return null;
                }
                
                if (_instance != null) return _instance;
                _instance = FindFirstObjectByType<T>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(T).ToString() + " [SingletonMono]");
                    _instance = singletonObject.AddComponent<T>();
                    // 确保单例在场景加载时不被销毁
                    DontDestroyOnLoad(singletonObject);
                }
#if UNITY_EDITOR
                CheckThreadID();
#endif
               
                return _instance;
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// 主线程ID
        /// </summary>
        private static int _mainThreadId=1;
        private static int _nowThreadId;
        
        private static void CheckThreadID()
        {
            _nowThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (_mainThreadId!=_nowThreadId)
            {
                Debug.LogWarning($"警告！！您正在通过非Unity主线程访问单例！！请注意线程安全！");
            }
        }

        #endif
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[{typeof(T)}] 发现多个实例，销毁多余的实例");
                Destroy(gameObject);
            }
            else
            {
                _instance = (T)this;
                DontDestroyOnLoad(gameObject);
            }
#if UNITY_EDITOR
            //记录主线程ID
            _mainThreadId= System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
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