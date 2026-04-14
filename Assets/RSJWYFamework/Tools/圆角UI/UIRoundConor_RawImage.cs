using UnityEngine;
using UnityEngine.UI;

namespace RSJWYFamework.Runtime
{
   /// <summary>
   /// 圆角UI
   /// https://blog.csdn.net/q1242416158/article/details/134735046
   /// </summary>
    [ExecuteAlways]
    public class UIRoundConor_RawImage : RawImage
    {
        const float defaultCorner = 4;
        [Tooltip("是否锁定拐角,锁定数值时每个拐角值相同;否则使用各个拐角的值")] [SerializeField]
        protected bool m_IsLockCorner = true;
        
        [SerializeField] [Tooltip("拐角数值")] 
        protected Vector4 m_Corner4 = new Vector4(defaultCorner, defaultCorner, defaultCorner, defaultCorner);
        [SerializeField] [Tooltip("拐角数值")]  protected float m_Corner = defaultCorner;
 
        [Tooltip("中心颜色")] [SerializeField] 
        protected Color m_CenterColor = Color.white; // Default to white to multiply correctly
        [Tooltip("边缘线颜色")] [SerializeField][Range(0,255)]
        protected float m_BorderWidth = 0;
        [Tooltip("边缘线颜色")] [SerializeField] 
        protected Color m_BorderColor = Color.black;
        
        private static Shader s_defaultShader;
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        private static readonly int RoundedCornerID = Shader.PropertyToID("_RoundedCorner");
        private static readonly int BorderWidthID = Shader.PropertyToID("_BorderWidth");
        private static readonly int BorderColorID = Shader.PropertyToID("_BorderColor");
        private static readonly int WidthID = Shader.PropertyToID("_Width");
        private static readonly int HeightID = Shader.PropertyToID("_Height");

        private Material m_InstanceMaterial;
        
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
            RefreshMaterial();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshMaterial();
        }
 
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            RefreshMaterial();
        }
#endif
        protected override void OnRectTransformDimensionsChange()
        {
           base.OnRectTransformDimensionsChange();
           UpdateMaterial();
        }

        private void RefreshMaterial()
        {
            if (s_defaultShader == null) s_defaultShader = Shader.Find("Custom/OneSidedSprite");
            
            if (m_InstanceMaterial == null || m_InstanceMaterial.shader != s_defaultShader)
            {
                m_InstanceMaterial = new Material(s_defaultShader);
                m_InstanceMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            
            this.material = m_InstanceMaterial;
            UpdateMaterial();
        }

        protected void UpdateMaterial()
        {
            if (this.material == null) return;
            
            // Ensure we are setting properties on the material instance we own
            if (m_InstanceMaterial == null) return;

            m_InstanceMaterial.SetVector(RoundedCornerID, Corner4);
            m_InstanceMaterial.SetVector(Color1, m_CenterColor);
            m_InstanceMaterial.SetFloat(BorderWidthID, m_BorderWidth);
            m_InstanceMaterial.SetColor(BorderColorID, m_BorderColor);
            m_InstanceMaterial.SetFloat(WidthID, rectTransform.rect.size.x); // Local scale handled in shader usually via UV or vertex pos, but here passing size
            m_InstanceMaterial.SetFloat(HeightID, rectTransform.rect.size.y);
        }
        
        protected void SetMaterial()
        {
            // Compatibility method if needed, but redirects to UpdateMaterial
            RefreshMaterial();
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_InstanceMaterial != null)
            {
                if (Application.isPlaying) Destroy(m_InstanceMaterial);
                else DestroyImmediate(m_InstanceMaterial);
                m_InstanceMaterial = null;
            }
        }
    }
}
