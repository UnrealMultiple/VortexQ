using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Extension;
using Vortex.Protocol.Models;

namespace Vortex.Bot.Utility.Images;

public class ServerOnlinePlayerItem
{
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public bool IsLogin { get; set; }
    public string? PlayTime { get; set; }
    public long? UserId { get; set; }
}

public class ServerOnlineSection
{
    public string ServerName { get; set; } = string.Empty;
    public int OnlineCount { get; set; }
    public int MaxCount { get; set; }
    public List<ServerOnlinePlayerItem> Players { get; set; } = new();
}

public class ServerOnlineBuilder
{
    public List<ServerOnlineSection> Sections { get; } = new();
    internal ServerOnlineGenerate Generator { get; } = new();

    public static ServerOnlineBuilder Create() => new();

    public ServerOnlineBuilder AddSection(string serverName, int onlineCount, int maxCount, List<Player> players)
    {
        var section = new ServerOnlineSection
        {
            ServerName = serverName,
            OnlineCount = onlineCount,
            MaxCount = maxCount,
            Players = [.. players.Select(p =>
            {
                var user = TerrariaUser.GetUserByName(p.Name, serverName);
                return new ServerOnlinePlayerItem
                {
                    Name = p.Name,
                    Group = p.Group,
                    Prefix = p.Prefix,
                    IsLogin = p.IsLogin,
                    UserId = user?.Id
                };
            })]
        };
        Sections.Add(section);
        return this;
    }

    public ServerOnlineBuilder AddSection(ServerOnlineSection section)
    {
        Sections.Add(section);
        return this;
    }

    public byte[] Build() => Generator.Generate(this);
}

public class ServerOnlineGenerate : IImageGenerator<ServerOnlineBuilder>
{
    public Color BackgroundColorStart { get; set; } = Color.FromRgba(240, 245, 255, 255);
    public Color BackgroundColorEnd { get; set; } = Color.FromRgba(230, 240, 255, 255);
    public Color CardBackgroundColor { get; set; } = Color.FromRgba(255, 255, 255, 255);
    public Color CardBorderColor { get; set; } = Color.FromRgba(220, 230, 245, 255);
    public Color CardShadowColor { get; set; } = Color.FromRgba(0, 0, 0, 15);
    public Color HeaderTextColor { get; set; } = Color.FromRgba(30, 40, 60, 255);
    public Color SubTextColor { get; set; } = Color.FromRgba(100, 110, 130, 255);
    public Color AccentColor { get; set; } = Color.FromRgba(59, 130, 246, 255);
    public Color SuccessColor { get; set; } = Color.FromRgba(34, 197, 94, 255);
    public Color OnlineIndicatorColor { get; set; } = Color.FromRgba(34, 197, 94, 255);
    public Color PlayerNameColor { get; set; } = Color.FromRgba(40, 50, 70, 255);
    public Color GroupTextColor { get; set; } = Color.FromRgba(120, 130, 150, 255);
    public Color DividerColor { get; set; } = Color.FromRgba(230, 235, 245, 255);
    public Color PlayerCardBgColor { get; set; } = Color.FromRgba(248, 250, 252, 255);
    public Color PlayerCardBorderColor { get; set; } = Color.FromRgba(235, 240, 245, 255);
    public Color AvatarBgColor { get; set; } = Color.FromRgba(230, 240, 255, 255);

    public int Padding { get; set; } = 50;
    public int CardPadding { get; set; } = 30;
    public int CardSpacing { get; set; } = 25;
    public int PlayerItemHeight { get; set; } = 75;
    public int PlayerItemSpacing { get; set; } = 12;
    public int AvatarSize { get; set; } = 44;
    public int ColumnsPerSection { get; set; } = 3;
    public int MaxPlayersPerColumn { get; set; } = 6;
    public int SectionSpacing { get; set; } = 40;

