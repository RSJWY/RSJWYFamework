namespace RSJWYFamework.Runtime
{
    public abstract class StateNodeBase
    {
        /// <summary>
        /// 关联到流程控制器
        /// </summary>
        public StateMachine _sm { get;internal set; }

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

        /// <summary>
        /// 流程帧更新
        /// </summary>
        public virtual void OnUpdate(){}

        /// <summary>
        /// 流程秒更新
        /// </summary>
        public virtual void OnUpdateSecond(){}
    }
}