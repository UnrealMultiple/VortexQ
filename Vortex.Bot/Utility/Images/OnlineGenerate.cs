using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Vortex.Bot.Utility.Images;

public class OnlineCell
{
    public long Uin { get; set; }
    public string Text { get; set; }
    public Color Color { get; set; }
    public bool UseColor { get; set; }

    public OnlineCell(long uin, string text, Color color)
    {
        Uin = uin;
        Text = text;
        Color = color;
        UseColor = true;
    }

    public OnlineCell(long uin, string text)
    {
        Uin = uin;
        Text = text;
    }
}

public class OnlineContent
{
    public string Title { get; set; } = "在线人数";
    public List<OnlineCell> OnlineCells { get; set; } = [];
}

public class OnlineBuilder
{
    public List<OnlineContent> Contents { get; } = [];
    internal OnlineGenerate Generator { get; } = new();

    public static OnlineBuilder Create() => new();

    public OnlineBuilder Add(string tileName, params OnlineCell[] cells)
    {
        OnlineContent content = new()
        {
            Title = tileName,
            OnlineCells = [.. cells]
        };
        Contents.Add(content);
        return this;
    }

    public OnlineBuilder SetFontSize(int size)
    {
        Generator.Config.FontSize = size;
        return this;
    }

    public OnlineBuilder SetTitleFontSize(int size)
    {
        Generator.Config.TitleFontSize = size;
        return this;
    }

    public OnlineBuilder SetTilePadding(int padding)
    {
        Generator.TilePadding = padding;
        return this;
    }

    public OnlineBuilder SetAvatarSize(int size)
    {
        Generator.Config.AvatarSize = size;
        return this;
    }

    public OnlineBuilder SetAvatarPadding(int padding)
    {
        Generator.AvatarPadding = padding;
        return this;
    }

    public OnlineBuilder SetLineMax(int max)
    {
        Generator.LineMax = max;
        return this;
    }

    public OnlineBuilder SetSpacing(int spacing)
    {
        Generator.Spacing = spacing;
        return this;
    }

    public OnlineBuilder SetCardTopPadding(int padding)
    {
        Generator.CardTopPadding = padding;
        return this;
    }

    public OnlineBuilder SetCardBottomPadding(int padding)
    {
        Generator.CardBottomPadding = padding;
        return this;
    }

    public OnlineBuilder SetCardMargin(int margin)
    {
        Generator.Config.CardMargin = margin;
        return this;
    }

    public OnlineBuilder SetCardDrawPadding(int padding)
    {
        Generator.CardDrawPadding = padding;
        return this;
    }

    public OnlineBuilder SetOnlinePadding(int padding)
    {
        Generator.OnlinePadding = padding;
        return this;
    }

    public byte[] Build() => Generator.Generate(this);
}

public class OnlineGenerate : ImageGeneratorBase, IImageGenerator<OnlineBuilder>
{
    public int TilePadding { get; set; } = 40;
    public int AvatarPadding { get; set; } = 10;
    public int LineMax { get; set; } = 6;
    public int Spacing { get; set; } = 40;
    public int CardTopPadding { get; set; } = 300;
    public int CardBottomPadding { get; set; } = 200;
    public int CardDrawPadding { get; set; } = 100;
    public int OnlinePadding { get; set; } = 200;

    private OnlineBuilder? _currentBuilder;
    private List<int> _contentHeights = new();

    public byte[] Generate(OnlineBuilder builder)
    {
        _currentBuilder = builder;
        try
        {
            return base.Generate();
        }
        finally
        {
            _currentBuilder = null;
            _contentHeights.Clear();
        }
    }

