using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Random = UnityEngine.Random;
 
public class Painting : MonoBehaviour
{
    private class Stroke
    {
        public Vector3 startPosition = Vector3.zero;
        public float lastDistance;
        public float brushScale = 0.5f;
        
        // For TwoOrderBézierCurse
        public Vector3[] PositionArray = new Vector3[3];
        public int a = 0;
        
        // For ThreeOrderBézierCurse
        public Vector3[] PositionArray1 = new Vector3[4];
        public int b = 0;
        public float[] speedArray = new float[4];
        public int s = 0;
    }

    private RenderTexture texRender;   //画布
    public Material mat;     //给定的shader新建材质
    public Texture brushTypeTexture;   //画笔纹理，半透明
    private Camera mainCamera;
    public float brushScale = 0.5f; // Keep for inspector or default, though strokes have their own
    [Range(0.1f, 5.0f)]
    public float brushSizeMultiplier = 1.0f;
    public Color brushColor = Color.black;
    public RawImage raw;                   //使用UGUI的RawImage显示，方便进行添加UI,将pivot设为(0.5,0.5)
    
    public int num = 50;
    public Vector2 texRenderResolution;
    
    public bool isInputEnabled = true;

    private Dictionary<int, Stroke> activeStrokes = new Dictionary<int, Stroke>();

    void Start()
    {
        if (texRenderResolution.x == 0 || texRenderResolution.y == 0)
        {
            texRenderResolution = new Vector2(1920, 1080);
        }

        texRender = new RenderTexture((int)texRenderResolution.x, (int)texRenderResolution.y, 24, RenderTextureFormat.ARGB32);
        Clear(texRender);
        
        InitPaintingImage();
    }

    public void InitPaintingImage()
    {
        RenderTexture.active = texRender; // 设置活动渲染纹理

        Color semiTransparentColor = new Color(1, 1, 1, 0f); // 半透明黑色
        GL.Clear(true, true, semiTransparentColor);
        RenderTexture.active = null; // 重置活动渲染纹理
        DrawImage();
    }

