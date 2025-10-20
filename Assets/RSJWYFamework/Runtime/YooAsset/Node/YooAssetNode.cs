namespace RSJWYFamework.Runtime
{
    public class YooAssetNode : StateNodeBase
    { 
        protected int _retryCount = 0;
    
        public override void OnInit()
        {
            // 初始化时重置重试计数器
            _retryCount = 0;
        }

        public override void OnClose()
        {
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
        }

        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
        }
        
        /// <summary>
        /// 判断是否应该重试
        /// </summary>
        /// <param name="maxRetries">最大重试次数，-1表示无限重试</param>
        /// <returns>是否应该重试</returns>
        protected virtual bool ShouldRetry(int maxRetries)
        {
            // -1 表示无限重试
            if (maxRetries == -1)
            {
                return true;
            }
            
            // 检查是否还有重试次数
            return _retryCount < maxRetries;
        }
        
        /// <summary>
        /// 获取剩余重试次数的描述
        /// </summary>
        /// <param name="maxRetries">最大重试次数</param>
        /// <returns>剩余重试次数描述</returns>
        protected virtual string GetRemainingRetries(int maxRetries)
        {
            if (maxRetries == -1)
            {
                return "无限";
            }
            
            return (maxRetries - _retryCount).ToString();
        }
    }
}