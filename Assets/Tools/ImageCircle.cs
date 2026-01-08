using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// Image可用的球形图像处理
    /// </summary>
    public class ImageCircle :Image
    {
        /// <summary>
        /// 三角形面数
        /// </summary>
        private int segements;
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);
            toFill.Clear();
            segements = 100;
            //先获得rect的宽高
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            //再获得uv
            //overrideSprite 用于修改图片，但是不会把原来的图片给消除掉
            //uv的四个坐标相对于uv的四个顶点[x,y,z,w]
            //然后求出uv宽高映射到实际宽高
            Vector4 uv = overrideSprite != null ? DataUtility.GetOuterUV(overrideSprite) : Vector4.zero;
            float uvWidth = uv.z - uv.x;
            float uvHeight = uv.w - uv.y;
            Vector2 uvCenter = new Vector2(uvWidth * 0.5f, uvHeight * 0.5f);
            Vector2 converRatio = new Vector2(uvWidth / width, uvHeight / height);

            //绘制圆形，所以需要知道弧度制。
            //segements代表有几个三角形面
            //一周的弧度为2Π，所以每一个三角形面的弧度为2Π/segements
            //圆形的半径的定义比较模糊，建议随便定义。
            float radian = (2 * Mathf.PI) / segements;
            float radius = width * 0.5f;


            //然后需要算出各个顶点坐标。
            //1，先算出圆心坐标
            UIVertex origin = new UIVertex();
            origin.color = color;
            origin.position = Vector2.zero;
            origin.uv0 = uvCenter + origin.position * converRatio;
            toFill.AddVert(origin);

            //2.依次算出其他点，每次弧度值会+=radian
            int vertexCount = segements + 1;///顶点数比面数多一
            float curRadian = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                float x = Mathf.Cos(curRadian) * radius;
                float y = Mathf.Sin(curRadian) * radius;
                curRadian += radian;

                UIVertex tempV = new UIVertex();
                tempV.color = color;
                tempV.position = new Vector2(x, y);
                tempV.uv0 = uvCenter + tempV.position * converRatio;
                toFill.AddVert(tempV);
            }

            //最后把由顶点生成各个三角形面
            //三角形面由三个点组成，取决于你给的索引。索引取决于addVert的顺序
            //如果是三个点是顺时针排列就是正面，否则是反面。
            //反面不会绘制。
            //所以圆心的索引是0，然后其他顶点围着圆心，依次+1
            //故(i,0,i+1)组成一个三角形面
            int id = 1;
            for (int i = 0; i < segements; i++)
            {
                toFill.AddTriangle(id, 0, id + 1);
                id++;
            }
        }

    }
}
