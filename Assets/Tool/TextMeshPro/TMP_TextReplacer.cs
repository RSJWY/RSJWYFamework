using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// TextMeshPro 文本替换工具
    /// <para>功能：在编辑器和运行时替换 TextMeshPro 文本中的特定字符，并应用不同的字体、大小和偏移。</para>
    /// <para>适用于：中英文混排、特殊符号使用不同字体、强调特定文字等场景。</para>
    /// <para>在项目设置中的TextMeshPro设置里，指定一字体文件目录</para>
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public class TMP_TextReplacer : MonoBehaviour
    {
        [Tooltip("源文本内容。请在此处编辑文本，而不是在 TMP 组件中编辑。\n(The source text content. Edit this instead of the TMP component text.)")]
        [TextArea(5, 20)]
        public string originalText;

        [System.Serializable]
        public class ReplacementRule
        {
            [Tooltip("要搜索的目标字符或字符串。\n(The character or string to search for.)")]
            public string target;
            
            [Tooltip("用于替换的字符串。如果为空，则使用目标字符串（仅改变样式）。\n(The string to replace it with. If empty, the target string is used.)")]
            public string replacement;
            
            [Tooltip("使用的字体资源。\n注意：此字体必须放置在 'Resources' 文件夹中，或添加到 TMP 设置的 'Default Font Asset' 列表中，才能通过标签生效。\n(The Font Asset to use.)")]
            public TMP_FontAsset fontAsset;
            
            [Tooltip("字体大小百分比 (例如 100 为正常大小, 150 为 1.5 倍)。\n(Size percentage, e.g., 100 for normal, 150 for 1.5x.)")]
            public float sizePercent = 100f;
            
            [Tooltip("垂直偏移量 (em 单位)。用于对齐不同基线的字体 (例如 0.1 或 -0.1)。\n(Vertical offset in em units. Useful for aligning different fonts.)")]
            public float yOffset = 0f;
        }

        [Tooltip("替换规则列表")]
        public List<ReplacementRule> rules = new List<ReplacementRule>();

        [Tooltip("是否在值更改时自动更新文本。\n(Automatically update text when values change.)")]
        public bool autoUpdate = true;

        private TMP_Text _tmpText;

        private void OnEnable()
        {
            _tmpText = GetComponent<TMP_Text>();
            UpdateText();
        }

        private void OnValidate()
        {
            if (autoUpdate)
            {
                UpdateText();
            }
        }

        /// <summary>
        /// 更新文本显示
        /// <para>根据 originalText 和 rules 生成最终的富文本字符串，并赋值给 TMP 组件。</para>
        /// </summary>
        public void UpdateText()
        {
            if (_tmpText == null) _tmpText = GetComponent<TMP_Text>();
            if (_tmpText == null) return;

            // 如果源文本为空，清空 TMP 文本并返回
            if (string.IsNullOrEmpty(originalText))
            {
                // 我们不清除 TMP 文本，如果我们还没有初始化我们的文本（避免在第一次添加时擦除现有文本）
                // 但由于这是一个实用程序，我们假设 'originalText' 是主数据。
                // 为了安全起见：如果 originalText 为空但 tmp 有文本，也许我们应该先复制它？
                // 目前，让我们尊重 originalText。
                _tmpText.text = "";
                return;
            }

            string processedText = originalText;

            if (rules != null)
            {
                foreach (var rule in rules)
                {
                    if (string.IsNullOrEmpty(rule.target)) continue;

                    // 确定替换内容：如果有指定替换字符则使用，否则使用原字符（仅改样式）
                    string replaceContent = string.IsNullOrEmpty(rule.replacement) ? rule.target : rule.replacement;
                    
                    // 构建富文本标签
                    string prefix = "";
                    string suffix = "";

                    // 字体标签 <font="FontName">
                    if (rule.fontAsset != null)
                    {
                        // TMP 使用字体资源名称作为 <font> 标签参数。
                        // 确保字体资源名称不包含特殊字符，或者 TMP 版本支持引号。
                        prefix += $"<font=\"{rule.fontAsset.name}\">";
                        suffix = "</font>" + suffix; // 标签闭合顺序需相反：[A [B text] B] A
                    }

                    // 大小标签 <size=120%>
                    if (Mathf.Abs(rule.sizePercent - 100f) > 0.01f)
                    {
                        prefix += $"<size={rule.sizePercent}%>";
                        suffix = "</size>" + suffix;
                    }

                    // 垂直偏移标签 <voffset=0.1em>
                    if (Mathf.Abs(rule.yOffset) > 0.001f)
                    {
                        prefix += $"<voffset={rule.yOffset}em>";
                        suffix = "</voffset>" + suffix;
                    }

                    string finalReplacement = prefix + replaceContent + suffix;

                    // 执行替换
                    processedText = processedText.Replace(rule.target, finalReplacement);
                }
            }

            // 仅当文本实际发生变化时才应用，避免不必要的脏标记
            if (_tmpText.text != processedText)
            {
                _tmpText.text = processedText;
                
                // 如果在编辑器模式下且未运行，标记对象为已修改，以便保存更改
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(_tmpText);
                }
#endif
            }
        }
        
        /// <summary>
        /// 从 TMP 组件导入当前文本到 Original Text 字段
        /// <para>右键菜单功能。</para>
        /// </summary>
        [ContextMenu("Import Text from TMP (从 TMP 导入文本)")]
        public void ImportFromTMP()
        {
            if (_tmpText == null) _tmpText = GetComponent<TMP_Text>();
            if (_tmpText != null)
            {
                originalText = _tmpText.text;
                // 注意：这里直接导入了 TMP 的当前文本。
                // 如果该文本已经包含了我们生成的标签，再次应用规则可能会导致标签嵌套。
                // 这个功能主要用于初始设置。
            }
        }
    }
}
