# 定时任务执行器 (Timer Executor)

一个强大的定时任务执行系统，完全集成到RSJWYFamework框架中，提供类似协程的功能，支持任务取消、优先级管理和灵活的调度机制。

## 核心特性

- **框架集成**: 完全集成到RSJWYFamework的模块系统中
- **生命周期管理**: 支持ILife接口的完整生命周期
- **任务取消**: 支持单个任务、批量任务和全部任务的取消
- **优先级支持**: 任务按优先级执行
- **时间控制**: 支持受时间缩放影响和不受影响的任务
- **性能优化**: 内置性能监控和时间片控制
- **扩展方法**: 提供便捷的扩展方法简化使用

## 快速开始

### 1. 模块注册

确保TimerExecutor已添加到ModuleManager中：

```csharp
// 在ModuleManager中注册TimerExecutor
ModuleManager.AddModule<TimerExecutor>();
```

### 2. 基础使用

```csharp
// 获取定时任务执行器
var timerExecutor = ModuleManager.GetModule<TimerExecutor>();

// 延迟执行
string taskId = timerExecutor.DelayCall(() => 
{
    Debug.Log("2秒后执行");
}, 2f, "DelayTask");

// 重复执行
string repeatId = timerExecutor.RepeatCall(() => 
{
    Debug.Log("每秒执行一次，共5次");
}, 0f, 1f, 5, "RepeatTask");
```

### 3. 使用扩展方法

```csharp
// 静态便捷方法
TimerTaskExtensions.Delay(1f, () => Debug.Log("1秒后执行"));
TimerTaskExtensions.NextFrame(() => Debug.Log("下一帧执行"));
TimerTaskExtensions.EverySecond(() => Debug.Log("每秒执行"), 10);

// 条件等待
TimerTaskExtensions.WaitUntil(
    () => someCondition,
    () => Debug.Log("条件满足"),
    0.1f, // 检查间隔
    10f   // 超时时间
);
```

## API 参考

### TimerExecutor 主要方法

#### 任务创建
- `DelayCall(Action, float, string)` - 延迟执行
- `RepeatCall(Action, float, float, int, string)` - 重复执行
- `AddTask(ITimerTask)` - 添加自定义任务

#### 任务管理
- `CancelTask(string)` - 取消指定任务
- `CancelTasksByName(string)` - 取消同名任务
- `CancelAllTasks()` - 取消所有任务
- `HasTask(string)` - 检查任务是否存在
- `GetTask(string)` - 获取任务实例
- `GetAllActiveTasks()` - 获取所有活跃任务

#### 属性
- `ActiveTaskCount` - 活跃任务数量
- `EnableVerboseLogging` - 启用详细日志
- `MaxTasksPerFrame` - 每帧最大任务数
- `MaxExecutionTimePerFrame` - 每帧最大执行时间

### TimerTaskExtensions 扩展方法

#### 静态方法
- `Delay(float, Action, string, bool)` - 延迟执行
- `Repeat(float, float, Action, int, string, bool)` - 重复执行
- `NextFrame(Action, string)` - 下一帧执行
- `EverySecond(Action, int, string)` - 每秒执行
- `RepeatForFrames(Action, int, string)` - 按帧重复

#### 条件方法
- `WaitUntil(Func<bool>, Action, float, float, string)` - 等待条件满足
- `WaitWhile(Func<bool>, Action, float, float, string)` - 等待条件不满足

#### Action扩展
- `DelayCall(this Action, float, string)` - Action延迟执行
- `RepeatCall(this Action, float, float, int, string)` - Action重复执行

#### 任务管理扩展
- `CancelTimer(this string)` - 取消任务
- `HasTimer(this string)` - 检查任务
- `GetTimer(this string)` - 获取任务

## 自定义任务

继承TimerTaskBase创建自定义任务：

```csharp
public class MyCustomTask : TimerTaskBase
{
    public MyCustomTask(string name, float delay, float interval, int maxCount)
        : base(name, delay, interval, maxCount, false)
    {
    }

    protected override async UniTask OnExecuteAsync()
    {
        // 自定义执行逻辑
        Debug.Log($"执行自定义任务: {TaskName}");
        
        // 支持异步操作
        await UniTask.Delay(100);
        
        Debug.Log("自定义任务完成");
    }
}

// 使用自定义任务
var customTask = new MyCustomTask("MyTask", 1f, 2f, 5);
string taskId = timerExecutor.AddTask(customTask);
```

## 任务取消

### 单个任务取消
```csharp
string taskId = TimerTaskExtensions.Delay(5f, () => Debug.Log("不会执行"));
taskId.CancelTimer(); // 或 timerExecutor.CancelTask(taskId);
```

### 批量取消
```csharp
// 取消所有名为"TestTask"的任务
timerExecutor.CancelTasksByName("TestTask");

// 取消所有任务
timerExecutor.CancelAllTasks();
```

## 性能优化

### 时间片控制
```csharp
timerExecutor.MaxTasksPerFrame = 10; // 每帧最多执行10个任务
timerExecutor.MaxExecutionTimePerFrame = 5f; // 每帧最多执行5毫秒
```

### 优先级设置
```csharp
// 高优先级任务会优先执行
var highPriorityTask = new MyCustomTask("HighPriority", 0f, 1f, 1);
highPriorityTask.Priority = 100;
```

## 注意事项

1. **模块依赖**: 确保TimerExecutor已正确注册到ModuleManager
2. **任务ID**: 任务ID是唯一的，重复ID会覆盖之前的任务
3. **内存管理**: 任务完成或取消后会自动清理，无需手动管理
4. **线程安全**: 所有操作都在主线程执行，确保Unity API的安全调用
5. **性能考虑**: 大量任务时建议调整MaxTasksPerFrame和MaxExecutionTimePerFrame

## 示例场景

### 游戏倒计时
```csharp
int countdown = 10;
TimerTaskExtensions.Repeat(0f, 1f, () => 
{
    Debug.Log($"倒计时: {countdown}");
    countdown--;
}, 10, "GameCountdown");
```

### 技能冷却
```csharp
public void UseSkill()
{
    // 使用技能
    Debug.Log("技能释放");
    
    // 冷却时间
    TimerTaskExtensions.Delay(5f, () => 
    {
        Debug.Log("技能冷却完成");
        skillAvailable = true;
    }, "SkillCooldown");
}
```

### 定期保存
```csharp
TimerTaskExtensions.EverySecond(() => 
{
    SaveGameData();
}, -1, "AutoSave"); // -1表示无限重复
```

这个定时任务系统为RSJWYFamework提供了强大而灵活的任务调度能力，完全符合框架的设计理念和使用习惯。