    public float TitleFontSize { get; set; } = 42;
    public float ServerNameFontSize { get; set; } = 28;
    public float StatsFontSize { get; set; } = 20;
    public float PlayerNameFontSize { get; set; } = 18;
    public float GroupFontSize { get; set; } = 14;
    public float HeaderStatsFontSize { get; set; } = 36;

    private ServerOnlineBuilder? _currentBuilder;
    private int _totalWidth;
    private int _totalHeight;

    public byte[] Generate(ServerOnlineBuilder builder)
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

        int columnWidth = 320;
        int sectionWidth = (columnWidth * ColumnsPerSection) + (CardPadding * 2) + ((ColumnsPerSection - 1) * 20);

        int width = sectionWidth + (Padding * 2);

        int headerHeight = 180;
        int currentY = headerHeight + Padding;

        foreach (var section in _currentBuilder.Sections)
        {
            int sectionHeaderHeight = 60;
            int sectionContentHeight;
            
            if (section.Players.Count == 0)
            {
                sectionContentHeight = sectionHeaderHeight + 80;
            }
            else
            {
                int playerRows = (int)Math.Ceiling((double)section.Players.Count / ColumnsPerSection);
                sectionContentHeight = sectionHeaderHeight + (playerRows * PlayerItemHeight) + ((playerRows - 1) * PlayerItemSpacing);
            }

            int sectionHeight = sectionContentHeight + (CardPadding * 2);
            currentY += sectionHeight + SectionSpacing;
        }

        int height = currentY + Padding;

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

        var circleBrush1 = new SolidBrush(Color.FromRgba(59, 130, 246, 20));
        ctx.Fill(circleBrush1, new EllipsePolygon(-50, -50, 200, 200));

