using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using YooAsset;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 流程执行开始
    /// </summary>
    public sealed class SwitchSceneStartStateNode:StateNodeBase
    {
        public override void OnInit()
        {
            
        }

        public override void OnClose()
        {
            
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            AppLogger.Log("流程执行开始");
            var nextType= (Type)_sm.GetBlackboardValue("LoadTransitionContent");
            _sm.SwitchNode(nextType);
        }

        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
            
        }

        public override void OnUpdate()
        {
            
        }
    }
    /// <summary>
    /// 加载过度内容
    /// <remarks>主要是不让过渡状态过于枯燥，用于初始化用户自定义的场景过渡内容</remarks>
    /// </summary>
    public abstract class LoadTransitionContentStateNode : StateNodeBase
    {
        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            UniTask.Create(async () =>
            {
                AppLogger.Log("加载用户自定义过度内容");
                await LoadTransitionContentEvent(lastProcedureBase);
                var nextType= (Type)_sm.GetBlackboardValue("Deinitialization");
                _sm.SwitchNode(nextType);
            });
        }

        /// <summary>
        /// 加载过场动画管理器
        /// </summary>
        /// <param name="owner">状态机持有者</param>>
        /// <returns></returns>
        protected abstract UniTask LoadTransitionContentEvent(StateNodeBase last);
    }
    /// <summary>
    /// 反初始化上一个场景
    /// </summary>
    public abstract class DeinitializationStateNode : StateNodeBase
    {
        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            UniTask.Create(async () =>
            {
                AppLogger.Log("反初始化上一个场景");
                await Deinitialization(lastProcedureBase);
                var nextType= (Type)_sm.GetBlackboardValue("SwitchTransitionContent");
                _sm.SwitchNode(nextType);
            });
        }

        /// <summary>
        /// 反序列化上一个场景
        /// </summary>
        /// <returns></returns>
        protected abstract UniTask Deinitialization(StateNodeBase last);
    }
    
    /// <summary>
    /// 切换到中转场景
    /// </summary>
    public abstract class SwitchToTransferSceneStateNode:StateNodeBase
    {
        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            UniTask.Create(async () =>
            {
                AppLogger.Log("切换到中转场景");
                // 获取当前活动场景名称
                string currentSceneName = SceneManager.GetActiveScene().name;
                await UniTask.Yield(PlayerLoopTiming.Update);
                await LoadSwitchToTransferSceneEvent(lastProcedureBase);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                string nextSceneName = SceneManager.GetActiveScene().name;
                if (nextSceneName==currentSceneName)
                    AppLogger.Warning($"[SwitchSceneOperation]切换到中转场景时警告！上一个场景{currentSceneName}和下一个场景{nextSceneName}名称一致!!");
                var nextType= (Type)_sm.GetBlackboardValue("LastClearType");
                _sm.SwitchNode(nextType);
            });
        }
        /// <summary>
        /// 加载中转场景
        /// </summary>
        /// <remarks>这里是否实际进入中转场景全靠自觉，流程内会对比前后两个场景名称，并发出警告。</remarks>
        /// <param name="last"></param>
        /// <returns></returns>
        protected abstract UniTask LoadSwitchToTransferSceneEvent(StateNodeBase last);
    }

   
    
    /// <summary>
    /// 清理资源-在中转场景内调用
    /// </summary>
    public abstract class LastClearStateNode:StateNodeBase
    {
        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            UniTask.Create(async () =>
            {
                AppLogger.Log("清理不需要的资源");
                await Clear(lastProcedureBase);
                _sm.SwitchNextNode();
                var nextType= (Type)_sm.GetBlackboardValue("PreLoadType");
                _sm.SwitchNode(nextType);
            });
        }

        /// <summary>
        /// 清理上一个场景相关资源
        /// </summary>
        protected abstract UniTask Clear(StateNodeBase lastProcedureBase);
    }
    /// <summary>
    /// 加载下一个场景资源
    /// </summary>
    public abstract class PreLoadStateNode:StateNodeBase
    {
        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            UniTask.Create(async () =>
            {
                AppLogger.Log("预加载下一场景需要的资源");
                await PreLoad(lastProcedureBase);
                var nextType= (Type)_sm.GetBlackboardValue("LoadNextSceneType");
                _sm.SwitchNode(nextType);
            });
        }
        /// <summary>
        /// 预加载下一个场景资源
        /// </summary>
        protected abstract UniTask PreLoad(StateNodeBase lastProcedureBase);
    }
    /// <summary>
    /// 加载并切换下一个场景
    /// </summary>
    public abstract class LoadNextSceneStateNode:StateNodeBase
    {
        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            UniTask.Create(async () =>
            {
                AppLogger.Log("加载并切换下一个场景");
                string currentSceneName = SceneManager.GetActiveScene().name;
                await UniTask.Yield(PlayerLoopTiming.Update);
                await LoadNextScene(lastProcedureBase);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                string nextSceneName = SceneManager.GetActiveScene().name;
                if (nextSceneName==currentSceneName)
                    //如果有中转场景，则本警告无效
                    AppLogger.Warning($"[SwitchSceneOperation]切换到下一场景时警告！上一个场景{currentSceneName}和下一个场景{nextSceneName}名称一致!!");
                var nextType= (Type)_sm.GetBlackboardValue("NextSceneInitType");
                _sm.SwitchNode(nextType);
            });
        }
        protected abstract UniTask LoadNextScene(StateNodeBase lastProcedureBase);
    }

    /// <summary>
    /// 下一个场景初始化流程
    /// </summary>
    public abstract class NextSceneInitStateNode : StateNodeBase
    {
        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            UniTask.Create(async () =>
            {
                AppLogger.Log("正在初始化下一个场景");
                await SceneInit(lastProcedureBase);
                _sm.SwitchNode<SwitchSceneDoneStateNode>();
            });
        }
        protected abstract UniTask SceneInit(StateNodeBase lastProcedureBase);
    }
   
    /// <summary>
    /// 流程执行结束
    /// </summary>
    public sealed class SwitchSceneDoneStateNode:StateNodeBase
    {
        public override void OnInit()
        {
            
        }

        public override void OnClose()
        {
            
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            AppLogger.Log("流程结束");
            StopStateMachine("场景切换完成");
        }

        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
            
        }

        public override void OnUpdate()
        {
            
        }
    }
}