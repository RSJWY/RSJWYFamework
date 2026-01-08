using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/// <summary>
/// 绘画控制器，支持多点触控、鼠标绘制，并使用贝塞尔曲线进行笔触平滑处理。
/// 依赖 UGUI 的 RawImage 进行显示，使用 RenderTexture 进行离屏渲染。
/// </summary>
public class Painting : MonoBehaviour
{
    /// <summary>
    /// 内部类：记录单个笔画（触摸点或鼠标）的状态数据。
    /// </summary>
    private class Stroke
    {
        /// <summary>
        /// 笔画的起始位置（上一帧位置）。
        /// </summary>
        public Vector3 startPosition = Vector3.zero;

        /// <summary>
        /// 上一次计算出的笔画宽度基准距离（速度相关）。
        /// </summary>
        public float lastDistance;

        /// <summary>
        /// 当前笔画的宽度缩放比例。
        /// </summary>
        public float brushScale = 0.5f;
        
        // --- 二阶贝塞尔曲线相关参数 ---
        /// <summary>
        /// 存储二阶贝塞尔曲线需要的3个控制点。
        /// </summary>
        public Vector3[] PositionArray = new Vector3[3];
        /// <summary>
        /// 当前已收集的二阶曲线控制点数量索引。
        /// </summary>
        public int a = 0;
        
        // --- 三阶贝塞尔曲线相关参数 ---
        /// <summary>
        /// 存储三阶贝塞尔曲线需要的4个控制点。
        /// </summary>
        public Vector3[] PositionArray1 = new Vector3[4];
        /// <summary>
        /// 当前已收集的三阶曲线控制点数量索引。
        /// </summary>
        public int b = 0;
        /// <summary>
        /// 存储最近4帧的速度（距离），用于平滑笔触粗细变化。
        /// </summary>
        public float[] speedArray = new float[4];
        /// <summary>
        /// 当前已收集的速度数据数量索引。
        /// </summary>
        public int s = 0;
    }

    [Header("渲染设置")]
    /// <summary>
    /// 画布纹理，用于存储绘画结果。
    /// </summary>
    private RenderTexture texRender;
    
    /// <summary>
    /// 用于绘制笔触的材质，需指定Shader。
    /// </summary>
    public Material mat;
    
    /// <summary>
    /// 笔触纹理，建议使用半透明图片以获得更好的叠加效果。
    /// </summary>
    public Texture brushTypeTexture;

    [Header("笔刷设置")]
    /// <summary>
    /// 基础笔刷缩放比例（默认值）。
    /// </summary>
    public float brushScale = 0.5f;

    /// <summary>
    /// 笔刷大小的全局倍率，可在Inspector中调整。
    /// </summary>
    [Range(0.1f, 5.0f)]
    public float brushSizeMultiplier = 1.0f;
    
    /// <summary>
    /// 笔刷颜色。
    /// </summary>
    public Color brushColor = Color.black;

    [Header("UI设置")]
    /// <summary>
    /// 用于显示画布的 UGUI RawImage 组件。
    /// 注意：Pivot 建议设置为 (0.5, 0.5)。
    /// </summary>
    public RawImage raw;
    
    [Header("平滑参数")]
    /// <summary>
    /// 贝塞尔曲线插值点的数量，数值越大曲线越平滑，但性能开销越大。
    /// </summary>
    public int num = 50;

    /// <summary>
    /// 画布纹理的分辨率。若为 (0,0) 则默认为 1920x1080。
    /// </summary>
    public Vector2 texRenderResolution;
    
    /// <summary>
    /// 是否允许输入（开启/关闭绘画功能）。
    /// </summary>
    public bool isInputEnabled = true;

    /// <summary>
    /// 存储当前活跃的笔画，Key 为触摸ID（FingerId），Value 为笔画状态对象。
    /// </summary>
    private Dictionary<int, Stroke> activeStrokes = new Dictionary<int, Stroke>();

    void Start()
    {
        // 初始化画布分辨率
        if (texRenderResolution.x == 0 || texRenderResolution.y == 0)
        {
            texRenderResolution = new Vector2(1920, 1080);
        }

        // 创建 RenderTexture
        texRender = new RenderTexture((int)texRenderResolution.x, (int)texRenderResolution.y, 24, RenderTextureFormat.ARGB32);
        
        // 初始清空画布
        Clear(texRender);
        
        // 初始化绘画状态
        InitPaintingImage();
    }

    /// <summary>
    /// 初始化或重置绘画图像。
    /// </summary>
    public void InitPaintingImage()
    {
        RenderTexture.active = texRender; // 设置活动渲染纹理为当前画布

        Color semiTransparentColor = new Color(1, 1, 1, 0f); // 使用完全透明的颜色清空
        GL.Clear(true, true, semiTransparentColor);
        RenderTexture.active = null; // 重置活动渲染纹理
        
        // 刷新显示
        DrawImage();
    }

