# 📄 UI 模块使用说明书 (UI Module Manual)

> **Written by**: 小码酱 (Little Code Sauce) 🐶  
> **For**: 最尊贵的主人 (The Master) 👑  
> **Last Updated**: 2025

---

## 🌟 模块简介 (Overview)

主人，这是为您精心准备的 UI 管理模块！它采用经典的 **栈式管理 (Stack-based Management)**，非常适合处理层级分明的 UI 页面（比如：主界面 -> 设置 -> 确认弹窗）。

整个模块的设计目标是：**轻量**、**易用**、**高性能**。( •̀ ω •́ )y

## 📂 核心文件 (Core Components)

| 文件名 | 职责描述 |
| :--- | :--- |
| **UIPageManager.cs** | **大管家**。负责页面的加载、缓存、进栈、出栈。会自动创建 `UIRoot`。 |
| **UIPageBase.cs** | **页面基类**。所有具体的 UI 页面（如 `MainMenuPage`）都必须继承它。 |
| **UIEventListener.cs** | **事件监听器**。高效的事件处理工具，替代性能较差的 `EventTrigger`。 |

---

## 🚀 快速上手 (Quick Start)

### 1. 准备 UI Prefab
请把您的 UI Prefab 放在 `Resources/UIPrefab/` 目录下。
> **注意**：如果路径不对，小码酱会找不到它的哦！(T_T)

### 2. 创建页面脚本
创建一个继承自 `UIPageBase` 的脚本，并挂载到 Prefab 上。

```csharp
using RSJWYFamework.Runtime;

public class MyTestPage : UIPageBase
{
    public override void OnEnter(object data)
    {
        base.OnEnter(data);
        // 页面显示时的逻辑，比如刷新数据
        AppLogger.Log("页面打开啦！");
    }

    public override void OnExit()
    {
        base.OnExit();
        // 页面关闭时的清理逻辑
    }
}
```

### 3. 打开页面
在任何地方获取 `UIPageManager` 并调用 `Push`。

```csharp
// 假设您的 Prefab 名字是 "MyTestPage"
ModuleManager.GetModule<UIPageManager>().Push("MyTestPage");
```

---

## 🔧 详细功能 (Features)

### UIPageManager (页面管理器)

- **Push(pageName, data)**: 打开新页面。旧页面会暂停 (`OnPause`)，新页面进入 (`OnEnter`)。
- **Pop()**: 关闭当前页面。当前页面退出 (`OnExit`)，下层页面恢复 (`OnResume`)。
- **Replace(pageName, data)**: 替换当前页面。相当于先 Pop 再 Push。
- **Preload(pageName)**: 预加载页面到缓存，但不显示。

### UIPageBase (生命周期)

主人，请记住这些生命周期方法的调用顺序哦：

1. **OnEnter**: 页面入栈并显示时调用。
2. **OnPause**: 页面被新页面覆盖（压在下面）时调用。
3. **OnResume**: 上层页面关闭，本页面重新回到栈顶时调用。
4. **OnExit**: 页面出栈并隐藏/销毁时调用。

### UIEventListener (高性能事件)

小码酱优化了事件监听，避免了 `EventTrigger` 的额外开销！直接在代码里绑定吧：

```csharp
// 获取监听器
var listener = UIEventListener.Get(gameObject);

// 绑定点击
listener.onClick = () => {
    AppLogger.Log("点到我啦！");
};

// 绑定拖拽
listener.onDrag = (delta) => {
    transform.Translate(delta);
};
```

---

## 🐶 小码酱的碎碎念 (Tips & Warnings)

1. **缓存机制**: 页面关闭后不会被销毁，而是隐藏并放入缓存池。如果内存紧张，记得在 `Shutdown` 时清理哦！
2. **UIRoot**: 只要场景里没有 `UIRoot`，小码酱就会自动创建一个。如果您想自己定制 Canvas（比如调整分辨率适配），请手动在场景里创建一个名为 `UIRoot` 的 Canvas。
3. **资源加载**: 目前使用的是 `Resources.Load`。如果主人以后升级到 **Addressables** 或 **YooAsset**，请记得去 `UIPageManager.GetPageInstance` 方法里修改加载逻辑！
4. **不要偷懒**: 记得给 Prefab 挂载对应的 Page 脚本，不然会报错的！(>_<)

---

**Master, keep coding and stay awesome!** ✨
