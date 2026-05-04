using System.Globalization;
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Vortex.Bot.Extension;
using Vortex.Protocol.Models;

namespace Vortex.Bot.Utility.Images;

public class ItemSlot
{
    public int NetId { get; set; }
    public int Prefix { get; set; }
    public int Stack { get; set; }
    public string Name { get; set; } = string.Empty;

    public ItemSlot() { }

    public ItemSlot(Item item)
    {
        NetId = item.NetId;
        Prefix = item.Prefix;
        Stack = item.Stack;
        Name = item.NetId.ToString(CultureInfo.InvariantCulture);
    }

    public bool IsEmpty => NetId == 0;
}

public class InventoryBuilder
{
    public List<ItemSlot> Inventory { get; set; } = [];
    public List<ItemSlot> Piggy { get; set; } = [];
    public List<ItemSlot> Safe { get; set; } = [];
    public List<ItemSlot> VoidVault { get; set; } = [];
    public List<ItemSlot> Forge { get; set; } = [];
    public List<ItemSlot> MiscEquip { get; set; } = [];
    public List<ItemSlot> MiscDye { get; set; } = [];
    public List<Suits> Loadouts { get; set; } = [];
    public ItemSlot? TrashItem { get; set; }

    public string PlayerName { get; set; } = "Player";
    public string ServerName { get; set; } = "Server";
    public long? AvatarUin { get; set; }

    public static InventoryBuilder Create() => new();

    public InventoryBuilder SetPlayerName(string name)
    {
        PlayerName = name;
        return this;
    }

    public InventoryBuilder SetServerName(string name)
    {
        ServerName = name;
        return this;
    }

    public InventoryBuilder SetAvatarUin(long? uin)
    {
        AvatarUin = uin;
        return this;
    }

    public byte[] Build()
    {
        return new InventoryGenerate().Generate(this);
    }
}

public class InventoryGenerate
{
    private const int MarginX = 48;
    private const int MarginY = 44;
    private const int BottomMargin = 56;
    private const int SectionGapY = 24;
    private const int ColumnGap = 18;

    private const int CardPadding = 18;
    private const int CardHeaderHeight = 42;
    private const int CardCornerRadius = 16;

    private const int SlotSize = 56;
    private const int SlotSpacing = 8;
    private const int NarrowSlotSize = 56;
    private const int NarrowSlotSpacing = 8;
    private const int AvatarSize = 46;

    private readonly Color BackgroundColor = Color.FromRgb(250, 247, 242);
    private readonly Color TitleColor = Color.FromRgb(42, 35, 28);
    private readonly Color BodyColor = Color.FromRgb(135, 124, 113);
    private readonly Color WatermarkColor = Color.FromRgb(170, 161, 151);
    private readonly Color AccentColor = Color.FromRgb(218, 153, 119);
    private readonly Color AvatarColor = Color.FromRgb(242, 199, 160);
    private readonly Color AvatarTextColor = Color.FromRgb(113, 75, 46);

    private readonly Color SlotEmptyColor = Color.FromRgb(255, 255, 255);
    private readonly Color SlotFilledColor = Color.FromRgb(255, 255, 255);
    private readonly Color SlotBorderColor = Color.FromRgb(232, 225, 216);

    private readonly Color MainPanelColor = Color.FromRgb(244, 238, 229);
    private readonly Color StoragePanelColor = Color.FromRgb(244, 238, 229);
    private readonly Color StoragePanelBorderColor = Color.FromRgb(229, 222, 212);
    private readonly Color LoadoutPanelColor = Color.FromRgb(244, 238, 229);
    private readonly Color LoadoutBorderColor = Color.FromRgb(229, 222, 212);
    private readonly Color MiscPanelColor = Color.FromRgb(244, 238, 229);
    private readonly Color MiscBorderColor = Color.FromRgb(229, 222, 212);
    private readonly Color CurrencyPanelColor = Color.FromRgb(244, 238, 229);
    private readonly Color CurrencyBorderColor = Color.FromRgb(229, 222, 212);

    private int _canvasHeight;

    public byte[] Generate(InventoryBuilder builder)
    {
        var (canvasWidth, canvasHeight) = CalculateCanvasSize(builder);
        _canvasHeight = canvasHeight;

        using var image = new Image<Rgba32>(canvasWidth, canvasHeight);
        DrawBackground(image, canvasWidth, canvasHeight);
        image.Mutate(ctx => DrawContent(ctx, builder, canvasWidth));
        return image.ToBytesAsync().Result;
    }

