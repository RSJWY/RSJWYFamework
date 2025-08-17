using UnityEngine;
using UnityEngine.UI;

namespace RSJWYFamework.Runtime
{
   /// <summary>
   /// 圆角UI
   /// https://blog.csdn.net/q1242416158/article/details/134735046
   /// </summary>
    public class UIRoundConor_RawImage : RawImage
    {
        const float defaultCorner = 4;
        [Tooltip("是否锁定拐角,锁定数值时每个拐角值相同;否则使用各个拐角的值")] [SerializeField]
        protected bool m_IsLockCorner = true;
        
        [SerializeField] [Tooltip("拐角数值")] 
        protected Vector4 m_Corner4 = new Vector4(defaultCorner, defaultCorner, defaultCorner, defaultCorner);
        [SerializeField] [Tooltip("拐角数值")]  protected float m_Corner = defaultCorner;
 
        [Tooltip("中心颜色")] [SerializeField] 
        protected Color m_CenterColor = Color.clear;
        [Tooltip("边缘线颜色")] [SerializeField][Range(0,255)]
        protected float m_BorderWidth = 1;
        [Tooltip("边缘线颜色")] [SerializeField] 
        protected Color m_BorderColor = Color.black;
        
        private static Shader s_defaultShader;
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        private static readonly int RoundedCornerID = Shader.PropertyToID("_RoundedCorner");
        private static readonly int BorderWidthID = Shader.PropertyToID("_BorderWidth");
        private static readonly int BorderColorID = Shader.PropertyToID("_BorderColor");
        private static readonly int WidthID = Shader.PropertyToID("_Width");
        private static readonly int HeightID = Shader.PropertyToID("_Height");
        
        /// <summary>
        /// 角点
        /// x=topLeft
        /// y=topRight
        /// z=bottomRight
        /// w=bottomLeft
        /// </summary>
        public Vector4 Corner4
        {
            get
            { 
                var corner = m_IsLockCorner
                    ? new Vector4(m_Corner, m_Corner, m_Corner, m_Corner)
                    : m_Corner4;
                return corner;
            }
            set
            {
                m_Corner4 = value;
                m_Corner = m_Corner4.x;
                UpdateMaterial();
            }
        }
 
        public float Corner
        {
            set
            {
                m_Corner = value;
                m_Corner4 = new Vector4(value, value, value, value);
                UpdateMaterial();
            }
        }
        /// <summary>
        /// 描边宽度
        /// </summary>
        public float BorderWidth
        {
            get => m_BorderWidth;
            set
            {
                m_BorderWidth = value;
                UpdateMaterial();
            }
        }
 
        /// <summary>
        /// 描边颜色
        /// </summary>
        public Color BorderColor
        {
            get => m_BorderColor;
            set
            {
                m_BorderColor = value;
                UpdateMaterial();
            }
        }
        
 
        protected override void Awake()
        {
            base.Awake();
            if (s_defaultShader == null) s_defaultShader = Shader.Find("Custom/UI/RoundConor");
            material = new Material(s_defaultShader);
            SetMaterial();
        }
 
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetMaterial();
        }
#endif
        protected override void OnRectTransformDimensionsChange()
        {
           base.OnRectTransformDimensionsChange();
           SetMaterial();
        }
        protected void SetMaterial()
        {
            var material1 = new Material(material);
            material1.SetVector(RoundedCornerID, Corner4);
            material1.SetVector(Color1, m_CenterColor);
            material1.SetFloat(BorderWidthID, m_BorderWidth);
            material1.SetColor(BorderColorID, m_BorderColor);
            material1.SetFloat(WidthID, rectTransform.rect.size.x*transform.localScale.x);
            material1.SetFloat(HeightID, rectTransform.rect.size.y*transform.localScale.y);
            material = material1;
        }
 
        /// <summary>
        /// 设置点击区域,圆角区域不可被点击
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="eventCamera"></param>
        /// <returns></returns>
        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out localPoint))
            {
                if (!float.IsNaN(localPoint.x)&&!float.IsNaN(localPoint.y))
                {
                    var realCorner = Corner4;
                    var localRect = rectTransform.rect;
 
                    var topLeft = new Vector2(localRect.xMin + realCorner.x, localRect.yMin + realCorner.x);
                    if (localPoint.x < topLeft.x && localPoint.y < topLeft.y)
                    {
                        if (Vector2.Distance(topLeft, localPoint) > realCorner.x)
                        {
                            return false;
                        }
                    }
 
                    var topRight = new Vector2(localRect.xMax - realCorner.y, localRect.yMin + realCorner.y);
                    if (localPoint.x > topRight.x && localPoint.y < topRight.y)
                    {
                        if (Vector2.Distance(topRight, localPoint) > realCorner.y)
                        {
                            return false;
                        }
                    }
 
                    var bottomRight = new Vector2(localRect.xMax - realCorner.z, localRect.yMax - realCorner.z);
                    if (localPoint.x > bottomRight.x && localPoint.y > bottomRight.y)
                    {
                        if (Vector2.Distance(bottomRight, localPoint) > realCorner.z)
                        {
                            return false;
                        }
                    }
 
                    var bottomLeft = new Vector2(localRect.xMin + realCorner.w, localRect.yMax - realCorner.w);
                    if (localPoint.x < bottomLeft.x && localPoint.y > bottomLeft.y)
                    {
                        if (Vector2.Distance(bottomLeft, localPoint) > realCorner.w)
                        {
                            return false;
                        }
                    }
                }
            }
 
            return base.Raycast(sp, eventCamera);
        }
    }
}