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
        /// <param name="name"></param>
        /// <param name="procedureController"></param>
        public void AddProcedure(string name,StateMachine procedureController,bool isAutoRun=false) 
        {
            if (Procedures.ContainsKey(name))
            {
                AppLogger.Error($"添加流程失败：流程 {name} 已存在！");
            }
            else
            {
                Procedures.Add(name,procedureController);
                if (isAutoRun)
                {
                    procedureController.StartNode();
                }
            }
        }
        /// <summary>
        /// 移除一个流程控制器
        /// </summary>
        public void RemoveProcedure(string name) 
        {
            if (Procedures.ContainsKey(name))
            {
                Procedures.Remove(name);
            }
        }
        /// <summary>
        /// 获取一个流程控制器
        /// </summary>
        /// <param name="name">流程控制器绑定的名字</param>
        /// <returns></returns>
        public StateMachine GetProcedure(string name)
        {
            if (Procedures.ContainsKey(name))
            {
                return Procedures[name];
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