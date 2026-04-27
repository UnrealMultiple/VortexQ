using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Vortex.Bot.Extension;

namespace Vortex.Bot.Utility.Images;

public class MenuCell(string text, string smallText)
{
    public string Text { get; set; } = text;
    public string SmallText { get; set; } = smallText;
    public Color Color { get; set; } = Color.Black;
    public bool UseColor { get; set; }

    public MenuCell(string text, string smallText, Color color) : this(text, smallText)
    {
        Color = color;
        UseColor = true;
    }
}

public class MenuBuilder
{
    public List<MenuCell> MenuCells { get; } = new();
    internal MenuGenerate Generator { get; } = new();

    public static MenuBuilder Create() => new();

    public MenuBuilder AddCell(string text, string description)
    {
        MenuCells.Add(new MenuCell(text, description));
        return this;
    }

    public MenuBuilder AddCell(params MenuCell[] cells)
    {
        MenuCells.AddRange(cells);
        return this;
    }

    public MenuBuilder SetFontSize(int fontSize)
    {
        Generator.Config.FontSize = fontSize;
        return this;
    }

    public MenuBuilder SetSmallFontSize(int smallFontSize)
    {
        Generator.SmallFontSize = smallFontSize;
        return this;
    }

    public MenuBuilder SetCellSpaced(int cellSpaced)
    {
        Generator.CellSpaced = cellSpaced;
        return this;
    }

    public MenuBuilder SetCellWidth(int cellWidth)
    {
        Generator.CellWidth = cellWidth;
        return this;
    }

    public MenuBuilder SetMargin(int margin)
    {
        Generator.Config.CardMargin = margin;
        return this;
    }

    public MenuBuilder SetTopMargin(int topMargin)
    {
        Generator.Config.CardTopMargin = topMargin;
        return this;
    }

    public MenuBuilder SetTextCellSpacing(int textCellSpacing)
    {
        Generator.TextCellSpacing = textCellSpacing;
        return this;
    }

    public MenuBuilder SetLineMaxMenu(int lineMaxMenu)
    {
        Generator.LineMaxMenu = lineMaxMenu;
        return this;
    }

    public MenuBuilder SetAvatarSize(int avatarSize)
    {
        Generator.Config.AvatarSize = avatarSize;
        return this;
    }

    public MenuBuilder SetAvatarTop(int avatarTop)
    {
        Generator.AvatarTop = avatarTop;
        return this;
    }

    public MenuBuilder SetAvatarBottom(int avatarBottom)
    {
        Generator.AvatarBottom = avatarBottom;
        return this;
    }

    public MenuBuilder SetMemberUin(long memberUin)
    {
        Generator.Config.MemberUin = memberUin;
        return this;
    }

    public MenuBuilder SetCardColor(Color cardColor)
    {
        Generator.Config.CardBackgroundColor = cardColor;
        return this;
    }

    public byte[] Build()
    {
        return Generator.Generate(this);
    }
}

public class MenuGenerate : ImageGeneratorBase, IImageGenerator<MenuBuilder>
{
    public int SmallFontSize { get; set; } = 20;
    public int CellSpaced { get; set; } = 70;
    public int CellWidth { get; set; } = 400;
    public int TextCellSpacing { get; set; } = 20;
    public int LineMaxMenu { get; set; } = 3;
    public new int AvatarTop { get; set; } = 50;
    public int AvatarBottom { get; set; } = 50;
    public int CardPadding { get; set; } = 100;
    public int CardTopPadding { get; set; } = 200;

    private MenuBuilder? _currentBuilder;
    private int _totalWidth;
    private int _totalHeight;

    public byte[] Generate(MenuBuilder builder)
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

        var font = CreateFont(Config.FontSize);
        var smallFont = CreateFont(SmallFontSize);
        var signatureFont = CreateFont(Config.SignatureFontSize);

        _totalWidth = (CardMargin * 2) + (CellWidth * LineMaxMenu) + (TextCellSpacing * (LineMaxMenu - 1)) + (CardPadding * 2);
        _totalHeight = (Config.CardTopMargin * 2) + AvatarTop + Config.AvatarSize + AvatarBottom + CellSpaced + (CardTopPadding * 2);

        var currentLineHeight = 0;
        var cellCountInLine = 0;