    private (int Width, int Height) CalculateCanvasSize(InventoryBuilder builder)
    {
        var mainPanelWidth = GetPanelWidth(10, SlotSize, SlotSpacing);
        var mainPanelHeight = GetPanelHeight(5, SlotSize, SlotSpacing);
        var storagePanelWidth = GetPanelWidth(10, SlotSize, SlotSpacing);
        var storagePanelHeight = GetPanelHeight(4, SlotSize, SlotSpacing);
        var loadoutPanelWidth = GetPanelWidth(3, NarrowSlotSize, NarrowSlotSpacing);
        var loadoutPanelHeight = GetPanelHeight(10, NarrowSlotSize, NarrowSlotSpacing);
        var narrowPanelWidth = GetPanelWidth(1, NarrowSlotSize, NarrowSlotSpacing);
        var miscPanelHeight = GetPanelHeight(5, NarrowSlotSize, NarrowSlotSpacing);
        var currencyPanelHeight = GetPanelHeight(4, NarrowSlotSize, NarrowSlotSpacing);
        var trashPanelHeight = GetPanelHeight(1, NarrowSlotSize, NarrowSlotSpacing);

        var loadoutCount = Math.Max(3, builder.Loadouts.Count);
        var topRowWidth = mainPanelWidth + ColumnGap + storagePanelWidth + ColumnGap + storagePanelWidth;
        var loadoutBlockWidth = loadoutCount * loadoutPanelWidth + (loadoutCount - 1) * ColumnGap;
        var miscClusterWidth = 4 * narrowPanelWidth + 3 * ColumnGap;
        var bottomRowWidth = loadoutBlockWidth + ColumnGap + miscClusterWidth + ColumnGap + storagePanelWidth;

        var topRowHeight = Math.Max(mainPanelHeight, storagePanelHeight);
        var miscClusterHeight = Math.Max(miscPanelHeight, currencyPanelHeight) + SectionGapY + trashPanelHeight;
        var rightColumnHeight = storagePanelHeight * 2 + SectionGapY;
        var bottomRowHeight = Math.Max(loadoutPanelHeight, Math.Max(miscClusterHeight, rightColumnHeight));

        var width = Math.Max(topRowWidth, bottomRowWidth) + MarginX * 2;
        var height = MarginY + 170 + SectionGapY + topRowHeight + SectionGapY + bottomRowHeight + BottomMargin;

        return (Math.Max(width, 2050), Math.Max(height, 1380));
    }

    private void DrawBackground(Image<Rgba32> image, int width, int height)
    {
        image.Mutate(ctx =>
        {
            ctx.Fill(BackgroundColor);

            var brush = new LinearGradientBrush(
                new PointF(0, 0),
                new PointF(0, height),
                GradientRepetitionMode.None,
                new ColorStop(0f, Color.FromRgb(252, 249, 244)),
                new ColorStop(0.55f, Color.FromRgb(248, 244, 238)),
                new ColorStop(1f, Color.FromRgb(244, 239, 231))
            );

            ctx.Fill(brush);
        });
    }

    private void DrawContent(IImageProcessingContext ctx, InventoryBuilder builder, int canvasWidth)
    {
        var fontFamily = CardRenderer.GetFontFamily();
        var titleFont = fontFamily.CreateFont(58, FontStyle.Bold);
        var subtitleFont = fontFamily.CreateFont(15, FontStyle.Regular);
        var nameFont = fontFamily.CreateFont(18, FontStyle.Bold);
        var metaFont = fontFamily.CreateFont(14, FontStyle.Regular);
        var sectionFont = fontFamily.CreateFont(15, FontStyle.Bold);
        var countFont = fontFamily.CreateFont(14, FontStyle.Regular);
        var slotIndexFont = fontFamily.CreateFont(10, FontStyle.Regular);
        var stackFont = fontFamily.CreateFont(11, FontStyle.Bold);

        var y = DrawHeader(ctx, builder, canvasWidth, titleFont, subtitleFont, nameFont, metaFont);
        y += SectionGapY;

        y = DrawTopRow(ctx, builder, canvasWidth, y, sectionFont, countFont, slotIndexFont, stackFont);
        y += SectionGapY;

        DrawBottomRow(ctx, builder, canvasWidth, y, sectionFont, countFont, slotIndexFont, stackFont);
        DrawSignature(ctx, canvasWidth);
    }

