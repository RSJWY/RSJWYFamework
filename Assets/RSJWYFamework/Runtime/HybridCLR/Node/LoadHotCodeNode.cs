using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using HybridCLR;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 加载热更代码 Assembly 节点
    /// </summary>
    public class LoadHotCodeNode : StateNodeBase<LoadHotCodeAsyncOperation>
    {
        public override void OnInit()
        {
        }

        public override void OnClose()
        {
        }

        public override async UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
            AppLogger.Log($"[LoadHotCodeNode] 开始装载程序集...");
            await LoadHotCode();
        }

        private async UniTask LoadHotCode()
        {
            // 从黑板获取数据
            var hotCodeList = (HotCodeDLL)_sm.GetBlackboardValue("LoadList");
            var hotCodeBytesMap = (Dictionary<string, HotCodeBytes>)_sm.GetBlackboardValue("HotcodeDic");
            var aotMetadataMap = (Dictionary<string, byte[]>)_sm.GetBlackboardValue("MFAOTDic");
            
            Dictionary<string, Assembly> loadedAssemblies = new();

            // 注意：HybridCLR 的元数据加载和 Assembly 加载建议在主线程进行，避免潜在的线程安全问题
            
            string currentProcessingAssembly = "";

            try
            {
                // 1. 加载 AOT 补充元数据
                // 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
                // 热更新dll不缺元数据，不需要补充。
                HomologousImageMode mode = HomologousImageMode.SuperSet;
                
                foreach (string aotDllName in hotCodeList.MetadataForAOTAssemblies)
                {
                    currentProcessingAssembly = aotDllName;
                    
                    if (aotMetadataMap.TryGetValue(aotDllName, out byte[] dllBytes))
                    {
                        // 加载assembly对应的dll，会自动为它hook
                        LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
                        if (err != LoadImageErrorCode.OK)
                        {
                            AppLogger.Error($"[LoadHotCodeNode] 加载补充元数据失败: {currentProcessingAssembly}, 错误码: {err}");
                        }
                        else
                        {
                            AppLogger.Log($"[LoadHotCodeNode] 成功加载AOT补充元数据: {currentProcessingAssembly}");
                        }
                    }
                    else
                    {
                         AppLogger.Warning($"[LoadHotCodeNode] 缺失AOT元数据字节流: {currentProcessingAssembly}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                string errorMsg = $"[LoadHotCodeNode] 加载补充元数据时发生异常: {currentProcessingAssembly}, 详情: {ex}";
                AppLogger.Error(errorMsg);
                _sm.Stop(500, errorMsg);
                throw new AppException(errorMsg);
            }

            // 2. 加载热更 DLL
            try
            {
#if UNITY_EDITOR
                // 编辑器模式下，直接从 CurrentDomain 获取已加载的 Assembly
                var currentAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var hotAssetName in hotCodeList.HotCode)
                {
                    currentProcessingAssembly = hotAssetName;
                    var assembly = currentAssemblies.FirstOrDefault(a => a.GetName().Name == hotAssetName);
                    
                    if (assembly != null)
                    {
                        loadedAssemblies.Add(hotAssetName, assembly);
                        AppLogger.Log($"[LoadHotCodeNode] (Editor) 已获取热更程序集: {hotAssetName}");
                    }
                    else
                    {
                        AppLogger.Warning($"[LoadHotCodeNode] (Editor) 无法在 CurrentDomain 找到程序集: {hotAssetName}");
                    }
                }
#else
                // 真机模式下，使用 Assembly.Load 加载字节流
                foreach (var hotAssetName in hotCodeList.HotCode)
                {
                    currentProcessingAssembly = hotAssetName;
                    
                    if (hotCodeBytesMap.TryGetValue(hotAssetName, out var bytesData))
                    {
                        Assembly assembly;
                        if (bytesData.pdbBytes != null && bytesData.pdbBytes.Length > 0)
                        {
                            assembly = Assembly.Load(bytesData.dllBytes, bytesData.pdbBytes);
                        }
                        else
                        {
                            assembly = Assembly.Load(bytesData.dllBytes);
                        }
                        
                        loadedAssemblies.Add(hotAssetName, assembly);
                        AppLogger.Log($"[LoadHotCodeNode] 已加载热更程序集: {hotAssetName}");
                    }
                    else
                    {
                        AppLogger.Error($"[LoadHotCodeNode] 缺失热更DLL字节流: {hotAssetName}");
                    }
                }
#endif
            }
            catch (System.Exception ex)
            {
                string errorMsg = $"[LoadHotCodeNode] 加载热更程序集时发生异常: {currentProcessingAssembly}, 详情: {ex}";
                AppLogger.Error(errorMsg);
                _sm.Stop(500, errorMsg);
                throw new AppException(errorMsg);
            }

            // 清理缓存的字节流，释放内存
            hotCodeBytesMap.Clear();
            aotMetadataMap.Clear();
            
            // 将加载好的 Assembly 存入黑板 (如果后续流程需要) 或者直接存入 Manager
            _sm.SetBlackboardValue("HotCodeAssembly", loadedAssemblies);
            
            // 切换到完成节点
            _sm.SwitchNode<LoadHotCodeDoneNode>();
            
            // 保持 await 语义
            await UniTask.Yield();
        }
    }
}