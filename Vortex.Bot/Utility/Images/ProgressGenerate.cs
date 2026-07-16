using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Vortex.Bot.Extension;

namespace Vortex.Bot.Utility.Images;

public class BossProgressItem(string name, string imagePath, bool isKilled)
{
    public string Name { get; set; } = name;

    public string ImagePath { get; set; } = imagePath;

    public bool IsKilled { get; set; } = isKilled;

    public DateTime? KillTime { get; set; }

    public BossProgressItem(string name, string imagePath, bool isKilled, DateTime? killTime) : this(name, imagePath, isKilled)
    {
        KillTime = killTime;
    }
}

public class ProgressBuilder
{
    public List<BossProgressItem> BossItems { get; } = new();
    internal ProgressGenerate Generator { get; } = new();

    public static ProgressBuilder Create() => new();

    public ProgressBuilder AddBoss(string name, string imagePath, bool isKilled)
    {
        BossItems.Add(new BossProgressItem(name, imagePath, isKilled));
        return this;
    }

    public ProgressBuilder AddBoss(string name, string imagePath, bool isKilled, DateTime? killTime)
    {
        BossItems.Add(new BossProgressItem(name, imagePath, isKilled, killTime));
        return this;
    }

    public ProgressBuilder AddBosses(IEnumerable<BossProgressItem> items)
    {
        BossItems.AddRange(items);
        return this;
    }

    public ProgressBuilder SetServerName(string serverName)
    {
        Generator.ServerName = serverName;
        return this;
    }

    public ProgressBuilder SetTitle(string title)
    {
        Generator.Title = title;
        return this;
    }

    public ProgressBuilder SetItemsPerRow(int count)
    {
        Generator.ItemsPerRow = count;
        return this;
    }

    public ProgressBuilder SetCardSize(int size)
    {
        Generator.CardSize = size;
        return this;
    }

    public ProgressBuilder SetCardSpacing(int spacing)
    {
        Generator.CardSpacing = spacing;
        return this;
    }

    public ProgressBuilder SetBackgroundColorStart(Color color)
    {
        Generator.BackgroundColorStart = color;
        return this;
    }

    public ProgressBuilder SetBackgroundColorEnd(Color color)
    {
        Generator.BackgroundColorEnd = color;
        return this;
    }

    public ProgressBuilder SetKilledColor(Color color)
    {
        Generator.KilledColor = color;
        return this;
    }

    public ProgressBuilder SetUnkilledColor(Color color)
    {
        Generator.UnkilledColor = color;
        return this;
    }

    public ProgressBuilder SetFontSize(float size)
    {
        Generator.FontSize = size;
        return this;
    }

    public ProgressBuilder SetTitleFontSize(float size)
    {
        Generator.TitleFontSize = size;
        return this;
    }

    public ProgressBuilder SetMaxDisplayCount(int count)
    {
        Generator.MaxDisplayCount = count;
        return this;
    }

    public ProgressBuilder SetPadding(int padding)
    {
        Generator.Padding = padding;
        return this;
    }

    public ProgressBuilder ShowKillTime(bool show)
    {
        Generator.ShowKillTime = show;
        return this;
    }

    public byte[] Build()
    {
        return Generator.Generate(this);
    }
}

public class ProgressGenerate : IImageGenerator<ProgressBuilder>
{
    public string ServerName { get; set; } = "Terraria";
    public string Title { get; set; } = "Boss 击杀进度";
    public int ItemsPerRow { get; set; } = 4;
    public int CardSize { get; set; } = 280;
    public int CardSpacing { get; set; } = 30;
    public float FontSize { get; set; } = 24;
    public float TitleFontSize { get; set; } = 56;
    public float ServerNameFontSize { get; set; } = 42;
    public int MaxDisplayCount { get; set; } = 50;
    public int Padding { get; set; } = 80;
    public bool ShowKillTime { get; set; } = false;

