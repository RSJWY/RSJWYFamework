# AppConfig 配置系统使用指南

## 🌟 核心功能
本系统支持两项核心能力，旨在解决配置管理中的“协作冲突”与“热更需求”：
1. **模块化扩展**：各模块可独立维护自己的配置项，无需修改主文件（基于 `partial class`）。
2. **运行时覆盖**：通过外部 JSON 文件动态修改配置，无需重新打包（基于 `JSON Populate`）。

---

## 🏗️ 第一部分：模块化配置扩展 (开发期)

为了避免 `AppConfig.cs` 变得臃肿且难以多人协作，我们推荐使用 `partial class` 将配置分散到各个模块的目录下。

### 📝 如何添加新配置？
在你的模块文件夹中创建一个 `.cs` 文件（例如 `LoginModuleConfig.cs`），并按以下格式编写：

```csharp
using Sirenix.OdinInspector;
using UnityEngine;

// ⚠️ 关键点1：命名空间必须与 AppConfig 保持一致
namespace RSJWYFamework.Runtime
{
    // ⚠️ 关键点2：使用 partial 关键字
    public partial class AppConfig
    {
        // ⚠️ 关键点3：使用 FoldoutGroup 将你的配置折叠起来，保持面板整洁
        [FoldoutGroup("登录模块")]
        [LabelText("开启游客登录")]
        // ⚠️ 关键点4：字段名强制加上模块前缀（如 Login_），防止与其他模块字段重名冲突
        public bool Login_EnableGuest = true;

        [FoldoutGroup("登录模块")]
        [LabelText("超时时间")]
        public float Login_Timeout = 5.0f;
    }
}
```

### ✅ 最佳实践
1. **命名空间**：必须是 `RSJWYFamework.Runtime`，否则无法合并。
2. **字段前缀**：**强制**建议字段名带上模块前缀（如 `Combat_xxx`, `Net_xxx`）。因为 `partial` 最终会合并成一个类，如果两个模块都定义了 `MaxCount`，编译器会报错。
3. **Inspector 分组**：请务必使用 `[FoldoutGroup]`，否则主配置面板会充斥着各模块的杂乱字段。

---

## 🚀 第二部分：运行时配置覆盖 (部署期)

打包后的程序可以通过外部文件修改配置，适用于服务器部署、现场调试或临时参数调整。

### 📂 操作步骤

1. **文件位置**：进入打包后的 `StreamingAssets` 文件夹（编辑器下为 `Assets/StreamingAssets`）。
2. **创建文件**：新建名为 `AppConfig.json` 的文件。
3. **编写内容**：

```json
{
    "ProjectName": "这是被JSON覆盖后的项目名",
    "Loglevel": "Error",
    "Login_EnableGuest": false,
    "Login_Timeout": 999.9
}
```

### ⚠️ 关键规则
1. **增量覆盖**：只写你想改的字段，没写的字段保持代码（ScriptableObject）中的默认值。
2. **名称匹配**：Key 必须与代码中的 `public` 变量名 **完全一致**（大小写敏感）。
3. **支持 Partial**：无论字段定义在哪个文件中（主文件或模块扩展文件），只要属于 `AppConfig`，都可以被覆盖。
4. **生效时机**：程序启动初始化 `AppConfigManager` 时自动检测并加载。

---

## 🔧 原理说明
- **扩展原理**：利用 C# `partial` 关键字将分布在不同文件中的代码合并为一个类。
- **覆盖原理**：利用 `Newtonsoft.Json.PopulateObject` 将 JSON 数据反射注入到内存中已加载的 `AppConfig` 实例中。此操作是“非破坏性”的，仅修改 JSON 中存在的字段。