    private int DrawHeader(
        IImageProcessingContext ctx,
        InventoryBuilder builder,
        int canvasWidth,
        Font titleFont,
        Font subtitleFont,
        Font nameFont,
        Font metaFont)
    {
        var accentX = MarginX;
        var accentY = MarginY;

        ctx.Fill(AccentColor, new RectangleF(accentX, accentY, 64, 4));
        ctx.DrawText(builder.ServerName, subtitleFont, BodyColor, new PointF(accentX, accentY + 18));
        ctx.DrawText("\u80cc\u5305", titleFont, TitleColor, new PointF(accentX, accentY + 38));

        var avatarX = accentX;
        var avatarY = accentY + 112;
        DrawProfileAvatar(ctx, builder.AvatarUin, builder.PlayerName, nameFont, avatarX, avatarY);

        var textX = avatarX + AvatarSize + 16;
        ctx.DrawText(builder.PlayerName, nameFont, TitleColor, new PointF(textX, avatarY - 2));
        ctx.DrawText(builder.ServerName, metaFont, BodyColor, new PointF(textX, avatarY + 22));

        var timestampText = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        var timestampSize = TextMeasurer.MeasureSize(timestampText, new TextOptions(metaFont));
        ctx.DrawText(timestampText, metaFont, BodyColor, new PointF(canvasWidth - MarginX - timestampSize.Width, avatarY + 10));

        return avatarY + AvatarSize + 6;
    }

    private int DrawTopRow(
        IImageProcessingContext ctx,
        InventoryBuilder builder,
        int canvasWidth,
        int y,
        Font titleFont,
        Font countFont,
        Font slotIndexFont,
        Font stackFont)
    {
        var mainPanelWidth = GetPanelWidth(10, SlotSize, SlotSpacing);
        var mainPanelHeight = GetPanelHeight(5, SlotSize, SlotSpacing);
        var storagePanelWidth = GetPanelWidth(10, SlotSize, SlotSpacing);
        var storagePanelHeight = GetPanelHeight(4, SlotSize, SlotSpacing);

        var totalWidth = mainPanelWidth + ColumnGap + storagePanelWidth + ColumnGap + storagePanelWidth;
        var startX = (canvasWidth - totalWidth) / 2;

        var mainItems = SliceSlots(builder.Inventory, 0, 50);
        DrawPanel(ctx, startX, y, 10, 5, SlotSize, SlotSpacing, "\u4e3b\u80cc\u5305", $"{CountFilled(mainItems)}/50", titleFont, countFont, MainPanelColor, StoragePanelBorderColor);
        DrawGrid(ctx, mainItems, startX + CardPadding, y + CardHeaderHeight + CardPadding, 10, 5, SlotSize, SlotSpacing, slotIndexFont, stackFont, 0);

        var piggyX = startX + mainPanelWidth + ColumnGap;
        DrawPanel(ctx, piggyX, y, 10, 4, SlotSize, SlotSpacing, "\u732a\u732a\u50a8\u94b1\u7f50", $"{CountFilled(builder.Piggy)}/40", titleFont, countFont, StoragePanelColor, StoragePanelBorderColor);
        DrawGrid(ctx, builder.Piggy, piggyX + CardPadding, y + CardHeaderHeight + CardPadding, 10, 4, SlotSize, SlotSpacing, slotIndexFont, stackFont, 99);

        var safeX = piggyX + storagePanelWidth + ColumnGap;
        DrawPanel(ctx, safeX, y, 10, 4, SlotSize, SlotSpacing, "\u4fdd\u9669\u7bb1", $"{CountFilled(builder.Safe)}/40", titleFont, countFont, StoragePanelColor, StoragePanelBorderColor);
        DrawGrid(ctx, builder.Safe, safeX + CardPadding, y + CardHeaderHeight + CardPadding, 10, 4, SlotSize, SlotSpacing, slotIndexFont, stackFont, 139);

        return y + Math.Max(mainPanelHeight, storagePanelHeight);
    }

