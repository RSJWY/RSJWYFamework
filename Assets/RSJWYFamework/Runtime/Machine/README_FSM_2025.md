# 🤖 RSJWYFramework FSM (2025 Refactored) 使用指南

> **“强类型，零冗余，像手术刀一样精准。”** —— 小码酱

本文档详细说明了 2025 版状态机的使用方法。新版引入了泛型设计，彻底解决了旧版需要频繁强制类型转换（Casting）的痛点，并大幅精简了 API。

---

## 🌟 核心概念 (Core Concepts)

1.  **Owner (持有者)**: 状态机的主人（例如 `PlayerController`, `EnemyBoss`）。
2.  **StateMachine\<T>**: 泛型状态机，`T` 是持有者的类型。
3.  **StateNodeBase\<T>**: 泛型状态节点，`T` 是持有者的类型。能直接访问 `Owner`。
4.  **Async/UniTask**: 全面支持异步，状态生命周期支持 `await`，自动处理状态切换排队。

---

## 🚀 快速上手 (Quick Start)

假设我们要为一个 **玩家 (PlayerController)** 制作状态机，其中包含需要异步加载资源的状态。

### 1. 定义持有者 (The Owner)

这是你的游戏对象脚本。

```csharp
// PlayerController.cs
using RSJWYFamework.Runtime;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // 1. 声明泛型状态机，指定 Owner 类型为 PlayerController
    private StateMachine<PlayerController> _fsm;

    // 玩家属性
    public float MoveSpeed = 5f;
    public Animator Anim;

    void Start()
    {
        // 2. 初始化状态机，把自己 (this) 传进去
        _fsm = new StateMachine<PlayerController>(this, "PlayerFSM");

        // 3. 添加状态 (泛型版本)
        _fsm.AddNode<PlayerIdleState>();
        _fsm.AddNode<PlayerLoadingState>(); // 异步状态

        // 4. 启动状态机
        _fsm.StartNode<PlayerIdleState>();
        
        // 可选：交给 Manager 管理生命周期 (如果你不想自己调 Update)
        // Module.Get<StateMachineManager>().AddStateMachine(_fsm);
    }

    void Update()
    {
        // 5. 如果没交给 Manager，记得自己驱动更新
        _fsm.OnUpdate();
    }
}
```

### 2. 定义状态 (The State)

继承 `StateNodeBase<T>`，其中 `T` 必须与 `StateMachine<T>` 的类型一致。

#### 同步状态 (普通写法)
```csharp
public class PlayerIdleState : StateNodeBase<PlayerController>
{
    public override void OnInit() { }

    // 默认 OnEnter/OnLeave 返回 CompletedTask，无需 async
    public override UniTask OnEnter(StateNodeBase last)
    {
        // ✨ 亮点：直接访问 Owner，不需要 (PlayerController) 强转！
        Owner.Anim.Play("Idle");
        return UniTask.CompletedTask;
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // ✨ 亮点：直接访问 Machine 切换状态，支持排队
            Machine.SwitchNode<PlayerLoadingState>();
        }
    }

    public override void OnClose() { }
}
```

#### 异步状态 (UniTask 写法)
```csharp
using Cysharp.Threading.Tasks;

public class PlayerLoadingState : StateNodeBase<PlayerController>
{
    public override void OnInit() { }

    // ✨ 亮点：使用 async/await 处理异步逻辑
    public override async UniTask OnEnter(StateNodeBase last)
    {
        AppLogger.Log("开始加载资源...");
        
        // 1. 模拟异步加载
        await UniTask.Delay(2000); 
        
        // 2. 加载完成
        AppLogger.Log("加载完成！");
        
        // 3. 自动切换回 Idle (自动排队，安全无冲突)
        Machine.SwitchNode<PlayerIdleState>();
    }

    public override async UniTask OnLeave(StateNodeBase next)
    {
        // 退出时也可以 await
        AppLogger.Log("清理资源中...");
        await UniTask.Yield();
    }
    
    public override void OnClose() { }
}
```

