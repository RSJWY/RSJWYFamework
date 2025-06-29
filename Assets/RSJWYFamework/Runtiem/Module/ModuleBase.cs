using UnityEngine;

namespace RSJWYFamework.Runtiem
{
    public abstract class ModuleBase :MonoBehaviour, IModule
    {
        public abstract void Initialize();
        public abstract void ModuleUpdate();
        public virtual void ModulePerSecondUpdate(){}
        public virtual void ModuleFixedUpdate(){}
        public virtual void ModuleLateUpdate(){}
        public abstract void Shutdown();
    }

}