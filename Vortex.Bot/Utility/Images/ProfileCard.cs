using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Vortex.Bot.Extension;

namespace Vortex.Bot.Utility.Images;

public class ProfileItem(string label, string value)
{
    public string Label { get; set; } = label;
    public string Value { get; set; } = value;
    public Color LabelColor { get; set; } = Color.DarkSlateGray;
    public Color ValueColor { get; set; } = Color.Black;
    public Color ValueBackgroundColor { get; set; } = Color.White;
    public bool UseEllipseBackground { get; set; } = true;
}

public class ProfileItemBuilder
{
    public List<ProfileItem> Items { get; } = [];
    internal ProfileCard Generator { get; } = new();

    public static ProfileItemBuilder Create() => new();

    public ProfileItemBuilder AddItem(string label, string value)
    {
        Items.Add(new ProfileItem(label, value));
        return this;
    }

    public ProfileItemBuilder AddItem(string label, string value, Color labelColor, Color valueColor, Color valueBackgroundColor)
    {
        ProfileItem item = new(label, value)
        {
            LabelColor = labelColor,
            ValueColor = valueColor,
            ValueBackgroundColor = valueBackgroundColor
        };
        Items.Add(item);
        return this;
    }

    public ProfileItemBuilder AddSpecialItem(string label, string value, bool useEllipseBackground)
    {
        ProfileItem item = new(label, value)
        {
            UseEllipseBackground = useEllipseBackground
        };
        Items.Add(item);
        return this;
    }

    public ProfileItemBuilder SetMemberUin(long memberUin)
    {
        Generator.Config.MemberUin = memberUin;
        return this;
    }

    public ProfileItemBuilder SetCardOpacity(byte cardOpacity)
    {
        Generator.CardOpacity = cardOpacity;
        return this;
    }

    public ProfileItemBuilder SetCardWidth(int cardWidth)
    {
        Generator.CardWidth = cardWidth;
        return this;
    }

    public ProfileItemBuilder SetCardCornerRadius(float cardCornerRadius)
    {
        Generator.Config.CardCornerRadius = cardCornerRadius;
        return this;
    }

    public ProfileItemBuilder SetCardTopMargin(int cardTopMargin)
    {
        Generator.Config.CardTopMargin = cardTopMargin;
        return this;
    }

    public ProfileItemBuilder SetCardBottomMargin(int cardBottomMargin)
    {
        Generator.Config.CardBottomMargin = cardBottomMargin;
        return this;
    }

    public ProfileItemBuilder SetContentTopMargin(int contentTopMargin)
    {
        Generator.Config.ContentTopMargin = contentTopMargin;
        return this;
    }

    public ProfileItemBuilder SetContentBottomMargin(int contentBottomMargin)
    {
        Generator.Config.ContentBottomMargin = contentBottomMargin;
        return this;
    }

    public ProfileItemBuilder SetRowSpacing(int rowSpacing)
    {
        Generator.RowSpacing = rowSpacing;
        return this;
    }

    public ProfileItemBuilder SetTitle(string title)
    {
        Generator.Config.Title = title;
        return this;
    }

    public ProfileItemBuilder SetSignature(string signature)
    {
        Generator.Config.Signature = signature;
        return this;
    }

    public ProfileItemBuilder SetTitleColor(Color titleColor)
    {
        Generator.Config.TitleColor = titleColor;
        return this;
    }

    public ProfileItemBuilder SetSignatureColor(Color signatureColor)
    {
        Generator.Config.SignatureColor = signatureColor;
        return this;
    }

    public ProfileItemBuilder SetDefaultLabelColor(Color defaultLabelColor)
    {
        Generator.DefaultLabelColor = defaultLabelColor;
        return this;
    }

    public ProfileItemBuilder SetDefaultValueColor(Color defaultValueColor)
    {
        Generator.DefaultValueColor = defaultValueColor;
        return this;
    }

    public ProfileItemBuilder SetDefaultValueBackgroundColor(Color defaultValueBackgroundColor)
    {
        Generator.DefaultValueBackgroundColor = defaultValueBackgroundColor;
        return this;
    }

    public ProfileItemBuilder SetTitleFontSize(float titleFontSize)
    {
        Generator.Config.TitleFontSize = titleFontSize;
        return this;
    }

    public ProfileItemBuilder SetNormalFontSize(float normalFontSize)
    {
        Generator.NormalFontSize = normalFontSize;
        return this;
    }

    public ProfileItemBuilder SetSmallFontSize(float smallFontSize)
    {
        Generator.Config.SignatureFontSize = smallFontSize;
        return this;
    }

    public ProfileItemBuilder SetAvatarSize(int avatarSize)
    {
        Generator.Config.AvatarSize = avatarSize;
        return this;
    }

    public ProfileItemBuilder SetAvatarBorderSize(int avatarBorderSize)
    {
        Generator.AvatarBorderSize = avatarBorderSize;
        return this;
    }

    public byte[] Build()
    {
        return Generator.Generate(this);
    }
}

public class ProfileCard : ImageGeneratorBase, IImageGenerator<ProfileItemBuilder>
{
    public byte CardOpacity { get; set; } = 230;
    public int CardWidth { get; set; } = 450;
    public int RowSpacing { get; set; } = 50;
    public Color DefaultLabelColor { get; set; } = Color.DarkSlateGray;
    public Color DefaultValueColor { get; set; } = Color.Black;
    public Color DefaultValueBackgroundColor { get; set; } = Color.White;
    public float NormalFontSize { get; set; } = 18;
    public int AvatarBorderSize { get; set; } = 5;