    protected override (int Width, int Height) ComputeLayout()
    {
        if (_currentBuilder == null) throw new InvalidOperationException("Builder not set");

        Font font = CreateFont(Config.FontSize);
        Font titleFont = CreateFont(Config.TitleFontSize);

        FontRectangle titleSize = TextMeasurer.MeasureSize("测", new TextOptions(titleFont));

        int width = (Config.CardMargin * 2) + (CardDrawPadding * 2) + (LineMax * Config.AvatarSize) + ((LineMax - 1) * Spacing);

        _contentHeights.Clear();
        foreach (OnlineContent content in _currentBuilder.Contents)
        {
            int height = TilePadding + (int)titleSize.Height;

            if (content.OnlineCells.Count == 0)
            {
                height += Config.AvatarSize + AvatarPadding + Spacing;
            }
            else
            {
                int cellCount = 0;
                foreach (OnlineCell cell in content.OnlineCells)
                {
                    FontRectangle textSize = TextMeasurer.MeasureSize(cell.Text, new TextOptions(font)
                    {
                        WrappingLength = Config.AvatarSize,
                        WordBreaking = WordBreaking.BreakAll
                    });

                    if (cellCount % LineMax == 0 && cellCount != 0)
                    {
                        height += Config.AvatarSize + AvatarPadding + Spacing;
                    }

                    cellCount++;
                }

                height += Config.AvatarSize + AvatarPadding + Spacing;
            }

            height += OnlinePadding + TilePadding;
            _contentHeights.Add(height);
        }

        int totalHeight = CardTopPadding + CardBottomPadding + _contentHeights.Sum();

        return (width, totalHeight);
    }

    protected override void DrawContent(IImageProcessingContext ctx, int width, int height)
    {
        if (_currentBuilder == null) throw new InvalidOperationException("Builder not set");

        Font font = CreateFont(Config.FontSize);
        Font titleFont = CreateFont(Config.TitleFontSize);

        float yOffset = CardTopPadding;
        for (int i = 0; i < _currentBuilder.Contents.Count; i++)
        {
            OnlineContent content = _currentBuilder.Contents[i];
            int contentHeight = _contentHeights[i];

            DrawCardBackgroundWithGlassEffect(ctx, Config.CardMargin, (int)yOffset, width - (Config.CardMargin * 2), contentHeight);

            yOffset += TilePadding;

            FontRectangle titleSize = TextMeasurer.MeasureSize(content.Title, new TextOptions(titleFont));
            float titleX = (width - titleSize.Width) / 2;
            ctx.DrawText(content.Title, titleFont, Color.Black, new PointF(titleX, yOffset));
            yOffset += titleFont.Size + TilePadding;

            int cellCount = 0;
            foreach (OnlineCell cell in content.OnlineCells)
            {
                int row = cellCount / LineMax;
                int col = cellCount % LineMax;
                int centerX = width / 2;
                int x = centerX + ((col % 2 == 0 ? 1 : -1) * ((col + 1) / 2) * (Config.AvatarSize + Spacing));
                if (content.OnlineCells.Count == 1)
                {
                    x = centerX - (Config.AvatarSize / 2);
                }
                int y = (int)(yOffset + (row * (Config.AvatarSize + Spacing + font.Size)));

                using Image<Rgba32> avatar = ImageUtility.GetAvatar(cell.Uin, Config.AvatarSize);
                ctx.DrawImage(avatar, new Point(x, y), 1);

                Color textColor = cell.UseColor ? cell.Color : Color.Black;
                RichTextOptions textOptions = new RichTextOptions(font)
                {
                    WrappingLength = Config.AvatarSize,
                    WordBreaking = WordBreaking.BreakAll,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Origin = new PointF(x + (Config.AvatarSize / 2), y + Config.AvatarSize + AvatarPadding)
                };

                ctx.DrawText(textOptions, cell.Text, textColor);

                cellCount++;
            }

            if (content.OnlineCells.Count == 0)
            {
                yOffset += Config.AvatarSize + AvatarPadding + Spacing;
            }
            else
            {
                yOffset += (int)Math.Ceiling(content.OnlineCells.Count / (double)LineMax) * (Config.AvatarSize + Spacing + font.Size);
            }

            yOffset += OnlinePadding + TilePadding;
        }
    }
}
