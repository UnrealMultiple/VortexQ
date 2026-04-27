using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace Vortex.Bot.Utility.Images;

public class ListCell
{
    public string Text { get; set; }
    public Color TextColor { get; set; } = Color.Black;
    public bool UseTextColor { get; set; }
    public bool UseBackgroundColor { get; set; }

    public ListCell(string text, Color textColor)
    {
        Text = text;
        TextColor = textColor;
        UseTextColor = true;
    }

    public ListCell(string text)
    {
        Text = text;
    }
}

public class ListBuilder
{
    public List<ListCell> Items { get; } = new();
    internal ListGenerate Generator { get; } = new();

    public static ListBuilder Create() => new();

    public ListBuilder SetTitle(string title)
    {
        Generator.Config.Title = title;
        return this;
    }

    public ListBuilder AddItem(string item)
    {
        Items.Add(new ListCell(item));
        return this;
    }

    public ListBuilder AddItem(ListCell item)
    {
        Items.Add(item);
        return this;
    }

    public ListBuilder AddItems(IEnumerable<string> items)
    {
        foreach (string item in items)
            Items.Add(new ListCell(item));
        return this;
    }

    public ListBuilder AddItems(IEnumerable<ListCell> items)
    {
        Items.AddRange(items);
        return this;
    }

    public ListBuilder SetFontSize(int size)
    {
        Generator.Config.FontSize = size;
        return this;
    }

    public ListBuilder SetLineMaxTextLength(int length)
    {
        Generator.Config.LineMaxTextLength = length;
        return this;
    }

    public ListBuilder SetTitleFontSize(int size)
    {
        Generator.Config.TitleFontSize = size;
        return this;
    }

    public ListBuilder SetSignatureFontSize(int size)
    {
        Generator.Config.SignatureFontSize = size;
        return this;
    }

    public ListBuilder SetGap(int gap)
    {
        Generator.Config.Gap = gap;
        return this;
    }

    public ListBuilder SetListMargin(int margin)
    {
        Generator.ListMargin = margin;
        return this;
    }

    public ListBuilder SetCardMargin(int margin)
    {
        Generator.Config.CardMargin = margin;
        return this;
    }

    public ListBuilder SetListBottomMargin(int margin)
    {
        Generator.ListBottomMargin = margin;
        return this;
    }

    public ListBuilder SetCardTopMargin(int margin)
    {
        Generator.Config.CardTopMargin = margin;
        return this;
    }

    public ListBuilder SetCardBottomMargin(int margin)
    {
        Generator.Config.CardBottomMargin = margin;
        return this;
    }

    public ListBuilder SetSignature(string signature)
    {
        Generator.Config.Signature = signature;
        return this;
    }

    public ListBuilder SetMemberUin(long uin)
    {
        Generator.Config.MemberUin = uin;
        return this;
    }

    public ListBuilder SetMinListWidth(int width)
    {
        Generator.Config.MinWidth = width;
        return this;
    }

    public ListBuilder SetCardBackgroundColor(Color color)
    {
        Generator.Config.CardBackgroundColor = color;
        return this;
    }

    public ListBuilder SetListFontColor(Color color)
    {
        Generator.ListFontColor = color;
        return this;
    }

    public ListBuilder SetTitleColor(Color color)
    {
        Generator.Config.TitleColor = color;
        return this;
    }

    public ListBuilder SetSignatureColor(Color color)
    {
        Generator.Config.SignatureColor = color;
        return this;
    }

    public ListBuilder SetListThicknessColor(Color color)
    {
        Generator.ListThicknessColor = color;
        return this;
    }

    public byte[] Build()
    {
        return Generator.Generate(this);
    }
}

public class ListGenerate : ImageGeneratorBase, IImageGenerator<ListBuilder>
{
    public int ListMargin { get; set; } = 100;
    public int ListBottomMargin { get; set; } = 80;
    public int ListTopMargin { get; set; } = 400;
    public Color ListFontColor { get; set; } = Color.Black;
    public Color ListThicknessColor { get; set; } = Color.DarkGray;

    private ListBuilder? _currentBuilder;
    private int[] _rowHeights = Array.Empty<int>();

