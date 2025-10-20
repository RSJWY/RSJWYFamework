using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 获取DLL
    /// </summary>
    public class LoadDLLByteNode:StateNodeBase
    {
        public override void OnInit()
        {
        }

        public override void OnClose()
        {
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            AppLogger.Log($"加载热更代码数据");
            UniTask.Create(async () =>
            {
                var package=ModuleManager.GetModule<YooAssetManager>().GetPackage("RawFilePackage");
                if (!package.CheckLocationValid("HotUpdateCode_HotList"))
                {
                    AppLogger.Error("无法加载列表文件");
                    StopStateMachine($"加载热更代码列表失败",500);
                    return;
                }
                    
                //获取列表
                var MFALisRFH = package.LoadRawFileAsync("HotUpdateCode_HotList");
                await MFALisRFH.ToUniTask();
                var loadLis = JsonConvert.DeserializeObject<HotCodeDLL>(MFALisRFH.GetRawFileText());
                var hotCodeBytesMap = new Dictionary<string, HotCodeBytes>();
                //加载热更代码和pdb
                foreach (var asset in loadLis.HotCode)
                {
                    //string dllPath = MyTool.GetYooAssetWebRequestPath(asset);
                    string _dllname = $"{asset}.dll";
                    string _pdbname = $"{asset}.pdb";
                    //Debug.Log($"加载资产:{_n}");
                    //资源地址是否有效
                    if (package.CheckLocationValid(_dllname))
                    {
                        var hotcode = new HotCodeBytes();
                        //执行加载
                        var _rfh = package.LoadRawFileAsync(_dllname);
                        await _rfh.ToUniTask();
                        //转byte数组
                        hotcode.dllBytes = _rfh.GetRawFileData();
                        if (package.CheckLocationValid(_pdbname))
                        {
                            var _rfhPDB = package.LoadRawFileAsync(_pdbname);
                            await _rfhPDB.ToUniTask();
                            //转byte数组
                            hotcode.pdbBytes = _rfhPDB.GetRawFileData();
                        }
                        else
                        {
                            AppLogger.Warning($"热更获取PDB数据流程，加载PDB资源文件地址：{_dllname}无效，将无法打印行号");
                        }
                        hotCodeBytesMap.Add(asset,hotcode);
                        //Debug.Log($"dll:{asset}  size:{assetData.Length}");
                        AppLogger.Log($"热更加载DLL流程，加载资源dll:{_dllname}");
                        _rfh.Release();
                    }
                    else
                    {
                        AppLogger.Error($"热更获取DLL数据流程，加载热更代码资源文件地址：{_dllname}无效");
                    }
                }
                //加载元数据
                var MFAOTbytesMap = new Dictionary<string, byte[]>();
                foreach (var asset in loadLis.MetadataForAOTAssemblies)
                {
                    //string dllPath = MyTool.GetYooAssetWebRequestPath(asset);
                    string _dllname = $"{asset}.dll";
                    //Debug.Log($"加载资产:{_n}");
                    //资源地址是否有效
                    if (package.CheckLocationValid(_dllname))
                    {
                        //执行加载
                        var _rfh = package.LoadRawFileAsync(_dllname);
                        await _rfh.ToUniTask();
                        //转byte数组
                        var MFAOT = _rfh.GetRawFileData();
                        MFAOTbytesMap.Add(asset,MFAOT);
                        //Debug.Log($"dll:{asset}  size:{assetData.Length}");
                        AppLogger.Log($"热更加载DLL流程，加载补充元数据资源dll:{_dllname}");
                        _rfh.Release();
                    }
                    else
                    {
                        AppLogger.Error($"热更获取DLL数据流程，加载资源文件地址：{_dllname}无效");
                    }
                }
                SetBlackboardValue("LoadList",loadLis);
                SetBlackboardValue("HotcodeDic",hotCodeBytesMap);
                SetBlackboardValue("MFAOTDic",MFAOTbytesMap);
                SwitchToNode<LoadHotCodeNode>();
            });
        }

        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
        }

        public override void OnUpdate()
        {
        }

        public override void OnUpdateSecond()
        {
        }
    }
}