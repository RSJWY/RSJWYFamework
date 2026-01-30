using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 获取DLL字节流节点
    /// </summary>
    public class LoadDLLByteNode : StateNodeBase<LoadHotCodeAsyncOperation>
    {
        public override void OnInit()
        {
        }

        public override void OnClose()
        {
        }

        public override async UniTask OnEnter(StateNodeBase lastProcedureBase)
        {
            AppLogger.Log($"[LoadDLLByteNode] 开始加载热更代码数据...");
            await ExecuteTask();
        }

        private async UniTask ExecuteTask()
        {
            var package = ModuleManager.GetModule<YooAssetManager>().GetPackage("RawFilePackage");
            const string HOT_LIST_NAME = "HotUpdateCode_HotList";

            if (!package.CheckLocationValid(HOT_LIST_NAME))
            {
                AppLogger.Error($"[LoadDLLByteNode] 无法找到热更列表文件: {HOT_LIST_NAME}");
                _sm.Stop(500, "无法得到热更新dll列表");
                return;
            }

            // 1. 加载热更列表 JSON
            var listFileHandle = package.LoadRawFileAsync(HOT_LIST_NAME);
            await listFileHandle.ToUniTask();
            
            var hotCodeList = JsonConvert.DeserializeObject<HotCodeDLL>(listFileHandle.GetRawFileText());
            var hotCodeBytesMap = new Dictionary<string, HotCodeBytes>();

            // 2. 加载热更 DLL 和 PDB
            foreach (var assetName in hotCodeList.HotCode)
            {
                string dllName = $"{assetName}.dll";
                string pdbName = $"{assetName}.pdb";

                if (package.CheckLocationValid(dllName))
                {
                    var hotCodeData = new HotCodeBytes();

                    // 加载 DLL
                    var dllHandle = package.LoadRawFileAsync(dllName);
                    await dllHandle.ToUniTask();
                    hotCodeData.dllBytes = dllHandle.GetRawFileData();
                    dllHandle.Release(); // 及时释放句柄，数据已拷贝到 byte[]

                    // 加载 PDB (可选)
                    if (package.CheckLocationValid(pdbName))
                    {
                        var pdbHandle = package.LoadRawFileAsync(pdbName);
                        await pdbHandle.ToUniTask();
                        hotCodeData.pdbBytes = pdbHandle.GetRawFileData();
                        pdbHandle.Release();
                    }
                    else
                    {
                        AppLogger.Warning($"[LoadDLLByteNode] PDB文件缺失: {pdbName}，将无法显示堆栈行号");
                    }

                    hotCodeBytesMap.Add(assetName, hotCodeData);
                    AppLogger.Log($"[LoadDLLByteNode] 已加载热更DLL: {dllName}");
                }
                else
                {
                    AppLogger.Error($"[LoadDLLByteNode] 严重错误：找不到热更DLL资源: {dllName}");
                    // 这里是否需要 Stop 视业务需求而定，通常缺失核心代码应该报错停止
                }
            }

            // 3. 加载 AOT 补充元数据 DLL
            var aotMetadataMap = new Dictionary<string, byte[]>();
            foreach (var assetName in hotCodeList.MetadataForAOTAssemblies)
            {
                string dllName = $"{assetName}.dll";

                if (package.CheckLocationValid(dllName))
                {
                    var dllHandle = package.LoadRawFileAsync(dllName);
                    await dllHandle.ToUniTask();
                    
                    var dllBytes = dllHandle.GetRawFileData();
                    aotMetadataMap.Add(assetName, dllBytes);
                    
                    AppLogger.Log($"[LoadDLLByteNode] 已加载AOT补充元数据: {dllName}");
                    dllHandle.Release();
                }
                else
                {
                    AppLogger.Error($"[LoadDLLByteNode] 严重错误：找不到AOT元数据资源: {dllName}");
                }
            }

            // 4. 传递数据到黑板
            _sm.SetBlackboardValue("LoadList", hotCodeList);
            _sm.SetBlackboardValue("HotcodeDic", hotCodeBytesMap);
            _sm.SetBlackboardValue("MFAOTDic", aotMetadataMap);
            
            _sm.SwitchNode<LoadHotCodeNode>();
        }

        public override void OnUpdate()
        {
        }

        public override void OnUpdateSecond()
        {
        }
    }
}