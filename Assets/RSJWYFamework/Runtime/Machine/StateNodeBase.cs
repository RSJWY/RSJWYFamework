using System;
using Cysharp.Threading.Tasks;

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
        public virtual void OnInit(){}
        
        /// <summary>
        /// 流程关闭
        /// 移除流程时调用
        /// </summary>
        public virtual void OnClose(){}

        /// <summary>
        /// 进入当前流程 (Async)
        /// </summary>
        /// <param name="lastProcedureBase">上一个离开的流程</param>
        public abstract UniTask OnEnter(StateNodeBase lastProcedureBase);

        /// <summary>
        /// 离开当前流程 (Async)
        /// </summary>
        /// <param name="nextProcedureBase">下一个进入的流程</param>
        public abstract UniTask OnLeave(StateNodeBase nextProcedureBase);
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
        
        /// <summary>
        /// 节点停止回调
        /// 当状态机停止时调用，用于节点的停止处理
        /// </summary>
        /// <param name="reason">停止原因</param>
        public virtual void OnStop(string reason = "状态机停止"){}
        #endregion
        
        // 移除冗余的“中间人”代理方法 (Option 1 改造)
        // 子类请直接通过 _sm 访问状态机功能
        // 或继承 StateNodeBase<T> 以获得强类型支持
    }

    /// <summary>
    /// 泛型状态节点基类 (2025 Refactored)
    /// 提供强类型的 Owner 和 Machine 访问
    /// </summary>
    /// <typeparam name="T">持有者类型</typeparam>
    public abstract class StateNodeBase<T> : StateNodeBase where T : class
    {
        /// <summary>
        /// 强类型的状态机引用
        /// </summary>
        public new StateMachine<T> Machine => _sm as StateMachine<T>;

        /// <summary>
        /// 强类型的持有者引用
        /// </summary>
        public T Owner => Machine?.Owner;
    }
}