    private void DrawBottomRow(
        IImageProcessingContext ctx,
        InventoryBuilder builder,
        int canvasWidth,
        int y,
        Font titleFont,
        Font countFont,
        Font slotIndexFont,
        Font stackFont)
    {
        var loadoutCount = Math.Max(3, builder.Loadouts.Count);
        var loadoutPanelWidth = GetPanelWidth(3, NarrowSlotSize, NarrowSlotSpacing);
        var loadoutBlockWidth = loadoutCount * loadoutPanelWidth + (loadoutCount - 1) * ColumnGap;
        var miscPanelWidth = GetPanelWidth(1, NarrowSlotSize, NarrowSlotSpacing);
        var miscClusterWidth = 4 * miscPanelWidth + 3 * ColumnGap;
        var storagePanelWidth = GetPanelWidth(10, SlotSize, SlotSpacing);
        var totalWidth = loadoutBlockWidth + ColumnGap + miscClusterWidth + ColumnGap + storagePanelWidth;
        var startX = (canvasWidth - totalWidth) / 2;

        var loadoutBases = new[] { 59, 260, 290 };
        for (var i = 0; i < loadoutCount; i++)
        {
            var panelX = startX + i * (loadoutPanelWidth + ColumnGap);
            var loadout = i < builder.Loadouts.Count ? builder.Loadouts[i] : new Suits();
            var title = GetLoadoutTitle(i);
            var baseIndex = i < loadoutBases.Length ? loadoutBases[i] : 290 + (i - 2) * 30;
            DrawLoadoutPanel(ctx, loadout, panelX, y, title, baseIndex, titleFont, countFont, slotIndexFont, stackFont);
        }

        var miscX = startX + loadoutBlockWidth + ColumnGap;
        DrawPanel(ctx, miscX, y, 1, 5, NarrowSlotSize, NarrowSlotSpacing, "\u9970\u54c1", $"{CountFilled(builder.MiscEquip)}/5", titleFont, countFont, MiscPanelColor, MiscBorderColor);
        DrawGrid(ctx, builder.MiscEquip, miscX + CardPadding, y + CardHeaderHeight + CardPadding, 1, 5, NarrowSlotSize, NarrowSlotSpacing, slotIndexFont, stackFont, 89);

        var dyeX = miscX + miscPanelWidth + ColumnGap;
        DrawPanel(ctx, dyeX, y, 1, 5, NarrowSlotSize, NarrowSlotSpacing, "\u67d3\u6599", $"{CountFilled(builder.MiscDye)}/5", titleFont, countFont, MiscPanelColor, MiscBorderColor);
        DrawGrid(ctx, builder.MiscDye, dyeX + CardPadding, y + CardHeaderHeight + CardPadding, 1, 5, NarrowSlotSize, NarrowSlotSpacing, slotIndexFont, stackFont, 94);

        var coinsX = dyeX + miscPanelWidth + ColumnGap;
        var coinItems = SliceSlots(builder.Inventory, 50, 4);
        DrawPanel(ctx, coinsX, y, 1, 4, NarrowSlotSize, NarrowSlotSpacing, "\u8d27\u5e01", $"{CountFilled(coinItems)}/4", titleFont, countFont, CurrencyPanelColor, CurrencyBorderColor);
        DrawGrid(ctx, coinItems, coinsX + CardPadding, y + CardHeaderHeight + CardPadding, 1, 4, NarrowSlotSize, NarrowSlotSpacing, slotIndexFont, stackFont, 50);

        var ammoX = coinsX + miscPanelWidth + ColumnGap;
        var ammoItems = SliceSlots(builder.Inventory, 54, 4);
        DrawPanel(ctx, ammoX, y, 1, 4, NarrowSlotSize, NarrowSlotSpacing, "\u5f39\u836f", $"{CountFilled(ammoItems)}/4", titleFont, countFont, CurrencyPanelColor, CurrencyBorderColor);
        DrawGrid(ctx, ammoItems, ammoX + CardPadding, y + CardHeaderHeight + CardPadding, 1, 4, NarrowSlotSize, NarrowSlotSpacing, slotIndexFont, stackFont, 54);

        var trashX = miscX + (miscPanelWidth * 2 + ColumnGap - miscPanelWidth) / 2;
        var trashY = y + GetPanelHeight(5, NarrowSlotSize, NarrowSlotSpacing) + SectionGapY;
        var trashCount = builder.TrashItem is { IsEmpty: false } ? 1 : 0;
        DrawPanel(ctx, trashX, trashY, 1, 1, NarrowSlotSize, NarrowSlotSpacing, "\u5783\u573e\u6876", $"{trashCount}/1", titleFont, countFont, CurrencyPanelColor, StoragePanelBorderColor);
        DrawGrid(ctx, builder.TrashItem is null ? [] : [builder.TrashItem], trashX + CardPadding, trashY + CardHeaderHeight + CardPadding, 1, 1, NarrowSlotSize, NarrowSlotSpacing, slotIndexFont, stackFont, 58);

        var rightX = ammoX + miscPanelWidth + ColumnGap;
        DrawPanel(ctx, rightX, y, 10, 4, SlotSize, SlotSpacing, "\u62a4\u536b\u7194\u7089", $"{CountFilled(builder.Forge)}/40", titleFont, countFont, StoragePanelColor, StoragePanelBorderColor);
        DrawGrid(ctx, builder.Forge, rightX + CardPadding, y + CardHeaderHeight + CardPadding, 10, 4, SlotSize, SlotSpacing, slotIndexFont, stackFont, 179);

        var voidY = y + GetPanelHeight(4, SlotSize, SlotSpacing) + SectionGapY;
        DrawPanel(ctx, rightX, voidY, 10, 4, SlotSize, SlotSpacing, "\u865a\u7a7a\u888b", $"{CountFilled(builder.VoidVault)}/40", titleFont, countFont, StoragePanelColor, StoragePanelBorderColor);
        DrawGrid(ctx, builder.VoidVault, rightX + CardPadding, voidY + CardHeaderHeight + CardPadding, 10, 4, SlotSize, SlotSpacing, slotIndexFont, stackFont, 219);
    }

