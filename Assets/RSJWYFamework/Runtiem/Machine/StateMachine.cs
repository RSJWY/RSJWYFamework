using System;
using System.Collections.Generic;
namespace RSJWYFamework.Runtime
{
    public class StateMachine
    {
         public string st_Name;
         /// <summary>
         /// 状态机持有者
         /// </summary>
         public System.Object Owner { private set; get; }
         
         public uint Priority{ private set; get; }
        
        /// <summary>
        /// 当前节点
        /// </summary>
        private StateNodeBase _currentProcedureBase;
        
        /// <summary>
        /// 任意节点切换事件（上一个离开的节点、下一个进入的节点）
        /// </summary>
        public event Action<StateNodeBase, StateNodeBase> ProcedureSwitchEvent;
        
        /// <summary>
        /// 黑板数据
        /// </summary>
        private readonly Dictionary<string, System.Object> blackboard = new (100);
        
        /// <summary>
        /// 节点表
        /// </summary>
        private readonly Dictionary<Type, StateNodeBase> Procedures = new(100);
        /// <summary>
        /// 所有节点
        /// </summary>
        private readonly List<Type> ProcedureTypes = new(100);


        public StateMachine(Object Owner,string name,uint priority=0)
        {
            this.Owner = Owner;
            this.Priority = priority;
            st_Name=string.IsNullOrEmpty(name)?Utility.Timestamp.UnixTimestampMilliseconds.ToString():name;
        }
        
        /// <summary>
        /// 帧更新
        /// </summary>
        /// <param name="time"></param>
        /// <param name="realtime"></param>
        public void OnUpdate()
        {
            _currentProcedureBase?.OnUpdate();
        }
        /// <summary>
        /// 秒更新
        /// </summary>
        /// <param name="time"></param>
        /// <param name="realtime"></param>
        public void OnUpdateSecond()
        {
            _currentProcedureBase?.OnUpdateSecond();
        }
        
        #region 黑板行为
        /// <summary>
        /// 设置黑板数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetBlackboardValue(string key, object value)
        {
            if (blackboard.ContainsKey(key))
                blackboard.Add(key, value);
            else
                blackboard[key] = value;
        }
        /// <summary>
        /// 获取黑板数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetBlackboardValue(string key)
        {
            if (blackboard.TryGetValue(key, out Object value))
            {
                return value;
            }
            else
            {
                AppLogger.Warning($"未能从黑板中获取数据：{key}");
                return null;
            }
        }
        /// <summary>
        /// 清空黑板数据
        /// </summary>
        public void ClearBlackboard()
        {
            blackboard.Clear();
        }

        #endregion


        #region 节点行为

        /// <summary>
        /// 获取现在正在执行的节点
        /// </summary>
        /// <returns></returns>
        public Type GetNowNode()
        {
            return _currentProcedureBase.GetType();
        }
        /// <summary>
        /// 切换到指定节点
        /// </summary>
        public void SwitchNode<TStateNodeBase>() where TStateNodeBase : StateNodeBase
        {
            SwitchNode(typeof(TStateNodeBase));
        }
        
        /// <summary>
        /// 切换到指定节点
        /// </summary>
        /// <param name="type"></param>
        /// <exception cref="RSJWYException"></exception>
        public void SwitchNode(Type type)
        {
            if (type.IsAssignableFrom(typeof(StateNodeBase)))
                throw new AppException($"切换节点失败：节点 {type.Name} 并非继承自节点基类！");
            if (Procedures.ContainsKey(type))
            {
                if (_currentProcedureBase == Procedures[type])
                    return;

                var lastProcedure = _currentProcedureBase;
                var nextProcedure = Procedures[type];
                if (lastProcedure != null)
                {
                    lastProcedure.OnLeave(nextProcedure);
                }
                _currentProcedureBase = nextProcedure;
                nextProcedure.OnEnter(lastProcedure);

                ProcedureSwitchEvent?.Invoke(lastProcedure, nextProcedure);
            }
            else
            {
                throw new AppException( $"切换节点失败：不存在节点 {type.Name} 或者节点未激活！");
            }
        }
        /// <summary>
        /// 切换到下一节点，
        /// 如果当前是最后一个节点，则切换到第一个节点
        /// </summary>
        public void SwitchNextNode()
        {
            int index = ProcedureTypes.IndexOf(_currentProcedureBase.GetType());
            if (index >= ProcedureTypes.Count - 1)
            {
                SwitchNode(ProcedureTypes[0]);
            }
            else
            {
                SwitchNode(ProcedureTypes[index + 1]);
            }
        }
        /// <summary>
        /// 开始节点，从指定的开始源开始
        /// </summary>
        /// <typeparam name="TStateNodeBase"></typeparam>
        public void StartNode<TStateNodeBase>()
        {
            SwitchNode(typeof(TStateNodeBase));
        }
        /// <summary>
        /// 开始节点，从第一个开始
        /// </summary>
        public void StartNode()
        {
            SwitchNode(ProcedureTypes[0]);
        }
        
        /// <summary>
        /// 判断一个节点是否存在
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsExistNode<TStateNodeBase>()where TStateNodeBase : StateNodeBase
        {
            return Procedures.ContainsKey(typeof(StateNodeBase));
        }
        /// <summary>
        /// 添加一个节点
        /// </summary>
        /// <param name="procedureBase"></param>
        /// <exception cref="RSJWYException"></exception>
        public void AddNode(StateNodeBase procedureBase)
        {
            Type _t = procedureBase.GetType();
            
            if (_t.IsAssignableFrom(typeof(StateNodeBase)))
                throw new AppException( $"增加节点失败：节点 {_t.Name} 并非继承自节点基类！");
            
            if (!Procedures.ContainsKey(_t))
            {
                Procedures.Add(_t, procedureBase);
                ProcedureTypes.Add(_t);
                procedureBase._sm = this;
                procedureBase.OnInit();
            }
            else
            {
                throw new AppException($"添加节点节点失败：节点 {_t.Name} 已存在！");
            }
        }
        /// <summary>
        /// 添加一个节点
        /// 使用本方法必须传递的是一个包含无参构造函数的节点类，否则会抛出异常
        /// 否则请使用AddProcedure(ProcedureBase procedureBase)传递实例化好的
        /// </summary>
        public void AddNode<TStateNodeBase>() where TStateNodeBase : StateNodeBase,new()
        {
            var type = typeof(TStateNodeBase);
            var procedure = Activator.CreateInstance<TStateNodeBase>();
            if (!Procedures.ContainsKey(type))
            {
                Procedures.Add(type, procedure);
                ProcedureTypes.Add(type);
                procedure._sm = this;
                procedure.OnInit();
            }
            else
            {
                throw new AppException($"添加节点失败：节点 {type.Name} 已存在！");
            }
        }
        /// <summary>
        /// 移除一个节点
        /// </summary>
        /// <param name="type"></param>
        /// <exception cref="RSJWYException"></exception>
        public void RemoveNode(Type type)
        {
            if (!Procedures.TryGetValue(type,out var procedure))
            {
                procedure.OnClose();
                Procedures.Remove(type);
                ProcedureTypes.Remove(type);
                if (_currentProcedureBase == procedure)
                {
                    _currentProcedureBase = null;
                }
            }
            else
            {
                throw new AppException( $"移除节点失败：节点 {type.Name} 不存在！");
            }
        }

        #endregion

    }
}