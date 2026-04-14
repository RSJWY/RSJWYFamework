# ComfyUI for Unity

## 简介
这是一个用于 Unity 的 ComfyUI 任务处理模块，基于泛型状态机 (`StateMachine<T>`) 实现，提供了完整的异步任务生命周期管理。支持 WebSocket 实时通信、任务提交、进度监听、结果下载以及错误处理。

## 核心特性
- **泛型状态机驱动**：使用 `StateMachine<ComfyUITaskAsyncOperation>` 管理任务流程，逻辑清晰，易于扩展。
- **智能任务匹配**：内置 `TaskIDHandler`，完美解决 WebSocket 消息与 HTTP POST 返回顺序不一致（如任务缓存秒完）的问题。
- **全异步流**：基于 `UniTask` 和 `TouchSocket`，全程异步非阻塞。
- **自动连接**：任务启动时自动建立 WebSocket 连接，确保不漏掉任何消息。

## 快速开始

### 1. 创建并启动任务

```csharp
using RSJWYFamework.Runtime;
using Newtonsoft.Json.Linq;

// 准备参数
string clientId = Guid.NewGuid().ToString(); // 建议每个任务使用独立 ClientId
string serverAddress = "127.0.0.1:8188";
JObject workflowJson = JObject.Parse(File.ReadAllText("workflow.json"));
bool useWss = false;

// 定义获取图片 URL 的回调（因为 ComfyUI 返回的是文件名，需要拼接成完整 URL）
ComfyUITaskAsyncOperation.GetHistoryImageURLHandle getUrlHandle = (json, promptId) => 
{
    // 解析 json 找到 output 节点的 filename
    // 拼接成 http://ip:port/view?filename=xxx...
    string url = ParseUrlFromJson(json); 
    return new GetHistoryImageURLResult { Success = true, ImageURL = url };
};

// 创建任务操作对象
var operation = new ComfyUITaskAsyncOperation(
    clientId, 
    workflowJson, 
    serverAddress, 
    getUrlHandle, 
    useWss, 
    this // owner
);

// 启动任务
operation.Start();

// 等待任务完成
await operation;

// 获取结果
if (operation.Status == AppAsyncOperationStatus.Succeed)
{
    Texture2D resultTexture = operation.DownloadedTexture;
    Debug.Log("任务成功，获取到图片！");
}
else
{
    Debug.LogError($"任务失败：{operation.Error}");
}
```

## 架构说明

### 状态机流程

任务执行流程如下：

1.  **启动 (Start)**: 
    - 建立 WebSocket 连接。
    - 连接成功后进入 `PostNode`。
2.  **提交任务 (ComfyUIPostNode)**:
    - 向 ComfyUI 发送 HTTP POST 请求提交工作流。
    - 获取 `prompt_id` 并注册到 `TaskIDHandler`。
    - 切换到 `WaitWebsocketNode` (如果任务未瞬间完成)。
3.  **等待执行 (ComfyUIWaitWebsocketNode)**:
    - 等待 WebSocket 收到 `execution_success` 消息。
    - `TaskIDHandler` 匹配 `prompt_id` 后触发状态切换。
4.  **下载结果 (ComfyUIDownloadResultNode)**:
    - 通过 HTTP GET 请求获取生成历史。
    - 解析图片 URL 并下载 Texture。
    - 任务标记为完成。

### 核心类

- **`ComfyUITaskAsyncOperation`**: 任务入口，持有状态机和所有上下文数据（ClientId, Json, Result 等）。
- **`TaskIDHandler`**: 处理并发和乱序的关键辅助类。它同时监听 WebSocket 的 `execution_success` 和 POST 请求的返回，确保无论谁先谁后，都能正确触发下一步。
- **`ComfyUIWebSocketJsonData`**: 定义了 ComfyUI WebSocket 消息的强类型结构。

## 注意事项

1.  **执行顺序**: 代码已优化为“先连 WebSocket 再 Post”，防止任务执行太快导致漏掉完成消息。
2.  **线程安全**: 内部已处理多线程上下文切换，但在 UI 更新结果时请确保在主线程。
3.  **依赖**: 
    - `UniTask`
    - `TouchSocket`
    - `Newtonsoft.Json`

## 常见问题与解决方案

### 1. GUI 错误 (ArgumentException)
**现象**: `System.ArgumentException: You can only call GUI functions from inside OnGUI.`
**原因**: 尝试在 `Completed` 回调或其他非 `OnGUI` 生命周期中直接调用 `GUILayout` 或 `GUI` 方法。
**解决**: 在回调中只更新数据（如保存 Texture 到变量），在 `OnGUI` 方法中进行绘制。

### 2. 连接被拒绝 (ConnectionRefused)
**现象**: `System.Net.WebException: Error: ConnectFailure`
**原因**: ComfyUI 服务未启动，或端口配置错误。
**解决**: 确保 ComfyUI 已运行且 IP/端口配置正确。模块内部已增加异常捕获，连接失败会通过 `Status=Failed` 和 `Error` 属性返回，不会导致程序崩溃。

### 3. 任务瞬间完成导致流程卡住
**现象**: WebSocket 连接后没有收到消息，或状态机一直停留在等待。
**原因**: 某些 ComfyUI 任务（如加载缓存）执行极快，在 WebSocket 建立前已完成。
**解决**: 本模块采用了“双重确认”机制 (`TaskIDHandler`)，无论 WebSocket 消息先到还是 HTTP 响应先到，都能正确匹配任务 ID 并触发下一步。确保代码中先连接 WebSocket 再提交 HTTP 请求。
