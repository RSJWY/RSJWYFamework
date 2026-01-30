
using System;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 可异步等待的状态机操作封装
    /// <para>将状态机的生命周期映射到异步操作的生命周期</para>
    /// </summary>
    public class AppStateMachineAsyncOperation : AppAsyncOperationBase
    {
        /// <summary>
        /// 状态机实例
        /// </summary>
        protected StateMachine _stateMachine;

        /// <summary>
        /// 初始状态类型
        /// </summary>
        private Type _startStateType;



        /// <summary>
        /// 供子类延迟初始化状态机
        /// </summary>
        /// <param name="stateMachine"></param>
        /// <param name="startStateType"></param>
        protected void InitStateMachine(StateMachine stateMachine, Type startStateType)
        {
            if (_stateMachine != null)
            {
                 _stateMachine.StateMachineTerminatedEvent -= OnStateMachineTerminated;
            }

            _stateMachine = stateMachine;
            _startStateType = startStateType;

            if (_stateMachine != null)
            {
                SetAsyncOperationName(_stateMachine.st_Name);
                _stateMachine.StateMachineTerminatedEvent += OnStateMachineTerminated;
            }
        }

        /// <summary>
        /// 析构/释放时移除事件监听，防止内存泄漏
        /// </summary>
        ~AppStateMachineAsyncOperation()
        {
            if (_stateMachine != null)
            {
                _stateMachine.StateMachineTerminatedEvent -= OnStateMachineTerminated;
                _stateMachine.ClearBlackboard();
            }
        }
        /// <summary>
        /// 状态机终止事件回调
        /// </summary>
        /// <param name="sm"></param>
        /// <param name="reason"></param>
        /// <param name="code"></param>
        private void OnStateMachineTerminated(StateMachine sm, string reason, int code)
        {
            // 状态机正常结束 (Code 0 通常约定为成功)
            if (code == 0)
            {
                Status = AppAsyncOperationStatus.Succeed;
            }
            else
            {
                // 状态机异常结束
                Status = AppAsyncOperationStatus.Failed;
                Error = reason ?? $"StateMachine terminated with code {code}";
            }
        }
        /// <summary>
        /// 内部开始异步操作
        /// </summary>
        internal override void InternalStart()
        {
            if (_stateMachine == null)
            {
                Status = AppAsyncOperationStatus.Failed;
                Error = "StateMachine is null";
                return;
            }

            if (_startStateType != null)
            {
                _stateMachine.SwitchNode(_startStateType);
            }
            else
            {
                AppLogger.Warning("AppStateMachineAsyncOperation started without a start state type.");
            }
        }
        /// <summary>
        /// 内部更新异步操作
        /// </summary>
        internal override void InternalUpdate()
        {
            if (Status != AppAsyncOperationStatus.Processing) return;
            _stateMachine?.OnUpdate();
        }
        /// <summary>
        /// 内部秒更新
        /// </summary>
        internal override void InternalSecondUpdate()
        {
            if (Status != AppAsyncOperationStatus.Processing) return;
            _stateMachine?.OnUpdateSecond();
        }

        /// <summary>
        /// 内部无缩放秒更新
        /// </summary>
        internal override void InternalSecondUnScaleTimeUpdate()
        {
            if (Status != AppAsyncOperationStatus.Processing) return;
            _stateMachine?.OnUpdateSecondUnScaleTime();
        }
        /// <summary>
        /// 内部中止异步操作
        /// </summary>
        internal override void InternalAbort()
        {
            // 外部强行终止异步操作时，停止状态机
            _stateMachine?.Stop(999, "Operation Aborted");
        }
        
    }
}