    void Update()
    {
        if (!isInputEnabled) return;

        // 优先处理触摸输入
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
            // 如果没有触摸，则处理鼠标输入 (Pointer ID -1)
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            InitPaintingImage();
        }
        DrawImage();
    }

    void HandleInput(int pointerId, Vector2 position, TouchPhase phase)
    {
        Vector3 pos = new Vector3(position.x, position.y, 0);

        if (phase == TouchPhase.Began)
        {
            // 仅仅响应使用区域的触摸
            if (IsPointerOverDrawingArea(position))
            {
                Stroke stroke = new Stroke();
                stroke.startPosition = pos;
                // 初始化画笔大小
                stroke.brushScale = SetScale(0); 
                activeStrokes[pointerId] = stroke;
                
                OnMouseMove(stroke, pos);
            }
        }
        else if (phase == TouchPhase.Moved || phase == TouchPhase.Stationary)
        {
            if (activeStrokes.TryGetValue(pointerId, out Stroke stroke))
            {
                OnMouseMove(stroke, pos);
            }
        }
        else if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
        {
            if (activeStrokes.TryGetValue(pointerId, out Stroke stroke))
            {
                OnMouseUp(stroke);
                activeStrokes.Remove(pointerId);
            }
        }
    }

    bool IsPointerOverDrawingArea(Vector2 screenPos)
    {
        if (raw == null) return false;
        Camera cam = GetRawImageCamera();
        return RectTransformUtility.RectangleContainsScreenPoint(raw.rectTransform, screenPos, cam);
    }

    Camera GetRawImageCamera()
    {
        if (raw.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        else
            return raw.canvas.worldCamera;
    }
 
    public void SetInputEnabled(bool isEnabled)
    {
        isInputEnabled = isEnabled;
        if (!isEnabled)
        {
            activeStrokes.Clear();
        }
    }

    void OnMouseUp(Stroke stroke)
    {
        stroke.startPosition = Vector3.zero;
        stroke.a = 0;
        stroke.b = 0;
        stroke.s = 0;
    }

    //设置画笔宽度
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
 
    void OnMouseMove(Stroke stroke, Vector3 pos)
    {
        if (stroke.startPosition == Vector3.zero)
        {
            stroke.startPosition = pos;
        }
 
        Vector3 endPosition = pos;
        float distance = Vector3.Distance(stroke.startPosition, endPosition);
        stroke.brushScale = SetScale(distance);
        ThreeOrderBézierCurse(stroke, pos, distance, 0.05f);
 
        stroke.startPosition = endPosition;
        stroke.lastDistance = distance;
    }
 
    void Clear(RenderTexture destTexture)
    {
        Graphics.SetRenderTarget(destTexture);
        GL.PushMatrix();
        GL.Clear(true, true,new Color(26,26,26));
        GL.PopMatrix();
    }
 
    void DrawBrush(RenderTexture destTexture, int x, int y, Texture sourceTexture, Color color, float scale)
    {
        DrawBrush(destTexture, new Rect(x, y, sourceTexture.width, sourceTexture.height), sourceTexture, color, scale);
    }
    void DrawBrush(RenderTexture destTexture, Rect destRect, Texture sourceTexture, Color color, float scale)
    {
        // Convert screen position (destRect.x, destRect.y) to local point in RawImage
        // Note: destRect.x and destRect.y come from Input.mousePosition (screen coordinates)
        Vector2 screenPoint = new Vector2(destRect.x, destRect.y);
        Vector2 localPoint;
        
        Camera cam = GetRawImageCamera();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(raw.rectTransform, screenPoint, cam, out localPoint))
        {
            // Normalize local point to UV space (0..1)
            // rect.xMin is -width/2, rect.yMin is -height/2 (if pivot is 0.5, 0.5)
            float u = (localPoint.x - raw.rectTransform.rect.xMin) / raw.rectTransform.rect.width;
            float v = (localPoint.y - raw.rectTransform.rect.yMin) / raw.rectTransform.rect.height;

            // Calculate brush size in Normalized Device Coordinates (0..1) relative to destTexture
            // We scale the brush texture size by 'scale' and then normalize by destTexture size
            float normWidth = (sourceTexture.width * scale) / destTexture.width;
            float normHeight = (sourceTexture.height * scale) / destTexture.height;

            float left = u - normWidth / 2.0f;
            float right = u + normWidth / 2.0f;
            float top = v + normHeight / 2.0f;
            float bottom = v - normHeight / 2.0f;

            Graphics.SetRenderTarget(destTexture);

            GL.PushMatrix();
            GL.LoadOrtho();

            mat.SetTexture("_MainTex", brushTypeTexture);
            mat.SetColor("_Color", color);
            mat.SetPass(0);

            GL.Begin(GL.QUADS);

            GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(left, bottom, 0); // Bottom-Left
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(right, bottom, 0); // Bottom-Right
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(right, top, 0);   // Top-Right
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(left, top, 0);    // Top-Left

            GL.End();
            GL.PopMatrix();
        }
    }
    bool bshow = true;
    void DrawImage()
    {
        raw.texture = texRender;
    }
    public void OnClickClear()
    {
        Clear(texRender);
    }
 
    //二阶贝塞尔曲线
    private void TwoOrderBézierCurse(Stroke stroke, Vector3 pos, float distance)
    {
        stroke.PositionArray[stroke.a] = pos;
        stroke.a++;
        if (stroke.a == 3)
        {
            for (int index = 0; index < num; index++)
            {
                Vector3 middle = (stroke.PositionArray[0] + stroke.PositionArray[2]) / 2;
                stroke.PositionArray[1] = (stroke.PositionArray[1] - middle) / 2 + middle;
 
                float t = (1.0f / num) * index / 2;
                Vector3 target = Mathf.Pow(1 - t, 2) * stroke.PositionArray[0] + 2 * (1 - t) * t * stroke.PositionArray[1] +
                                 Mathf.Pow(t, 2) * stroke.PositionArray[2];
                float deltaSpeed = (float)(distance - stroke.lastDistance) / num;
                DrawBrush(texRender, (int)target.x, (int)target.y, brushTypeTexture, brushColor, SetScale(stroke.lastDistance + (deltaSpeed * index)));
            }
            stroke.PositionArray[0] = stroke.PositionArray[1];
            stroke.PositionArray[1] = stroke.PositionArray[2];
            stroke.a = 2;
        }
        else
        {
            DrawBrush(texRender, (int)pos.x, (int)pos.y, brushTypeTexture,
                brushColor, stroke.brushScale);
        }
    }
    //三阶贝塞尔曲线，获取连续4个点坐标，通过调整中间2点坐标，画出部分（我使用了num/1.5实现画出部分曲线）来使曲线平滑;通过速度控制曲线宽度。
    private void ThreeOrderBézierCurse(Stroke stroke, Vector3 pos, float distance, float targetPosOffset)
    {
        //记录坐标
        stroke.PositionArray1[stroke.b] = pos;
        stroke.b++;
        //记录速度
        stroke.speedArray[stroke.s] = distance;
        stroke.s++;
        if (stroke.b == 4)
        {
            Vector3 temp1 = stroke.PositionArray1[1];
            Vector3 temp2 = stroke.PositionArray1[2];
 
            //修改中间两点坐标
            Vector3 middle = (stroke.PositionArray1[0] + stroke.PositionArray1[2]) / 2;
            stroke.PositionArray1[1] = (stroke.PositionArray1[1] - middle) * 1.5f + middle;
            middle = (temp1 + stroke.PositionArray1[3]) / 2;
            stroke.PositionArray1[2] = (stroke.PositionArray1[2] - middle) * 2.1f + middle;
 
            for (int index1 = 0; index1 < num / 1.5f; index1++)
            {
                float t1 = (1.0f / num) * index1;
                Vector3 target = Mathf.Pow(1 - t1, 3) * stroke.PositionArray1[0] +
                                 3 * stroke.PositionArray1[1] * t1 * Mathf.Pow(1 - t1, 2) +
                                 3 * stroke.PositionArray1[2] * t1 * t1 * (1 - t1) + stroke.PositionArray1[3] * Mathf.Pow(t1, 3);
                //float deltaspeed = (float)(distance - lastDistance) / num;
                //获取速度差值（存在问题，参考）
                float deltaspeed = (float)(stroke.speedArray[3] - stroke.speedArray[0]) / num;
                //float randomOffset = Random.Range(-1/(speedArray[0] + (deltaspeed * index1)), 1 / (speedArray[0] + (deltaspeed * index1)));
                //模拟毛刺效果
                float randomOffset = Random.Range(-targetPosOffset, targetPosOffset);
                DrawBrush(texRender, (int)(target.x + randomOffset), (int)(target.y + randomOffset), brushTypeTexture, brushColor, SetScale(stroke.speedArray[0] + (deltaspeed * index1)));
            }
 
            stroke.PositionArray1[0] = temp1;
            stroke.PositionArray1[1] = temp2;
            stroke.PositionArray1[2] = stroke.PositionArray1[3];
 
            stroke.speedArray[0] = stroke.speedArray[1];
            stroke.speedArray[1] = stroke.speedArray[2];
            stroke.speedArray[2] = stroke.speedArray[3];
            stroke.b = 3;
            stroke.s = 3;
        }
        else
        {
            DrawBrush(texRender, (int)pos.x, (int)pos.y, brushTypeTexture,
                brushColor, stroke.brushScale);
        }
 
    }

    /// <summary>
    /// Returns a new Texture2D copy of the current painting.
    /// You are responsible for destroying this texture when done to avoid memory leaks.
    /// </summary>
    /// <returns>A new Texture2D containing the painting content.</returns>
    public Texture2D GetPaintingTexture()
    {
        if (texRender == null) return null;

        // Create a new Texture2D with the same dimensions as the RenderTexture
        Texture2D texture = new Texture2D(texRender.width, texRender.height, TextureFormat.ARGB32, false);
        
        // Remember the currently active render texture
        RenderTexture currentActiveRT = RenderTexture.active;

        // Set the supplied RenderTexture as the active one
        RenderTexture.active = texRender;

        // Read the pixels from the RenderTexture into the Texture2D
        texture.ReadPixels(new Rect(0, 0, texRender.width, texRender.height), 0, 0);
        texture.Apply();

        // Restore the previously active render texture
        RenderTexture.active = currentActiveRT;

        return texture;
    }
}
