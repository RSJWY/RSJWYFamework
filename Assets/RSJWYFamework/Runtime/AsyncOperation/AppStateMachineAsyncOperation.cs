
using System;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 可异步等待的状态机
    /// </summary>
    public class AppStateMachineAsyncOperation:AppAsyncOperationBase
    {
        /// <summary>
        /// 状态机
        /// </summary>
        private StateMachine _stateMachine;
        /// <summary>
        /// 待切换状态类型
        /// </summary>
        private Type  _pendingSwitchType;
        
        internal override void InternalStart()
        {
            _stateMachine.SwitchNode(_pendingSwitchType);
        }
        internal override void InternalUpdate()
        {
            _stateMachine.OnUpdate();
        }

        internal override void InternalSecondUpdate()
        {
            _stateMachine.OnUpdateSecond();
        }

        internal override void InternalSecondUnScaleTimeUpdate()
        {
            _stateMachine.OnUpdateSecondUnScaleTime();
        }

        internal override void InternalAbort()
        {
            _stateMachine.Stop();
        }
        public AppStateMachineAsyncOperation (StateMachine stateMachine,Type  startStateType)
        {
            _stateMachine = stateMachine;
            _pendingSwitchType = startStateType;
        }
        /// <summary>
        /// 启动异步操作
        /// <remarks>
        /// 启动异步操作，状态机生命周期管理将移交到异步操作系统
        /// </remarks>
        /// </summary>
        public void StartAsync()
        {
            this.StartAddAppAsyncOperationSystem(_stateMachine.st_Name);
        }
    }
}