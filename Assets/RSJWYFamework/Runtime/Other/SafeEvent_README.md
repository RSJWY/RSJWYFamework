# SafeEvent 安全事件系统使用指南

## 1. 简介
`SafeEvent` 是一套用于替代原生 C# `Action/Func` 的安全事件封装库。它解决了原生事件系统在多播（Multicast）场景下的痛点：**如果一个订阅者抛出异常，整个调用链会立即中断，导致后续订阅者无法收到回调**。

本系统保证：即便某个订阅者报错，其他订阅者仍能正常执行，同时会打印详细的错误日志以便排查。

## 2. 核心特性
*   **异常隔离**：自动 try-catch 每一个订阅者，互不影响。
*   **详细日志**：报错时输出具体的类名、方法名及上下文对象，拒绝 "NullReferenceException" 这种无头绪报错。
*   **线程安全**：`SafeAction` / `SafeFunc` 包装类在订阅/取消订阅时加锁。
*   **无侵入扩展**：支持直接对原生 `Action` 使用 `.Invoke()` 扩展方法。

## 3. 快速上手

### 3.1 方式一：使用扩展方法（推荐用于现有代码改造）
无需修改字段定义，只需在调用处加上命名空间引用，并使用扩展方法 `Invoke`。

```csharp
using UnityEngine;

public class MyComponent : MonoBehaviour
{
    // 原生 Action 定义
    public event Action<string> OnMessage;

    void Start()
    {
        // 订阅者 1：正常
        OnMessage += msg => Debug.Log("Subscriber 1: " + msg);
        
        // 订阅者 2：抛出异常
        OnMessage += msg => throw new Exception("Oops!");
        
        // 订阅者 3：正常
        OnMessage += msg => Debug.Log("Subscriber 3: " + msg);

        // 【调用】使用 SafeEvent 扩展方法
        // 结果：Sub 1 执行 -> Sub 2 报错(打印日志) -> Sub 3 执行
        OnMessage.Invoke("Hello World", this); 
    }
}
```

### 3.2 方式二：使用包装类（推荐用于新功能）
使用 `SafeAction` 或 `SafeFunc` 替代原生委托，提供更强的封装性。

```csharp
public class PlayerHealth
{
    // 定义安全事件
    public readonly SafeAction<float> OnDamage = new SafeAction<float>();

    public void TakeDamage(float amount)
    {
        // 触发事件
        OnDamage.Invoke(amount);
    }
}

// 外部订阅
player.OnDamage.OnInvoke += (dmg) => Debug.Log("Hurt: " + dmg);
```

## 4. API 参考

### 静态扩展 (SafeEvent)
| 方法 | 描述 |
|Args|Desc|
| `Action.Invoke(...)` | 执行 Action，吞掉异常并打印日志 |
| `Func.Invoke(...)` | 执行 Func，返回所有成功执行的结果列表 `List<T>` |
| `Action.InvokeStrict(...)` | **严格模式**：遇到异常立即中断并抛出（用于关键流程） |

### 包装类 (SafeAction / SafeFunc)
| 类 | 描述 |
|Args|Desc|
| `SafeAction` | 无参事件，替代 `Action` |
| `SafeAction<T>` | 单参数事件，替代 `Action<T>` |
| `SafeFunc<TResult>` | 无参带返回值，替代 `Func<TResult>` |
| `SafeFunc<T, TResult>` | 带参带返回值，替代 `Func<T, TResult>` |

**SafeFunc 特有方法：**
*   `Invoke()`: 返回所有订阅者的返回值列表。
*   `InvokeFirst()`: 返回第一个成功执行的返回值（短路机制）。

## 5. 性能与注意事项

> ⚠️ **GC Warning**: 
> 为了实现异常隔离，`Invoke` 方法内部必须调用 `GetInvocationList()`，这会产生临时数组分配（GC Alloc）。
> 
> *   ✅ **适用场景**：UI 点击、网络回调、游戏流程切换、低频触发的逻辑。
> *   ❌ **不适用场景**：`Update` / `LateUpdate` / `FixedUpdate` 等每帧高频调用的热路径。