    void Update()
    {
        if (!isInputEnabled) return;

        // 优先处理触摸输入 (多点触控)
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                HandleInput(t.fingerId, t.position, t.phase);
            }
        }
        else
        {
            // 如果没有触摸，则处理鼠标输入 (Pointer ID 设为 -1)
            // 这确保了在 PC 端也能正常测试
            if (Input.GetMouseButtonDown(0))
            {
                HandleInput(-1, Input.mousePosition, TouchPhase.Began);
            }
            else if (Input.GetMouseButton(0))
            {
                HandleInput(-1, Input.mousePosition, TouchPhase.Moved);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                HandleInput(-1, Input.mousePosition, TouchPhase.Ended);
            }
        }

        // 快捷键重置画布
        if (Input.GetKeyDown(KeyCode.Space))
        {
            InitPaintingImage();
        }
        
        // 更新 RawImage 显示
        DrawImage();
    }

    /// <summary>
    /// 统一处理输入逻辑（触摸或鼠标）。
    /// </summary>
    /// <param name="pointerId">输入指针的唯一ID（FingerId 或 -1）。</param>
    /// <param name="position">屏幕坐标位置。</param>
    /// <param name="phase">输入阶段（开始、移动、结束）。</param>
    void HandleInput(int pointerId, Vector2 position, TouchPhase phase)
    {
        Vector3 pos = new Vector3(position.x, position.y, 0);

        if (phase == TouchPhase.Began)
        {
            // 仅当触摸点位于绘图区域内时才开始新的笔画
            if (IsPointerOverDrawingArea(position))
            {
                Stroke stroke = new Stroke();
                stroke.startPosition = pos;
                // 初始化笔画粗细
                stroke.brushScale = SetScale(0); 
                activeStrokes[pointerId] = stroke;
                
                OnMouseMove(stroke, pos);
            }
        }
        else if (phase == TouchPhase.Moved || phase == TouchPhase.Stationary)
        {
            // 处理移动：如果该指针ID已有活跃笔画，则更新绘制
            if (activeStrokes.TryGetValue(pointerId, out Stroke stroke))
            {
                OnMouseMove(stroke, pos);
            }
        }
        else if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
        {
            // 处理结束：完成笔画并移除状态
            if (activeStrokes.TryGetValue(pointerId, out Stroke stroke))
            {
                OnMouseUp(stroke);
                activeStrokes.Remove(pointerId);
            }
        }
    }

    /// <summary>
    /// 判断屏幕上的点是否位于绘图区域 (RawImage) 内。
    /// </summary>
    bool IsPointerOverDrawingArea(Vector2 screenPos)
    {
        if (raw == null) return false;
        Camera cam = GetRawImageCamera();
        return RectTransformUtility.RectangleContainsScreenPoint(raw.rectTransform, screenPos, cam);
    }

    /// <summary>
    /// 获取渲染 RawImage 的摄像机。
    /// Overlay 模式下返回 null，其他模式返回 worldCamera。
    /// </summary>
    Camera GetRawImageCamera()
    {
        if (raw.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        else
            return raw.canvas.worldCamera;
    }
 
    /// <summary>
    /// 启用或禁用输入。禁用时会清除所有活跃笔画。
    /// </summary>
    public void SetInputEnabled(bool isEnabled)
    {
        isInputEnabled = isEnabled;
        if (!isEnabled)
        {
            activeStrokes.Clear();
        }
    }

    /// <summary>
    /// 笔画结束时的清理操作。
    /// </summary>
    void OnMouseUp(Stroke stroke)
    {
        stroke.startPosition = Vector3.zero;
        stroke.a = 0;
        stroke.b = 0;
        stroke.s = 0;
    }

    /// <summary>
    /// 根据移动距离（速度）计算笔刷大小，模拟压感效果（速度越快笔触越细）。
    /// </summary>
    /// <param name="distance">两帧之间的移动距离。</param>
    /// <returns>计算后的笔刷缩放比例。</returns>
    float SetScale(float distance)
    {
        float Scale = 0;
        if (distance < 100)
        {
            Scale = 0.8f - 0.005f * distance;
        }
        else
        {
            Scale = 0.425f - 0.00125f * distance;
        }
        if (Scale <= 0.05f)
        {
            Scale = 0.05f;
        }
        return Scale * brushSizeMultiplier;
    }
 
    /// <summary>
    /// 处理移动并进行绘制的核心方法。
    /// </summary>
    void OnMouseMove(Stroke stroke, Vector3 pos)
    {
        if (stroke.startPosition == Vector3.zero)
        {
            stroke.startPosition = pos;
        }
 
        Vector3 endPosition = pos;
        float distance = Vector3.Distance(stroke.startPosition, endPosition);
        stroke.brushScale = SetScale(distance);
        
        // 使用三阶贝塞尔曲线进行平滑绘制
        ThreeOrderBézierCurse(stroke, pos, distance, 0.05f);
 
        stroke.startPosition = endPosition;
        stroke.lastDistance = distance;
    }
 
    /// <summary>
    /// 清空 RenderTexture 为指定颜色（默认为深灰色）。
    /// </summary>
    void Clear(RenderTexture destTexture)
    {
        Graphics.SetRenderTarget(destTexture);
        GL.PushMatrix();
        GL.Clear(true, true,new Color(26,26,26)); // 注意：这里使用了深灰色背景，如果需要透明背景可调整
        GL.PopMatrix();
    }
 
    /// <summary>
    /// 绘制笔刷纹理（重载方法1）。
    /// </summary>
    void DrawBrush(RenderTexture destTexture, int x, int y, Texture sourceTexture, Color color, float scale)
    {
        DrawBrush(destTexture, new Rect(x, y, sourceTexture.width, sourceTexture.height), sourceTexture, color, scale);
    }

    /// <summary>
    /// 绘制笔刷纹理（核心绘制逻辑）。
    /// 将屏幕坐标转换为 UV 坐标，并使用 GL 绘制 Quad。
    /// </summary>
    void DrawBrush(RenderTexture destTexture, Rect destRect, Texture sourceTexture, Color color, float scale)
    {
        // 屏幕坐标 (destRect.x, destRect.y)
        Vector2 screenPoint = new Vector2(destRect.x, destRect.y);
        Vector2 localPoint;
        
        Camera cam = GetRawImageCamera();

        // 将屏幕点转换为 RawImage 的局部坐标
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(raw.rectTransform, screenPoint, cam, out localPoint))
        {
            // 将局部坐标归一化到 UV 空间 (0..1)
            // rect.xMin 是 -width/2, rect.yMin 是 -height/2 (假设 pivot 为 0.5, 0.5)
            float u = (localPoint.x - raw.rectTransform.rect.xMin) / raw.rectTransform.rect.width;
            float v = (localPoint.y - raw.rectTransform.rect.yMin) / raw.rectTransform.rect.height;

            // 计算笔刷在目标纹理上的归一化大小
            float normWidth = (sourceTexture.width * scale) / destTexture.width;
            float normHeight = (sourceTexture.height * scale) / destTexture.height;

            // 计算绘制 Quad 的四个顶点 UV 坐标
            float left = u - normWidth / 2.0f;
            float right = u + normWidth / 2.0f;
            float top = v + normHeight / 2.0f;
            float bottom = v - normHeight / 2.0f;

            // 设置渲染目标
            Graphics.SetRenderTarget(destTexture);

            GL.PushMatrix();
            GL.LoadOrtho(); // 加载正交投影矩阵

            // 设置材质属性
            mat.SetTexture("_MainTex", brushTypeTexture);
            mat.SetColor("_Color", color);
            mat.SetPass(0);

            // 开始绘制 Quad
            GL.Begin(GL.QUADS);

            GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(left, bottom, 0); // 左下
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(right, bottom, 0); // 右下
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(right, top, 0);   // 右上
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(left, top, 0);    // 左上

            GL.End();
            GL.PopMatrix();
        }
    }
    
    // bool bshow = true; // 未使用的变量
    
    /// <summary>
    /// 将渲染结果应用到 UI 上。
    /// </summary>
    void DrawImage()
    {
        raw.texture = texRender;
    }

    /// <summary>
    /// 公开方法：清空画布（按钮点击事件等）。
    /// </summary>
    public void OnClickClear()
    {
        Clear(texRender);
    }
 
    /// <summary>
    /// 二阶贝塞尔曲线算法。
    /// 收集3个点后绘制一段曲线。
    /// </summary>
    private void TwoOrderBézierCurse(Stroke stroke, Vector3 pos, float distance)
    {
        stroke.PositionArray[stroke.a] = pos;
        stroke.a++;
        if (stroke.a == 3)
        {
            // 收集满3个点，开始插值绘制
            for (int index = 0; index < num; index++)
            {
                // 计算控制点
                Vector3 middle = (stroke.PositionArray[0] + stroke.PositionArray[2]) / 2;
                stroke.PositionArray[1] = (stroke.PositionArray[1] - middle) / 2 + middle;
 
                // 计算贝塞尔曲线点
                float t = (1.0f / num) * index / 2;
                Vector3 target = Mathf.Pow(1 - t, 2) * stroke.PositionArray[0] + 2 * (1 - t) * t * stroke.PositionArray[1] +
                                 Mathf.Pow(t, 2) * stroke.PositionArray[2];
                
                // 线性插值计算当前点的速度，用于控制笔触大小
                float deltaSpeed = (float)(distance - stroke.lastDistance) / num;
                
                // 绘制笔触
                DrawBrush(texRender, (int)target.x, (int)target.y, brushTypeTexture, brushColor, SetScale(stroke.lastDistance + (deltaSpeed * index)));
            }
            // 滚动数组，为下一段曲线做准备
            stroke.PositionArray[0] = stroke.PositionArray[1];
            stroke.PositionArray[1] = stroke.PositionArray[2];
            stroke.a = 2;
        }
        else
        {
            // 点不足时直接绘制当前点
            DrawBrush(texRender, (int)pos.x, (int)pos.y, brushTypeTexture,
                brushColor, stroke.brushScale);
        }
    }

    /// <summary>
    /// 三阶贝塞尔曲线算法。
    /// 获取连续4个点坐标，通过调整中间2点坐标使曲线平滑；通过速度插值控制曲线宽度变化。
    /// </summary>
    /// <param name="targetPosOffset">模拟毛刺效果的随机偏移量。</param>
    private void ThreeOrderBézierCurse(Stroke stroke, Vector3 pos, float distance, float targetPosOffset)
    {
        // 记录坐标
        stroke.PositionArray1[stroke.b] = pos;
        stroke.b++;
        // 记录速度
        stroke.speedArray[stroke.s] = distance;
        stroke.s++;
        
        if (stroke.b == 4)
        {
            Vector3 temp1 = stroke.PositionArray1[1];
            Vector3 temp2 = stroke.PositionArray1[2];
 
            // 修改中间两点坐标以平滑连接
            Vector3 middle = (stroke.PositionArray1[0] + stroke.PositionArray1[2]) / 2;
            stroke.PositionArray1[1] = (stroke.PositionArray1[1] - middle) * 1.5f + middle;
            middle = (temp1 + stroke.PositionArray1[3]) / 2;
            stroke.PositionArray1[2] = (stroke.PositionArray1[2] - middle) * 2.1f + middle;
 
            // 插值绘制
            for (int index1 = 0; index1 < num / 1.5f; index1++)
            {
                float t1 = (1.0f / num) * index1;
                Vector3 target = Mathf.Pow(1 - t1, 3) * stroke.PositionArray1[0] +
                                 3 * stroke.PositionArray1[1] * t1 * Mathf.Pow(1 - t1, 2) +
                                 3 * stroke.PositionArray1[2] * t1 * t1 * (1 - t1) + stroke.PositionArray1[3] * Mathf.Pow(t1, 3);
                
                // 获取速度差值进行插值
                float deltaspeed = (float)(stroke.speedArray[3] - stroke.speedArray[0]) / num;
                
                // 模拟毛刺效果 (随机偏移)
                float randomOffset = Random.Range(-targetPosOffset, targetPosOffset);
                
                DrawBrush(texRender, (int)(target.x + randomOffset), (int)(target.y + randomOffset), brushTypeTexture, brushColor, SetScale(stroke.speedArray[0] + (deltaspeed * index1)));
            }
 
            // 滚动更新坐标数组
            stroke.PositionArray1[0] = temp1;
            stroke.PositionArray1[1] = temp2;
            stroke.PositionArray1[2] = stroke.PositionArray1[3];
 
            // 滚动更新速度数组
            stroke.speedArray[0] = stroke.speedArray[1];
            stroke.speedArray[1] = stroke.speedArray[2];
            stroke.speedArray[2] = stroke.speedArray[3];
            stroke.b = 3;
            stroke.s = 3;
        }
        else
        {
            // 点不足时直接绘制
            DrawBrush(texRender, (int)pos.x, (int)pos.y, brushTypeTexture,
                brushColor, stroke.brushScale);
        }
    }

    /// <summary>
    /// 获取当前绘画内容的 Texture2D 副本。
    /// 注意：使用完毕后请务必销毁返回的 Texture2D 以免内存泄漏。
    /// </summary>
    /// <returns>包含绘画内容的 Texture2D。</returns>
    public Texture2D GetPaintingTexture()
    {
        if (texRender == null) return null;

        // 创建与 RenderTexture 相同尺寸的 Texture2D
        Texture2D texture = new Texture2D(texRender.width, texRender.height, TextureFormat.ARGB32, false);
        
        // 记录当前激活的 RenderTexture
        RenderTexture currentActiveRT = RenderTexture.active;

        // 设置当前 RenderTexture 为激活状态以便读取
        RenderTexture.active = texRender;

        // 读取像素到 Texture2D
        texture.ReadPixels(new Rect(0, 0, texRender.width, texRender.height), 0, 0);
        texture.Apply();

        // 恢复之前激活的 RenderTexture
        RenderTexture.active = currentActiveRT;

        return texture;
    }
}
