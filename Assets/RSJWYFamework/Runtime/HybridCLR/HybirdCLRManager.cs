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
    [ModuleDependency(typeof(EventManager))][ModuleDependency(typeof(EventManager))]
    public class HybirdCLRManager:ModuleBase
    {
        /// <summary>
        /// 加载到的程序集
        /// </summary>
        private static Dictionary<string, Assembly> HotCode = new();
        
        public async UniTask LoadHotCodeDLL()
        {
            var op = new LoadHotCodeAsyncOperation(this);
            await op.ToUniTask();
            HotCode = op.HotCode;
        }

       

        public override void Initialize()
        {
            HotCode.Clear();
        }

        public override void Shutdown()
        {
            HotCode.Clear();
        }

        public override void LifeUpdate()
        {
        }
    }
}