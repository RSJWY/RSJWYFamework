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
        /// 结束事件
        /// <remarks>第一个是上一个流程，第二个是下一个流程</remarks>
        /// </summary>
        public event Action<StateMachine, string, int> StateMachineTerminatedNodeEvent; 

        private SwitchSceneOperation(Builder builder)
        {
            _sc = new StateMachine<SwitchSceneOperation>(this,"场景过度切换");
            _sc.ProcedureSwitchEvent += SwitchSceneOperationEvent;
            _sc.StateMachineTerminatedEvent += StateMachineTerminatedEvent;

            // 1. 实例化节点 (如果为空则使用默认空节点)
            var startNode = new SwitchSceneStartStateNode();
            var loadTransitionContentNode = builder.LoadTransitionContent ?? new NoneLoadTransitionContentStateNode();
            var deinitializationNode = builder.Deinitialization ?? new NoneDeinitializationStateNode();
            var switchToTransferSceneNode = builder.SwitchToTransferScene ?? new NoneSwitchToTransferSceneStateNode();
            var lastClearNode = builder.LastClear ?? new NoneLastClearStateNode();
            var preLoadNode = builder.PreLoad ?? new NonePreLoadStateNode();
            var loadNextSceneNode = builder.LoadNextScene;
            if (loadNextSceneNode == null) throw new AppException("请确保加载下一个场景流程不为空");
            var nextSceneInitNode = builder.NextSceneInit ?? new NoneNextSceneInitStateNode();
            var doneNode = new SwitchSceneDoneStateNode();

            // 2. 链接流程链 (Chain Responsibility)
            startNode.NextNodeType = loadTransitionContentNode.GetType();
            loadTransitionContentNode.NextNodeType = deinitializationNode.GetType();
            deinitializationNode.NextNodeType = switchToTransferSceneNode.GetType();
            switchToTransferSceneNode.NextNodeType = lastClearNode.GetType();
            lastClearNode.NextNodeType = preLoadNode.GetType();
            preLoadNode.NextNodeType = loadNextSceneNode.GetType();
            loadNextSceneNode.NextNodeType = nextSceneInitNode.GetType();
            nextSceneInitNode.NextNodeType = doneNode.GetType();
            
            // 3. 添加节点到状态机
            _sc.AddNode(startNode);
            _sc.AddNode(loadTransitionContentNode);
            _sc.AddNode(deinitializationNode);
            _sc.AddNode(switchToTransferSceneNode);
            _sc.AddNode(lastClearNode);
            _sc.AddNode(preLoadNode);
            _sc.AddNode(loadNextSceneNode);
            _sc.AddNode(nextSceneInitNode);
            _sc.AddNode(doneNode);

            // 4. 设置黑板数据
            if (builder.Blackboard != null)
            {
                foreach (var kv in builder.Blackboard)
                {
                    _sc.SetBlackboardValue(kv.Key, kv.Value);
                }
            }

            // 5. 添加到管理器并启动
            ModuleManager.GetModule<StateMachineManager>().AddStateMachine(_sc);
        }

      

        /// <summary>
        /// 兼容旧版构造函数 (已过时，请使用 Builder)
        /// </summary>
        [Obsolete("构造函数参数过多，请使用 SwitchSceneOperation.CreateBuilder()...Build()")]
        public SwitchSceneOperation(
            LoadTransitionContentStateNode loadTransitionContentStateNode,DeinitializationStateNode deinitializationStateNode,
            SwitchToTransferSceneStateNode switchToTransferSceneStateNode,
            LoadNextSceneStateNode loadNextSceneStateNode, LastClearStateNode lastClearStateNode, 
            PreLoadStateNode preLoadStateNode,[NotNull] NextSceneInitStateNode nextSceneInitStateNode,
            Dictionary<string,object>blackboardKeyValue) 
            : this(new Builder()
                .SetLoadTransition(loadTransitionContentStateNode)
                .SetDeinitialization(deinitializationStateNode)
                .SetSwitchToTransfer(switchToTransferSceneStateNode)
                .SetLoadNextScene(loadNextSceneStateNode)
                .SetLastClear(lastClearStateNode)
                .SetPreLoad(preLoadStateNode)
                .SetNextSceneInit(nextSceneInitStateNode)
                .SetBlackboard(blackboardKeyValue))
        {
        }

        /// <summary>
        /// 创建构建器
        /// </summary>
        public static Builder CreateBuilder()
        {
            return new Builder();
        }

        /// <summary>
        /// 场景切换操作构建器
        /// </summary>
        public class Builder
        {
            internal LoadTransitionContentStateNode LoadTransitionContent;
            internal DeinitializationStateNode Deinitialization;
            internal SwitchToTransferSceneStateNode SwitchToTransferScene;
            internal LastClearStateNode LastClear;
            internal PreLoadStateNode PreLoad;
            internal LoadNextSceneStateNode LoadNextScene;
            internal NextSceneInitStateNode NextSceneInit;
            internal Dictionary<string, object> Blackboard;

            public Builder SetLoadTransition(LoadTransitionContentStateNode node)
            {
                LoadTransitionContent = node;
                return this;
            }

            public Builder SetDeinitialization(DeinitializationStateNode node)
            {
                Deinitialization = node;
                return this;
            }

            public Builder SetSwitchToTransfer(SwitchToTransferSceneStateNode node)
            {
                SwitchToTransferScene = node;
                return this;
            }

            public Builder SetLastClear(LastClearStateNode node)
            {
                LastClear = node;
                return this;
            }

            public Builder SetPreLoad(PreLoadStateNode node)
            {
                PreLoad = node;
                return this;
            }

            public Builder SetLoadNextScene(LoadNextSceneStateNode node)
            {
                LoadNextScene = node;
                return this;
            }

            public Builder SetNextSceneInit(NextSceneInitStateNode node)
            {
                NextSceneInit = node;
                return this;
            }

            public Builder SetBlackboard(Dictionary<string, object> blackboard)
            {
                Blackboard = blackboard;
                return this;
            }

            public SwitchSceneOperation Build()
            {
                return new SwitchSceneOperation(this);
            }
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
        
        private void StateMachineTerminatedEvent(StateMachine arg1, string msg, int code)
        {
            StateMachineTerminatedNodeEvent?.Invoke(arg1, msg, code);
        }
    }
}