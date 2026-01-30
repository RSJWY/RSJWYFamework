using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 切换场景流程，请不要进行等待，并且确保初始化后没有后文
    /// <remarks>
    /// 大致流程是：开始-加载用户自定义过场内容-反序列化上一个场景-进入用户自定义中转场景-清理上一个场景-预加载下一个场景所需资源-切换下一个场景-初始化下一个场景-结束
    /// </remarks>
    /// </summary>
    public class SwitchSceneOperation
    {
        private readonly StateMachine _sc;
        /// <summary>
        /// 切换流程回调
        /// <remarks>第一个是上一个流程，第二个是下一个流程</remarks>
        /// </summary>
        public event Action<StateNodeBase,StateNodeBase> SwitchStateNodeEvent; 

        /// <summary>
        /// 初始化场景切换流程
        /// </summary>
        /// <param name="loadTransitionContentStateNode">加载用户自定义的场景过渡内容-可为null</param>
        /// <param name="deinitializationStateNode">反初始化上一个场景-可以为null</param>
        /// <param name="switchToTransferSceneStateNode">加载用户自定义的中转场景-</param>
        /// <param name="lastClearStateNode">清理上一个场景资源-可为null</param>
        /// <param name="preLoadStateNode">预加载下一个场景资源-可为null</param>
        /// <param name="loadNextSceneStateNode">加载下一个场景-必须实现，直接跳转</param>
        /// <param name="nextSceneInitStateNode">下一个场景初始化操作-可为null</param>
        /// <param name="blackboardKeyValue">设置黑板数据，为键对值。会写入状态机的黑板数据里</param>
        public SwitchSceneOperation(
            LoadTransitionContentStateNode loadTransitionContentStateNode,DeinitializationStateNode deinitializationStateNode,
            SwitchToTransferSceneStateNode switchToTransferSceneStateNode,
            LoadNextSceneStateNode loadNextSceneStateNode, LastClearStateNode lastClearStateNode, 
            PreLoadStateNode preLoadStateNode,[NotNull] NextSceneInitStateNode nextSceneInitStateNode,
            Dictionary<string,object>blackboardKeyValue)
        {
            _sc = new StateMachine(this,"场景过度切换");
            _sc.ProcedureSwitchEvent += SwitchSceneOperationEvent;
            //启动节点
            _sc.AddNode<SwitchSceneStartStateNode>();
            //用户自定义的场景过渡内容
            loadTransitionContentStateNode ??= new NoneLoadTransitionContentStateNode();
            _sc.AddNode(loadTransitionContentStateNode);
            var loadTransitionContent = loadTransitionContentStateNode.GetType();
            //反序列化上一个场景
            deinitializationStateNode ??= new NoneDeinitializationStateNode();
            _sc.AddNode(deinitializationStateNode);
            var deinitialization = deinitializationStateNode.GetType();
            //切换到中转场景
            switchToTransferSceneStateNode??= new NoneSwitchToTransferSceneStateNode();
            _sc.AddNode(switchToTransferSceneStateNode);
            var switchTransitionContent = switchToTransferSceneStateNode.GetType();
            //清理上一个场景
            lastClearStateNode ??= new NoneLastClearStateNode();
            _sc.AddNode(lastClearStateNode);
            var lastClearType = lastClearStateNode.GetType();
            //预加载下一个场景资源
            preLoadStateNode??= new NonePreLoadStateNode();
            _sc.AddNode(preLoadStateNode);
            var loadNextSceneType = preLoadStateNode.GetType();
            //加载下一个场景
            if (loadNextSceneStateNode==null)
            {
                throw new AppException("请确保加载下一个场景流程不为空");
            }
            _sc.AddNode(loadNextSceneStateNode);
            var preLoadType = loadNextSceneStateNode.GetType();
            //下一个场景初始化信息
            nextSceneInitStateNode??= new NoneNextSceneInitStateNode();
            _sc.AddNode(nextSceneInitStateNode);
            var nextSceneInitType= nextSceneInitStateNode.GetType();
            //场景切换流程结束
            _sc.AddNode<SwitchSceneDoneStateNode>();
            //设置黑板数据
            if (blackboardKeyValue!= null)
            {
                foreach (var blackboard in blackboardKeyValue)
                {
                    _sc.SetBlackboardValue(blackboard.Key,blackboard.Value);
                }
            }
            _sc.SetBlackboardValue("LoadTransitionContent",loadTransitionContent);
            _sc.SetBlackboardValue("Deinitialization",deinitialization);
            _sc.SetBlackboardValue("SwitchTransitionContent",switchTransitionContent);
            _sc.SetBlackboardValue("LastClearType",lastClearType);
            _sc.SetBlackboardValue("PreLoadType",preLoadType);
            _sc.SetBlackboardValue("LoadNextSceneType",loadNextSceneType);
            _sc.SetBlackboardValue("NextSceneInitType",nextSceneInitType);
            //添加到管理器并启动
            ModuleManager.GetModule<StateMachineManager>().AddStateMachine(_sc);
        }

        /// <summary>
        /// 开始切换流程动作
        /// </summary>
        public void StartSwitchScene()
        {
            _sc.StartNode<SwitchSceneStartStateNode>();
        }
        /// <summary>
        /// 流程切换回调
        /// </summary>
        /// <param name="last">上一流程</param>
        /// <param name="next">下一流程</param>
        void SwitchSceneOperationEvent(StateNodeBase last, StateNodeBase next)
        {
            if (next is SwitchSceneDoneStateNode)
            {
                //切换到结尾后，退出
                ModuleManager.GetModule<StateMachineManager>().RemoveStateMachine(_sc.st_Name);
            }
            SwitchStateNodeEvent?.Invoke(last,next);
        }
    }
}