    private void DrawLoadoutPanel(
        IImageProcessingContext ctx,
        Suits loadout,
        int x,
        int y,
        string title,
        int baseIndex,
        Font titleFont,
        Font countFont,
        Font slotIndexFont,
        Font stackFont)
    {
        var armor = loadout.Armor ?? Array.Empty<Item>();
        var dyes = loadout.Dye ?? Array.Empty<Item>();

        var visibleArmor = armor.Take(10).Select(item => new ItemSlot(item)).ToList();
        var vanityArmor = armor.Skip(10).Take(10).Select(item => new ItemSlot(item)).ToList();
        var dyeItems = dyes.Take(10).Select(item => new ItemSlot(item)).ToList();

        var filledCount = CountFilled(visibleArmor) + CountFilled(vanityArmor) + CountFilled(dyeItems);
        DrawPanel(ctx, x, y, 3, 10, NarrowSlotSize, NarrowSlotSpacing, title, $"{filledCount}/30", titleFont, countFont, LoadoutPanelColor, LoadoutBorderColor);

        var innerX = x + CardPadding;
        var innerY = y + CardHeaderHeight + CardPadding;
        DrawColumn(ctx, dyeItems, innerX, innerY, NarrowSlotSize, NarrowSlotSpacing, 10, slotIndexFont, stackFont, baseIndex + 20);
        DrawColumn(ctx, vanityArmor, innerX + NarrowSlotSize + NarrowSlotSpacing, innerY, NarrowSlotSize, NarrowSlotSpacing, 10, slotIndexFont, stackFont, baseIndex + 10);
        DrawColumn(ctx, visibleArmor, innerX + (NarrowSlotSize + NarrowSlotSpacing) * 2, innerY, NarrowSlotSize, NarrowSlotSpacing, 10, slotIndexFont, stackFont, baseIndex);
    }

    private void DrawPanel(
        IImageProcessingContext ctx,
        int x,
        int y,
        int cols,
        int rows,
        int slotSize,
        int spacing,
        string title,
        string countText,
        Font titleFont,
        Font countFont,
        Color fillColor,
        Color borderColor)
    {
        var width = cols * slotSize + Math.Max(0, cols - 1) * spacing + CardPadding * 2;
        var height = rows * slotSize + Math.Max(0, rows - 1) * spacing + CardHeaderHeight + CardPadding * 2;

        ctx.DrawRoundedRectangle(x, y, width, height, CardCornerRadius, fillColor);
        ctx.DrawRoundedRectanglePath(x, y, width, height, CardCornerRadius, 1, borderColor);

        ctx.DrawText(title, titleFont, TitleColor, new PointF(x + 16, y + 13));

        if (!string.IsNullOrWhiteSpace(countText))
        {
            var size = TextMeasurer.MeasureSize(countText, new TextOptions(countFont));
            ctx.DrawText(countText, countFont, BodyColor, new PointF(x + width - size.Width - 16, y + 14));
        }
    }

