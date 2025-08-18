using System;
using System.Text;

public static class RandomDataGenerator
{
    private static readonly Random _random = new Random();
    // 可用于生成文本的字符集（字母、数字和常见符号）
    private const string TextChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_-+=[]{}|;:,.<>?";

    /// <summary>
    /// 生成随机长度的字节数组
    /// </summary>
    /// <param name="minLength">最小长度</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>随机长度的字节数组</returns>
    public static byte[] GenerateRandomByteArray(int minLength, int maxLength)
    {
        if (minLength < 0)
            throw new ArgumentException("最小长度不能为负数", nameof(minLength));
        if (maxLength < minLength)
            throw new ArgumentException("最大长度不能小于最小长度", nameof(maxLength));

        // 随机生成长度
        int length = _random.Next(minLength, maxLength + 1);
        byte[] buffer = new byte[length];
        
        // 填充随机字节
        _random.NextBytes(buffer);
        
        return buffer;
    }

    /// <summary>
    /// 生成随机长度的文本数据字节数组（UTF8编码）
    /// </summary>
    /// <param name="minLength">最小字符数</param>
    /// <param name="maxLength">最大字符数</param>
    /// <returns>文本数据的字节数组</returns>
    public static byte[] GenerateRandomTextByteArray(int minLength, int maxLength)
    {
        if (minLength < 0)
            throw new ArgumentException("最小长度不能为负数", nameof(minLength));
        if (maxLength < minLength)
            throw new ArgumentException("最大长度不能小于最小长度", nameof(maxLength));

        // 随机生成字符数量
        int charCount = _random.Next(minLength, maxLength + 1);
        StringBuilder sb = new StringBuilder(charCount);

        // 生成随机文本
        for (int i = 0; i < charCount; i++)
        {
            int index = _random.Next(TextChars.Length);
            sb.Append(TextChars[index]);
        }

        // 转换为UTF8字节数组
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}