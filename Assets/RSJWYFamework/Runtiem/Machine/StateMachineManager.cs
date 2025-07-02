using System.Collections.Generic;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 状态机生命周期管理器
    /// 可以把状态机放入本控制器下，自动接管生命周期。
    /// </summary>
    [Module]
    public class StateMachineManager:ModuleBase
    {
        Dictionary<string,StateMachine>Procedures = new(100);
        /// <summary>
        /// 添加一个流程控制器
        /// </summary>
        public void AddStateMachine(StateMachine stateMachine,bool isAutoRun=false) 
        {
            
            if (Procedures.ContainsKey(stateMachine.st_Name))
            {
                AppLogger.Error($"添加流程失败：流程 {stateMachine.st_Name} 已存在！");
            }
            else
            {
                Procedures.Add(stateMachine.st_Name,stateMachine);
                if (isAutoRun)
                {
                    stateMachine.StartNode();
                }
            }
        }
        /// <summary>
        /// 移除一个流程控制器
        /// </summary>
        public void RemoveStateMachine(string st_Name) 
        {
            if (Procedures.ContainsKey(st_Name))
            {
                Procedures.Remove(st_Name);
            }
        }
        /// <summary>
        /// 获取一个流程控制器
        /// </summary>
        /// <param name="name">流程控制器绑定的名字</param>
        /// <returns></returns>
        public StateMachine GetStateMachine(string st_Name)
        {
            if (Procedures.ContainsKey(st_Name))
            {
                return Procedures[st_Name];
            }
            return null;
        }
        
        public override void Initialize()
        {
            
        }

        public override void Shutdown()
        {
        }

        public override void ModulePerSecondUpdate()
        {
            foreach (var procedure in Procedures)
            {
                procedure.Value.OnUpdateSecond();
            }
        }

        public override void ModuleUpdate()
        {
            foreach (var procedure in Procedures)
            {
                procedure.Value.OnUpdate();
            }
        }

      
    }
}