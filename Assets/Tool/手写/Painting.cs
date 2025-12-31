using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
 
public class Painting : MonoBehaviour
{
 
    private RenderTexture texRender;   //画布
    public Material mat;     //给定的shader新建材质
    public Texture brushTypeTexture;   //画笔纹理，半透明
    private Camera mainCamera;
    public float brushScale = 0.5f;
    [Range(0.1f, 5.0f)]
    public float brushSizeMultiplier = 1.0f;
    public Color brushColor = Color.black;
    public RawImage raw;                   //使用UGUI的RawImage显示，方便进行添加UI,将pivot设为(0.5,0.5)
    private float lastDistance;
    private Vector3[] PositionArray = new Vector3[3];
    private int a = 0;
    private Vector3[] PositionArray1 = new Vector3[4];
    private int b = 0;
    private float[] speedArray = new float[4];
    private int s = 0;
    public int num = 50;
    public Vector2 texRenderResolution;

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
    Vector3 startPosition = Vector3.zero;
    Vector3 endPosition = Vector3.zero;
    public bool isInputEnabled = true;

    void Update()
    {
        if (!isInputEnabled) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                OnMouseMove(new Vector3(touch.position.x, touch.position.y, 0));
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                OnMouseUp();
            }
        }
        else
        {
            if (Input.GetMouseButton(0))
            {
                OnMouseMove(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
            }
            if (Input.GetMouseButtonUp(0))
            {
                OnMouseUp();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            InitPaintingImage();
        }
        DrawImage();
    }
 
    public void SetInputEnabled(bool isEnabled)
    {
        isInputEnabled = isEnabled;
        if (!isEnabled)
        {
            OnMouseUp();
        }
    }

    void OnMouseUp()
    {
        startPosition = Vector3.zero;
        //brushScale = 0.5f;
        a = 0;
        b = 0;
        s = 0;
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
        //return 0.1f;
    }
 
    void OnMouseMove(Vector3 pos)
    {
        if (startPosition == Vector3.zero)
        {
            startPosition = pos;
        }

        endPosition = pos;
        float distance = Vector3.Distance(startPosition, endPosition);
        brushScale = SetScale(distance);
        ThreeOrderBézierCurse(pos, distance, 0.05f);
 
        startPosition = endPosition;
        lastDistance = distance;
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
        
        // Find the camera responsible for the RawImage
        Camera cam = null;
        if (raw.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            cam = null;
        else
            cam = raw.canvas.worldCamera; // Should use worldCamera for ScreenSpaceCamera or WorldSpace

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
    public void TwoOrderBézierCurse(Vector3 pos, float distance)
    {
        PositionArray[a] = pos;
        a++;
        if (a == 3)
        {
            for (int index = 0; index < num; index++)
            {
                Vector3 middle = (PositionArray[0] + PositionArray[2]) / 2;
                PositionArray[1] = (PositionArray[1] - middle) / 2 + middle;
 
                float t = (1.0f / num) * index / 2;
                Vector3 target = Mathf.Pow(1 - t, 2) * PositionArray[0] + 2 * (1 - t) * t * PositionArray[1] +
                                 Mathf.Pow(t, 2) * PositionArray[2];
                float deltaSpeed = (float)(distance - lastDistance) / num;
                DrawBrush(texRender, (int)target.x, (int)target.y, brushTypeTexture, brushColor, SetScale(lastDistance + (deltaSpeed * index)));
            }
            PositionArray[0] = PositionArray[1];
            PositionArray[1] = PositionArray[2];
            a = 2;
        }
        else
        {
            DrawBrush(texRender, (int)endPosition.x, (int)endPosition.y, brushTypeTexture,
                brushColor, brushScale);
        }
    }
    //三阶贝塞尔曲线，获取连续4个点坐标，通过调整中间2点坐标，画出部分（我使用了num/1.5实现画出部分曲线）来使曲线平滑;通过速度控制曲线宽度。
    private void ThreeOrderBézierCurse(Vector3 pos, float distance, float targetPosOffset)
    {
        //记录坐标
        PositionArray1[b] = pos;
        b++;
        //记录速度
        speedArray[s] = distance;
        s++;
        if (b == 4)
        {
            Vector3 temp1 = PositionArray1[1];
            Vector3 temp2 = PositionArray1[2];
 
            //修改中间两点坐标
            Vector3 middle = (PositionArray1[0] + PositionArray1[2]) / 2;
            PositionArray1[1] = (PositionArray1[1] - middle) * 1.5f + middle;
            middle = (temp1 + PositionArray1[3]) / 2;
            PositionArray1[2] = (PositionArray1[2] - middle) * 2.1f + middle;
 
            for (int index1 = 0; index1 < num / 1.5f; index1++)
            {
                float t1 = (1.0f / num) * index1;
                Vector3 target = Mathf.Pow(1 - t1, 3) * PositionArray1[0] +
                                 3 * PositionArray1[1] * t1 * Mathf.Pow(1 - t1, 2) +
                                 3 * PositionArray1[2] * t1 * t1 * (1 - t1) + PositionArray1[3] * Mathf.Pow(t1, 3);
                //float deltaspeed = (float)(distance - lastDistance) / num;
                //获取速度差值（存在问题，参考）
                float deltaspeed = (float)(speedArray[3] - speedArray[0]) / num;
                //float randomOffset = Random.Range(-1/(speedArray[0] + (deltaspeed * index1)), 1 / (speedArray[0] + (deltaspeed * index1)));
                //模拟毛刺效果
                float randomOffset = Random.Range(-targetPosOffset, targetPosOffset);
                DrawBrush(texRender, (int)(target.x + randomOffset), (int)(target.y + randomOffset), brushTypeTexture, brushColor, SetScale(speedArray[0] + (deltaspeed * index1)));
            }
 
            PositionArray1[0] = temp1;
            PositionArray1[1] = temp2;
            PositionArray1[2] = PositionArray1[3];
 
            speedArray[0] = speedArray[1];
            speedArray[1] = speedArray[2];
            speedArray[2] = speedArray[3];
            b = 3;
            s = 3;
        }
        else
        {
            DrawBrush(texRender, (int)endPosition.x, (int)endPosition.y, brushTypeTexture,
                brushColor, brushScale);
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