        var circleBrush2 = new SolidBrush(Color.FromRgba(139, 92, 246, 15));
        ctx.Fill(circleBrush2, new EllipsePolygon(width + 50, height - 100, 300, 300));
    }

    private void DrawContent(IImageProcessingContext ctx, int width, int height)
    {
        if (_currentBuilder == null) throw new InvalidOperationException("Builder not set");

        var titleFont = CreateFont(TitleFontSize, FontStyle.Bold);
        var statsFont = CreateFont(HeaderStatsFontSize, FontStyle.Bold);
        var subFont = CreateFont(StatsFontSize);

        float currentY = Padding + 20;

        DrawMainTitle(ctx, "服务器在线玩家", titleFont, currentY, width);
        currentY += TitleFontSize + 10;

        int totalOnline = _currentBuilder.Sections.Sum(s => s.OnlineCount);
        int totalMax = _currentBuilder.Sections.Sum(s => s.MaxCount);
        DrawHeaderStats(ctx, totalOnline, totalMax, statsFont, subFont, currentY, width);
        currentY += 100;
        ctx.DrawLine(DividerColor, 2, new PointF(Padding, currentY), new PointF(width - Padding, currentY));
        currentY += 30;
        foreach (var section in _currentBuilder.Sections)
        {
            currentY = DrawServerSection(ctx, section, (int)currentY, width);
            currentY += SectionSpacing;
        }
    }

    private void DrawMainTitle(IImageProcessingContext ctx, string title, Font font, float y, int width)
    {
        var titleOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(width / 2f, y)
        };
        ctx.DrawText(titleOptions, title, HeaderTextColor);
    }

    private void DrawHeaderStats(IImageProcessingContext ctx, int online, int max, Font statsFont, Font subFont, float y, int width)
    {
        string onlineText = $"{online}";
        string maxText = $"{max}";
        string separator = "/";

        var onlineSize = TextMeasurer.MeasureSize(onlineText, new TextOptions(statsFont));
        var separatorSize = TextMeasurer.MeasureSize(separator, new TextOptions(statsFont));
        var maxSize = TextMeasurer.MeasureSize(maxText, new TextOptions(statsFont));

        float totalWidth = onlineSize.Width + separatorSize.Width + maxSize.Width + 20;
        float startX = (width - totalWidth) / 2f;

        var onlineOptions = new RichTextOptions(statsFont)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Origin = new PointF(startX, y)
        };
        ctx.DrawText(onlineOptions, onlineText, SuccessColor);

        var sepOptions = new RichTextOptions(statsFont)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Origin = new PointF(startX + onlineSize.Width + 5, y)
        };
        ctx.DrawText(sepOptions, separator, SubTextColor);

        var maxOptions = new RichTextOptions(statsFont)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Origin = new PointF(startX + onlineSize.Width + separatorSize.Width + 15, y)
        };
        ctx.DrawText(maxOptions, maxText, HeaderTextColor);

        var labelOptions = new RichTextOptions(subFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new PointF(width / 2f, y + statsFont.Size + 5)
        };
        ctx.DrawText(labelOptions, "在线玩家", SubTextColor);
    }

    private float DrawServerSection(IImageProcessingContext ctx, ServerOnlineSection section, int y, int width)
    {
        var serverFont = CreateFont(ServerNameFontSize, FontStyle.Bold);
        var countFont = CreateFont(StatsFontSize);
        var playerNameFont = CreateFont(PlayerNameFontSize);
        var groupFont = CreateFont(GroupFontSize);

        int sectionWidth = width - (Padding * 2);
        int columnWidth = (sectionWidth - (CardPadding * 2) - ((ColumnsPerSection - 1) * 20)) / ColumnsPerSection;

        int sectionContentHeight;
        if (section.Players.Count == 0)
        {
            sectionContentHeight = 60 + 80; 
        }
        else
        {
            int playerRows = (int)Math.Ceiling((double)section.Players.Count / ColumnsPerSection);
            sectionContentHeight = 60 + (playerRows * PlayerItemHeight) + ((playerRows - 1) * PlayerItemSpacing);
        }
        int sectionHeight = sectionContentHeight + (CardPadding * 2);

        ctx.DrawRoundedRectangle(Padding + 2, y + 2, sectionWidth, sectionHeight, 16, CardShadowColor.ToPixel<Rgba32>());
        ctx.DrawRoundedRectangle(Padding, y, sectionWidth, sectionHeight, 16, CardBackgroundColor.ToPixel<Rgba32>());
        ctx.DrawRoundedRectanglePath(Padding, y, sectionWidth, sectionHeight, 16, 1, CardBorderColor.ToPixel<Rgba32>());
        ctx.Fill(AccentColor, new RectangularPolygon(Padding, y + 20, 4, sectionHeight - 40));

        float contentY = y + CardPadding;

        var serverOptions = new RichTextOptions(serverFont)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Origin = new PointF(Padding + CardPadding + 10, contentY + 5)
        };
        ctx.DrawText(serverOptions, section.ServerName, HeaderTextColor);

        string countText = $"{section.OnlineCount}/{section.MaxCount}";
        var countOptions = new RichTextOptions(countFont)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Origin = new PointF(width - Padding - CardPadding, contentY + 10)
        };
        ctx.DrawText(countOptions, countText, SubTextColor);

        contentY += 50;
        if (section.Players.Count == 0)
        {
            var emptyFont = CreateFont(PlayerNameFontSize);
            var emptyOptions = new RichTextOptions(emptyFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Origin = new PointF(width / 2f, contentY + 30)
            };
            ctx.DrawText(emptyOptions, "暂无在线玩家", SubTextColor);
        }
        else
        {
            var columns = new List<List<ServerOnlinePlayerItem>>();
            for (int i = 0; i < ColumnsPerSection; i++)
            {
                columns.Add(new List<ServerOnlinePlayerItem>());
            }

            for (int i = 0; i < section.Players.Count; i++)
            {
                int colIndex = i % ColumnsPerSection;
                columns[colIndex].Add(section.Players[i]);
            }

            int startX = Padding + CardPadding + 10;
            for (int col = 0; col < ColumnsPerSection; col++)
            {
                int colX = startX + (col * (columnWidth + 20));
                float colY = contentY;

                foreach (var player in columns[col])
                {
                    DrawPlayerItem(ctx, player, colX, (int)colY, columnWidth, playerNameFont, groupFont);
                    colY += PlayerItemHeight + PlayerItemSpacing;
                }
            }
        }

        return y + sectionHeight;
    }

    private void DrawPlayerItem(IImageProcessingContext ctx, ServerOnlinePlayerItem player, int x, int y, int width, Font nameFont, Font groupFont)
    {
        int cardPadding = 8;
        int cardHeight = PlayerItemHeight - PlayerItemSpacing;
        int innerX = x + cardPadding;
        int innerY = y + cardPadding / 2;
        int innerWidth = width - cardPadding * 2;
        int innerHeight = cardHeight - cardPadding;

        ctx.DrawRoundedRectangle(innerX, innerY, innerWidth, innerHeight, 10, PlayerCardBgColor.ToPixel<Rgba32>());
        ctx.DrawRoundedRectanglePath(innerX, innerY, innerWidth, innerHeight, 10, 1, PlayerCardBorderColor.ToPixel<Rgba32>());

        float indicatorX = innerX + 12;
        float indicatorY = innerY + innerHeight / 2f;
        ctx.Fill(Color.FromRgba(34, 197, 94, 40), new EllipsePolygon(indicatorX, indicatorY, 10, 10));
        ctx.Fill(OnlineIndicatorColor, new EllipsePolygon(indicatorX, indicatorY, 5, 5));

        int avatarX = innerX + 28;
        int avatarY = innerY + (innerHeight - AvatarSize) / 2;
        DrawPlayerAvatar(ctx, player, avatarX, avatarY, AvatarSize);

        int textX = avatarX + AvatarSize + 10;
        int textY = innerY + 16;

        var nameOptions = new RichTextOptions(nameFont)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Origin = new PointF(textX, textY)
        };
        ctx.DrawText(nameOptions, player.Name, PlayerNameColor);

        if (!string.IsNullOrEmpty(player.Group))
        {
            var groupOptions = new RichTextOptions(groupFont)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Origin = new PointF(textX, textY + 20)
            };
            ctx.DrawText(groupOptions, player.Group, GroupTextColor);
        }

        if (player.IsLogin)
        {
            var loginIconX = innerX + innerWidth - 20;
            var loginIconY = innerY + innerHeight / 2f;
            ctx.Fill(Color.FromRgba(100, 180, 100, 255), new EllipsePolygon(loginIconX, loginIconY, 6, 6));
        }
    }

    private static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
    {
        return CardRenderer.CreateFont(size, style);
    }

    private void DrawPlayerAvatar(IImageProcessingContext ctx, ServerOnlinePlayerItem player, int x, int y, int size)
    {
        ctx.Fill(Color.White, new EllipsePolygon(x + size / 2f, y + size / 2f, size / 2f + 2, size / 2f + 2));
        if (player.UserId.HasValue && player.UserId.Value > 0)
        {
            try
            {
                using var avatar = ImageUtility.GetAvatar(player.UserId.Value, size);
                DrawCircularAvatar(ctx, avatar, x, y, size);
                return;
            }
            catch
            {
            }
        }

        ctx.Fill(AvatarBgColor, new EllipsePolygon(x + size / 2f, y + size / 2f, size / 2f, size / 2f));

        string initial = !string.IsNullOrEmpty(player.Name) ? player.Name[0].ToString().ToUpper() : "?";
        var initialFont = CreateFont(16, FontStyle.Bold);
        var initialOptions = new RichTextOptions(initialFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new PointF(x + size / 2f, y + size / 2f)
        };
        ctx.DrawText(initialOptions, initial, AccentColor);
    }

    private static void DrawCircularAvatar(IImageProcessingContext ctx, Image<Rgba32> avatar, int x, int y, int size)
    {
        using var circularAvatar = avatar.CutCircles(size, 2);
        ctx.DrawImage(circularAvatar, new Point(x - 2, y - 2), 1f);
    }
}
