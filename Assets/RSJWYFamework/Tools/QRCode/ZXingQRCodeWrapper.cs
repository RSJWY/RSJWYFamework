
using System.Collections.Generic;
using UnityEngine;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;

/// <summary>
/// ZXing 二维码封装
/// 1、二维码的生成
/// 2、二维码的识别
/// </summary>
public class ZXingQRCodeWrapper
{
    #region GenerateQRCode

    /*
     使用方法，类似如下：
     ZXingQRCodeWrapper.GenerateQRCode("Hello Wrold!", 512, 512);
     ZXingQRCodeWrapper.GenerateQRCode("I Love You!", 256, 256, Color.red);
     ZXingQRCodeWrapper.GenerateQRCode("中间带图片的二维码图片", Color.green, icon);
     .......
         */


    /// <summary>
    /// 生成2维码 方法一
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <returns></returns>
    public static Texture2D GenerateQRCode(string content)
    {
        return GenerateQRCode(content, 256, 256);
    }

    /// <summary>
    /// 生成2维码 方法一
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public static Texture2D GenerateQRCode(string content, int width, int height)
    {
        // 编码成color32
        EncodingOptions options = null;
        BarcodeWriter writer = new BarcodeWriter();
        options = new EncodingOptions
        {
            Width = width,
            Height = height,
            Margin = 1,
        };
        options.Hints.Add(EncodeHintType.CHARACTER_SET, "UTF-8");
        writer.Format = BarcodeFormat.QR_CODE;
        writer.Options = options;
        Color32[] colors = writer.Write(content);

        // 转成texture2d
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(colors);
        texture.Apply();



        return texture;
    }

    /// <summary>
    /// 尝试生成二维码，使用默认颜色与边距。
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="texture">输出纹理</param>
    /// <returns></returns>
    public static bool TryGenerateQRCode(string content, int width, int height, out Texture2D texture)
    {
        return TryGenerateQRCode(content, width, height, Color.black, Color.white, 1, ErrorCorrectionLevel.M, out texture);
    }

    /// <summary>
    /// 尝试生成二维码，指定前景色。
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="color">前景色</param>
    /// <param name="texture">输出纹理</param>
    /// <returns></returns>
    public static bool TryGenerateQRCode(string content, int width, int height, Color color, out Texture2D texture)
    {
        return TryGenerateQRCode(content, width, height, color, Color.white, 1, ErrorCorrectionLevel.M, out texture);
    }

    /// <summary>
    /// 尝试生成二维码，支持前景/背景色、边距和纠错等级。
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="foregroundColor">前景色</param>
    /// <param name="backgroundColor">背景色</param>
    /// <param name="margin">边距</param>
    /// <param name="errorCorrection">纠错等级</param>
    /// <param name="texture">输出纹理</param>
    /// <returns></returns>
    public static bool TryGenerateQRCode(
        string content,
        int width,
        int height,
        Color foregroundColor,
        Color backgroundColor,
        int margin,
        ErrorCorrectionLevel errorCorrection,
        out Texture2D texture)
    {
        texture = null;
        if (string.IsNullOrEmpty(content) || width <= 0 || height <= 0)
        {
            return false;
        }

        try
        {
            BitMatrix bitMatrix;
            texture = GenerateQRCode(
                content,
                width,
                height,
                foregroundColor,
                backgroundColor,
                margin,
                errorCorrection,
                out bitMatrix
            );
            return texture != null;
        }
        catch
        {
            texture = null;
            return false;
        }
    }

    /// <summary>
    /// 生成2维码 方法二
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="color">前景色</param>
    /// <returns></returns>
    public static Texture2D GenerateQRCode(string content, Color color)
    {
        return GenerateQRCode(content, 256, 256, color);
    }

    /// <summary>
    /// 生成2维码 方法二
    /// 经测试：能生成任意尺寸的正方形
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="color">前景色</param>
    /// <returns></returns>
    public static Texture2D GenerateQRCode(string content, int width, int height, Color color)
    {
        BitMatrix bitMatrix;
        Texture2D texture = GenerateQRCode(content, width, height, color, out bitMatrix);

        return texture;
    }