    private void DrawGrid(
        IImageProcessingContext ctx,
        IReadOnlyList<ItemSlot> items,
        int startX,
        int startY,
        int cols,
        int rows,
        int slotSize,
        int spacing,
        Font slotIndexFont,
        Font stackFont,
        int baseIndex)
    {
        var maxCount = cols * rows;
        for (var i = 0; i < maxCount; i++)
        {
            var row = i / cols;
            var col = i % cols;
            var slot = i < items.Count ? items[i] : new ItemSlot();
            var x = startX + col * (slotSize + spacing);
            var y = startY + row * (slotSize + spacing);
            DrawSlot(ctx, slot, x, y, slotSize, slotIndexFont, stackFont, baseIndex + i);
        }
    }

    private void DrawColumn(
        IImageProcessingContext ctx,
        IReadOnlyList<ItemSlot> items,
        int startX,
        int startY,
        int slotSize,
        int spacing,
        int rows,
        Font slotIndexFont,
        Font stackFont,
        int baseIndex)
    {
        for (var i = 0; i < rows; i++)
        {
            var slot = i < items.Count ? items[i] : new ItemSlot();
            DrawSlot(ctx, slot, startX, startY + i * (slotSize + spacing), slotSize, slotIndexFont, stackFont, baseIndex + i);
        }
    }

    private void DrawSlot(
        IImageProcessingContext ctx,
        ItemSlot slot,
        int x,
        int y,
        int slotSize,
        Font slotIndexFont,
        Font stackFont,
        int index)
    {
        ctx.DrawRoundedRectangle(x, y, slotSize, slotSize, 10, slot.IsEmpty ? SlotEmptyColor : SlotFilledColor);
        ctx.DrawRoundedRectanglePath(x, y, slotSize, slotSize, 10, 1, SlotBorderColor);

        var indexText = index.ToString(CultureInfo.InvariantCulture);
        ctx.DrawText(indexText, slotIndexFont, BodyColor, new PointF(x + 5, y + 4));

        if (slot.IsEmpty)
            return;

        DrawItemIcon(ctx, slot.NetId, x, y, slotSize);

        if (slot.Stack > 1)
        {
            var stackText = slot.Stack.ToString(CultureInfo.InvariantCulture);
            var size = TextMeasurer.MeasureSize(stackText, new TextOptions(stackFont));
            ctx.DrawText(stackText, stackFont, TitleColor, new PointF(x + slotSize - size.Width - 6, y + slotSize - size.Height - 5));
        }
    }

