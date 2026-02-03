# Screen 模块优化与食用指南 📖

> 🐶 **小码酱的温馨提示**：本模块经过了性能和安全优化的精心调教，请主人务必仔细阅读以下指南哦！

## 1. 概述 (Overview)
`Screen` 模块主要负责 Windows 独立应用程序构建（Standalone Builds）的窗口定位、调整大小以及鼠标捕获逻辑。经过重构，它现在更省内存（GC 减少）且更安全啦！

## 2. 核心变化与优化 (Key Changes & Optimizations)
- **静态助手 (Static Helper)**：`CWinScreen` 现在是 `static class` 而不是 `MonoBehaviour`，去掉了多余的生命周期开销，轻量级起飞！🚀
- **GC 瘦身 (GC Reduction)**：`EnumWindowsCallback` 现在使用缓存的 `StringBuilder` 和委托实例，彻底杜绝了窗口枚举时的垃圾回收卡顿。✨
- **安全解析 (Safe Parsing)**：命令行参数解析现在使用 `int.TryParse`，再也不会因为乱输参数导致游戏崩溃啦！🛡️
- **逻辑净化 (Clean Logic)**：移除了 `ScreenManager` 中硬编码的 `Input` 检测，建议主人使用统一的 Input System，保持代码整洁！🧹

## 3. 食用方法 (Usage)

### 配置 (Configuration)
本模块会从 `AppConfigManager` 读取 "Screen" 部分的配置。
JSON 结构如下：
```json
{
    "X": 0,
    "Y": 0,
    "Width": 1920,
    "Height": 1080,
    "CaptureMouseOnInit": true
}
```

### 命令行参数 (Command Line Arguments)
主人可以使用命令行参数来覆盖配置文件（优先级更高哦）：
- `-title <string>`: 设置窗口标题。
- `-winX <int>`: 窗口 X 坐标。
- `-winY <int>`: 窗口 Y 坐标。
- `-resX <int>`: 窗口宽度。
- `-resY <int>`: 窗口高度。

**举个栗子：**
```bash
Game.exe -title "My Game" -winX 100 -winY 100 -resX 1280 -resY 720
```

### API 调用 (API)
- `ScreenManager.CaptureMouse()`: 锁定光标到窗口中心并隐藏（FPS 模式必备）。
- `ScreenManager.ReleaseMouse()`: 解锁并显示光标。
- `ScreenManager.SetMouseCapture(bool)`: 切换捕获状态。

## 4. 维护注意事项 (Maintenance Notes)
- **输入处理 (Input Handling)**：**千万不要**把 `Input.GetKeyDown` 加回 `LifeUpdate` 里！请使用项目中的 `InputManager` 或 Unity 的新 Input System 事件，这样才优雅！(｡•̀ᴗ-)✧
- **Windows API**：`CWinScreen` 依赖 `user32.dll`，所以它只能在 Windows 平台（或编辑器下）工作（已用 `#if UNITY_STANDALONE_WIN || UNITY_EDITOR` 保护）。

---
> _"Documentation is the love letter I write to your future self."_ 💌
