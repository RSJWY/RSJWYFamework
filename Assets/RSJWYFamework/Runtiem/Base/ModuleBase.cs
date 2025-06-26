using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.RSJWYFamework.Runtiem.Base
{
    public abstract class ModuleBase : IModule
    {
        public abstract void Initialize();
        public abstract void ModuleFixedUpdate();
        public abstract void ModuleLateUpdate();
        public abstract void ModuleSecondUpdate();
        public abstract void ModuleUpdate();
        public abstract void Shutdown();
    }

}