        foreach (var cell in _currentBuilder.MenuCells)
        {
            var textOptions = new TextOptions(font) { WrappingLength = CellWidth };
            var textSize = TextMeasurer.MeasureSize(cell.Text, textOptions);
            var smallTextOptions = new TextOptions(smallFont) { WrappingLength = CellWidth };
            var smallTextSize = TextMeasurer.MeasureSize(cell.SmallText, smallTextOptions);

            var cellHeight = (int)(textSize.Height + smallTextSize.Height) + (TextCellSpacing * 2) + 8;

            if (cellCountInLine >= LineMaxMenu)
            {
                _totalHeight += currentLineHeight + CellSpaced;
                currentLineHeight = cellHeight;
                cellCountInLine = 1;
            }
            else
            {
                currentLineHeight = Math.Max(currentLineHeight, cellHeight);
                cellCountInLine++;
            }
        }

        _totalHeight += currentLineHeight;

        var signatureOptions = new TextOptions(signatureFont) { WrappingLength = _totalWidth };
        var signatureSize = TextMeasurer.MeasureSize(Config.Signature, signatureOptions);
        _totalHeight += (int)signatureSize.Height + 60;

        return (_totalWidth, _totalHeight);
    }

    protected override void DrawContent(IImageProcessingContext ctx, int width, int height)
    {
        if (_currentBuilder == null) throw new InvalidOperationException("Builder not set");

        var font = CreateFont(Config.FontSize);
        var smallFont = CreateFont(SmallFontSize);

        var currentX = CardMargin + CardPadding;
        var currentY = Config.CardTopMargin + AvatarTop + Config.AvatarSize + AvatarBottom + CardTopPadding;
        var currentLineHeight = 0;
        var cellCountInLine = 0;

        DrawCardBackgroundWithGlassEffect(ctx, CardPadding, CardTopPadding, width - (2 * CardPadding), height - (CardTopPadding * 2));
        DrawCenteredAvatar(ctx, AvatarTop + CardTopPadding, width);

        foreach (var cell in _currentBuilder.MenuCells)
        {
            DrawMenuCell(ctx, cell, ref currentX, ref currentY, ref currentLineHeight, ref cellCountInLine, font, smallFont);
        }

        var signatureFont = CreateFont(Config.SignatureFontSize);
        var signatureOptions = new TextOptions(signatureFont) { WrappingLength = width };
        var signatureSize = TextMeasurer.MeasureSize(Config.Signature, signatureOptions);
        var signatureX = (width - signatureSize.Width) / 2;
        var signatureY = currentY + currentLineHeight + 60;
        ctx.DrawText(Config.Signature, signatureFont, Color.Gray, new PointF(signatureX, signatureY));
    }

    private void DrawMenuCell(IImageProcessingContext ctx, MenuCell cell, ref int currentX, ref int currentY, ref int currentLineHeight, ref int cellCountInLine, Font font, Font smallFont)
    {
        var textOptions = new TextOptions(font) { WrappingLength = CellWidth };
        var textSize = TextMeasurer.MeasureSize(cell.Text, textOptions);
        var smallTextOptions = new TextOptions(smallFont) { WrappingLength = CellWidth };
        var smallTextSize = TextMeasurer.MeasureSize(cell.SmallText, smallTextOptions);

        var cellHeight = (int)(textSize.Height + smallTextSize.Height) + (TextCellSpacing * 2) + 8;

        if (cellCountInLine >= LineMaxMenu)
        {
            currentX = CardMargin + CardPadding;
            currentY += currentLineHeight + CellSpaced;
            currentLineHeight = cellHeight;
            cellCountInLine = 0;
        }
        else
        {
            currentLineHeight = Math.Max(currentLineHeight, cellHeight);
        }

        var textColor = cell.UseColor ? cell.Color : Color.Black;

        ctx.DrawRoundedRectangle(currentX, currentY, CellWidth, currentLineHeight, 30, Config.CardBackgroundColor);
        ctx.DrawRoundedRectanglePath(currentX, currentY, CellWidth, currentLineHeight, 30, 6, Color.Wheat);

        var totalTextHeight = textSize.Height + smallTextSize.Height + 8;
        var startY = currentY + (currentLineHeight - totalTextHeight) / 2;

        ctx.DrawText(new RichTextOptions(font)
        {
            Origin = new PointF(currentX + (CellWidth / 2), startY + textSize.Height / 2),
            WrappingLength = CellWidth,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        }, cell.Text, textColor);

        ctx.DrawText(new RichTextOptions(smallFont)
        {
            Origin = new PointF(currentX + (CellWidth / 2), startY + textSize.Height + 8 + smallTextSize.Height / 2),
            WrappingLength = CellWidth,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        }, cell.SmallText, Color.DarkGray);

        currentX += CellWidth + TextCellSpacing;
        cellCountInLine++;
    }
}
