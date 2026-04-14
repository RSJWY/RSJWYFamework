using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[ExecuteInEditMode]
public class HexagonImage : MonoBehaviour, IMeshModifier
{
    private Image image;
    private RectTransform rectTransform;
    private Mesh cachedMesh;
    private Vector3[] cachedVertices3D;
    private Vector2[] cachedUvs;
    private int[] cachedTriangles;
    private bool meshInitialized = false;
    
    private float[] lastVertexDistances;
    
    [Range(0f, 1f)]
    public float[] vertexDistances = new float[6] { 1f, 1f, 1f, 1f, 1f, 1f };

    void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        image.type = Image.Type.Simple;
        image.useSpriteMesh = true;
        
        lastVertexDistances = new float[6];
        for (int i = 0; i < 6; i++)
        {
            lastVertexDistances[i] = vertexDistances[i];
        }
        
        meshInitialized = true;
        image.SetAllDirty();
    }


    void OnEnable()
    {
        image.SetAllDirty();
    }
    
    void OnValidate()
    {
        if (!image) image = GetComponent<Image>();
        if (!rectTransform) rectTransform = GetComponent<RectTransform>();
        
        if (image && rectTransform && meshInitialized)
        {
            UpdateHexagonMesh();
        }
    }
    
    void Update()
    {
        if (!meshInitialized) return;
        
        bool needUpdate = false;
        
        bool distancesChanged = false;
        for (int i = 0; i < 6; i++)
        {
            if (lastVertexDistances[i] != vertexDistances[i])
            {
                distancesChanged = true;
                lastVertexDistances[i] = vertexDistances[i];
            }
        }
        
        if (distancesChanged)
        {
            needUpdate = true;
        }
        
        if (needUpdate)
        {
            UpdateHexagonMesh();
        }
    }

    void InitializeMesh()
    {
        cachedMesh = new Mesh();
        cachedVertices3D = new Vector3[7];
        cachedUvs = new Vector2[7];
        cachedTriangles = new int[18];
        
        cachedTriangles[0] = 6;
        cachedTriangles[1] = 0;
        cachedTriangles[2] = 1;
        
        cachedTriangles[3] = 6;
        cachedTriangles[4] = 1;
        cachedTriangles[5] = 2;
        
        cachedTriangles[6] = 6;
        cachedTriangles[7] = 2;
        cachedTriangles[8] = 3;
        
        cachedTriangles[9] = 6;
        cachedTriangles[10] = 3;
        cachedTriangles[11] = 4;
        
        cachedTriangles[12] = 6;
        cachedTriangles[13] = 4;
        cachedTriangles[14] = 5;
        
        cachedTriangles[15] = 6;
        cachedTriangles[16] = 5;
        cachedTriangles[17] = 0;
        
        meshInitialized = true;
    }

    public void UpdateHexagonMesh()
    {
        if (!meshInitialized) return;
        
        image.SetAllDirty();
    }
    
    public void ModifyMesh(Mesh mesh)
    {
        if (!meshInitialized) return;
        
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        float radius = Mathf.Min(width, height) * 0.5f;
        
        Vector2 center = Vector2.zero;
        
        Vector3[] vertices = new Vector3[7];
        Vector2[] uvs = new Vector2[7];
        int[] triangles = new int[18];
        
        triangles[0] = 6;
        triangles[1] = 0;
        triangles[2] = 1;
        
        triangles[3] = 6;
        triangles[4] = 1;
        triangles[5] = 2;
        
        triangles[6] = 6;
        triangles[7] = 2;
        triangles[8] = 3;
        
        triangles[9] = 6;
        triangles[10] = 3;
        triangles[11] = 4;
        
        triangles[12] = 6;
        triangles[13] = 4;
        triangles[14] = 5;
        
        triangles[15] = 6;
        triangles[16] = 5;
        triangles[17] = 0;
        
        for (int i = 0; i < 6; i++)
        {
            float angle = 60 * i * Mathf.Deg2Rad;
            float distance = 0f;
            distance = Mathf.Clamp01(vertexDistances[i]) * radius;
            Vector2 vertex = center + new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
            uvs[i] = new Vector2((vertex.x / width) + 0.5f, (vertex.y / height) + 0.5f);
            vertices[i] = new Vector3(vertex.x, vertex.y, 0);
        }
        uvs[6] = new Vector2(0.5f, 0.5f);
        vertices[6] = new Vector3(center.x, center.y, 0);
        
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
    
    public void ModifyMesh(VertexHelper verts)
    {
        if (!meshInitialized) return;
        
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        float radius = Mathf.Min(width, height) * 0.5f;
        
        Vector2 center = Vector2.zero;
        
        verts.Clear();
        
        // 添加中心点
        verts.AddVert(new Vector3(center.x, center.y, 0), image.color, new Vector2(0.5f, 0.5f));
        
        // 添加六边形顶点
        for (int i = 0; i < 6; i++)
        {
            float angle = 60 * i * Mathf.Deg2Rad;
            float distance = 0f;
            distance = Mathf.Clamp01(vertexDistances[i]) * radius;
            Vector2 vertex = center + new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
            Vector2 uv = new Vector2((vertex.x / width) + 0.5f, (vertex.y / height) + 0.5f);
            verts.AddVert(new Vector3(vertex.x, vertex.y, 0), image.color, uv);
        }
        
        // 添加三角形
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            verts.AddTriangle(0, i + 1, next + 1);
        }
    }
}