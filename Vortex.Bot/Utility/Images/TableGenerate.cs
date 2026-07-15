using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace Vortex.Bot.Utility.Images;

public struct TableCell
{
    public string Text { get; set; }
    public Color TextColor { get; set; } = Color.Black;
    public Color BackgroundColor { get; set; } = Color.White;
    public bool UseTextColor { get; set; }
    public bool UseBackgroundColor { get; set; }
    public FontStyle FontStyle { get; set; } = FontStyle.Regular;

    public TableCell(string text, Color textColor, Color backgroundColor)
    {
        Text = text;
        TextColor = textColor;
        BackgroundColor = backgroundColor;
        UseTextColor = true;
        UseBackgroundColor = true;
    }

    public TableCell(string text, Color textColor)
    {
        Text = text;
        TextColor = textColor;
        UseTextColor = true;
    }

    public TableCell(string text, Color textColor, FontStyle fontStyle)
    {
        Text = text;
        TextColor = textColor;
        FontStyle = fontStyle;
        UseTextColor = true;
    }

    public TableCell(string text)
    {
        Text = text;
    }
}

public class TableContent
{
    public List<TableCell> Content { get; set; } = [];
}

public class TableBuilder
{
    public List<TableContent> Rows { get; } = [];
    public List<TableCell> Header { get; } = [];
    internal TableGenerate Generator { get; } = new();

    public static TableBuilder Create() => new();

    public TableBuilder SetHeader(params string[] headers)
    {
        if (headers == null || headers.Length == 0) return this;
        foreach (var value in headers)
            Header.Add(new TableCell(value));
        return this;
    }

    public TableBuilder SetHeader(params TableCell[] headers)
    {
        if (headers == null || headers.Length == 0) return this;
        Header.AddRange(headers);
        return this;
    }

    public TableBuilder AddRow(params string[] rows)
    {
        if (rows == null || rows.Length == 0) return this;

        var content = new TableContent();
        foreach (var value in rows)
            content.Content.Add(new TableCell(value));
        Rows.Add(content);
        return this;
    }

    public TableBuilder AddRow(params TableCell[] rows)
    {
        if (rows == null || rows.Length == 0) return this;

        var content = new TableContent();
        content.Content.AddRange(rows);
        Rows.Add(content);
        return this;
    }

    public TableBuilder SetTitle(string title)
    {
        Generator.Config.Title = title;
        return this;
    }

    public TableBuilder SetLineMaxTextLength(int maxTextLength)
    {
        Generator.Config.LineMaxTextLength = maxTextLength;
        return this;
    }

    public TableBuilder SetMemberUin(long memberUin)
    {
        Generator.Config.MemberUin = memberUin;
        return this;
    }

    public TableBuilder SetSignature(string signature)
    {
        Generator.Config.Signature = signature;
        return this;
    }

    public TableBuilder SetCardBackgroundColor(Color color)
    {
        Generator.Config.CardBackgroundColor = color;
        return this;
    }

    public TableBuilder SetTableFontColor(Color color)
    {
        Generator.TableFontColor = color;
        return this;
    }

    public TableBuilder SetTitleColor(Color color)
    {
        Generator.Config.TitleColor = color;
        return this;
    }

    public TableBuilder SetSignatureColor(Color color)
    {
        Generator.Config.SignatureColor = color;
        return this;
    }

    public TableBuilder SetTableThicknessColor(Color color)
    {
        Generator.TableThicknessColor = color;
        return this;
    }

    public TableBuilder SetFontSize(int fontSize)
    {
        Generator.Config.FontSize = fontSize;
        return this;
    }

    public TableBuilder SetGap(int gap)
    {
        Generator.Config.Gap = gap;
        return this;
    }

    public TableBuilder SetTableMargin(int tableMargin)
    {
        Generator.TableMargin = tableMargin;
        return this;
    }

    public TableBuilder SetCardMargin(int cardMargin)
    {
        Generator.Config.CardMargin = cardMargin;
        return this;
    }

