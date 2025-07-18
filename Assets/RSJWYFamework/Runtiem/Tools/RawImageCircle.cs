using UnityEngine;
using UnityEngine.UI;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// RawImage可用的球形图像处理
    /// </summary>
    public class RawImageCircle : RawImage
    {
        /// <summary>
        /// 三角形面数
        /// </summary>
        private int segments;

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);
            toFill.Clear();

            segments = 100;

            // 获取Rect的宽高
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            // 计算圆形的UV坐标
            float uvWidth = 1f; // RawImage的UV范围通常是0到1
            float uvHeight = 1f;
            Vector2 uvCenter = new Vector2(0.5f, 0.5f); // UV中心
            Vector2 converRatio = new Vector2(uvWidth / width, uvHeight / height);

            // 绘制圆形
            float radian = (2 * Mathf.PI) / segments;
            float radius = Mathf.Min(width, height) * 0.5f; // 确保圆形在矩形内

            // 1. 计算圆心坐标
            UIVertex origin = new UIVertex();
            origin.color = color;
            origin.position = Vector2.zero;
            origin.uv0 = uvCenter; // 圆心的UV坐标
            toFill.AddVert(origin);

            // 2. 依次计算其他点
            int vertexCount = segments + 1; // 顶点数比面数多一
            float curRadian = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                float x = Mathf.Cos(curRadian) * radius;
                float y = Mathf.Sin(curRadian) * radius;
                curRadian += radian;

                UIVertex tempV = new UIVertex();
                tempV.color = color;
                tempV.position = new Vector2(x, y);
                tempV.uv0 = uvCenter + new Vector2(x * converRatio.x, y * converRatio.y); // 计算UV
                toFill.AddVert(tempV);
            }

            // 3. 生成三角形面
            int id = 1;
            for (int i = 0; i < segments; i++)
            {
                toFill.AddTriangle(id, 0, id + 1);
                id++;
            }
        }
    }
}