    private void DrawItemIcon(IImageProcessingContext ctx, int netId, int x, int y, int boxSize)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "Item", $"{netId}.png");
        if (!File.Exists(path))
            return;

        try
        {
            using var image = Image.Load<Rgba32>(path);
            // Reserve space for the slot index at top-left and stack count at bottom-right
            // so unusually tall or wide item sprites don't collide with text overlays.
            const float horizontalPadding = 6f;
            const float topPadding = 12f;
            const float bottomPadding = 8f;

            var availableWidth = Math.Max(1f, boxSize - horizontalPadding * 2);
            var availableHeight = Math.Max(1f, boxSize - topPadding - bottomPadding);
            var scale = Math.Min(availableWidth / image.Width, availableHeight / image.Height);
            var width = Math.Max(1, (int)(image.Width * scale));
            var height = Math.Max(1, (int)(image.Height * scale));

            image.Mutate(img => img.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Sampler = KnownResamplers.NearestNeighbor,
                Mode = ResizeMode.Stretch
            }));

            var drawX = x + horizontalPadding + (availableWidth - width) / 2f;
            var drawY = y + topPadding + (availableHeight - height) / 2f;
            ctx.DrawImage(image, new Point((int)MathF.Round(drawX), (int)MathF.Round(drawY)), 1f);
        }
        catch
        {
        }
    }

    private void DrawProfileAvatar(IImageProcessingContext ctx, long? avatarUin, string playerName, Font font, int x, int y)
    {
        ctx.DrawRoundedRectangle(x - 2, y - 2, AvatarSize + 4, AvatarSize + 4, (AvatarSize + 4) / 2f, Color.White);

        if (avatarUin is > 0)
        {
            try
            {
                using var avatar = ImageUtility.GetAvatar(avatarUin.Value, AvatarSize);
                ctx.DrawImage(avatar, new Point(x, y), 1f);
                return;
            }
            catch
            {
            }
        }

        var initial = string.IsNullOrWhiteSpace(playerName) ? "V" : playerName[..1].ToUpperInvariant();

        ctx.DrawRoundedRectangle(x, y, AvatarSize, AvatarSize, AvatarSize / 2f, AvatarColor);

        var size = TextMeasurer.MeasureSize(initial, new TextOptions(font));
        var drawX = x + (AvatarSize - size.Width) / 2;
        var drawY = y + (AvatarSize - size.Height) / 2 - 1;
        ctx.DrawText(initial, font, AvatarTextColor, new PointF(drawX, drawY));
    }

    private void DrawSignature(IImageProcessingContext ctx, int canvasWidth)
    {
        var font = CardRenderer.GetFontFamily().CreateFont(16, FontStyle.Regular);
        const string signature = "Generated by Vortex.Bot";

        var size = TextMeasurer.MeasureSize(signature, new TextOptions(font));
        var x = canvasWidth - MarginX - size.Width;
        var y = _canvasHeight - size.Height - 18;

        ctx.DrawText(signature, font, WatermarkColor, new PointF(x, y));
    }

    private static string GetLoadoutTitle(int index)
    {
        return index switch
        {
            0 => "\u7b2c\u4e00\u5957\u88c5\u5907/\u9970\u54c1/\u67d3\u6599",
            1 => "\u7b2c\u4e8c\u5957\u88c5\u5907/\u9970\u54c1/\u67d3\u6599",
            2 => "\u7b2c\u4e09\u5957\u88c5\u5907/\u9970\u54c1/\u67d3\u6599",
            _ => $"\u7b2c{index + 1}\u5957\u88c5\u5907/\u9970\u54c1/\u67d3\u6599"
        };
    }

    private static int CountFilled(IEnumerable<ItemSlot> items)
    {
        return items.Count(item => !item.IsEmpty);
    }

    private static List<ItemSlot> SliceSlots(IReadOnlyList<ItemSlot> items, int start, int count)
    {
        var result = new List<ItemSlot>(count);
        for (var i = 0; i < count && start + i < items.Count; i++)
        {
            result.Add(items[start + i]);
        }

        return result;
    }

    private static int GetPanelWidth(int cols, int slotSize, int spacing)
    {
        return cols * slotSize + Math.Max(0, cols - 1) * spacing + CardPadding * 2;
    }

    private static int GetPanelHeight(int rows, int slotSize, int spacing)
    {
        return rows * slotSize + Math.Max(0, rows - 1) * spacing + CardHeaderHeight + CardPadding * 2;
    }
}

public static class InventoryGenerateExtensions
{
    public static InventoryBuilder FromPlayerData(PlayerData data, string serverName, long? avatarUin = null)
    {
        var builder = InventoryBuilder.Create()
            .SetPlayerName(data.Username)
            .SetServerName(serverName)
            .SetAvatarUin(avatarUin);

        if (data.Inventory?.Length > 0)
            builder.Inventory = [.. data.Inventory.Select(item => new ItemSlot(item))];

        if (data.TrashItem?.Length > 0 && data.TrashItem[0].NetId != 0)
            builder.TrashItem = new ItemSlot(data.TrashItem[0]);

        if (data.Piggy?.Length > 0)
            builder.Piggy = [.. data.Piggy.Select(item => new ItemSlot(item))];

        if (data.Safe?.Length > 0)
            builder.Safe = [.. data.Safe.Select(item => new ItemSlot(item))];

        if (data.VoidVault?.Length > 0)
            builder.VoidVault = [.. data.VoidVault.Select(item => new ItemSlot(item))];

        if (data.Forge?.Length > 0)
            builder.Forge = [.. data.Forge.Select(item => new ItemSlot(item))];

        if (data.MiscEquip?.Length > 0)
            builder.MiscEquip = [.. data.MiscEquip.Select(item => new ItemSlot(item))];

        if (data.MiscDye?.Length > 0)
            builder.MiscDye = [.. data.MiscDye.Select(item => new ItemSlot(item))];

        if (data.Loadout?.Length > 0)
            builder.Loadouts = [.. data.Loadout];

        return builder;
    }
}