    public byte[] Generate(ListBuilder builder)
    {
        _currentBuilder = builder;
        try
        {
            return base.Generate();
        }
        finally
        {
            _currentBuilder = null;
            _rowHeights = Array.Empty<int>();
        }
    }

    protected override (int Width, int Height) ComputeLayout()
    {
        if (_currentBuilder == null) throw new InvalidOperationException("Builder not set");

        var tableFont = CreateFont(Config.FontSize);
        var textSize = MeasureText("A", tableFont);

        var textOption = new RichTextOptions(tableFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            WrappingLength = textSize.Width * Config.LineMaxTextLength,
            WordBreaking = WordBreaking.BreakAll
        };

        var rowHeights = new int[_currentBuilder.Items.Count];
        var width = (int)textSize.Width;

        for (var i = 0; i < _currentBuilder.Items.Count; i++)
        {
            var size = TextMeasurer.MeasureSize(_currentBuilder.Items[i].Text, textOption);
            rowHeights[i] = (int)size.Height;
            width = (int)Math.Max(Config.MinWidth, Math.Max(width, size.Width));
        }

        var maxWidth = width + 2 * ListMargin;
        var maxHeight = rowHeights.Sum(h => h + 2 * Gap) + ListTopMargin + ListBottomMargin;

        _rowHeights = rowHeights;

        return (maxWidth + 2 * CardMargin, maxHeight + CardTopMargin + CardBottomMargin);
    }

    protected override void DrawContent(IImageProcessingContext ctx, int width, int height)
    {
        if (_currentBuilder == null) throw new InvalidOperationException("Builder not set");

        var tableFont = CreateFont(Config.FontSize);
        var titleFont = CreateFont(Config.TitleFontSize);
        var signFont = CreateFont(Config.SignatureFontSize);

        var contentWidth = width - 2 * CardMargin;
        var contentHeight = height - CardTopMargin - CardBottomMargin;

        DrawCardBackgroundWithGlassEffect(ctx, CardMargin, CardTopMargin, contentWidth, contentHeight);
        DrawTitle(ctx, Config.Title, titleFont, CardMargin, CardTopMargin + 30, contentWidth);
        DrawCenteredAvatar(ctx, CardTopMargin + 120, width, 200);
        DrawContentText(ctx, _currentBuilder, tableFont, contentWidth);
        DrawHorizontalLines(ctx, _currentBuilder, contentWidth, contentHeight);
        DrawSignature(ctx, signFont, CardMargin, CardTopMargin + ListTopMargin + contentHeight - ListTopMargin - ListBottomMargin + 40, contentWidth);
    }



    private void DrawContentText(IImageProcessingContext ctx, ListBuilder builder, Font tableFont, int maxWidth)
    {
        var yOffset = CardTopMargin + ListTopMargin;
        var textSize = MeasureText("A", tableFont);

        for (var i = 0; i < builder.Items.Count; i++)
        {
            var textOption = new RichTextOptions(tableFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                WrappingLength = textSize.Width * Config.LineMaxTextLength,
                WordBreaking = WordBreaking.BreakAll,
                Origin = new PointF(CardMargin + (maxWidth / 2), yOffset + (_rowHeights[i] / 2) + Gap)
            };

            var textColor = builder.Items[i].UseTextColor ? builder.Items[i].TextColor : ListFontColor;
            ctx.DrawText(textOption, builder.Items[i].Text, textColor);
            yOffset += _rowHeights[i] + 2 * Gap;
        }
    }

    private void DrawHorizontalLines(IImageProcessingContext ctx, ListBuilder builder, int maxWidth, int maxHeight)
    {
        var yOffset = CardTopMargin + ListTopMargin;
        var lineStartX = CardMargin + ListMargin;
        var lineEndX = CardMargin + ListMargin + maxWidth - 2 * ListMargin;

        for (var i = 0; i <= builder.Items.Count; i++)
        {
            DrawHorizontalLine(ctx, lineStartX, yOffset, lineEndX);
            if (i < builder.Items.Count)
                yOffset += _rowHeights[i] + 2 * Gap;
        }

        var bottomY = CardTopMargin + ListTopMargin + maxHeight - ListTopMargin - ListBottomMargin;
        DrawHorizontalLine(ctx, lineStartX, bottomY, lineEndX);
    }
}
