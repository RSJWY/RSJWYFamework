using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 热更管理器
    /// </summary>
    [Module(10)]
    public class HybirdCLRManager:ModuleBase
    {
        /// <summary>
        /// 加载到的程序集
        /// </summary>
        private static Dictionary<string, Assembly> HotCode = new();
        
        public async UniTask LoadHotCodeDLL()
        {
            var op = new LoadHotCodeAsyncOperation(this);
            await op.UniTask();
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