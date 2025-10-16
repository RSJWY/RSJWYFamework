namespace RSJWYFamework.Runtime
{
    public abstract class StateNodeBase
    {
        /// <summary>
        /// 关联到流程控制器
        /// </summary>
        public StateMachine _sm { get;internal set; }
        #region 抽象方法
        /// <summary>
        /// 流程初始化
        /// 添加流程时调用
        /// </summary>
        public abstract void OnInit();
        
        /// <summary>
        /// 流程关闭
        /// 移除流程时调用
        /// </summary>
        public abstract void OnClose();
        
        /// <summary>
        /// 进入当前流程
        /// </summary>
        /// <param name="lastProcedureBase">上一个离开的流程</param>
        public abstract void OnEnter(StateNodeBase lastProcedureBase);

        /// <summary>
        /// 离开当前流程
        /// </summary>
        /// <param name="nextProcedureBase">下一个进入的流程</param>
        public abstract void OnLeave(StateNodeBase nextProcedureBase);
        #endregion 
        #region 虚方法
        /// <summary>
        /// 流程帧更新
        /// </summary>
        public virtual void OnUpdate(){}

        /// <summary>
        /// 流程秒更新
        /// </summary>
        public virtual void OnUpdateSecond(){}
        #endregion
        
        #region 状态机控制便捷方法
        
        /// <summary>
        /// 终止状态机（便捷方法）
        /// </summary>
        /// <param name="reason">终止原因</param>
        protected void TerminateStateMachine(string reason = "节点请求终止")
        {
            _sm?.AutoTerminate(reason);
        }
        
        /// <summary>
        /// 重启状态机（便捷方法）
        /// </summary>
        /// <param name="startNodeType">重启后的起始节点类型，如果为null则使用第一个节点</param>
        /// <param name="reason">重启原因</param>
        protected void RestartStateMachine(System.Type startNodeType = null, string reason = "节点请求重启")
        {
            if (_sm == null)
            {
                AppLogger.Warning("无法重启状态机：状态机引用为空");
                return;
            }
            
            try
            {
                AppLogger.Log($"节点 {GetType().Name} 请求重启状态机，原因：{reason}");
                
                // 如果状态机未结束，先终止它
                if (!_sm.IsTerminated)
                {
                    _sm.Terminate($"为重启而终止：{reason}");
                }
                
                // 重置状态机
                _sm.Reset();
                
                // 重新启动
                if (startNodeType != null)
                {
                    _sm.StartNode(startNodeType);
                }
                else
                {
                    _sm.StartNode();
                }
                
                AppLogger.Log($"状态机重启成功，起始节点：{startNodeType?.Name ?? "默认第一个节点"}");
            }
            catch (System.Exception ex)
            {
                AppLogger.Error($"重启状态机时发生错误：{ex.Message}");
            }
        }


        
        /// <summary>
        /// 重启状态机到指定节点类型（泛型版本）
        /// </summary>
        /// <typeparam name="TNode">目标节点类型</typeparam>
        /// <param name="reason">重启原因</param>
        protected void RestartStateMachine<TNode>(string reason = "节点请求重启") where TNode : StateNodeBase
        {
            RestartStateMachine(typeof(TNode), reason);
        }
        
        /// <summary>
        /// 检查状态机是否已终止
        /// </summary>
        /// <returns>如果状态机已终止返回true，否则返回false</returns>
        protected bool IsStateMachineTerminated()
        {
            return _sm?.IsTerminated ?? true;
        }
        
        /// <summary>
        /// 检查状态机是否可以继续运行
        /// </summary>
        /// <returns>如果状态机可以继续运行返回true，否则返回false</returns>
        protected bool CanStateMachineContinue()
        {
            return _sm?.CanContinue() ?? false;
        }
        
        /// <summary>
        /// 获取状态机的终止原因
        /// </summary>
        /// <returns>终止原因，如果未终止则返回空字符串</returns>
        protected string GetStateMachineTerminationReason()
        {
            return _sm?.TerminationReason ?? string.Empty;
        }
        
        /// <summary>
        /// 获取状态机的终止时间戳
        /// </summary>
        /// <returns>终止时间戳，如果未终止则返回0</returns>
        protected long GetStateMachineTerminatedTime()
        {
            return _sm?.TerminatedTime ?? 0;
        }
        
        /// <summary>
        /// 切换到指定节点（如果状态机未终止）
        /// </summary>
        /// <param name="nodeType">目标节点类型</param>
        /// <param name="checkTerminated">是否检查终止状态，默认为true</param>
        protected void SwitchToNode(System.Type nodeType, bool checkTerminated = true)
        {
            if (_sm == null)
            {
                AppLogger.Warning("无法切换节点：状态机引用为空");
                return;
            }
            
            if (checkTerminated && _sm.IsTerminated)
            {
                AppLogger.Warning($"无法切换节点：状态机已终止，原因：{_sm.TerminationReason}");
                return;
            }
            
            _sm.SwitchNode(nodeType);
        }
        
        /// <summary>
        /// 切换到指定节点（泛型版本）
        /// </summary>
        /// <typeparam name="TNode">目标节点类型</typeparam>
        /// <param name="checkTerminated">是否检查终止状态，默认为true</param>
        protected void SwitchToNode<TNode>(bool checkTerminated = true) where TNode : StateNodeBase
        {
            SwitchToNode(typeof(TNode), checkTerminated);
        }
        
        #endregion
    }
}