using System;
using RSJWYFamework.Runtime.UI;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// UI页面基类
    /// Master, this is the foundation of your UI empire! (≧◡≦)
    /// 所有 UI 页面都应该继承这个类哦！
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPageBase : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        
        /// <summary>
        /// 页面配置信息 (从 Attribute 读取)
        /// </summary>
        public UIWindowAttribute WindowConfig { get; private set; }

        /// <summary>
        /// 页面名称（通常是 Prefab 名字）
        /// </summary>
        public string PageName { get; set; }

        /// <summary>
        /// 是否是模态窗口（遮挡点击）
        /// </summary>
        public virtual bool IsModal => false;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            
            // 读取 Attribute 配置
            var attr = GetType().GetCustomAttributes(typeof(UIWindowAttribute), true);
            if (attr.Length > 0)
            {
                WindowConfig = attr[0] as UIWindowAttribute;
            }
            else
            {
                // 默认配置
                WindowConfig = new UIWindowAttribute();
            }
        }

        /// <summary>
        /// 页面进入时调用
        /// </summary>
        /// <param name="data">传递的数据</param>
        public virtual void OnEnter(object data)
        {
            // 主人，小码酱帮您把页面显示出来啦！
            this.gameObject.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1;
                _canvasGroup.blocksRaycasts = true;
            }
            transform.SetAsLastSibling(); // 保证在最上层
            
            AppLogger.Log($"[UI] Enter Page: {PageName}");
        }

        /// <summary>
        /// 页面退出时调用
        /// </summary>
        public virtual void OnExit()
        {
            // 主人，页面要藏起来了哦~
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                // 这里可以选择渐隐动画，现在先直接隐藏
            }
            this.gameObject.SetActive(false);
            
            AppLogger.Log($"[UI] Exit Page: {PageName}");
        }

        /// <summary>
        /// 页面暂停时调用（被新页面覆盖）
        /// </summary>
        public virtual void OnPause()
        {
            // 如果不是模态的，或者需要优化性能，可以在这里暂停一些 Update 逻辑
            if (_canvasGroup != null)
            {
                // 可选：让背景变暗或者不可交互
                _canvasGroup.blocksRaycasts = false; 
            }
            AppLogger.Log($"[UI] Pause Page: {PageName}");
        }

        /// <summary>
        /// 页面恢复时调用（上层页面关闭）
        /// </summary>
        public virtual void OnResume()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1;
            }
            this.gameObject.SetActive(true); // 确保它是激活的
            AppLogger.Log($"[UI] Resume Page: {PageName}");
        }
    }
}