    public Color BackgroundColorStart { get; set; } = Color.FromRgba(15, 23, 42, 255);
    public Color BackgroundColorEnd { get; set; } = Color.FromRgba(30, 41, 59, 255);
    public Color CardBackgroundColor { get; set; } = Color.FromRgba(51, 65, 85, 230);
    public Color CardBackgroundColorDark { get; set; } = Color.FromRgba(30, 41, 59, 230);
    public Color KilledColor { get; set; } = Color.FromRgba(34, 197, 94, 255);
    public Color UnkilledColor { get; set; } = Color.FromRgba(71, 85, 105, 255);
    public Color TitleColor { get; set; } = Color.FromRgba(255, 255, 255, 255);
    public Color ServerNameColor { get; set; } = Color.FromRgba(148, 163, 184, 255);
    public Color TextColor { get; set; } = Color.FromRgba(255, 255, 255, 255);
    public Color TextColorDark { get; set; } = Color.FromRgba(148, 163, 184, 255);
    public Color ShadowColor { get; set; } = Color.FromRgba(0, 0, 0, 50);
    public Color ProgressBarBgColor { get; set; } = Color.FromRgba(71, 85, 105, 255);
    public Color ProgressBarFillColor { get; set; } = Color.FromRgba(59, 130, 246, 255);

    private ProgressBuilder? _currentBuilder;
    private int _totalWidth;
    private int _totalHeight;

    public byte[] Generate(ProgressBuilder builder)
    {
        _currentBuilder = builder;
        try
        {
            (var width, var height) = ComputeLayout();
            _totalWidth = width;
            _totalHeight = height;

            using var image = new Image<Rgba32>(width, height);

            image.Mutate(ctx =>
            {
                DrawBackground(ctx, width, height);
                DrawContent(ctx, width, height);
            });

            return image.ToBytesAsync().Result;
        }
        finally
        {
            _currentBuilder = null;
        }
    }

    private (int Width, int Height) ComputeLayout()
    {
        if (_currentBuilder == null) throw new InvalidOperationException("Builder not set");

        var itemCount = Math.Min(_currentBuilder.BossItems.Count, MaxDisplayCount);
        var rows = (int)Math.Ceiling((double)itemCount / ItemsPerRow);

        var cardTotalWidth = (CardSize * ItemsPerRow) + (CardSpacing * (ItemsPerRow - 1));
        var width = cardTotalWidth + (Padding * 2);

        var headerHeight = Padding + 20 + TitleFontSize * 1.2f + ServerNameFontSize + 20 + 40 + 70;

        var textAreaHeight = 70;
        var totalCardHeight = CardSize + textAreaHeight;
        var cardTotalHeight = totalCardHeight * rows + (CardSpacing * (rows - 1));

        var height = (int)(headerHeight + cardTotalHeight + 30);

        return (width, height);
    }

    private void DrawBackground(IImageProcessingContext ctx, int width, int height)
    {
        var gradientBrush = new LinearGradientBrush(
            new PointF(0, 0),
            new PointF(0, height),
            GradientRepetitionMode.None,
            new ColorStop(0, BackgroundColorStart),
            new ColorStop(1, BackgroundColorEnd)
        );

        ctx.Fill(gradientBrush);

        DrawDecorativeCircles(ctx, width, height);
    }

    private void DrawDecorativeCircles(IImageProcessingContext ctx, int width, int height)
    {
        var circleBrush1 = new SolidBrush(Color.FromRgba(59, 130, 246, 30));
        ctx.Fill(circleBrush1, new EllipsePolygon(-100, -100, 300, 300));

        var circleBrush2 = new SolidBrush(Color.FromRgba(139, 92, 246, 25));
        ctx.Fill(circleBrush2, new EllipsePolygon(width + 100, height + 100, 400, 400));

        var circleBrush3 = new SolidBrush(Color.FromRgba(236, 72, 153, 20));
        ctx.Fill(circleBrush3, new EllipsePolygon(width * 0.8f, height * 0.3f, 150, 150));
    }

    private void DrawContent(IImageProcessingContext ctx, int width, int height)
    {
        if (_currentBuilder == null) throw new InvalidOperationException("Builder not set");

        var titleFont = CreateFont(TitleFontSize, FontStyle.Bold);
        var serverFont = CreateFont(ServerNameFontSize);
        var statusFont = CreateFont(FontSize);

        float currentY = Padding + 20;
        DrawTitle(ctx, Title, titleFont, currentY, width);

        currentY += TitleFontSize * 1.2f;
        DrawServerName(ctx, $"{ServerName} 服务器", serverFont, currentY, width);

        currentY += ServerNameFontSize + 20;
        DrawOverallProgress(ctx, _currentBuilder.BossItems, currentY, width);

        currentY += 70;
        DrawBossGrid(ctx, _currentBuilder.BossItems, currentY, width);
        DrawSignature(ctx, width);
    }

