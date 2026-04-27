using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Vortex.Bot.Extension;

namespace Vortex.Bot.Utility;

public static class CardRenderer
{
    public static void DrawRoundedCard(IImageProcessingContext ctx, int x, int y, int width, int height, float cornerRadius, Color color)
    {
        ctx.DrawRoundedRectangle(x, y, width, height, cornerRadius, color);
    }

    public static void DrawRoundedCardWithBlur(IImageProcessingContext ctx, Image<Rgba32> backgroundImage, int x, int y, int width, int height, float cornerRadius, Color color, float blurAmount = 8f)
    {
        var cropRect = new Rectangle(x, y, width, height);
        using var cardBlur = backgroundImage.Clone(img => img.Crop(cropRect).GaussianBlur(blurAmount));
        ctx.DrawImage(cardBlur, new Point(x, y), 1f);
        var originalColor = color.ToPixel<Rgba32>();
        var alpha = originalColor.A > 0 ? originalColor.A : (byte)200;
        var glassOverlay = new Color(new Rgba32(255, 255, 255, (byte)(alpha * 0.7 + 50)));
        ctx.DrawRoundedRectangle(x, y, width, height, cornerRadius, glassOverlay);
        ctx.DrawRoundedRectanglePath(x, y, width, height, cornerRadius, 1, new Color(new Rgba32(255, 255, 255, 120)));
    }

    public static void DrawTitle(IImageProcessingContext ctx, string title, Font font, int x, int y, int maxWidth, Color color)
    {
        if (string.IsNullOrEmpty(title)) return;

        var titleSize = TextMeasurer.MeasureSize(title, new TextOptions(font));
        var titlePosition = new PointF(x + ((maxWidth - titleSize.Width) / 2), y);
        ctx.DrawText(title, font, color, titlePosition);
    }

    public static int DrawTitleWithOffset(IImageProcessingContext ctx, string title, Font font, int y, int canvasWidth, Color color, int extraSpacing = 20)
    {
        if (string.IsNullOrEmpty(title)) return y;

        var titleOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(canvasWidth / 2, y)
        };

        ctx.DrawText(titleOptions, title, color);

        var titleHeight = (int)TextMeasurer.MeasureSize(title, new TextOptions(font)).Height;
        return y + titleHeight + extraSpacing;
    }

    public static void DrawAvatar(IImageProcessingContext ctx, long memberUin, int size, int x, int y)
    {
        using var avatar = ImageUtility.GetAvatar(memberUin, size);
        ctx.DrawImage(avatar, new Point(x, y), 1f);
    }

    public static void DrawCenteredAvatar(IImageProcessingContext ctx, long memberUin, int size, int y, int canvasWidth)
    {
        var x = (canvasWidth - size) / 2;
        DrawAvatar(ctx, memberUin, size, x, y);
    }

    public static void DrawSignature(IImageProcessingContext ctx, string signature, Font font, int x, int y, int maxWidth, Color color)
    {
        if (string.IsNullOrEmpty(signature)) return;

        var signTextOption = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new PointF(x + (maxWidth / 2), y)
        };
        ctx.DrawText(signTextOption, signature, color);
    }

    public static void DrawSignatureCentered(IImageProcessingContext ctx, string signature, Font font, int y, int canvasWidth, Color color)
    {
        if (string.IsNullOrEmpty(signature)) return;

        var signatureOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(canvasWidth / 2, y)
        };
        ctx.DrawText(signatureOptions, signature, color);
    }

    public static void DrawHorizontalLine(IImageProcessingContext ctx, int x1, int y, int x2, Color color, float thickness = 1)
    {
        ctx.DrawLine(color, thickness, new PointF(x1, y), new PointF(x2, y));
    }

    public static void DrawVerticalLine(IImageProcessingContext ctx, int x, int y1, int y2, Color color, float thickness = 1)
    {
        ctx.DrawLine(color, thickness, new PointF(x, y1), new PointF(x, y2));
    }

    public static Image<Rgba32> PrepareBackground(int width, int height, string backgroundPath)
    {
        using var originalBackground = Image.Load<Rgba32>(backgroundPath);

        var background = new Image<Rgba32>(width, height);

        originalBackground.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Crop
        }));

        background.Mutate(x => x.DrawImage(originalBackground, new Point(0, 0), 1f));

        return background;
    }

    public static Image<Rgba32> CropBackground(this Image<Rgba32> background, int width, int height)
    {
        if (background.Width == width && background.Height == height)
            return background.Clone();

        var result = new Image<Rgba32>(width, height);

        var x = Math.Max(0, (background.Width - width) / 2);
        var y = Math.Max(0, (background.Height - height) / 2);

        result.Mutate(ctx => ctx.DrawImage(background, new Point(-x, -y), 1f));

        return result;
    }

    public static FontFamily GetFontFamily()
    {
        return ImageUtility.Instance.FontFamily;
    }

    public static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
    {
        return GetFontFamily().CreateFont(size, style);
    }

    public static FontRectangle MeasureText(string text, Font font, int? wrappingLength = null)
    {
        var options = new TextOptions(font);
        if (wrappingLength.HasValue)
        {
            options.WrappingLength = wrappingLength.Value;
        }
        return TextMeasurer.MeasureSize(text, options);
    }

    public static RichTextOptions CreateTextOptions(Font font, HorizontalAlignment hAlign = HorizontalAlignment.Center,
        VerticalAlignment vAlign = VerticalAlignment.Center, int? wrappingLength = null)
    {
        var options = new RichTextOptions(font)
        {
            HorizontalAlignment = hAlign,
            VerticalAlignment = vAlign
        };

        if (wrappingLength.HasValue)
        {
            options.WrappingLength = wrappingLength.Value;
        }

        return options;
    }
}
