using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 状态机生命周期管理器
    /// 可以把状态机放入本控制器下，自动接管生命周期。
    /// </summary>
    [Module]
    public class StateMachineManager:ModuleBase
    {
        Dictionary<string,StateMachine>StateMachineDic = new(100);
        
        /// <summary>
        /// 是否启用自动清理已结束的状态机
        /// </summary>
        public bool AutoCleanupTerminated { get; set; } = true;
        
        /// <summary>
        /// 自动清理的延迟时间（毫秒），避免频繁清理
        /// </summary>
        public long CleanupDelayMs { get; set; } = 5000; // 5秒延迟
        
        /// <summary>
        /// 添加一个流程控制器
        /// </summary>
        /// <param name="stateMachine">要添加的状态机</param>
        /// <param name="isAutoRun">是否自动运行</param>
        /// <exception cref="ArgumentNullException">当stateMachine为空时抛出</exception>
        /// <exception cref="AppException">当状态机名称已存在时抛出</exception>
        public void AddStateMachine(StateMachine stateMachine, bool isAutoRun = false) 
        {
            if (stateMachine == null)
                throw new ArgumentNullException(nameof(stateMachine), "状态机不能为空");
                
            if (string.IsNullOrEmpty(stateMachine.st_Name))
                throw new ArgumentException("状态机名称不能为空", nameof(stateMachine));
            
            if (StateMachineDic.ContainsKey(stateMachine.st_Name))
            {
                throw new AppException($"添加状态机失败：状态机 {stateMachine.st_Name} 已存在！");
            }
            
            try
            {
                StateMachineDic.Add(stateMachine.st_Name, stateMachine);
                
                // 订阅状态机结束事件
                if (AutoCleanupTerminated)
                {
                    stateMachine.StateMachineTerminatedEvent += OnStateMachineTerminated;
                }
                
                if (isAutoRun)
                {
                    stateMachine.StartNode();
                }
                AppLogger.Log($"成功添加状态机：{stateMachine.st_Name}，自动运行：{isAutoRun}");
            }
            catch (Exception ex)
            {
                // 如果启动失败，需要清理已添加的状态机
                StateMachineDic.Remove(stateMachine.st_Name);
                AppLogger.Error($"添加状态机 {stateMachine.st_Name} 时发生错误：{ex.Message}");
                throw new AppException($"添加状态机失败：{ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 移除一个流程控制器
        /// </summary>
        /// <param name="st_Name">状态机名称</param>
        /// <exception cref="ArgumentException">当名称为空时抛出</exception>
        /// <exception cref="AppException">当状态机不存在时抛出</exception>
        public void RemoveStateMachine(string st_Name) 
        {
            if (string.IsNullOrEmpty(st_Name))
                throw new ArgumentException("状态机名称不能为空", nameof(st_Name));
            
            if (StateMachineDic.TryGetValue(st_Name, out var stateMachine))
            {
                try
                {
                    // 取消订阅状态机结束事件
                    if (AutoCleanupTerminated)
                    {
                        stateMachine.StateMachineTerminatedEvent -= OnStateMachineTerminated;
                    }
                    
                    // 在移除前尝试停止状态机（如果有当前节点的话）
                    if (stateMachine.GetNowNode() != null)
                    {
                        AppLogger.Warning($"移除状态机 {st_Name} 时，该状态机仍有活动节点");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"停止状态机 {st_Name} 时发生错误：{ex.Message}");
                }
                
                StateMachineDic.Remove(st_Name);
                AppLogger.Log($"成功移除状态机：{st_Name}");
            }
            else
            {
                throw new AppException($"移除状态机失败：状态机 {st_Name} 不存在！");
            }
        }

        /// <summary>
        /// 获取一个流程控制器
        /// </summary>
        /// <param name="st_Name">流程控制器绑定的名字</param>
        /// <returns>找到的状态机，如果不存在则返回null</returns>
        public StateMachine GetStateMachine(string st_Name)
        {
            if (string.IsNullOrEmpty(st_Name))
            {
                AppLogger.Warning("获取状态机失败：名称不能为空");
                return null;
            }
            
            StateMachineDic.TryGetValue(st_Name, out var stateMachine);
            return stateMachine;
        }
        
        /// <summary>
        /// 检查状态机是否存在
        /// </summary>
        /// <param name="st_Name">状态机名称</param>
        /// <returns>如果存在返回true，否则返回false</returns>
        public bool HasStateMachine(string st_Name)
        {
            if (string.IsNullOrEmpty(st_Name))
                return false;
                
            return StateMachineDic.ContainsKey(st_Name);
        }
        
        /// <summary>
        /// 获取当前管理的状态机数量
        /// </summary>
        /// <returns>状态机数量</returns>
        public int GetStateMachineCount()
        {
            return StateMachineDic.Count;
        }
        
        public override void Initialize()
        {
            StateMachineDic.Clear();
            AppLogger.Log("StateMachineManager 初始化完成");
        }

        public override void Shutdown()
        {
            // 正确清理所有状态机资源
            foreach (var kvp in StateMachineDic)
            {
                try
                {
                    var stateMachine = kvp.Value;
                    if (stateMachine.GetNowNode() != null)
                    {
                        AppLogger.Warning($"关闭时状态机 {kvp.Key} 仍有活动节点");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"关闭状态机 {kvp.Key} 时发生错误：{ex.Message}");
                }
            }
            
            StateMachineDic.Clear();
            AppLogger.Log("StateMachineManager 已关闭，所有状态机已清理");
        }

        public override void LifeUpdate()
        {
            // 使用 Values 属性直接遍历，避免创建 KeyValuePair
            foreach (var stateMachine in StateMachineDic.Values)
            {
                try
                {
                    stateMachine.OnUpdate();
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"状态机 {stateMachine.st_Name} 更新时发生错误：{ex.Message}");
                }
            }
        }

        public override void LifePerSecondUpdate()
        {
            foreach (var stateMachine in StateMachineDic.Values)
            {
                try
                {
                    stateMachine.OnUpdateSecond();
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"状态机 {stateMachine.st_Name} 秒更新时发生错误：{ex.Message}");
                }
            }
        }

        public override void LifePerSecondUpdateUnScaleTime()
        {
            foreach (var stateMachine in StateMachineDic.Values)
            {
                try
                {
                    stateMachine.OnUpdateSecondUnScaleTime();
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"状态机 {stateMachine.st_Name} 非缩放秒更新时发生错误：{ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 状态机结束事件处理
        /// </summary>
        /// <param name="stateMachine">结束的状态机</param>
        /// <param name="reason">结束原因</param>
        /// <param name="stateCode">状态码</param>
        private void OnStateMachineTerminated(StateMachine stateMachine, string reason, int stateCode)
        {
            if (!AutoCleanupTerminated)
                return;
            
            AppLogger.Log($"状态机 {stateMachine.st_Name} 已结束，原因：{reason}，状态码：{stateCode} 将在 {CleanupDelayMs}ms 后自动清理");
            
            // 延迟清理，避免立即清理可能导致的问题
            _ = DelayedCleanup(stateMachine.st_Name);
        }
        
        /// <summary>
        /// 延迟清理已结束的状态机
        /// </summary>
        /// <param name="stateMachineName">状态机名称</param>
        private async UniTask DelayedCleanup(string stateMachineName)
        {
            try
            {
                await UniTask.Delay((int)CleanupDelayMs);
                
                if (StateMachineDic.TryGetValue(stateMachineName, out var stateMachine) && stateMachine.IsTerminated)
                {
                    RemoveStateMachine(stateMachineName);
                    AppLogger.Log($"自动清理已结束的状态机：{stateMachineName}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"自动清理状态机 {stateMachineName} 时发生错误：{ex.Message}");
            }
        }
        
        /// <summary>
        /// 手动清理所有已结束的状态机
        /// </summary>
        public void CleanupTerminatedStateMachines()
        {
            var terminatedNames = new List<string>();
            
            foreach (var kvp in StateMachineDic)
            {
                if (kvp.Value.IsTerminated)
                {
                    terminatedNames.Add(kvp.Key);
                }
            }
            
            foreach (var _name in terminatedNames)
            {
                try
                {
                    RemoveStateMachine(_name);
                    AppLogger.Log($"手动清理已结束的状态机：{_name}");
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"清理状态机 {_name} 时发生错误：{ex.Message}");
                }
            }
            
            if (terminatedNames.Count > 0)
            {
                AppLogger.Log($"手动清理完成，共清理 {terminatedNames.Count} 个已结束的状态机");
            }
        }
        
        /// <summary>
        /// 获取已结束的状态机数量
        /// </summary>
        /// <returns>已结束的状态机数量</returns>
        public int GetTerminatedStateMachineCount()
        {
            int count = 0;
            foreach (var stateMachine in StateMachineDic.Values)
            {
                if (stateMachine.IsTerminated)
                    count++;
            }
            return count;
        }
    }
}