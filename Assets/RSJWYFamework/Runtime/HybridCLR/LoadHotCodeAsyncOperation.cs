
using System.Collections.Generic;
using System.Reflection;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 加载热更代码
    /// </summary>
    public class LoadHotCodeAsyncOperation:AppGameAsyncOperation
    {
        enum LoadHotCodeSteps
        {
            None,
            Update,
            Done
        }
        /// <summary>
        /// 加载到的程序集
        /// </summary>
        public Dictionary<string, Assembly> HotCode { get; private set; } = new();
        private readonly StateMachine _smc;
        private LoadHotCodeSteps _steps = LoadHotCodeSteps.None;
        
        public LoadHotCodeAsyncOperation(object owner)
        {
            _smc = new StateMachine(owner,"加载热更代码");
            //创建流程
            _smc.AddNode(new LoadDLLByteNode());
            _smc.AddNode(new LoadHotCodeNode());
            _smc.AddNode(new LoadHotCodeDoneNode());
            
            //绑定事件
            _smc.StateMachineTerminatedEvent+=OnStateMachineTerminatedEvent;
            AppAsyncOperationSystem.StartOperation(typeof(LoadHotCodeAsyncOperation).FullName,this);
        }

        private void OnStateMachineTerminatedEvent(StateMachine stateMachine, string TerminationReason, int StatusCode, bool isRestarting)
        {
            if (isRestarting)return;
            
            if(stateMachine == _smc)
            {
                if(StatusCode==0)
                {
                    Status = AppAsyncOperationStatus.Succeed;
                    _steps = LoadHotCodeSteps.Done;
                }
                else
                {
                    Status = AppAsyncOperationStatus.Failed;
                    _steps = LoadHotCodeSteps.Done;
                }
            }
        }

        protected override void OnStart()
        {
            _steps = LoadHotCodeSteps.Update;
            _smc.StartNode<LoadDLLByteNode>();
        }

        protected override void OnUpdate()
        {
            if (_steps == LoadHotCodeSteps.None || _steps == LoadHotCodeSteps.Done)
                return;

            if(_steps == LoadHotCodeSteps.Update)
            {
                _smc.OnUpdate();
                if(_smc.GetNowNode() == typeof(LoadHotCodeDoneNode))
                {
                    Status = AppAsyncOperationStatus.Succeed;
                    _steps = LoadHotCodeSteps.Done;
                }
            }
        }

        protected override void OnSecondUpdate()
        {
            
        }

        protected override void OnAbort()
        {
        }

        protected override void OnSecondUpdateUnScaleTime()
        {
        }

    }
}