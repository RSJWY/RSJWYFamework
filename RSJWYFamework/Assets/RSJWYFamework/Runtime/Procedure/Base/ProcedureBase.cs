namespace RSJWYFamework.Runtime.Procedure.Base
{
    /// <summary>
    /// 流程基类
    /// </summary>
    public abstract class ProcedureBase
    {
        /// <summary>
        /// 关联到流程控制器
        /// </summary>
        public IProcedureController pc;
        /// <summary>
        /// 流程初始化
        /// 添加流程时调用
        /// </summary>
        public virtual void OnInit() { }
        /// <summary>
        /// 流程关闭
        /// 移除流程时调用
        /// </summary>
        public virtual void OnClose(){}

        /// <summary>
        /// 进入当前流程
        /// </summary>
        /// <param name="lastProcedure">上一个离开的流程</param>
        public void OnEnter(ProcedureBase lastProcedure){ }

        /// <summary>
        /// 离开当前流程
        /// </summary>
        /// <param name="nextProcedure">下一个进入的流程</param>
        public void OnLeave(ProcedureBase nextProcedure){ }

        /// <summary>
        /// 流程帧更新
        /// </summary>
        public void OnUpdate(){ }

        /// <summary>
        /// 流程秒更新
        /// </summary>
        public void OnUpdateSecond(){ }
    }
}