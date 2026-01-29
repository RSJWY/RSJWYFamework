using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    public class StateMachine
    {
        private struct StateChangeRequest
        {
            public enum RequestType { Switch, Stop, Restart }
            public RequestType Type;
            public Type TargetNodeType;
            public int StatusCode;
            public string Reason;
        }

        private readonly Queue<StateChangeRequest> _requestQueue = new();
        private bool _isTransitioning = false;

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
        /// 当前状态码（用户自定义状态标识）
        /// </summary>
        public int StatusCode { get; private set; } = 0;
        
        /// <summary>
        /// 任意节点切换事件（上一个离开的节点、下一个进入的节点）
        /// </summary>
        public event Action<StateNodeBase, StateNodeBase> ProcedureSwitchEvent;
        
        /// <summary>
        /// 状态机结束事件（状态机实例、终止原因、状态码、是否重启）
        /// </summary>
        public event Action<StateMachine, string, int,bool> StateMachineTerminatedEvent;
        
        /// <summary>
        /// 状态机重启事件（状态机实例、重启原因、重启前的节点、重启后的节点类型、状态码）
        /// </summary>
        public event Action<StateMachine, string, StateNodeBase, Type, int> StateMachineRestartEvent;
        
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
            // 如果状态机已终止或已停止，则不执行更新
            if (IsTerminated || IsPaused)
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
                    SwitchNode(targetType);
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
            // 如果状态机已终止或已停止，则不执行更新
            if (IsTerminated || IsPaused)
                return;
                
            _currentProcedureBase?.OnUpdateSecond();
        }
        
        /// <summary>
        /// 秒更新
        /// <remarks>不受时间缩放影响秒更新</remarks>
        /// </summary>
        public void OnUpdateSecondUnScaleTime()
        {
            // 如果状态机已终止或已停止，则不执行更新
            if (IsTerminated || IsPaused)
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
        
        /// <summary>
        /// 删除黑板中的指定键值对
        /// </summary>
        /// <param name="key">要删除的键</param>
        /// <returns>如果成功删除返回true，否则返回false</returns>
        public bool RemoveBlackboardValue(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                AppLogger.Warning("删除黑板数据时键不能为空");
                return false;
            }
            
            return blackboard.Remove(key);
        }
        
        /// <summary>
        /// 检查黑板中是否包含指定键
        /// </summary>
        /// <param name="key">要检查的键</param>
        /// <returns>如果包含该键返回true，否则返回false</returns>
        public bool HasBlackboardKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            
            return blackboard.ContainsKey(key);
        }
        
        /// <summary>
        /// 获取黑板中所有键的数量
        /// </summary>
        /// <returns>黑板中键值对的数量</returns>
        public int GetBlackboardCount()
        {
            return blackboard.Count;
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
        public void SwitchNode<TStateNodeBase>( bool isNextUpadeSwitch = false,int statusCode = 0) where TStateNodeBase : StateNodeBase
        {
            SwitchNode(typeof(TStateNodeBase), isNextUpadeSwitch, statusCode);
        }
        
        /// <summary>
        /// 切换到指定节点
        /// </summary>
        /// <param name="type">要切换到的节点类型</param>
        /// <param name="isNextUpadeSwitch">是否在下一帧切换，true为下一帧切换，false为立即切换</param>
        /// <param name="statusCode">状态码</param>
        /// <exception cref="AppException"></exception>
        /// <summary>
        /// 切换到指定节点 (Async Queue)
        /// </summary>
        public void SwitchNode(Type type, bool isNextUpadeSwitch = false, int statusCode = 0)
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
                if (_pendingSwitchType != null)
                {
                    AppLogger.Warning($"覆盖之前的延迟切换请求：{_pendingSwitchType.Name} -> {type.Name}");
                }
                _pendingSwitchType = type;
                AppLogger.Log($"设置延迟切换到节点：{type.Name}，状态码：{statusCode}，将在下一帧执行");
            }
            else
            {
                _requestQueue.Enqueue(new StateChangeRequest {
                    Type = StateChangeRequest.RequestType.Switch,
                    TargetNodeType = type,
                    StatusCode = statusCode
                });
                ProcessQueue().Forget();
            }
        }
        /// <summary>
        /// 处理状态切换请求队列
        /// </summary>
        private async UniTaskVoid ProcessQueue()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            try
            {
                while (_requestQueue.Count > 0)
                {
                    var req = _requestQueue.Dequeue();
                    try
                    {
                        switch (req.Type)
                        {
                            case StateChangeRequest.RequestType.Switch:
                                await ExecuteSwitchAsync(req.TargetNodeType, req.StatusCode);
                                break;
                            case StateChangeRequest.RequestType.Stop:
                                await ExecuteStopAsync(req.StatusCode, req.Reason);
                                break;
                            case StateChangeRequest.RequestType.Restart:
                                await ExecuteRestartAsync(req.TargetNodeType, req.Reason, req.StatusCode);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"状态机处理请求 {req.Type} 失败: {ex}");
                    }
                }
            }
            finally { _isTransitioning = false; }
        }

        private async UniTask ExecuteSwitchAsync(Type type, int statusCode)
        {
            if (!typeof(StateNodeBase).IsAssignableFrom(type))
                throw new AppException($"切换节点失败：节点 {type.Name} 并非继承自节点基类！");
                
            if (!Procedures.TryGetValue(type, out var nextProcedure))
                throw new AppException($"切换节点失败：不存在节点 {type.Name} 或者节点未激活！");
            
            if (_currentProcedureBase == nextProcedure) return;

            StatusCode = statusCode;
            var lastProcedure = _currentProcedureBase;
            
            if (lastProcedure != null)
                await lastProcedure.OnLeave(nextProcedure, false);
            
            _currentProcedureBase = nextProcedure;
            await nextProcedure.OnEnter(lastProcedure);

            ProcedureSwitchEvent?.Invoke(lastProcedure, nextProcedure);
        }

        private async UniTask ExecuteStopAsync(int statusCode, string reason)
        {
             if (IsTerminated) return;

            if (_currentProcedureBase != null)
            {
                try 
                {
                    await _currentProcedureBase.OnLeave(null, false);
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"状态机 {st_Name} 停止节点时出错：{ex.Message}");
                }
                _currentProcedureBase = null;
            }

            IsTerminated = true;
            StatusCode = statusCode;
            StateMachineTerminatedEvent?.Invoke(this, reason, StatusCode, false);
        }

        private async UniTask ExecuteRestartAsync(Type startNodeType, string reason, int statusCode)
        {
            if (!IsTerminated)
            {
                if (_currentProcedureBase != null)
                {
                    try { await _currentProcedureBase.OnLeave(null, true); }
                    catch (Exception ex) { AppLogger.Error($"状态机 {st_Name} 重启停止节点时出错：{ex.Message}"); }
                    _currentProcedureBase = null;
                }
            }

            IsTerminated = false;
            IsPaused = false;
            StatusCode = statusCode;
            _pendingSwitchType = null;
            StateMachineRestartEvent?.Invoke(this, reason, null, startNodeType, statusCode);

            if (startNodeType != null) StartNode(startNodeType);
            else StartNode();
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
        
        #region 状态机生命周期管理 (Simplified)

        /// <summary>
        /// 停止状态机（清空当前状态，重置标志位）
        /// </summary>
        /// <param name="reason">停止原因</param>
        /// <param name="statusCode">最终状态码</param>
        /// <summary>
        /// 停止状态机 (Async Queue)
        /// </summary>
        /// <param name="reason">停止原因</param>
        /// <param name="statusCode">最终状态码</param>
        public void Stop( int statusCode = 0,string reason = "Stopped")
        {
            if (IsTerminated) return;
            
            _requestQueue.Enqueue(new StateChangeRequest {
                Type = StateChangeRequest.RequestType.Stop,
                StatusCode = statusCode,
                Reason = reason
            });
            ProcessQueue().Forget();
        }

        /// <summary>
        /// 暂停状态机
        /// </summary>
        public bool IsPaused { get; set; } = false;

        /// <summary>
        /// 重启状态机 (Async Queue)
        /// </summary>
        /// <param name="startNodeType">启动节点类型，null则使用默认</param>
        /// <param name="reason">重启原因</param>
        /// <param name="statusCode">状态码</param>
        public void Restart(Type startNodeType = null, string reason = "Restart", int statusCode = 0)
        {
            _requestQueue.Enqueue(new StateChangeRequest {
                Type = StateChangeRequest.RequestType.Restart,
                TargetNodeType = startNodeType,
                StatusCode = statusCode,
                Reason = reason
            });
            ProcessQueue().Forget();
        }
        
        #endregion

    }

    /// <summary>
    /// 泛型状态机 (2025 Refactored)
    /// 提供强类型的 Owner 访问
    /// </summary>
    /// <typeparam name="T">持有者类型</typeparam>
    public class StateMachine<T> : StateMachine where T : class
    {
        /// <summary>
        /// 强类型的持有者
        /// </summary>
        public new T Owner => (T)base.Owner;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner">持有者实例</param>
        /// <param name="name">状态机名称</param>
        /// <param name="priority">优先级</param>
        public StateMachine(T owner, string name = null, uint priority = 0) : base(owner, name, priority)
        {
        }

        /// <summary>
        /// 添加节点（类型安全检查）
        /// </summary>
        /// <param name="procedureBase">状态节点</param>
        public new void AddNode(StateNodeBase procedureBase)    
        {
            // 🔒 安全锁：确保添加的节点类型与状态机的泛型类型 T 一致
            if (!(procedureBase is StateNodeBase<T>))
            {
                throw new AppException($"类型不匹配错误！\n" +
                                     $"状态机 Owner 类型: {typeof(T).Name}\n" +
                                     $"试图添加的节点类型: {procedureBase.GetType().Name}\n" +
                                     $"该节点必须继承自: StateNodeBase<{typeof(T).Name}>");
            }

            base.AddNode(procedureBase);
        }

        /// <summary>
        /// 添加节点（类型安全检查 - 泛型版）
        /// </summary>
        public new void AddNode<TNode>() where TNode : StateNodeBase, new()
        {
            // 实例化并检查
            var node = new TNode();
            AddNode(node); // 调用上面的 AddNode 进行检查
        }
    }
}