using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 热更管理器
    /// </summary>
    [Module()]
    [ModuleDependency(typeof(YooAssetManager))][ModuleDependency(typeof(StateMachineManager))]
    [ModuleDependency(typeof(AppAsyncOperationSystem))][ModuleDependency(typeof(DataManager))]
    [ModuleDependency(typeof(EventManager))]
    public class HybirdCLRManager:ModuleBase
    {
        public async UniTask LoadHotCodeDLL()
        {
            var op = new LoadHotCodeAsyncOperation();
            await op.ToUniTask();
        }

       

        public override void Initialize()
        {
        }

        public override void Shutdown()
        {
        }

        public override void LifeUpdate()
        {
        }
    }
}