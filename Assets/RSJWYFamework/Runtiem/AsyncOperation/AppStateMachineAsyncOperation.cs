
namespace RSJWYFamework.Runtiem
{
    /// <summary>
    /// 可异步等待的状态机
    /// </summary>
    public abstract class AppStateMachineAsyncOperation:AppGameAsyncOperation
    {
        protected StateMachine _stateMachine;
    }
}