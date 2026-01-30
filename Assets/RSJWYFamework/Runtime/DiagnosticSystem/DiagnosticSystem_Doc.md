# 诊断系统 (DiagnosticSystem) 使用说明

## 1. 简介 (Introduction)

`DiagnosticSystem` 是 RSJWYFamework 框架内置的性能监控与诊断工具。它主要用于实时监控框架中各个模块（Module）的生命周期执行耗时，帮助开发者快速定位造成游戏卡顿（Hiccup）或掉帧的性能瓶颈。

该系统设计轻量，默认关闭，开启后会对 `ModuleManager` 管理的所有模块的 `Update`、`FixedUpdate`、`LateUpdate` 等生命周期方法进行耗时统计。

## 2. 核心功能 (Features)

*   **模块级监控**：自动捕获所有实现了 `IModule` 接口并由 `ModuleManager` 管理的模块。
*   **多维度统计**：记录调用次数、总耗时、平均耗时、最大耗时（用于捕捉尖峰）、最小耗时。
*   **控制台集成**：内置 `IngameDebugConsole` 指令，支持运行时动态开启/关闭和查看报告。
*   **低侵入性**：基于静态类 `ModulePerformanceMonitor`，对业务逻辑代码无侵入。

## 3. 快速上手 (Quick Start)

本系统已集成到游戏的调试控制台（Ingame Debug Console）。你可以在游戏运行时通过输入指令来控制。

### 3.1 开启监控
在控制台中输入以下指令开启监控：
```bash
perf.monitor true
```
*开启后，系统将开始记录每一帧的模块执行数据。建议让游戏运行一段时间（例如进入战斗或复杂场景）以收集有代表性的数据。*

### 3.2 查看报告
当收集了一段时间数据后，输入以下指令打印报告：
```bash
perf.report
```
控制台将输出如下格式的日志：
```text
=== 模块性能报告 ===
AppAsyncOperationSystem.LifeUpdate: 调用次数=1200, 平均耗时=0.02ms, 最大耗时=1.50ms, 最小耗时=0.00ms
NetworkModule.LifeUpdate: 调用次数=1200, 平均耗时=0.15ms, 最大耗时=5.20ms, 最小耗时=0.01ms
...
```

### 3.3 清理数据
如果想重新开始一轮测试，可以先清理旧数据：
```bash
perf.clear
```

### 3.4 关闭监控
测试结束后，建议关闭监控以节省性能：
```bash
perf.monitor false
```

## 4. 编程接口 (API)

除了使用控制台指令，你也可以在代码中直接调用 API。

### 4.1 手动开启/关闭
```csharp
using RSJWYFamework.Runtime;

// 开启监控
ModulePerformanceMonitor.IsEnabled = true;
ModuleManager.EnablePerformanceMonitoring = true; // 确保模块管理器的埋点也开启

// 关闭监控
ModulePerformanceMonitor.IsEnabled = false;
```

### 4.2 自定义监控埋点
如果你想监控自己的非模块代码段，可以使用 `StartTimer` 和 `EndTimer`：

```csharp
public void MyComplexAlgorithm()
{
    // 定义一个唯一的 Key
    string key = "MyGameSystem.ComplexAlgo";
    
    // 开始计时
    ModulePerformanceMonitor.StartTimer(key);
    
    // ... 执行耗时操作 ...
    DoSomethingHeavy();
    
    // 结束计时
    ModulePerformanceMonitor.EndTimer(key);
}
```

### 4.3 获取数据对象
如果你需要将性能数据发送到外部服务器或绘制图表，可以获取原始数据对象：
```csharp
var allData = ModulePerformanceMonitor.GetAllPerformanceData();
foreach (var kvp in allData)
{
    string key = kvp.Key;
    var data = kvp.Value;
    Debug.Log($"Key: {key}, MaxTime: {data.MaxExecutionTime}");
}
```

## 5. 性能报告解读 (Interpreting Reports)

报告中的关键指标说明：

*   **调用次数 (CallCount)**: 该方法被执行的总次数。
*   **平均耗时 (AverageExecutionTime)**: 总耗时 / 调用次数。反映模块的常规负载。
*   **最大耗时 (MaxExecutionTime)**: **最关键指标**。如果平均耗时很低（0.1ms），但最大耗时很高（20ms），说明该模块存在偶尔的卡顿（Spike），通常由 GC、复杂的各种资源加载或逻辑计算引起。
*   **最小耗时 (MinExecutionTime)**: 这里的参考意义较小。

## 6. 注意事项 (Notes)

1.  **性能开销**：监控本身有微小的性能开销（主要是 `Stopwatch` 和字典查找）。虽然很小，但**建议仅在开发版本或调试模式下开启**，Release 版本请默认关闭。
2.  **准确性**：由于 C# `Stopwatch` 的精度限制和多线程影响，微秒级的数据仅供参考。重点关注**毫秒级**的异常数据。
3.  **AppDebugOperationInfo**：`DiagnosticSystem` 文件夹下还有一个 `AppDebugOperationInfo.cs`，这主要用于 `AppAsyncOperationSystem` 的调试显示，与性能监控模块是独立的，但都属于诊断工具集。