    private ProfileItemBuilder? _currentBuilder;
    private int _cardHeight;
    private int _backgroundWidth;
    private int _backgroundHeight;
    private int _cardX;
    private int _cardY;

    public byte[] Generate(ProfileItemBuilder builder)
    {
        _currentBuilder = builder;
        try
        {
            return base.Generate();
        }
        finally
        {
            _currentBuilder = null;
        }
    }

    protected override (int Width, int Height) ComputeLayout()
    {
        if (_currentBuilder == null) throw new InvalidOperationException("Builder not set");

        var titleFont = CreateFont(Config.TitleFontSize, FontStyle.Bold);
        var normalFont = CreateFont(NormalFontSize);

        var titleHeight = !string.IsNullOrEmpty(Config.Title) ?
            (int)TextMeasurer.MeasureSize(Config.Title, new TextOptions(titleFont)).Height + 30 : 0;
        var avatarAreaHeight = Config.AvatarSize + 30;
        var itemAreaHeight = (_currentBuilder.Items.Count * RowSpacing) + 20;
        var signatureHeight = !string.IsNullOrEmpty(Config.Signature) ? 50 : 0;
        var contentHeight = Config.ContentTopMargin + titleHeight + avatarAreaHeight + itemAreaHeight + signatureHeight + Config.ContentBottomMargin;
        _cardHeight = contentHeight;

        _backgroundHeight = _cardHeight + Config.CardTopMargin + Config.CardBottomMargin;
        _backgroundWidth = CardWidth + 50;

        _cardX = (_backgroundWidth - CardWidth) / 2;
        _cardY = Config.CardTopMargin;

        return (_backgroundWidth, _backgroundHeight);
    }

    protected override void DrawContent(IImageProcessingContext ctx, int width, int height)
    {
        if (_currentBuilder == null) throw new InvalidOperationException("Builder not set");

        var titleFont = CreateFont(Config.TitleFontSize, FontStyle.Bold);
        var normalFont = CreateFont(NormalFontSize);
        var smallFont = CreateFont(Config.SignatureFontSize);

        DrawCardBackgroundWithGlassEffect(ctx, _cardX, _cardY, CardWidth, _cardHeight, CardOpacity);

        var currentY = _cardY + Config.ContentTopMargin;

        if (!string.IsNullOrEmpty(Config.Title))
        {
            currentY = DrawTitleWithOffset(ctx, Config.Title, titleFont, currentY, width, 20);
        }

        currentY = DrawAvatarWithOffset(ctx, currentY, width);
        currentY = DrawProfileItems(ctx, normalFont, currentY);

        if (!string.IsNullOrEmpty(Config.Signature))
        {
            DrawSignatureCentered(ctx, smallFont, currentY + 10, width);
        }
    }

    private int DrawAvatarWithOffset(IImageProcessingContext ctx, int currentY, int canvasWidth)
    {
        var avatarX = (canvasWidth / 2) - (Config.AvatarSize / 2);
        DrawAvatar(ctx, avatarX, currentY, Config.AvatarSize);
        return currentY + Config.AvatarSize + 30;
    }

    private int DrawProfileItems(IImageProcessingContext ctx, Font normalFont, int currentY)
    {
        if (_currentBuilder == null) return currentY;

        var leftMargin = _cardX + 40;
        var rightMargin = _cardX + CardWidth - 40;

        foreach (ProfileItem item in _currentBuilder.Items)
        {
            var labelOptions = new RichTextOptions(normalFont)
            {
                Origin = new PointF(leftMargin, currentY),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            ctx.DrawText(labelOptions, item.Label, item.LabelColor);

            FontRectangle valueTextSize = TextMeasurer.MeasureSize(item.Value, new TextOptions(normalFont));
            var paddingX = 16;
            var paddingY = 6;
            var backgroundWidth = valueTextSize.Width + (paddingX * 2);
            var backgroundHeight = valueTextSize.Height + (paddingY * 2);
            var bgX = rightMargin - backgroundWidth;
            var bgY = currentY - paddingY;
            var valueTextX = bgX + paddingX;
            var valueTextY = bgY + paddingY;

            DrawValueBackgroundWithGlassEffect(ctx, (int)bgX, (int)bgY, (int)backgroundWidth, (int)backgroundHeight, item.UseEllipseBackground);

            var valueTextOptions = new RichTextOptions(normalFont)
            {
                Origin = new PointF(valueTextX, valueTextY),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            ctx.DrawText(valueTextOptions, item.Value, item.ValueColor);

            currentY += RowSpacing;
        }

        return currentY;
    }

    private void DrawValueBackgroundWithGlassEffect(IImageProcessingContext ctx, int x, int y, int width, int height, bool useEllipse)
    {
        if (_backgroundImage != null)
        {
            var cropRect = new Rectangle(x, y, width, height);
            using var bgBlur = _backgroundImage.Clone(img => img.Crop(cropRect).GaussianBlur(6));
            ctx.DrawImage(bgBlur, new Point(x, y), 1f);
        }

        var glassOverlay = new Color(new Rgba32(255, 255, 255, 50));
        var cornerRadius = useEllipse ? height / 2f : 6f;

        if (useEllipse)
        {
            ctx.DrawRoundedRectangle(x, y, width, height, cornerRadius, glassOverlay);
            ctx.DrawRoundedRectanglePath(x, y, width, height, cornerRadius, 1, new Color(new Rgba32(255, 255, 255, 50)));
        }
        else
        {
            ctx.DrawRoundedRectangle(x, y, width, height, cornerRadius, glassOverlay);
            ctx.DrawRoundedRectanglePath(x, y, width, height, cornerRadius, 1, new Color(new Rgba32(255, 255, 255, 50)));
        }
    }
}