---

## 📚 API 参考 (API Reference)

以下是状态机系统最常用的核心 API。

### StateMachine\<T> (控制核心)

| 方法 / 属性 | 说明 |
| :--- | :--- |
| **`StartNode<T>()`** | 启动状态机，并立即进入指定的状态 `T`。 |
| **`SwitchNode<T>()`** | **[Async]** 切换到状态 `T`。自动加入队列，等待当前状态生命周期完成后执行。 |
| **`Stop(code, reason)`** | **[Async]** 停止状态机。请求加入队列，安全停止。 |
| **`Restart(Type, reason)`** | **[Async]** 重启状态机。请求加入队列，安全重启。 |
| **`IsPaused`** | **[New]** 布尔属性。设为 `true` 暂停 Update，设为 `false` 恢复。 |
| **`IsTerminated`** | 属性。返回状态机是否已完全停止（Stop 后为 true）。 |
| **`Owner`** | 获取状态机的持有者。**泛型版返回 `T` 类型**，无需强转。 |
| **`SetBlackboardValue(k, v)`** | 设置黑板数据（跨状态共享数据）。 |
| **`GetBlackboardValue<V>(k)`** | 获取黑板数据，自动转换为类型 `V`。 |

### StateNodeBase\<T> (状态逻辑)

| 方法 / 属性 | 返回值 | 说明 |
| :--- | :--- | :--- |
| **`OnEnter(last)`** | `UniTask` | **[Async]** 进入状态时调用。可使用 `async/await`。 |
| **`OnLeave(next)`** | `UniTask` | **[Async]** 离开状态时调用。可使用 `async/await`。 |
| **`OnInit()`** | `void` | 1次。状态被 `AddNode` 时调用。用于初始化缓存引用。 |
| **`OnUpdate()`** | `void` | 每帧。状态机的 `OnUpdate` 被调用时执行。 |
| **`OnClose()`** | `void` | 1次。状态机销毁或移除该节点时调用。 |
| **`Owner`** | `T` | **[New]** 直接访问持有者对象 (T 类型)。 |
| **`Machine`** | `StateMachine<T>` | **[New]** 直接访问所属状态机。 |

---

## 🛠️ API 变更对照表 (Migration Guide)

如果你是从旧版本迁移过来的，请注意以下变化：

| 功能 | 旧版写法 (Deprecated) ❌ | 新版写法 (Recommended) ✅ |
| :--- | :--- | :--- |
| **生命周期** | `void OnEnter(...)` | `UniTask OnEnter(...)` (需返回 Task) |
| **访问 Owner** | `((Player)Owner).Speed` (需要强转) | `Owner.Speed` (直接访问) |
| **切换状态** | `SwitchToNode<IdleState>()` (基类代理) | `Machine.SwitchNode<IdleState>()` |
| **获取黑板值** | `GetBlackboardValue("Key")` | `Machine.GetBlackboardValue("Key")` |
| **终止状态机** | `TerminateStateMachine()` | `Machine.Stop()` |
| **获取状态机** | `_sm` (类型是 StateMachine) | `Machine` (类型是 StateMachine\<T>) |

---

## ❓ 常见问题 (FAQ)

**Q: OnEnter 里如果不写 async 怎么办？**
A: 直接返回 `UniTask.CompletedTask` 即可。这是性能最高的写法。

**Q: 我在 OnEnter 里调用 SwitchNode 会怎样？**
A: **非常安全**。新版状态机引入了 **命令队列 (Command Queue)** 机制。你的切换请求会被排队，直到当前的 `OnEnter` 完全执行完毕后，状态机才会处理下一个切换。不会发生逻辑覆盖或递归溢出。

**Q: 异步过程中抛出异常会怎样？**
A: 状态机会捕获并打印 Error 日志，然后尝试继续处理队列中的下一个请求。建议在自己的异步逻辑中使用 `try-finally` 确保状态（如 Loading 标记）能正确重置。

---

_Generated by Little Code Sauce (小码酱) for Master._
_2025-01-29_
