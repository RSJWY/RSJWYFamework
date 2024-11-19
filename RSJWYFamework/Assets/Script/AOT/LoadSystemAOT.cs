using Cysharp.Threading.Tasks;
using RSJWYFamework.Runtime.Base;
using RSJWYFamework.Runtime.Config;
using RSJWYFamework.Runtime.Logger;
using RSJWYFamework.Runtime.Main;
using UnityEngine;
using RSJWYFamework.Runtime.Driver;
using RSJWYFamework.Runtime.Scene;
using RSJWYFamework.Runtime.StateMachine;


namespace Script.AOT
{
    /// <summary>
    /// 框架的管理器，unity挂载
    /// </summary>
    public class LoadSystemAOT:MonoBehaviour
    {
        protected void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            StartApp().Forget();
        }

        async UniTaskVoid StartApp()
        {
            RSJWYLogger.Log("加载项目配置文件，并加载到数据管理中···");
            var projectset =  Resources.Load<ProjectConfig>("ProjectConfig");
            Main.DataManagerataManager.AddDataSet(projectset);
            RSJWYLogger.Log($"日志等级：{projectset.Loglevel}");
            RSJWYLogger.Loglevel = projectset.Loglevel;
            RSJWYLogger.Log("等待包初始化");
            await Main.YooAssetManager.LoadPackage();
            RSJWYLogger.Log("包初始化完成，加载热更代码");
            await Main.HybridClrManager.LoadHotCodeDLL();
            
            var toScecne= new SwitchSceneOperation(new LoadHotScene(),"加载热更入口场景",false);
            toScecne.StartProcedure();
        }
    }

    internal sealed class LoadHotScene : LoadNextSceneStateNode
    {
        public override void OnInit()
        {
            
        }

        public override void OnClose()
        {
        }

        public override void OnLeave(StateNodeBase nextStateNodeBase)
        {
        }

        public override void OnUpdate()
        {
        }

        public override void OnUpdateSecond()
        {
        }

        protected override async UniTask LoadNextScene(StateNodeBase lastStateNodeBase)
        {
            RSJWYLogger.Log("加载入口场景");
            Main.YooAssetManager.GetPackage("PrefabPackage",out var package);
            var scene = package.LoadSceneAsync("Scenes_Enter");
            await scene.ToUniTask();
        }
    }
}