    private void DrawSignature(IImageProcessingContext ctx, int width)
    {
        var signature = "Generated by Vortex.Bot";
        var signatureFont = CreateFont(14);

        var signatureSize = TextMeasurer.MeasureSize(signature, new TextOptions(signatureFont));
        var signatureX = (width - signatureSize.Width) / 2;
        var signatureY = _totalHeight - 25;

        ctx.DrawText(signature, signatureFont, ServerNameColor, new PointF(signatureX, signatureY));
    }

    private void DrawTitle(IImageProcessingContext ctx, string title, Font font, float y, int width)
    {
        var titleOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(width / 2f, y)
        };

        var shadowOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(width / 2f + 2, y + 2)
        };
        ctx.DrawText(shadowOptions, title, ShadowColor);
        ctx.DrawText(titleOptions, title, TitleColor);
    }

    private void DrawServerName(IImageProcessingContext ctx, string serverName, Font font, float y, int width)
    {
        var serverOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(width / 2f, y)
        };
        ctx.DrawText(serverOptions, serverName, ServerNameColor);
    }

    private void DrawOverallProgress(IImageProcessingContext ctx, List<BossProgressItem> items, float y, int width)
    {
        var killedCount = items.Count(i => i.IsKilled);
        var totalCount = items.Count;
        var progress = totalCount > 0 ? (float)killedCount / totalCount : 0;

        var barWidth = Math.Min(600, width - (Padding * 2));
        var barHeight = 16;
        var barX = (width - barWidth) / 2f;
        var barY = y;

        ctx.Fill(ProgressBarBgColor, new RectangularPolygon(barX, barY, barWidth, barHeight));

        var fillWidth = (int)(barWidth * progress);
        if (fillWidth > 0)
        {
            var gradientBrush = new LinearGradientBrush(
                new PointF(barX, barY),
                new PointF(barX + fillWidth, barY),
                GradientRepetitionMode.None,
                new ColorStop(0, Color.FromRgba(59, 130, 246, 255)),
                new ColorStop(1, Color.FromRgba(34, 197, 94, 255))
            );
            ctx.Fill(gradientBrush, new RectangularPolygon(barX, barY, fillWidth, barHeight));
        }

        var progressFont = CreateFont(FontSize - 4);
        var progressText = $"{killedCount}/{totalCount} ({progress:P0})";
        var progressOptions = new RichTextOptions(progressFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(width / 2f, barY + barHeight + 15)
        };
        ctx.DrawText(progressOptions, progressText, ServerNameColor);
    }

    private void DrawBossGrid(IImageProcessingContext ctx, List<BossProgressItem> items, float startY, int width)
    {
        var nameFont = CreateFont(FontSize - 2);
        var statusFont = CreateFont(FontSize - 4);

        var itemCount = Math.Min(items.Count, MaxDisplayCount);
        var cardTotalWidth = (CardSize * ItemsPerRow) + (CardSpacing * (ItemsPerRow - 1));
        var startX = (width - cardTotalWidth) / 2f;

        var currentX = startX;
        var currentY = startY;
        var textAreaHeight = 70;
        var totalCardHeight = CardSize + textAreaHeight;

        for (var i = 0; i < itemCount; i++)
        {
            var item = items[i];
            DrawBossCard(ctx, item, (int)currentX, (int)currentY, CardSize, nameFont, statusFont);

            if ((i + 1) % ItemsPerRow == 0)
            {
                currentX = startX;
                currentY += totalCardHeight + CardSpacing;
            }
            else
            {
                currentX += CardSize + CardSpacing;
            }
        }
    }

    private void DrawBossCard(IImageProcessingContext ctx, BossProgressItem item, int x, int y, int size, Font nameFont, Font statusFont)
    {
        var cornerRadius = 16;
        var shadowOffset = 4;
        var textAreaHeight = 70;
        var totalCardHeight = size + textAreaHeight;
        var imageAreaHeight = size;

        var cardBg = item.IsKilled ? CardBackgroundColor : CardBackgroundColorDark;
        var textColor = item.IsKilled ? TextColor : TextColorDark;
        var statusColor = item.IsKilled ? KilledColor : UnkilledColor;
        var statusText = item.IsKilled ? "已击杀" : "未击杀";

        ctx.DrawRoundedRectangle(x + shadowOffset, y + shadowOffset, size, totalCardHeight, cornerRadius, ShadowColor);
        ctx.DrawRoundedRectangle(x, y, size, totalCardHeight, cornerRadius, cardBg);

        var indicatorHeight = 5;
        ctx.Fill(statusColor, new RectangularPolygon(x, y, size, indicatorHeight));

        DrawBossImage(ctx, item, x, y + indicatorHeight, size, imageAreaHeight - indicatorHeight - 10);

        var textAreaTop = y + imageAreaHeight + 10;
        var textAreaBottom = y + totalCardHeight - 18;
        var textCenterX = x + size / 2f;

        var nameSize = TextMeasurer.MeasureSize(item.Name, new TextOptions(nameFont) { WrappingLength = size - 20 });
        var statusSize = TextMeasurer.MeasureSize(statusText, new TextOptions(statusFont));

        var lineSpacing = 6;
        var totalTextHeight = nameSize.Height + lineSpacing + statusSize.Height;
        var startY = textAreaTop + (textAreaBottom - textAreaTop - totalTextHeight) / 2;

        var nameY = startY + nameSize.Height / 2;
        var statusY = startY + nameSize.Height + lineSpacing + statusSize.Height / 2;

        var pillPaddingX = 16;
        var pillPaddingY = 12;
        var maxTextWidth = Math.Max(nameSize.Width, statusSize.Width);
        var pillWidth = maxTextWidth + (pillPaddingX * 2);
        var pillHeight = totalTextHeight + (pillPaddingY * 2);
        var pillX = textCenterX - pillWidth / 2f;
        var pillY = (nameY + statusY) / 2f - pillHeight / 2f;

        var pillColor = item.IsKilled
            ? Color.FromRgba(34, 197, 94, 50)
            : Color.FromRgba(148, 163, 184, 50);
        ctx.DrawRoundedRectangle(pillX, pillY, pillWidth, pillHeight, 10, pillColor);

        var nameOptions = new RichTextOptions(nameFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(textCenterX, nameY),
            WrappingLength = size - 20
        };
        ctx.DrawText(nameOptions, item.Name, textColor);

        var statusOptions = new RichTextOptions(statusFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(textCenterX, statusY)
        };
        ctx.DrawText(statusOptions, statusText, statusColor);
    }

    private void DrawBossImage(IImageProcessingContext ctx, BossProgressItem item, int x, int y, int cardWidth, int maxImageHeight)
    {
        var imageLoaded = false;

        try
        {
            if (File.Exists(item.ImagePath))
            {
                using var originalImage = Image.Load<Rgba32>(item.ImagePath);

                var bossImage = originalImage.Clone();

                var maxSize = Math.Min(cardWidth - 30, maxImageHeight - 10);
                var scale = Math.Min(
                    (float)maxSize / bossImage.Width,
                    (float)maxSize / bossImage.Height
                );

                var newWidth = (int)(bossImage.Width * scale);
                var newHeight = (int)(bossImage.Height * scale);

                var imageX = x + (cardWidth - newWidth) / 2;
                var imageY = y + (maxImageHeight - newHeight) / 2;
                if (!item.IsKilled)
                {
                    bossImage.Mutate(img => img.Grayscale().Brightness(0.7f));
                }

                bossImage.Mutate(img => img.Resize(newWidth, newHeight));
                ctx.DrawImage(bossImage, new Point(imageX, imageY), 1f);

                imageLoaded = true;
            }
        }
        catch (Exception)
        {
            imageLoaded = false;
        }

        if (!imageLoaded)
        {
            DrawPlaceholder(ctx, x, y, cardWidth, maxImageHeight, item.Name);
        }
    }

    private void DrawPlaceholder(IImageProcessingContext ctx, int x, int y, int width, int height, string name)
    {
        ctx.Fill(
            Color.FromRgba(200, 200, 200, 100),
            new RectangularPolygon(x + 20, y + 10, width - 40, height - 20)
        );

        var placeholderFont = CreateFont(FontSize + 10);
        var placeholderOptions = new RichTextOptions(placeholderFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new PointF(x + width / 2f, y + height / 2f)
        };
        ctx.DrawText(placeholderOptions, "?", UnkilledColor);
    }

    private Font CreateFont(float size, FontStyle style = FontStyle.Regular)
    {
        return CardRenderer.CreateFont(size, style);
    }
}
