using UnityEngine;

namespace RSJWYFamework.Runtiem.Module
{
    public abstract class ModuleBase :MonoBehaviour, IModule
    {
        public abstract void Initialize();
        public abstract void ModuleUpdate();
        public abstract void ModuleSecondUpdate();
        public abstract void ModuleFixedUpdate();
        public abstract void ModuleLateUpdate();
        public abstract void Shutdown();
    }

}