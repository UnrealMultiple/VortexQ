using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Vortex.Bot.Extension;

namespace Vortex.Bot.Utility.Images;

public abstract class ImageGeneratorBase
{
    public ImageGenerationConfig Config { get; } = new();
    protected string BackgroundPath => Config.BackgroundPath;
    protected int CardMargin => Config.CardMargin;
    protected int CardTopMargin => Config.CardTopMargin;
    protected int CardBottomMargin => Config.CardBottomMargin;
    protected int ContentTopMargin => Config.ContentTopMargin;
    protected int ContentBottomMargin => Config.ContentBottomMargin;
    protected int Gap => Config.Gap;
    protected int AvatarSize => Config.AvatarSize;
    protected int AvatarTop => Config.AvatarTop;
    protected float CardCornerRadius => Config.CardCornerRadius;
    protected Color CardBackgroundColor => Config.CardBackgroundColor;
    protected Color TitleColor => Config.TitleColor;
    protected Color SignatureColor => Config.SignatureColor;
    protected Color FontColor => Config.FontColor;
    protected Color ThicknessColor => Config.ThicknessColor;
    protected string Signature => Config.Signature;
    protected long MemberUin => Config.MemberUin;
    protected abstract (int Width, int Height) ComputeLayout();
    protected abstract void DrawContent(IImageProcessingContext ctx, int width, int height);

    protected Image<Rgba32>? _backgroundImage;

    public virtual byte[] Generate()
    {
        (var width, var height) = ComputeLayout();

        using var background = Image.Load<Rgba32>(BackgroundPath);
        _backgroundImage = background.Crop(width, height);

        _backgroundImage.Mutate(ctx => DrawContent(ctx, width, height));

        var result = _backgroundImage.ToBytesAsync().Result;
        _backgroundImage = null;
        return result;
    }

    protected void DrawCardBackgroundWithGlassEffect(IImageProcessingContext ctx, int x, int y, int width, int height, byte? opacity = null)
    {
        if (_backgroundImage == null) return;
        var bgColor = CardBackgroundColor.ToPixel<Rgba32>();
        if (opacity.HasValue)
        {
            bgColor.A = opacity.Value;
        }
        CardRenderer.DrawRoundedCardWithBlur(ctx, _backgroundImage, x, y, width, height, CardCornerRadius, new Color(bgColor));
    }

    protected static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
    {
        return CardRenderer.CreateFont(size, style);
    }

    protected static FontFamily GetFontFamily()
    {
        return CardRenderer.GetFontFamily();
    }

    protected void DrawCardBackground(IImageProcessingContext ctx, int x, int y, int width, int height)
    {
        CardRenderer.DrawRoundedCard(ctx, x, y, width, height, CardCornerRadius, CardBackgroundColor);
    }

    protected void DrawTitle(IImageProcessingContext ctx, string title, Font font, int x, int y, int maxWidth)
    {
        CardRenderer.DrawTitle(ctx, title, font, x, y, maxWidth, TitleColor);
    }

    protected int DrawTitleWithOffset(IImageProcessingContext ctx, string title, Font font, int y, int canvasWidth, int extraSpacing = 20)
    {
        return CardRenderer.DrawTitleWithOffset(ctx, title, font, y, canvasWidth, TitleColor, extraSpacing);
    }

    protected void DrawAvatar(IImageProcessingContext ctx, int x, int y, int? size = null)
    {
        CardRenderer.DrawAvatar(ctx, MemberUin, size ?? AvatarSize, x, y);
    }

    protected void DrawCenteredAvatar(IImageProcessingContext ctx, int y, int canvasWidth, int? size = null)
    {
        CardRenderer.DrawCenteredAvatar(ctx, MemberUin, size ?? AvatarSize, y, canvasWidth);
    }

    protected void DrawSignature(IImageProcessingContext ctx, Font font, int x, int y, int maxWidth)
    {
        CardRenderer.DrawSignature(ctx, Signature, font, x, y, maxWidth, SignatureColor);
    }

    protected void DrawSignatureCentered(IImageProcessingContext ctx, Font font, int y, int canvasWidth)
    {
        CardRenderer.DrawSignatureCentered(ctx, Signature, font, y, canvasWidth, SignatureColor);
    }

    protected void DrawHorizontalLine(IImageProcessingContext ctx, int x1, int y, int x2, float thickness = 1)
    {
        CardRenderer.DrawHorizontalLine(ctx, x1, y, x2, ThicknessColor, thickness);
    }

    protected void DrawVerticalLine(IImageProcessingContext ctx, int x, int y1, int y2, float thickness = 1)
    {
        CardRenderer.DrawVerticalLine(ctx, x, y1, y2, ThicknessColor, thickness);
    }

    protected FontRectangle MeasureText(string text, Font font, int? wrappingLength = null)
    {
        return CardRenderer.MeasureText(text, font, wrappingLength);
    }

    protected RichTextOptions CreateCenteredTextOptions(Font font, int? wrappingLength = null)
    {
        return CardRenderer.CreateTextOptions(font, HorizontalAlignment.Center, VerticalAlignment.Center, wrappingLength);
    }
}

public interface IImageGenerator<in TBuilder>
{
    byte[] Generate(TBuilder builder);
}
