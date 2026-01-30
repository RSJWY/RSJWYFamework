using System.Collections.Generic;
using System.Reflection;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 加载热更代码
    /// </summary>
    public class LoadHotCodeAsyncOperation : AppStateMachineAsyncOperation
    {

        public LoadHotCodeAsyncOperation()
        {
            // 1. 创建状态机实例 (现在可以使用 this 了，因为 base() 已经执行完毕)
            var sm = new StateMachine<LoadHotCodeAsyncOperation>(this, "加载热更代码");
            
            // 2. 配置状态机节点
            sm.AddNode(new LoadDLLByteNode());
            sm.AddNode(new LoadHotCodeNode());
            sm.AddNode(new LoadHotCodeDoneNode());
            // 3. 初始化基类
            InitStateMachine(sm, typeof(LoadDLLByteNode));
        }
    }
}