# 场景切换模块 (Scene Transition Module)

## 简介
本模块提供了一套基于状态机（FSM）的场景切换框架，支持高度自定义的场景加载流程。通过将场景切换过程拆分为独立的原子状态节点（State Node），实现了加载逻辑解耦、流程可视和灵活扩展。

## 核心架构

### 1. 线性状态流转
场景切换被抽象为一个线性的状态流转过程：
`Start` -> `LoadTransition` -> `Deinit` -> `Transfer` -> `Clear` -> `Preload` -> `LoadNext` -> `Init` -> `Done`

### 2. 关键类说明

*   **[SwitchSceneOperation](SwitchSceneOperation.cs)**
    *   **职责**：作为对外入口，负责组装状态机、连接流程节点并启动切换。
    *   **Builder 模式**：通过 `SwitchSceneOperation.CreateBuilder()` 创建构建器，链式配置各个阶段的实现。

*   **[SceneStateNodeBase](SceneProcedureBase.cs)**
    *   **职责**：所有场景流程节点的基类。
    *   **NextNodeType**：通过类型（Type）强引用指向下一个节点，摒弃了不安全的字符串索引。
    *   **OnExit()**：标准化的退出方法，自动触发向下一节点的跳转。

## 使用指南

### 1. 创建自定义流程节点
继承对应的基类（如 `LoadTransitionContentStateNode`），实现具体逻辑。

```csharp
// 示例：自定义加载页节点
public class MyLoadingPageNode : LoadTransitionContentStateNode
{
    protected override async UniTask LoadTransitionContentEvent(StateNodeBase last)
    {
        // 1. 执行具体的加载逻辑
        await UIManager.OpenWindowAsync("LoadingWindow");
        
        // 2. 逻辑执行完毕后，基类的 OnEnter 会自动调用 OnExit()
        // 从而触发状态机切换到 NextNodeType
    }
}
```

### 2. 发起场景切换
使用 `Builder` 构建并启动流程：

```csharp
// 构建切换操作
var op = SwitchSceneOperation.CreateBuilder()
    // 设置自定义的加载过渡效果（可选，不设置则使用默认空节点）
    .SetLoadTransition(new MyLoadingPageNode())
    // 设置具体的场景加载逻辑（必须）
    .SetLoadNextScene(new MySceneLoaderNode("GameMap_Level1"))
    // 传递参数到状态机黑板（可选）
    .SetBlackboard(new Dictionary<string, object> { {"Difficulty", "Hard"} })
    .Build();

// 启动流程
op.StartSwitchScene();
```

## 最佳实践 (Best Practices)

1.  **优先使用 Builder**：
    *   避免使用过时的长参数构造函数。
    *   Builder 模式清晰地展示了哪些步骤被自定义了，哪些使用了默认值。

2.  **依赖 OnExit() 机制**：
    *   在实现 `OnEnter` 时，确保最终会调用 `OnExit()`（直接或间接）。
    *   目前的基类（如 `LoadTransitionContentStateNode`）已经封装好了 `OnExit` 调用，子类只需实现抽象的 `...Event` 方法即可。

3.  **类型安全**：
    *   流程连接完全基于 C# `Type` 系统，不再依赖黑板中的字符串 Key。
    *   这消除了因拼写错误导致的运行时流程中断风险。

## 变更日志 (Refactoring Notes)
- **2026-01-31**:
  - **安全性提升**：引入 `NextNodeType` 属性，移除基于字符串的黑板跳转逻辑。
  - **API 易用性**：重构 `SwitchSceneOperation` 为 Builder 模式，解决构造函数参数爆炸问题。
  - **代码简化**：基类增加 `OnExit()` 方法，统一流程出口，减少重复代码。
