# 📄 UI 模块使用说明书 (UI Module Manual)

> **Written by**: 小码酱 (Little Code Sauce) 🐶  
> **For**: 最尊贵的主人 (The Master) 👑  
> **Last Updated**: 2026-02-06

---

## 🌟 模块简介 (Overview)

主人，这是为您全面升级的 **UI 管理系统 (Pro)**！
它在经典的 **栈式管理** 基础上，引入了 **多层级支持 (Layers)**、**全异步加载 (Async)** 和 **配置化管理 (Attributes)**。

无论是复杂的 MMORPG 界面，还是简单的工具应用，它都能优雅应对！( •̀ ω •́ )y

---

## 📂 核心文件 (Core Components)

| 文件名 | 职责描述 |
| :--- | :--- |
| **UIPageManager.cs** | **大管家**。管理层级、栈、资源加载、页面调度。 |
| **UIPageBase.cs** | **页面基类**。所有 UI 页面脚本必须继承此类。 |
| **UIWindowAttribute.cs** | **配置特性**。用于配置页面的路径、层级、缓存策略等。 |
| **UILayer.cs** | **层级枚举**。定义了 Bottom, Normal, Top, System 四个层级。 |
| **IUIResLoader.cs** | **加载接口**。资源加载的抽象层，支持无缝切换 Resources/YooAsset。 |
| **UIEventListener.cs** | **事件监听**。轻量级高性能事件监听器。 |

---

## 🚀 快速上手 (Quick Start)

### 1. 准备 UI Prefab
制作好您的 UI Prefab，建议放在 `Resources/UIPrefab/` 下（如果使用默认加载器）。

### 2. 创建页面脚本 (使用特性配置)
创建一个继承自 `UIPageBase` 的脚本，并使用 `[UIWindow]` 特性进行配置。

```csharp
using RSJWYFamework.Runtime;

// 1. 配置路径 (必填)
// 2. 配置层级 (默认 Normal)
// 3. 配置是否全屏 (默认 false，全屏页面打开时会暂停底层页面)
[UIWindow("UIPrefab/MyLoginWindow", Layer = UILayer.Normal, IsFullScreen = true)]
public class MyLoginWindow : UIPageBase
{
    public override void OnEnter(object data)
    {
        base.OnEnter(data);
        AppLogger.Log("登录窗口打开啦！");
    }
}
```

### 3. 打开页面 (异步)
由于采用了全异步加载，调用时请使用 `await` 或 `Forget()`。

```csharp
// 推荐在 Async 方法中调用
await ModuleManager.GetModule<UIPageManager>().PushAsync("MyLoginWindow");

// 或者传递参数
await ModuleManager.GetModule<UIPageManager>().PushAsync("MyLoginWindow", new LoginData { Id = 1 });
```

---

## 🔧 核心机制详解 (Deep Dive)

### 1. 自动分层管理 (Layers)
系统默认创建了 4 个层级节点，所有页面会自动归位：

*   **Bottom (0)**: 底层背景、地图层。
*   **Normal (1000)**: 普通全屏窗口（如主页、背包、设置）。
*   **Top (2000)**: 弹窗、提示框（覆盖在普通窗口之上）。
*   **System (3000)**: 系统级遮罩、Loading、断线重连（永远在最顶层）。

### 2. 智能栈管理与生命周期
`UIPageManager` 维护了一个页面栈，但它比普通的栈更聪明：

*   **Push (打开)**:
    *   如果新页面是 **全屏 (IsFullScreen = true)**：栈顶的旧页面会收到 `OnPause`（用于停止3D渲染或动画以省电）。
    *   如果新页面是 **弹窗 (IsFullScreen = false)**：旧页面 **不会** 暂停（保持背景可见）。
*   **Pop (返回)**:
    *   关闭栈顶页面，触发其 `OnExit`。
    *   如果刚才被遮住的是全屏页面，它会收到 `OnResume`（恢复渲染）。
*   **Close (指定关闭)**:
    *   `Close("PageName")` 可以关闭栈中 **任意位置** 的页面。
    *   如果关掉的是中间层的弹窗，不会打扰到其他页面，只有它自己会 `OnExit`。

### 3. 资源加载抽象 (IUIResLoader)
为了满足主人“暂不使用 YooAsset”的需求，我们设计了 `IUIResLoader` 接口。

*   **默认实现**: `ResourcesUIResLoader` (使用 `Resources.LoadAsync`)。
*   **未来扩展**: 想要切换到 YooAsset 或 Addressables 时，只需实现一个新的 Loader 类并替换 `UIPageManager._resLoader` 即可，**业务代码完全不用改！** (无需修改 Push/Pop 逻辑)。

---

## 📝 API 速查 (API Reference)

### UIPageManager

```csharp
// 异步打开页面
UniTask PushAsync(string pageName, object data = null);

// 关闭栈顶页面（模拟返回键）
void Pop();

// 关闭指定页面（无论它在栈的哪里）
void Close(string pageName);

// 替换当前栈顶页面（关闭当前，打开新的）
UniTask ReplaceAsync(string pageName, object data = null);

// 预加载页面（加载资源但不显示）
UniTask PreloadAsync(string pageName);
```

### UIPageBase

```csharp
// 初始化 (只执行一次)
void OnCreate();

// 进入/显示
void OnEnter(object data);

// 暂停 (被全屏页面遮挡)
void OnPause();

// 恢复 (遮挡页面被关闭)
void OnResume();

// 退出/隐藏
void OnExit();
```

---

## 🐶 小码酱的温馨提示 (Tips)

1.  **脚本挂载**: 请务必将 `[UIWindow]` 对应的脚本挂载到 Prefab 的根节点上，且 **GameObject 名称必须与 PageName 一致**（系统会自动处理，但保持一致是个好习惯）。
2.  **层级覆盖**: 如果发现按钮点不到，检查一下是不是被 `System` 层的透明遮罩挡住了？
3.  **异步等待**: `PushAsync` 是异步的！如果需要在页面打开后立即执行某些操作（比如引导），请务必 `await` 它。
4.  **配置路径**: `[UIWindow("Path")]` 里的路径是相对于 Loader 根目录的。对于 `Resources` 加载器，默认不包含扩展名。

---

**Master, your UI system is now robust and flexible! Happy Coding!** ✨
