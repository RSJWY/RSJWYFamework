using UnityEngine;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 需要挂到场景的模块基类
    /// </summary>
    public abstract class ModuleBase : MonoBehaviour, IModule
    {
        public virtual int Priority => 99;
        public abstract void Initialize();
        public abstract void Shutdown();
        public abstract void LifeUpdate();

        public virtual void LifePerSecondUpdate()
        {
        }

        public virtual void LifeFixedUpdate()
        {
        }

        public virtual void LifeLateUpdate()
        {
        }
    }

}