    public TableBuilder SetTableBottomMargin(int tableBottomMargin)
    {
        Generator.TableBottomMargin = tableBottomMargin;
        return this;
    }

    public TableBuilder SetCardTopMargin(int cardTopMargin)
    {
        Generator.Config.CardTopMargin = cardTopMargin;
        return this;
    }

    public TableBuilder SetCardBottomMargin(int cardBottomMargin)
    {
        Generator.Config.CardBottomMargin = cardBottomMargin;
        return this;
    }

    public byte[] Build()
    {
        if (Header.Count == 0) throw new Exception("you must set table header!");
        return Generator.Generate(this);
    }
}

public class TableGenerate : ImageGeneratorBase, IImageGenerator<TableBuilder>
{
    public int TableMargin { get; set; } = 50;
    public int TableBottomMargin { get; set; } = 80;
    public int TableTopMargin { get; set; } = 400;
    public Color TableFontColor { get; set; } = Color.Black;
    public Color TableThicknessColor { get; set; } = Color.DarkGray;

    private TableBuilder? _currentBuilder;
    private int[] _rowHeights = Array.Empty<int>();
    private int[] _columnWidths = Array.Empty<int>();

    public byte[] Generate(TableBuilder builder)
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
            _columnWidths = Array.Empty<int>();
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

        var rowHeights = new int[_currentBuilder.Rows.Count + 1];
        var columnWidths = new int[_currentBuilder.Header.Count];

        for (var j = 0; j < _currentBuilder.Header.Count; j++)
        {
            var size = TextMeasurer.MeasureSize(_currentBuilder.Header[j].Text, textOption);
            rowHeights[0] = Math.Max(rowHeights[0], (int)size.Height);
            columnWidths[j] = Math.Max(columnWidths[j], (int)size.Width);
        }

        for (var i = 0; i < _currentBuilder.Rows.Count; i++)
        {
            for (var j = 0; j < _currentBuilder.Rows[i].Content.Count; j++)
            {
                var cell = _currentBuilder.Rows[i].Content[j];
                var cellFont = CreateFont(Config.FontSize, cell.FontStyle);
                var cellTextSize = MeasureText("A", cellFont);
                var cellTextOption = new RichTextOptions(cellFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrappingLength = cellTextSize.Width * Config.LineMaxTextLength,
                    WordBreaking = WordBreaking.BreakAll
                };
                var size = TextMeasurer.MeasureSize(cell.Text, cellTextOption);
                rowHeights[i + 1] = Math.Max(rowHeights[i + 1], (int)size.Height);
                columnWidths[j] = Math.Max(columnWidths[j], (int)size.Width);
            }
        }

        var totalWidth = columnWidths.Sum() + 2 * Gap * _currentBuilder.Header.Count;
        if (totalWidth < Config.MinWidth)
        {
            var extraWidth = (Config.MinWidth - totalWidth) / _currentBuilder.Header.Count;
            for (var j = 0; j < columnWidths.Length; j++)
                columnWidths[j] += extraWidth;
        }

        _rowHeights = rowHeights;
        _columnWidths = columnWidths;

