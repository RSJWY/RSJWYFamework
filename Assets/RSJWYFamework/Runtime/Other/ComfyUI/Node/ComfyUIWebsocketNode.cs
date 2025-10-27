namespace RSJWYFamework.Runtime.Node
{
    public class ComfyUIWebsocketNode: StateNodeBase
    {
        /// <summary>
        /// ComfyUI工作任务ID
        /// </summary>
        private PromptInfo promptInfo;


        public override void OnInit()
        {
            
        }

        public override void OnClose()
        {
        }

        public override void OnEnter(StateNodeBase lastProcedureBase)
        {
            promptInfo=GetBlackboardValue<PromptInfo>("PROMPTID");
        }

        public override void OnLeave(StateNodeBase nextProcedureBase, bool isRestarting = false)
        {
            
        }
    }
}