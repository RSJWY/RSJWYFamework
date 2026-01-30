using System;
using UnityEngine;
using Newtonsoft.Json.Linq;
using RSJWYFamework.Runtime;
using RSJWYFamework.Runtime.Node;

public class ComfyUITaskTest : MonoBehaviour
{
    [Header("ComfyUI配置")]
    [SerializeField] private string serverAddress = "http://127.0.0.1:8188";
    [SerializeField] private bool useWSS = false;
    [SerializeField] private string clientId = "test-client-123";
    
    [Header("测试JSON数据")]
    [TextArea(10, 20)]
    [SerializeField] private string testJsonData = @"{
  ""3"": {
    ""inputs"": {
      ""seed"": 156680208700286,
      ""steps"": 20,
      ""cfg"": 8,
      ""sampler_name"": ""euler"",
      ""scheduler"": ""normal"",
      ""denoise"": 1,
      ""model"": [""4"", 0],
      ""positive"": [""6"", 0],
      ""negative"": [""7"", 0],
      ""latent_image"": [""5"", 0]
    },
    ""class_type"": ""KSampler""
  },
  ""4"": {
    ""inputs"": {
      ""ckpt_name"": ""v1-5-pruned-emaonly.ckpt""
    },
    ""class_type"": ""CheckpointLoaderSimple""
  },
  ""5"": {
    ""inputs"": {
      ""width"": 512,
      ""height"": 512,
      ""batch_size"": 1
    },
    ""class_type"": ""EmptyLatentImage""
  },
  ""6"": {
    ""inputs"": {
      ""text"": ""beautiful scenery nature glass bottle landscape, purple galaxy bottle"",
      ""clip"": [""4"", 1]
    },
    ""class_type"": ""CLIPTextEncode""
  },
  ""7"": {
    ""inputs"": {
      ""text"": ""text, watermark"",
      ""clip"": [""4"", 1]
    },
    ""class_type"": ""CLIPTextEncode""
  },
  ""8"": {
    ""inputs"": {
      ""samples"": [""3"", 0],
      ""vae"": [""4"", 2]
    },
    ""class_type"": ""VAEDecode""
  },
  ""9"": {
    ""inputs"": {
      ""filename_prefix"": ""ComfyUI"",
      ""images"": [""8"", 0]
    },
    ""class_type"": ""SaveImage""
  }
}";

    [Header("测试控制")]
    [SerializeField] private bool autoStartTest = false;
    
    private ComfyUITaskAsyncOperation currentTask;
    public Texture2D lastResultTexture;
    private ProgressData lastProgress;

    void Start()
    {
        if (autoStartTest)
        {
            StartComfyUITest();
        }
    }

    [ContextMenu("开始ComfyUI测试")]
    public void StartComfyUITest()
    {
        if (currentTask != null && currentTask.Status == AppAsyncOperationStatus.Processing)
        {
            Debug.LogWarning("已有任务正在运行中，请等待完成或停止当前任务");
            return;
        }

        Debug.Log("开始ComfyUI任务测试...");
        
        lastProgress = null; // 重置进度

        // 创建ComfyUI任务
        currentTask = new ComfyUITaskAsyncOperation(
            clientId,
            JObject.Parse(testJsonData), serverAddress,
            GetHistoryImageURLFromResponse,
            useWSS,
            this
        );

        // 监听任务完成事件
        currentTask.Completed += OnTaskCompleted;
        
        // 监听进度事件
        currentTask.OnProgress += OnTaskProgress;
        
        // 启动任务
        //currentTask.StartAddAppAsyncOperationSystem($"ComfyUITestTask_{clientId}");
        
        Debug.Log($"任务已启动，客户端ID: {clientId}");
        Debug.Log($"服务器地址: {serverAddress}");
        Debug.Log($"使用WebSocket: {useWSS}");
    }

    private void OnTaskProgress(ProgressData progress)
    {
        lastProgress = progress;
        Debug.Log($"任务进度: {progress.Value}/{progress.Max} (Node: {progress.Node})");
    }

    [ContextMenu("停止当前任务")]
    public void StopCurrentTask()
    {
        if (currentTask != null)
        {
            currentTask.Abort();
            Debug.Log("任务已停止");
        }
    }

    /// <summary>
    /// 从ComfyUI历史响应中提取图片URL的处理函数
    /// 这是一个简单的示例实现，您可以根据实际需要修改
    /// </summary>
    private GetHistoryImageURLResult GetHistoryImageURLFromResponse(JObject historyResponse,string prompt_id)
    {
        try
        {
            Debug.Log("收到历史响应数据: " + historyResponse.ToString());
            
            // 这里是一个简化的解析逻辑
            // 实际的ComfyUI响应结构可能不同，需要根据实际情况调整
            var ImageInfo = historyResponse[prompt_id]["outputs"]["9"]["images"][0];
            var imageURL = GetHistoryImageURLResult.GetFullImageURL(ImageInfo["filename"].ToString(),ImageInfo["type"].ToString());
            
            return new GetHistoryImageURLResult
            {
                ImageURL = imageURL,
                Success = true,
                Error = "未在响应中找到图片信息"
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析历史响应时出错: {ex.Message}");
            return new GetHistoryImageURLResult
            {
                ImageURL = string.Empty,
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 任务完成回调
    /// </summary>
    private void OnTaskCompleted(AppAsyncOperationBase operation)
    {
        var task = operation as ComfyUITaskAsyncOperation;
        if (task != null)
        {
            if (task.Status == AppAsyncOperationStatus.Succeed)
            {
                Debug.Log("✅ ComfyUI任务执行成功！");
                
                // 这里可以获取生成的图片等结果
                // 具体的结果获取方式需要根据实际的StateMachine实现来确定
            }
            else if (task.Status == AppAsyncOperationStatus.Failed)
            {
                Debug.LogError($"❌ ComfyUI任务执行失败: {task.Error}");
            }
            
            // 显示下载的图片
            if (task.DownloadedTexture != null)
            {
                lastResultTexture = task.DownloadedTexture;
            }
        }
        
        currentTask = null;
    }

    void Update()
    {
        // 显示当前任务状态
        if (currentTask != null)
        {
            // 这里可以添加UI显示或其他状态更新逻辑
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20));
        
        GUILayout.Label("ComfyUI任务测试");
        
        if (currentTask == null)
        {
            if (GUILayout.Button("开始测试"))
            {
                StartComfyUITest();
            }
        }
        else
        {
            GUILayout.Label($"任务状态: {currentTask.Status}");
            
            if (GUILayout.Button("停止任务"))
            {
                StopCurrentTask();
            }
            
            if (lastProgress != null)
            {
                GUILayout.Space(5);
                GUILayout.Label($"进度: {lastProgress.Value}/{lastProgress.Max}");
                GUILayout.Label($"当前节点: {lastProgress.Node}");
                
                // 绘制进度条
                float progress = (float)lastProgress.Value / lastProgress.Max;
                GUILayout.HorizontalSlider(progress, 0f, 1f);
            }
        }
        
        GUILayout.Space(10);
        GUILayout.Label($"服务器: {serverAddress}");
        GUILayout.Label($"客户端ID: {clientId}");
        
        if (lastResultTexture != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("生成结果:");
            // 限制图片显示大小，避免过大撑爆屏幕
            float aspect = (float)lastResultTexture.width / lastResultTexture.height;
            float width = Mathf.Min(400, lastResultTexture.width);
            float height = width / aspect;
            GUILayout.Box(lastResultTexture, GUILayout.Width(width), GUILayout.Height(height));
        }

        GUILayout.EndArea();
    }
}