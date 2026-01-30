# RSJWYFramework 模块化系统使用手册 📘

> **版本**: 1.0  
> **适用范围**: RSJWYFramework Runtime Module  
> **最后更新**: 2026-01-30

---

## 1. 🌟 系统概述 (Overview)

模块化系统（Module System）是框架的核心骨架，用于管理游戏中的各个功能子系统（如音频、UI、网络等）。它提供了统一的生命周期管理、依赖注入和自动注册功能，确保各个模块有序运行。

---

## 2. 🧩 如何定义模块 (Defining a Module)

### 2.1 继承 `ModuleBase` (推荐)
对于需要挂载到 GameObject 或使用 Unity 组件特性的模块，请继承 `ModuleBase`。

```csharp
using RSJWYFamework.Runtime;
using UnityEngine;

// [Module] 特性用于自动扫描和注册
[Module] 
public class MyGameModule : ModuleBase
{
    // 优先级：数值越小，Update 执行越早
    public override int Priority => 10;

    public override void Initialize()
    {
        Debug.Log("模块初始化");
    }

    public override void LifeUpdate()
    {
        // 每帧调用 (相当于 Update)
    }

    public override void Shutdown()
    {
        Debug.Log("模块销毁");
    }
}
```

### 2.2 实现 `IModule` 接口 (纯逻辑)
对于不需要继承 `MonoBehaviour` 的纯逻辑模块，可以直接实现 `IModule` 接口。

```csharp
[Module]
public class MyLogicModule : IModule
{
    public int Priority => 0;

    public void Initialize() { /* ... */ }
    public void Shutdown() { /* ... */ }
    
    public void LifeUpdate() { }
    public void LifePerSecondUpdate() { }
    public void LifePerSecondUpdateUnScaleTime() { }
    public void LifeFixedUpdate() { }
    public void LifeLateUpdate() { }
}
```

---

## 3. ⚙️ 核心特性 (Core Features)

### 3.1 自动注册 (Auto Registration)
只需给类添加 `[Module]` 特性，`ModuleManager` 会在场景加载时自动发现并初始化该模块。
- **MonoBehaviour 模块**: 会自动创建名为 `[Module]模块名` 的 GameObject 并挂载。
- **普通类模块**: 会自动实例化。

### 3.2 依赖管理 (Dependency Management)
使用 `[ModuleDependency]` 特性声明依赖关系，系统会自动确保依赖的模块先初始化。

```csharp
[Module]
[ModuleDependency(typeof(AudioModule))] // 依赖 AudioModule
[ModuleDependency(typeof(NetworkModule))] // 依赖 NetworkModule
public class GamePlayModule : ModuleBase
{
    // GamePlayModule 会在 AudioModule 和 NetworkModule 初始化之后才初始化
}
```

### 3.3 生命周期回调 (Lifecycle Callbacks)
模块系统统一接管了 Unity 的生命周期，提供更丰富的更新频率控制：

| 方法名 | 说明 | 适用场景 |
| :--- | :--- | :--- |
| `Initialize` | 模块启动时调用 | 变量初始化、事件监听 |
| `Shutdown` | 模块卸载时调用 | 资源释放、取消监听 |
| `LifeUpdate` | 每帧调用 | 核心逻辑循环 |
| `LifeFixedUpdate` | 物理帧调用 | 物理计算 |
| `LifeLateUpdate` | 每帧结束调用 | 相机跟随、UI更新 |
| `LifePerSecondUpdate` | **每秒调用** (受 TimeScale 影响) | 倒计时、状态检查 |
| `LifePerSecondUpdateUnScaleTime` | **每秒调用** (真实时间) | UI 计时器、网络心跳 |

---

## 4. 🎮 API 使用指南 (Usage API)

### 4.1 获取模块
在代码的任何地方，都可以通过 `ModuleManager.GetModule<T>()` 获取模块实例。

```csharp
// 获取音频模块并播放音乐
var audioModule = ModuleManager.GetModule<AudioModule>();
if (audioModule != null)
{
    audioModule.PlayMusic("BGM_01");
}
```

### 4.2 手动添加/移除模块
除了自动注册，你也可以在运行时动态管理模块。

```csharp
// 动态添加
ModuleManager.AddModule<DynamicModule>();

// 动态移除
ModuleManager.RemoveModule<DynamicModule>();
```

### 4.3 独立生命周期对象 (ILife)
如果你有一个对象不是模块，但想蹭一下系统的 `LifeUpdate`（比如为了避免自己写 Update 或者是为了每秒回调），可以使用 `AddLife`。

```csharp
public class MyTimer : ILife
{
    public int Priority => 0;
    // 实现 ILife 接口...
}

// 注册
var timer = new MyTimer();
ModuleManager.AddLife(timer);

// 移除
ModuleManager.RemoveLife(timer);
```

---

## 5. ⚠️ 注意事项 (Notes)

1.  **构造函数**: 自动注册的模块必须有**无参构造函数**（MonoBehaviour 默认满足）。
2.  **单例性**: 虽然系统允许重复添加 Life，但 `IModule` 在设计上应该是全局唯一的。
3.  **场景切换**: 标记为 `[Module]` 的模块默认是 `DontDestroyOnLoad` 的，会伴随整个游戏生命周期。