        var maxHeight = rowHeights.Sum(h => h + 2 * Gap) + TableTopMargin + TableBottomMargin;
        var maxWidth = columnWidths.Sum() + 2 * Gap * _currentBuilder.Header.Count + 2 * TableMargin;

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
        DrawHeaderText(ctx, _currentBuilder, tableFont);
        DrawContentText(ctx, _currentBuilder, tableFont);
        DrawVerticalLines(ctx, _currentBuilder, contentWidth, contentHeight);
        DrawHorizontalLines(ctx, _currentBuilder, contentWidth, contentHeight);
        DrawSignature(ctx, signFont, CardMargin, CardTopMargin + TableTopMargin + contentHeight - TableTopMargin - TableBottomMargin + 40, contentWidth);
    }

    private void DrawHeaderText(IImageProcessingContext ctx, TableBuilder builder, Font tableFont)
    {
        var xOffset = CardMargin + TableMargin;
        var textSize = MeasureText("A", tableFont);

        for (var j = 0; j < builder.Header.Count; j++)
        {
            var cell = builder.Header[j];
            var cellRect = new RectangleF(xOffset, CardTopMargin + TableTopMargin, _columnWidths[j] + 2 * Gap, _rowHeights[0] + 2 * Gap);

            if (cell.UseBackgroundColor)
                ctx.Fill(cell.BackgroundColor, cellRect);

            var cellFont = CreateFont(Config.FontSize, cell.FontStyle);
            var cellTextSize = MeasureText("A", cellFont);
            var textOption = new RichTextOptions(cellFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                WrappingLength = cellTextSize.Width * Config.LineMaxTextLength,
                WordBreaking = WordBreaking.BreakAll,
                Origin = new PointF(xOffset + (_columnWidths[j] / 2) + Gap, CardTopMargin + TableTopMargin + (_rowHeights[0] / 2) + Gap)
            };

            ctx.DrawText(textOption, cell.Text, cell.UseTextColor ? cell.TextColor : TableFontColor);
            xOffset += _columnWidths[j] + 2 * Gap;
        }
    }

    private void DrawContentText(IImageProcessingContext ctx, TableBuilder builder, Font tableFont)
    {
        var yOffset = CardTopMargin + TableTopMargin + _rowHeights[0] + 2 * Gap;
        var textSize = MeasureText("A", tableFont);

        for (var i = 0; i < builder.Rows.Count; i++)
        {
            var xOffset = CardMargin + TableMargin;
            for (var j = 0; j < builder.Rows[i].Content.Count; j++)
            {
                var cell = builder.Rows[i].Content[j];
                var cellRect = new RectangleF(xOffset, yOffset, _columnWidths[j] + 2 * Gap, _rowHeights[i + 1] + 2 * Gap);

                if (cell.UseBackgroundColor)
                    ctx.Fill(cell.BackgroundColor, cellRect);

                var textY = yOffset + (_rowHeights[i + 1] / 2) + Gap;
                var cellFont = CreateFont(Config.FontSize, cell.FontStyle);
                var cellTextSize = MeasureText("A", cellFont);
                var textOption = new RichTextOptions(cellFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    WrappingLength = cellTextSize.Width * Config.LineMaxTextLength,
                    WordBreaking = WordBreaking.BreakAll,
                    Origin = new PointF(xOffset + (_columnWidths[j] / 2) + Gap, textY)
                };

                ctx.DrawText(textOption, cell.Text, cell.UseTextColor ? cell.TextColor : TableFontColor);
                xOffset += _columnWidths[j] + 2 * Gap;
            }
            yOffset += _rowHeights[i + 1] + 2 * Gap;
        }
    }

    private void DrawVerticalLines(IImageProcessingContext ctx, TableBuilder builder, int maxWidth, int maxHeight)
    {
        var xOffset = CardMargin + TableMargin;
        var lineStartY = CardTopMargin + TableTopMargin;
        var lineEndY = CardTopMargin + TableTopMargin + maxHeight - TableTopMargin - TableBottomMargin;

        for (var j = 0; j <= builder.Header.Count; j++)
        {
            DrawVerticalLine(ctx, xOffset, lineStartY, lineEndY);
            if (j < builder.Header.Count)
                xOffset += _columnWidths[j] + 2 * Gap;
        }
    }

    private void DrawHorizontalLines(IImageProcessingContext ctx, TableBuilder builder, int maxWidth, int maxHeight)
    {
        var yOffset = CardTopMargin + TableTopMargin;
        var lineStartX = CardMargin + TableMargin;
        var lineEndX = CardMargin + TableMargin + maxWidth - 2 * TableMargin;

        for (var i = 0; i <= builder.Rows.Count; i++)
        {
            DrawHorizontalLine(ctx, lineStartX, yOffset, lineEndX);
            if (i < builder.Rows.Count)
                yOffset += _rowHeights[i] + 2 * Gap;
        }

        var bottomY = CardTopMargin + TableTopMargin + maxHeight - TableTopMargin - TableBottomMargin;
        DrawHorizontalLine(ctx, lineStartX, bottomY, lineEndX);
    }
}
