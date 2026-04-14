using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// HTML 到 TextMeshPro 富文本转换工具类
/// 提供将标准 HTML 标签转换为 Unity TMP 支持的富文本标签的功能。
/// 支持嵌套标签、颜色、字体大小、样式（加粗、斜体、下划线等）的转换。
/// </summary>
public static class HtmlToTmpConverter
{
    /// <summary>
    /// 标签信息类，用于在栈中跟踪标签的嵌套状态和闭合标签
    /// </summary>
    private class TagInfo
    {
        /// <summary>
        /// 标签名称（小写）
        /// </summary>
        public string TagName;
        
        /// <summary>
        /// 对应的闭合标签列表（如 </b>, </color> 等）
        /// 因为一个 HTML 标签可能对应多个 TMP 标签（如 style 属性包含 color 和 size）
        /// </summary>
        public List<string> ClosingTags;

        public TagInfo(string name)
        {
            TagName = name;
            ClosingTags = new List<string>();
        }
    }

    /// <summary>
    /// 将 HTML 字符串转换为 TMP 富文本格式
    /// </summary>
    /// <param name="html">输入的 HTML 字符串</param>
    /// <returns>转换后的 TMP 富文本字符串</returns>
    public static string Convert(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        // 移除 HTML 注释，避免干扰解析
        html = Regex.Replace(html, "<!--[\\s\\S]*?-->", "");

        StringBuilder sb = new StringBuilder();
        Stack<TagInfo> tagStack = new Stack<TagInfo>();
        int len = html.Length;
        int i = 0;

        // 遍历字符串进行逐字符解析
        while (i < len)
        {
            // 查找下一个标签起始位置 '<'
            int lt = html.IndexOf('<', i);
            if (lt == -1)
            {
                // 如果没有找到标签，将剩余文本解码并追加
                string text = html.Substring(i);
                sb.Append(System.Net.WebUtility.HtmlDecode(text));
                break;
            }

            // 处理标签前的纯文本内容
            if (lt > i)
            {
                string text = html.Substring(i, lt - i);
                sb.Append(System.Net.WebUtility.HtmlDecode(text));
            }

            // 查找标签结束位置 '>'
            int gt = html.IndexOf('>', lt);
            if (gt == -1)
            {
                // 如果找不到 '>'，说明不是有效的标签，作为普通文本处理
                sb.Append(System.Net.WebUtility.HtmlDecode(html.Substring(lt)));
                break;
            }

            // 提取标签内容（不含 <>）
            string tagContent = html.Substring(lt + 1, gt - lt - 1);
            // 处理标签逻辑
            ProcessTag(tagContent, sb, tagStack);

            // 移动索引到标签之后
            i = gt + 1;
        }

        // 闭合所有剩余未闭合的标签，确保格式正确
        while (tagStack.Count > 0)
        {
            var info = tagStack.Pop();
            for (int j = info.ClosingTags.Count - 1; j >= 0; j--)
            {
                sb.Append(info.ClosingTags[j]);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 处理单个标签内容
    /// 解析标签名、属性，并生成对应的 TMP 标签
    /// </summary>
    /// <param name="content">标签内部内容（去除了尖括号）</param>
    /// <param name="sb">结果构建器</param>
    /// <param name="stack">标签栈</param>
    private static void ProcessTag(string content, StringBuilder sb, Stack<TagInfo> stack)
    {
        content = content.Trim();
        if (string.IsNullOrEmpty(content)) return;

        // 判断是否为闭合标签（如 </div>）
        bool isClosing = content.StartsWith("/");
        
        string tagName;
        string attributes = "";
        
        if (isClosing)
        {
            // 解析闭合标签名称
            tagName = content.Substring(1).Trim();
            int space = tagName.IndexOf(' ');
            if (space != -1) tagName = tagName.Substring(0, space);
            tagName = tagName.ToLower();

            // 在栈中查找匹配的标签
            Stack<TagInfo> tempStack = new Stack<TagInfo>();
            bool found = false;
            
            foreach (var tag in stack)
            {
                if (tag.TagName == tagName)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                // 逐个出栈直到找到匹配的标签
                while (stack.Count > 0)
                {
                    var info = stack.Pop();
                    
                    // 输出对应的 TMP 闭合标签
                    for (int j = info.ClosingTags.Count - 1; j >= 0; j--)
                    {
                        sb.Append(info.ClosingTags[j]);
                    }

                    // 处理块级元素闭合后的换行（如 </p>, </div>）
                    if (info.TagName == "p" || info.TagName == "div")
                    {
                        sb.Append("\n");
                    }

                    // 找到目标标签后停止
                    if (info.TagName == tagName) break;
                }
            }
            return;
        }

        // 处理起始标签
        // 解析标签名和属性部分
        // 兼容处理：有些标签属性用空格分隔（HTML标准），有些用等号分隔（TMP特有，如 <color=#fff>）
        int spaceIndex = content.IndexOfAny(new char[] { ' ', '\t', '\n', '\r' });
        int equalsIndex = content.IndexOf('=');
        
        int separatorIndex = -1;
        // 确定分隔符位置：取空格和等号中较前的一个
        if (spaceIndex != -1 && equalsIndex != -1) separatorIndex = Math.Min(spaceIndex, equalsIndex);
        else if (spaceIndex != -1) separatorIndex = spaceIndex;
        else separatorIndex = equalsIndex;

        if (separatorIndex == -1)
        {
            // 没有属性
            tagName = content.ToLower();
            attributes = "";
        }
        else
        {
            // 分离标签名和属性字符串
            tagName = content.Substring(0, separatorIndex).ToLower();
            attributes = content.Substring(separatorIndex + 1);
        }

        // 处理自闭合标签（如 <br/>）
        bool isSelfClosing = content.EndsWith("/");
        if (isSelfClosing)
        {
            if (attributes.EndsWith("/")) attributes = attributes.Substring(0, attributes.Length - 1);
            else tagName = tagName.TrimEnd('/');
        }
        
        if (tagName.EndsWith("/")) tagName = tagName.TrimEnd('/');

        TagInfo newTag = new TagInfo(tagName);

        // 根据标签名进行转换
        switch (tagName)
        {
            case "b":
            case "strong":
                sb.Append("<b>");
                newTag.ClosingTags.Add("</b>");
                ProcessStyleAttribute(attributes, sb, newTag.ClosingTags);
                break;
            case "i":
            case "em":
                sb.Append("<i>");
                newTag.ClosingTags.Add("</i>");
                ProcessStyleAttribute(attributes, sb, newTag.ClosingTags);
                break;
            case "u":
                sb.Append("<u>");
                newTag.ClosingTags.Add("</u>");
                ProcessStyleAttribute(attributes, sb, newTag.ClosingTags);
                break;
            case "s":
            case "strike":
            case "del":
                sb.Append("<s>");
                newTag.ClosingTags.Add("</s>");
                ProcessStyleAttribute(attributes, sb, newTag.ClosingTags);
                break;
            case "br":
                sb.Append("\n");
                break;
            case "p":
            case "div":
                // 确保段落前有换行（除非是文本开头）
                if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.Append("\n");
                break;
            case "span":
                // span 标签主要处理 style 属性
                ProcessStyleAttribute(attributes, sb, newTag.ClosingTags);
                break;
            case "color":
                // 处理 TMP 的 <color> 标签
                if (!string.IsNullOrEmpty(attributes))
                {
                    string val = attributes.Trim();
                    // 移除可能存在的等号前缀
                    if (val.StartsWith("=")) val = val.Substring(1).Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        sb.Append($"<color={val}>");
                        newTag.ClosingTags.Add("</color>");
                    }
                }
                break;
            case "size":
                // 处理 TMP 的 <size> 标签
                if (!string.IsNullOrEmpty(attributes))
                {
                    string val = attributes.Trim();
                    if (val.StartsWith("=")) val = val.Substring(1).Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        sb.Append($"<size={val}>");
                        newTag.ClosingTags.Add("</size>");
                    }
                }
                break;
            case "font":
                // 处理旧式 font 标签
                ProcessFontAttribute(attributes, sb, newTag.ClosingTags);
                break;
            case "a":
                // 处理链接标签
                ProcessLinkAttribute(attributes, sb, newTag.ClosingTags);
                break;
        }

        // 如果不是自闭合且不是空元素标签，压入栈中等待闭合
        if (!isSelfClosing && !IsVoidTag(tagName))
        {
            stack.Push(newTag);
        }
        else
        {
            // 如果是自闭合或空元素，立即输出闭合标签（如果有）
            for (int j = newTag.ClosingTags.Count - 1; j >= 0; j--)
            {
                sb.Append(newTag.ClosingTags[j]);
            }
        }
    }

    /// <summary>
    /// 判断是否为空元素标签（不需要闭合的标签）
    /// </summary>
    private static bool IsVoidTag(string tagName)
    {
        return tagName == "br" || tagName == "hr" || tagName == "img" || tagName == "input" || tagName == "meta";
    }

    /// <summary>
    /// 解析 style 属性（如 style="color: red; font-size: 14px"）
    /// </summary>
    private static void ProcessStyleAttribute(string attributes, StringBuilder sb, List<string> closingTags)
    {
        var styleMatch = Regex.Match(attributes, "style\\s*=\\s*([\"'])(.*?)\\1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (styleMatch.Success)
        {
            string styleContent = styleMatch.Groups[2].Value;
            var styles = styleContent.Split(';');
            
            string color = null;
            string size = null;
            string bgColor = null;
            
            foreach (var style in styles)
            {
                var parts = style.Split(':');
                if (parts.Length < 2) continue;
                
                string key = parts[0].Trim().ToLower();
                string value = parts[1].Trim();
                
                if (key == "color") color = value;
                else if (key == "font-size") size = value;
                else if (key == "background-color") bgColor = value;
            }
            
            // 按特定顺序应用标签：Size -> Color -> Mark (由外向内)
            // 这种嵌套结构 <size><color>Text</color></size> 对 TMP 渲染更安全
            if (!string.IsNullOrEmpty(size))
            {
                string sizeVal = ParseSize(size);
                if (sizeVal != null)
                {
                    sb.Append($"<size={sizeVal}>");
                    closingTags.Add("</size>");
                }
            }
            
            if (!string.IsNullOrEmpty(color))
            {
                string hex = ParseColor(color);
                if (hex != null)
                {
                    sb.Append($"<color={hex}>");
                    closingTags.Add("</color>");
                }
            }

            if (!string.IsNullOrEmpty(bgColor))
            {
                // Unity TMP 对 <mark> 标签的支持有限，暂时忽略背景色
                // string hex = ParseColor(bgColor);
                // if (hex != null)
                // {
                //    sb.Append($"<mark={hex}>");
                //    closingTags.Add("</mark>");
                // }
            }
        }
    }

    /// <summary>
    /// 解析 font 标签属性（主要是 color）
    /// </summary>
    private static void ProcessFontAttribute(string attributes, StringBuilder sb, List<string> closingTags)
    {
        var colorMatch = Regex.Match(attributes, "color\\s*=\\s*([\"'])(.*?)\\1", RegexOptions.IgnoreCase);
        if (colorMatch.Success)
        {
            string hex = ParseColor(colorMatch.Groups[2].Value);
            if (hex != null)
            {
                sb.Append($"<color={hex}>");
                closingTags.Add("</color>");
            }
        }
    }
    
    /// <summary>
    /// 解析链接标签属性
    /// </summary>
    private static void ProcessLinkAttribute(string attributes, StringBuilder sb, List<string> closingTags)
    {
         var hrefMatch = Regex.Match(attributes, "href\\s*=\\s*([\"'])(.*?)\\1", RegexOptions.IgnoreCase);
         if (hrefMatch.Success)
         {
             sb.Append($"<link=\"{hrefMatch.Groups[2].Value}\">");
             closingTags.Add("</link>");
             // 为链接添加默认的蓝色下划线样式
             sb.Append("<u><color=#0000EE>");
             closingTags.Add("</color></u>");
         }
    }

    /// <summary>
    /// 解析颜色值，支持 hex 和 rgb() 格式
    /// </summary>
    private static string ParseColor(string colorStr)
    {
        if (string.IsNullOrEmpty(colorStr)) return null;
        colorStr = colorStr.Trim();
        
        if (colorStr.StartsWith("#")) return colorStr;
        
        if (colorStr.StartsWith("rgb"))
        {
            var match = Regex.Match(colorStr, @"rgba?\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)(?:,\s*[\d\.]+)?\s*\)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int r = int.Parse(match.Groups[1].Value);
                int g = int.Parse(match.Groups[2].Value);
                int b = int.Parse(match.Groups[3].Value);
                return $"#{r:X2}{g:X2}{b:X2}";
            }
        }
        
        return colorStr;
    }
    
    /// <summary>
    /// 解析字体大小，移除 'px' 后缀
    /// </summary>
    private static string ParseSize(string sizeStr)
    {
        if (string.IsNullOrEmpty(sizeStr)) return null;
        sizeStr = sizeStr.Trim().ToLower();
        if (sizeStr.EndsWith("px")) return sizeStr.Substring(0, sizeStr.Length - 2);
        return sizeStr;
    }
}