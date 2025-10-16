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
        /// 延迟切换的目标节点类型 - 每帧最多处理一个延迟切换
        /// </summary>
        private Type _pendingSwitchType = null;
        
        /// <summary>
        /// 状态机是否已结束
        /// </summary>
        public bool IsTerminated { get; private set; } = false;
        
        /// <summary>
        /// 状态机结束时间戳
        /// </summary>
        public long TerminatedTime { get; private set; } = 0;
        
        /// <summary>
        /// 状态机结束原因
        /// </summary>
        public string TerminationReason { get; private set; } = string.Empty;
        
        /// <summary>
        /// 任意节点切换事件（上一个离开的节点、下一个进入的节点）
        /// </summary>
        public event Action<StateNodeBase, StateNodeBase> ProcedureSwitchEvent;
        
        /// <summary>
        /// 状态机结束事件
        /// </summary>
        public event Action<StateMachine, string> StateMachineTerminatedEvent;
        
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
        public void OnUpdate()
        {
            // 如果状态机已终止，则不执行更新
            if (IsTerminated)
                return;
                
            // 处理延迟切换（每帧最多处理一个）
            ProcessPendingSwitch();
            
            _currentProcedureBase?.OnUpdate();
        }
        
        /// <summary>
        /// 处理延迟切换（每帧最多处理一个）
        /// </summary>
        private void ProcessPendingSwitch()
        {
            if (_pendingSwitchType != null)
            {
                var targetType = _pendingSwitchType;
                _pendingSwitchType = null; // 清空延迟切换标记
                try
                {
                    SwitchNodeImmediate(targetType);
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"延迟切换到节点 {targetType.Name} 失败：{ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 秒更新
        /// </summary>
        public void OnUpdateSecond()
        {
            if (IsTerminated)
                return;
                
            _currentProcedureBase?.OnUpdateSecond();
        }
        
        /// <summary>
        /// 秒更新
        /// <remarks>不受时间缩放影响秒更新</remarks>
        /// </summary>
        public void OnUpdateSecondUnScaleTime()
        {
            if (IsTerminated)
                return;
                
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
                blackboard[key] = value;
        }
        /// <summary>
        /// 获取黑板数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetBlackboardValue(string key)
        {
            // 参数有效性检查
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("键不能为空或空字符串", nameof(key));
            }

            // 尝试获取值
            if (blackboard.TryGetValue(key, out object value))
            {
                return value;
            }
            else
            {
                AppLogger.Warning($"未能从黑板中获取数据：{key}");
                return null; // 找不到时返回 null
            }
        }

        /// <summary>
        /// 获取黑板数据
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetBlackboardValue<T>(string key)
        {
            // 参数有效性检查
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("键不能为空或空字符串", nameof(key));
            }

            // 获取值并进行类型转换
            var value = GetBlackboardValue(key);
            if (value is T typedValue)
            {
                return typedValue;
            }
            else
            {
                // 如果未找到或类型不匹配，返回默认值
                return default;
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
            return _currentProcedureBase?.GetType();
        }
        /// <summary>
        /// 切换到指定节点
        /// </summary>
        public void SwitchNode<TStateNodeBase>() where TStateNodeBase : StateNodeBase
        {
            SwitchNode(typeof(TStateNodeBase), false);
        }
        
        /// <summary>
        /// 切换到指定节点
        /// </summary>
        /// <param name="type">要切换到的节点类型</param>
        /// <param name="isNextUpadeSwitch">是否在下一帧切换，true为下一帧切换，false为立即切换</param>
        /// <exception cref="AppException"></exception>
        public void SwitchNode(Type type, bool isNextUpadeSwitch = false)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), "节点类型不能为空");
                
            if (IsTerminated)
            {
                AppLogger.Warning($"状态机 {st_Name} 已终止，无法切换到节点 {type.Name}");
                return;
            }
                
            if (isNextUpadeSwitch)
            {
                // 设置延迟切换目标，如果已有延迟切换则覆盖（确保每帧最多一个延迟切换）
                if (_pendingSwitchType != null)
                {
                    AppLogger.Warning($"覆盖之前的延迟切换请求：{_pendingSwitchType.Name} -> {type.Name}");
                }
                _pendingSwitchType = type;
                AppLogger.Log($"设置延迟切换到节点：{type.Name}，将在下一帧执行");
            }
            else
            {
                // 立即切换
                SwitchNodeImmediate(type);
            }
        }
        
        /// <summary>
        /// 立即切换到指定节点（内部方法）
        /// </summary>
        /// <param name="type">要切换到的节点类型</param>
        /// <exception cref="AppException"></exception>
        private void SwitchNodeImmediate(Type type)
        {
            if (!typeof(StateNodeBase).IsAssignableFrom(type))
                throw new AppException($"切换节点失败：节点 {type.Name} 并非继承自节点基类！");
                
            if (!Procedures.TryGetValue(type, out var nextProcedure))
            {
                throw new AppException($"切换节点失败：不存在节点 {type.Name} 或者节点未激活！");
            }
            
            // 如果是同一个节点，直接返回
            if (_currentProcedureBase == nextProcedure)
                return;

            var lastProcedure = _currentProcedureBase;
            lastProcedure?.OnLeave(nextProcedure);
            
            _currentProcedureBase = nextProcedure;
            nextProcedure.OnEnter(lastProcedure);

            ProcedureSwitchEvent?.Invoke(lastProcedure, nextProcedure);
        }
        /// <summary>
        /// 切换到下一节点，
        /// 如果当前是最后一个节点，则切换到第一个节点
        /// </summary>
        public void SwitchNextNode(bool isLoop=false)
        {
            if (_currentProcedureBase == null)
            {
                AppLogger.Warning("切换节点失败：当前没有活动节点！");
                return;
            }
            
            if (ProcedureTypes.Count == 0)
            {
                AppLogger.Warning("切换节点失败：没有可用的节点！");
                return;
            }
            
            int index = ProcedureTypes.IndexOf(_currentProcedureBase.GetType());
            if (index >= ProcedureTypes.Count - 1)
            {
                if (isLoop)
                    SwitchNode(ProcedureTypes[0], false);
                else
                    AppLogger.Warning($"切换节点失败：当前节点 {_currentProcedureBase.GetType().Name} 是最后一个节点，不能切换到下一个节点！");
            }
            else
            {
                SwitchNode(ProcedureTypes[index + 1], false);
            }
        }
        /// <summary>
        /// 开始节点，从指定的开始源开始
        /// </summary>
        /// <typeparam name="TStateNodeBase"></typeparam>
        public void StartNode<TStateNodeBase>()
        {
            SwitchNode(typeof(TStateNodeBase), false);
        }
        
        /// <summary>
        /// 开始节点，从指定类型开始
        /// </summary>
        /// <param name="nodeType">要启动的节点类型</param>
        public void StartNode(Type nodeType)
        {
            if (nodeType == null)
                throw new ArgumentNullException(nameof(nodeType), "节点类型不能为空");
                
            SwitchNode(nodeType, false);
        }
        
        /// <summary>
        /// 开始节点，从第一个开始
        /// </summary>
        public void StartNode()
        {
            if (ProcedureTypes.Count == 0)
            {
                throw new AppException("启动节点失败：没有可用的节点！");
            }
            SwitchNode(ProcedureTypes[0], false);
        }
        
        /// <summary>
        /// 判断一个节点是否存在
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsExistNode<TStateNodeBase>()where TStateNodeBase : StateNodeBase
        {
            return Procedures.ContainsKey(typeof(TStateNodeBase));
        }
        /// <summary>
        /// 添加一个节点
        /// </summary>
        /// <param name="procedureBase"></param>
        /// <exception cref="RSJWYException"></exception>
        public void AddNode(StateNodeBase procedureBase)
        {
            if (procedureBase == null)
                throw new ArgumentNullException(nameof(procedureBase), "节点实例不能为空");
                
            Type _t = procedureBase.GetType();
            
            if (!typeof(StateNodeBase).IsAssignableFrom(_t))
                throw new AppException( $"增加节点失败：节点 {_t.Name} 并非继承自节点基类！");
            
            if (!Procedures.ContainsKey(_t))
            {
                Procedures.Add(_t, procedureBase);
                ProcedureTypes.Add(_t);
                procedureBase._sm = this;
                
                try
                {
                    procedureBase.OnInit();
                    AppLogger.Log($"成功添加并初始化节点：{_t.Name}");
                }
                catch (Exception ex)
                {
                    // 如果初始化失败，需要清理已添加的节点
                    Procedures.Remove(_t);
                    ProcedureTypes.Remove(_t);
                    procedureBase._sm = null;
                    AppLogger.Error($"节点 {_t.Name} 初始化失败：{ex.Message}");
                    throw new AppException($"添加节点失败：节点 {_t.Name} 初始化时发生错误", ex);
                }
            }
            else
            {
                throw new AppException($"添加节点失败：节点 {_t.Name} 已存在！");
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
            if (type == null)
                throw new ArgumentNullException(nameof(type), "节点类型不能为空");
                
            if (Procedures.TryGetValue(type,out var procedure))
            {
                try
                {
                    procedure.OnClose();
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"节点 {type.Name} 关闭时发生错误：{ex.Message}");
                }
                
                Procedures.Remove(type);
                ProcedureTypes.Remove(type);
                if (_currentProcedureBase == procedure)
                {
                    _currentProcedureBase = null;
                    AppLogger.Warning($"移除了当前活动节点 {type.Name}，状态机当前无活动节点");
                }
                else
                {
                    AppLogger.Log($"成功移除节点：{type.Name}");
                }
            }
            else
            {
                throw new AppException( $"移除节点失败：节点 {type.Name} 不存在！");
            }
        }

        #endregion
        
        #region 状态机结束管理
        
        /// <summary>
        /// 自动终止状态机（通常在最终节点中调用）
        /// </summary>
        /// <param name="reason">终止原因</param>
        public void AutoTerminate(string reason = "流程完成")
        {
            if (IsTerminated)
            {
                AppLogger.Warning($"状态机 {st_Name} 已经结束，无法自动终止");
                return;
            }
            
            AppLogger.Log($"状态机 {st_Name} 自动终止，原因：{reason}");
            Terminate(reason);
        }
        
        /// <summary>
        /// 终止状态机
        /// </summary>
        /// <param name="reason">结束原因</param>
        public void Terminate(string reason = "手动终止")
        {
            if (IsTerminated)
            {
                AppLogger.Warning($"状态机 {st_Name} 已经结束，无法重复终止");
                return;
            }
            
            try
            {
                // 停止当前节点
                if (_currentProcedureBase != null)
                {
                    _currentProcedureBase.OnLeave(null);
                    AppLogger.Log($"状态机 {st_Name} 终止时停止了当前节点：{_currentProcedureBase.GetType().Name}");
                }
                
                // 设置结束状态
                IsTerminated = true;
                TerminatedTime = Utility.Timestamp.UnixTimestampMilliseconds;
                TerminationReason = reason ?? "未知原因";
                _currentProcedureBase = null;
                _pendingSwitchType = null;
                
                // 触发结束事件
                StateMachineTerminatedEvent?.Invoke(this, TerminationReason);
                
                AppLogger.Log($"状态机 {st_Name} 已终止，原因：{TerminationReason}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"终止状态机 {st_Name} 时发生错误：{ex.Message}");
                // 即使出错也要设置结束状态
                IsTerminated = true;
                TerminatedTime = Utility.Timestamp.UnixTimestampMilliseconds;
                TerminationReason = $"终止时出错：{ex.Message}";
            }
        }
        
        /// <summary>
        /// 检查状态机是否可以继续运行
        /// </summary>
        /// <returns>如果已终止返回false，否则返回true</returns>
        public bool CanContinue()
        {
            return !IsTerminated;
        }
        
        /// <summary>
        /// 重置状态机（清除结束状态，允许重新启动）
        /// </summary>
        public void Reset()
        {
            if (!IsTerminated)
            {
                AppLogger.Warning($"状态机 {st_Name} 尚未结束，无需重置");
                return;
            }
            
            try
            {
                IsTerminated = false;
                TerminatedTime = 0;
                TerminationReason = string.Empty;
                _currentProcedureBase = null;
                _pendingSwitchType = null;
                
                AppLogger.Log($"状态机 {st_Name} 已重置，可以重新启动");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"重置状态机 {st_Name} 时发生错误：{ex.Message}");
            }
        }
        
        #endregion

    }
}