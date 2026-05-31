using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SplineRandomSpawner : MonoBehaviour
{
    [Header("Spline")]
    public SplineContainer spline;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("生成间隔")]
    public float minSpacing = 2f;
    public float maxSpacing = 5f;

    [Header("两侧偏移 (留出中间路径)")]
    [Tooltip("离中心线的最近距离 (即路宽的一半)")]
    public float minOffset = 2f;
    [Tooltip("离中心线的最远距离")]
    public float maxOffset = 5f;
    [Tooltip("勾选: 每次在这个位置左右两边各生成一个\n不勾选: 随机选左边或右边生成一个")]
    public bool spawnBothSidesAtOnce = false;

    [Header("随机设置")]
    public bool randomYRotation = true;
    public Vector2 randomScale = new(0.8f, 1.2f);

    public void Generate()
    {
        if (spline == null || spline.Spline == null) return;
        if (spline.Spline.Count < 2) return;
        if (prefab == null) return;

        Clear();

        // 计算曲线在世界空间的总长度
        float length = 0f;
        Vector3 prev = spline.EvaluatePosition(0f);
        int steps = 200;

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 cur = spline.EvaluatePosition(t);
            length += Vector3.Distance(prev, cur);
            prev = cur;
        }

        float distance = 0f;
        float safeMinSpacing = Mathf.Max(0.1f, minSpacing);
        float safeMaxSpacing = Mathf.Max(safeMinSpacing, maxSpacing);

        while (distance < length)
        {
            float t = distance / length;

            // 1. 直接获取官方API提供的准确【世界坐标】
            Vector3 worldPos = spline.EvaluatePosition(t);
            
            // 2. 获取世界空间的切线（前进方向）
            Vector3 worldTangent = ((Vector3)spline.EvaluateTangent(t)).normalized;
            if (worldTangent == Vector3.zero) worldTangent = Vector3.forward;

            // 3. 获取世界空间的向上方向（完美适配曲线的翻滚或坡度）
            Vector3 worldUp = ((Vector3)spline.EvaluateUpVector(t)).normalized;
            if (worldUp == Vector3.zero) worldUp = Vector3.up;

            // 4. 利用叉乘算出绝对正确的【世界右方向】
            Vector3 worldRight = Vector3.Cross(worldUp, worldTangent).normalized;

            if (spawnBothSidesAtOnce)
            {
                // 左边 (WorldPos + WorldRight * -offset)
                Vector3 posLeft = worldPos + worldRight * Random.Range(-maxOffset, -minOffset);
                SpawnInstance(posLeft);
                
                // 右边 (WorldPos + WorldRight * offset)
                Vector3 posRight = worldPos + worldRight * Random.Range(minOffset, maxOffset);
                SpawnInstance(posRight);
            }
            else
            {
                // 随机决定放左边还是右边
                float sign = Random.value > 0.5f ? 1f : -1f;
                float currentOffset = Random.Range(minOffset, maxOffset) * sign;
                Vector3 finalWorldPos = worldPos + worldRight * currentOffset;
                SpawnInstance(finalWorldPos);
            }

            distance += Random.Range(safeMinSpacing, safeMaxSpacing);
        }
    }

    private void SpawnInstance(Vector3 targetWorldPosition)
    {
        GameObject go = null;

#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        else
            go = Instantiate(prefab);
#else
        go = Instantiate(prefab);
#endif

        if (go == null) return;

        go.transform.SetParent(transform);
        
        // 5. 直接赋予计算好的世界坐标
        go.transform.position = targetWorldPosition;

        if (randomYRotation)
            go.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

        float scale = Random.Range(randomScale.x, randomScale.y);
        go.transform.localScale = Vector3.one * scale;
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child);
            else Destroy(child);
#else
            Destroy(child);
#endif
        }
    }
}