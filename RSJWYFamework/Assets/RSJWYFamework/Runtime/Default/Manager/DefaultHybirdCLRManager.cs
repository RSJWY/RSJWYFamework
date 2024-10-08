using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using RSJWYFamework.Runtime.HybridCLR.AsyncOperation;
using RSJWYFamework.Runtime.Module;

namespace RSJWYFamework.Runtime.HybridCLR
{
    /// <summary>
    /// 热更管理器
    /// </summary>
    public class DefaultHybirdClrManager:IModule
    {
        /// <summary>
        /// 加载到的程序集
        /// </summary>
        private static Dictionary<string, Assembly> HotCode = new();
        
        public void Init()
        {
            
        } 
        public async UniTask LoadHotCodeDLL()
        {
            var op = new LoadHotCodeOperation();
            Main.Main.RAsyncOperationSystem.StartOperation(string.Empty,op);
            await op.UniTask;
            HotCode = op.HotCode;
        }

       

        public void Close()
        {
            
        }
    }
}