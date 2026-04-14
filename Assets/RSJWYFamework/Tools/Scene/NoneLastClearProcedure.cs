using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace RSJWYFamework.Runtime
{

    /// <summary>
    /// 不需要加载场景过渡内容
    /// </summary>
    public sealed class NoneLoadTransitionContentStateNode : LoadTransitionContentStateNode
    {
        public override void OnInit()
        {
            
        }

        public override void OnClose()
        {
        }

       
        protected override async UniTask LoadTransitionContentEvent(StateNodeBase last)
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
        } 
        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// 不需要反初始化上一个场景
    /// </summary>
    public sealed class NoneDeinitializationStateNode : DeinitializationStateNode
    {
        public override void OnInit()
        {
            
        }

        public override void OnClose()
        {
        }


        protected override async UniTask Deinitialization(StateNodeBase last)
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
        }
    }

    public sealed class NoneSwitchToTransferSceneStateNode : SwitchToTransferSceneStateNode
    {
        public override void OnInit()
        {
            
        }

        public override void OnClose()
        {
        }


        protected override async UniTask LoadSwitchToTransferSceneEvent(StateNodeBase last)
        {
            var emptyScene = SceneManager.CreateScene("SwitchToTransferScene-Temporary");
            SceneManager.SetActiveScene(emptyScene);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        }
        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
        }
    }
    
    
    
    /// <summary>
    /// 不需要清理上一个场景数据
    /// </summary>
    public sealed class NoneLastClearStateNode : LastClearStateNode
    {
        public override void OnInit()
        {
        }

        public override void OnClose()
        {
        }

        public override void OnUpdate()
        {
        }

        public override void OnUpdateSecond()
        {
        }

        protected override async UniTask Clear(StateNodeBase lastProcedureBase)
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
        }
    }
    /// <summary>
    /// 不需要预加载下一个场景数据
    /// </summary>
    public sealed class NonePreLoadStateNode : PreLoadStateNode
    {
        public override void OnInit()
        {
        }

        public override void OnClose()
        {
        }

        public override void OnUpdate()
        {
        }

        public override void OnUpdateSecond()
        {
        }

        protected override async UniTask PreLoad(StateNodeBase lastProcedureBase)
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
        }
    }
    /// <summary>
    /// 不需要初始化下一个场景
    /// </summary>
    public sealed class NoneNextSceneInitStateNode : NextSceneInitStateNode
    {
        public override void OnInit()
        {
        }

        public override void OnClose()
        {
        }


        protected override async UniTask SceneInit(StateNodeBase lastProcedureBase)
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        public override UniTask OnLeave(StateNodeBase nextProcedureBase)
        {
            return UniTask.CompletedTask;
        }
    }
}