    /// <summary>
    /// 生成2维码 方法二
    /// 经测试：能生成任意尺寸的正方形
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="color">前景色</param>
    /// <param name="bitMatrix">输出 BitMatrix</param>
    public static Texture2D GenerateQRCode(string content, int width, int height, Color color, out BitMatrix bitMatrix)
    {
        return GenerateQRCode(
            content,
            width,
            height,
            color,
            Color.white,
            1,
            ErrorCorrectionLevel.H,
            out bitMatrix
        );
    }

    /// <summary>
    /// 生成二维码，支持前景/背景色、边距和纠错等级。
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="foregroundColor">前景色</param>
    /// <param name="backgroundColor">背景色</param>
    /// <param name="margin">边距</param>
    /// <param name="errorCorrection">纠错等级</param>
    /// <param name="bitMatrix">输出 BitMatrix</param>
    /// <returns></returns>
    public static Texture2D GenerateQRCode(
        string content,
        int width,
        int height,
        Color foregroundColor,
        Color backgroundColor,
        int margin,
        ErrorCorrectionLevel errorCorrection,
        out BitMatrix bitMatrix)
    {
        // 编码成color32
        MultiFormatWriter writer = new MultiFormatWriter();
        Dictionary<EncodeHintType, object> hints = new Dictionary<EncodeHintType, object>();
        //设置字符串转换格式，确保字符串信息保持正确
        hints.Add(EncodeHintType.CHARACTER_SET, "UTF-8");
        // 设置二维码边缘留白宽度（值越大留白宽度大，二维码就减小）
        hints.Add(EncodeHintType.MARGIN, margin);
        hints.Add(EncodeHintType.ERROR_CORRECTION, errorCorrection);
        //实例化字符串绘制二维码工具
        bitMatrix = writer.encode(content, BarcodeFormat.QR_CODE, width, height, hints);

        // 转成texture2d
        int w = bitMatrix.Width;
        int h = bitMatrix.Height;

        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (bitMatrix[x, y])
                {
                    texture.SetPixel(x, y, foregroundColor);
                }
                else
                {
                    texture.SetPixel(x, y, backgroundColor);
                }
            }
        }
        texture.Apply();

        return texture;
    }

    /// <summary>
    /// 生成2维码 方法三
    /// 在方法二的基础上，添加小图标 
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="color">前景色</param>
    /// <param name="centerIcon">中心图标</param>
    /// <returns></returns>
    public static Texture2D GenerateQRCode(string content, Color color, Texture2D centerIcon)
    {
        return GenerateQRCode(content, 256, 256, color, centerIcon);
    }

    /// <summary>
    /// 生成2维码 方法三
    /// 在方法二的基础上，添加小图标
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="color">前景色</param>
    /// <param name="centerIcon">中心图标</param>
    /// <returns></returns>
    public static Texture2D GenerateQRCode(string content, int width, int height, Color color, Texture2D centerIcon)
    {
        if (centerIcon == null)
        {
            return GenerateQRCode(content, width, height, color);
        }

        BitMatrix bitMatrix;
        Texture2D texture = GenerateQRCode(content, width, height, color, Color.white, 1, ErrorCorrectionLevel.H, out bitMatrix);
        int w = bitMatrix.Width;
        int h = bitMatrix.Height;

        // 添加小图
        int halfWidth = texture.width / 2;
        int halfHeight = texture.height / 2;
        int maxHalfIconSize = Mathf.Max(1, Mathf.Min(halfWidth, halfHeight) / 5);
        int halfWidthOfIcon = Mathf.Min(centerIcon.width / 2, maxHalfIconSize);
        int halfHeightOfIcon = Mathf.Min(centerIcon.height / 2, maxHalfIconSize);
        int iconCenterX = centerIcon.width / 2;
        int iconCenterY = centerIcon.height / 2;
        int centerOffsetX = 0;
        int centerOffsetY = 0;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                centerOffsetX = x - halfWidth;
                centerOffsetY = y - halfHeight;
                if (Mathf.Abs(centerOffsetX) <= halfWidthOfIcon && Mathf.Abs(centerOffsetY) <= halfHeightOfIcon)
                {
                    texture.SetPixel(
                        x,
                        y,
                        centerIcon.GetPixel(
                            iconCenterX + centerOffsetX,
                            iconCenterY + centerOffsetY
                        )
                    );
                }
            }
        }
        texture.Apply();

        return texture;
    }

    /// <summary>
    /// 生成二维码，支持前景/背景色、边距和纠错等级。
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="foregroundColor">前景色</param>
    /// <param name="backgroundColor">背景色</param>
    /// <param name="margin">边距</param>
    /// <param name="errorCorrection">纠错等级</param>
    /// <returns></returns>
    public static Texture2D GenerateQRCode(
        string content,
        int width,
        int height,
        Color foregroundColor,
        Color backgroundColor,
        int margin,
        ErrorCorrectionLevel errorCorrection)
    {
        BitMatrix bitMatrix;
        return GenerateQRCode(
            content,
            width,
            height,
            foregroundColor,
            backgroundColor,
            margin,
            errorCorrection,
            out bitMatrix
        );
    }

    /// <summary>
    /// 生成二维码并返回 Sprite。
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns></returns>
    public static Sprite GenerateQRCodeToSprite(string content, int width, int height)
    {
        Texture2D texture = GenerateQRCode(content, width, height);
        return texture == null ? null : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// 生成二维码 Sprite，指定前景色。
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="color">前景色</param>
    /// <returns></returns>
    public static Sprite GenerateQRCodeToSprite(string content, int width, int height, Color color)
    {
        Texture2D texture = GenerateQRCode(content, width, height, color);
        return texture == null ? null : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// 生成二维码 Sprite，支持前景/背景色、边距和纠错等级。
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="foregroundColor">前景色</param>
    /// <param name="backgroundColor">背景色</param>
    /// <param name="margin">边距</param>
    /// <param name="errorCorrection">纠错等级</param>
    /// <returns></returns>
    public static Sprite GenerateQRCodeToSprite(
        string content,
        int width,
        int height,
        Color foregroundColor,
        Color backgroundColor,
        int margin,
        ErrorCorrectionLevel errorCorrection)
    {
        Texture2D texture = GenerateQRCode(content, width, height, foregroundColor, backgroundColor, margin, errorCorrection);
        return texture == null ? null : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// 将纹理编码为 PNG 字节数组。
    /// </summary>
    /// <param name="texture">纹理</param>
    /// <returns></returns>
    public static byte[] EncodeToPNG(Texture2D texture)
    {
        return texture == null ? null : texture.EncodeToPNG();
    }

    /// <summary>
    /// 生成二维码并编码为 PNG 字节数组。
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns></returns>
    public static byte[] EncodeToPNG(string content, int width, int height)
    {
        Texture2D texture = GenerateQRCode(content, width, height);
        return texture == null ? null : texture.EncodeToPNG();
    }

    /// <summary>
    /// 生成二维码并编码为 PNG 字节数组，指定前景色。
    /// </summary>
    /// <param name="content">二维码内容</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="color">前景色</param>
    /// <returns></returns>
    public static byte[] EncodeToPNG(string content, int width, int height, Color color)
    {
        Texture2D texture = GenerateQRCode(content, width, height, color);
        return texture == null ? null : texture.EncodeToPNG();
    }

    #endregion

    #region ScanQRCode

    /*
     使用方法，类似如下：
     Result result = ZXingQRCodeWrapper.ScanQRCode(data, webCamTexture.width, webCamTexture.height);
         
         */

    /// <summary>
    /// 传入图片识别
    /// </summary>
    /// <param name="textureData">纹理数据</param>
    /// <param name="textureDataWidth">纹理宽度（兼容参数，内部使用纹理尺寸）</param>
    /// <param name="textureDataHeight">纹理高度（兼容参数，内部使用纹理尺寸）</param>
    /// <returns></returns>
    public static Result ScanQRCode(Texture2D textureData, int textureDataWidth, int textureDataHeight)
    {
        return ScanQRCode(textureData.GetPixels32(), textureData.width, textureData.height);
    }

    /// <summary>
    /// 传入图片像素识别
    /// </summary>
    /// <param name="textureData">像素数据</param>
    /// <param name="textureDataWidth">像素宽度</param>
    /// <param name="textureDataHeight">像素高度</param>
    /// <returns></returns>
    public static Result ScanQRCode(Color32[] textureData, int textureDataWidth, int textureDataHeight)
    {
        BarcodeReader reader = new BarcodeReader();
        Result result = reader.Decode(textureData, textureDataWidth, textureDataHeight);

        return result;
    }

    /// <summary>
    /// 传入像素识别，支持解码选项。
    /// </summary>
    /// <param name="textureData">像素数据</param>
    /// <param name="textureDataWidth">像素宽度</param>
    /// <param name="textureDataHeight">像素高度</param>
    /// <param name="options">解码选项</param>
    /// <returns></returns>
    public static Result ScanQRCode(Color32[] textureData, int textureDataWidth, int textureDataHeight, DecodingOptions options)
    {
        BarcodeReader reader = new BarcodeReader
        {
            Options = options
        };
        Result result = reader.Decode(textureData, textureDataWidth, textureDataHeight);

        return result;
    }

    /// <summary>
    /// 传入纹理识别，支持解码选项。
    /// </summary>
    /// <param name="textureData">纹理数据</param>
    /// <param name="options">解码选项</param>
    /// <returns></returns>
    public static Result ScanQRCode(Texture2D textureData, DecodingOptions options)
    {
        return ScanQRCode(textureData.GetPixels32(), textureData.width, textureData.height, options);
    }

    /// <summary>
    /// 传入像素识别，支持 TryHarder 和格式过滤。
    /// </summary>
    /// <param name="textureData">像素数据</param>
    /// <param name="textureDataWidth">像素宽度</param>
    /// <param name="textureDataHeight">像素高度</param>
    /// <param name="tryHarder">是否启用 TryHarder</param>
    /// <param name="possibleFormats">可识别的码制列表</param>
    /// <returns></returns>
    public static Result ScanQRCode(
        Color32[] textureData,
        int textureDataWidth,
        int textureDataHeight,
        bool tryHarder,
        IList<BarcodeFormat> possibleFormats)
    {
        DecodingOptions options = new DecodingOptions
        {
            TryHarder = tryHarder
        };
        if (possibleFormats != null && possibleFormats.Count > 0)
        {
            options.PossibleFormats = possibleFormats;
        }
        return ScanQRCode(textureData, textureDataWidth, textureDataHeight, options);
    }

    /// <summary>
    /// 传入纹理识别，支持 TryHarder 和格式过滤。
    /// </summary>
    /// <param name="textureData">纹理数据</param>
    /// <param name="tryHarder">是否启用 TryHarder</param>
    /// <param name="possibleFormats">可识别的码制列表</param>
    /// <returns></returns>
    public static Result ScanQRCode(Texture2D textureData, bool tryHarder, IList<BarcodeFormat> possibleFormats)
    {
        return ScanQRCode(textureData.GetPixels32(), textureData.width, textureData.height, tryHarder, possibleFormats);
    }

    /// <summary>
    /// 传入像素识别多个二维码。
    /// </summary>
    /// <param name="textureData">像素数据</param>
    /// <param name="textureDataWidth">像素宽度</param>
    /// <param name="textureDataHeight">像素高度</param>
    /// <returns></returns>
    public static Result[] ScanQRCodes(Color32[] textureData, int textureDataWidth, int textureDataHeight)
    {
        BarcodeReader reader = new BarcodeReader();
        Result[] results = reader.DecodeMultiple(textureData, textureDataWidth, textureDataHeight);

        return results;
    }

    /// <summary>
    /// 传入像素识别多个二维码，支持解码选项。
    /// </summary>
    /// <param name="textureData">像素数据</param>
    /// <param name="textureDataWidth">像素宽度</param>
    /// <param name="textureDataHeight">像素高度</param>
    /// <param name="options">解码选项</param>
    /// <returns></returns>
    public static Result[] ScanQRCodes(Color32[] textureData, int textureDataWidth, int textureDataHeight, DecodingOptions options)
    {
        BarcodeReader reader = new BarcodeReader
        {
            Options = options
        };
        Result[] results = reader.DecodeMultiple(textureData, textureDataWidth, textureDataHeight);

        return results;
    }

    /// <summary>
    /// 传入纹理识别多个二维码。
    /// </summary>
    /// <param name="textureData">纹理数据</param>
    /// <returns></returns>
    public static Result[] ScanQRCodes(Texture2D textureData)
    {
        return ScanQRCodes(textureData.GetPixels32(), textureData.width, textureData.height);
    }

    /// <summary>
    /// 传入纹理识别多个二维码，支持解码选项。
    /// </summary>
    /// <param name="textureData">纹理数据</param>
    /// <param name="options">解码选项</param>
    /// <returns></returns>
    public static Result[] ScanQRCodes(Texture2D textureData, DecodingOptions options)
    {
        return ScanQRCodes(textureData.GetPixels32(), textureData.width, textureData.height, options);
    }

    #endregion
}
