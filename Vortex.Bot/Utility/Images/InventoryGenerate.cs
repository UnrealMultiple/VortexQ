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
        Name = item.NetId.ToString();
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

    public byte[] Build()
    {
        return new InventoryGenerate().Generate(this);
    }
}

public class InventoryGenerate
{
    public int SlotSize { get; set; } = 70;
    public int SlotSpacing { get; set; } = 4;
    public int CardPadding { get; set; } = 10;
    public int CardCornerRadius { get; set; } = 8;
    public int RegionGapX { get; set; } = 40;
    public int RegionGapY { get; set; } = 70;

    public Color BackgroundColor { get; set; } = Color.FromRgb(245, 247, 250);
    public Color CardColor { get; set; } = Color.FromRgb(255, 255, 255);
    public Color CardBorderColor { get; set; } = Color.FromRgb(220, 225, 235);
    public Color CardShadowColor { get; set; } = Color.FromRgba(0, 0, 0, 25);
    public Color SlotEmptyColor { get; set; } = Color.FromRgb(235, 238, 242);
    public Color SlotBorderColor { get; set; } = Color.FromRgb(210, 215, 225);
    public Color SlotFilledColor { get; set; } = Color.FromRgb(245, 247, 250);
    public Color TitleColor { get; set; } = Color.FromRgb(50, 55, 70);
    public Color RegionTitleColor { get; set; } = Color.FromRgb(80, 85, 100);
    public Color ItemCountColor { get; set; } = Color.FromRgb(255, 255, 255);
    public Color ItemCountBackgroundColor { get; set; } = Color.FromRgba(0, 0, 0, 170);

    public Color MainInventoryHeaderColor { get; set; } = Color.FromRgb(100, 149, 237);
    public Color CoinAmmoHeaderColor { get; set; } = Color.FromRgb(255, 193, 7);
    public Color PiggyHeaderColor { get; set; } = Color.FromRgb(255, 112, 67);
    public Color SafeHeaderColor { get; set; } = Color.FromRgb(81, 181, 165);
    public Color VoidVaultHeaderColor { get; set; } = Color.FromRgb(126, 87, 194);
    public Color ForgeHeaderColor { get; set; } = Color.FromRgb(120, 144, 156);
    public Color LoadoutHeaderColor { get; set; } = Color.FromRgb(77, 182, 172);

    public int ItemIconPadding { get; set; } = 8;

    public float TitleFontSize { get; set; } = 34;
    public float RegionTitleFontSize { get; set; } = 28;
    public float ItemCountFontSize { get; set; } = 18;
    public int CardHeaderHeight { get; set; } = 38;

    // Loadout
    public int TileSlotSize { get; set; } = 70;
    public int TileSlotSpacing { get; set; } = 4;
    public int TileCardPadding { get; set; } = 10;
    public int TileGapX { get; set; } = 220;

    // TileCard
    public int TitleCardWidth { get; set; } = 320;
    public int TitleCardHeight { get; set; } = 95;
    public int TitleCardHeaderHeight { get; set; } = 38;
    public float TitleCardHeaderFontSize { get; set; } = 24;
    public float TitleCardPlayerNameFontSize { get; set; } = 36;
    public float TitleCardServerNameFontSize { get; set; } = 14;
    public Color TitleCardHeaderColor { get; set; } = Color.FromRgb(135, 93, 93);
    public int TitleCardYOffset { get; set; } = -20;
    public int TitleCardBottomGap { get; set; } = 90;

    private const int MarginX = 50;
    private const int MarginY = 80;
    private const int BottomMargin = 30;
    private const int TitleHeight = 70;

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
        var mainWidth = 10 * SlotSize + 9 * SlotSpacing;
        var mainHeight = 5 * SlotSize + 4 * SlotSpacing;

        var coinWidth = 2 * SlotSize + 1 * SlotSpacing;
        var coinHeight = 4 * SlotSize + 3 * SlotSpacing;

        var storageWidth = 10 * SlotSize + 9 * SlotSpacing;
        var storageHeight = 4 * SlotSize + 3 * SlotSpacing;

        var row1Height = Math.Max(mainHeight, coinHeight) + CardHeaderHeight;
        var row2Height = storageHeight + CardHeaderHeight;
        var row3Height = storageHeight + CardHeaderHeight;

        var loadoutHeight = 10 * TileSlotSize + 9 * TileSlotSpacing + CardHeaderHeight;
        var totalHeight = MarginY + TitleHeight + row1Height + RegionGapY + row2Height + RegionGapY + row3Height + RegionGapY + loadoutHeight + BottomMargin;
        totalHeight += 4 * 25;
        var row1Width = mainWidth + RegionGapX + coinWidth;
        var row2Width = storageWidth + RegionGapX + storageWidth;
        var row3Width = storageWidth + RegionGapX + storageWidth;
        var row4Width = 3 * (3 * TileSlotSize + 2 * TileSlotSpacing) + 2 * TileGapX;
        var maxWidth = Math.Max(Math.Max(row1Width, row2Width), Math.Max(row3Width, row4Width));
        var totalWidth = maxWidth + MarginX * 2;

        return (totalWidth, totalHeight);
    }

    private void DrawBackground(Image<Rgba32> image, int width, int height)
    {
        image.Mutate(ctx =>
        {
            ctx.Fill(BackgroundColor);

            var gradientBrush = new LinearGradientBrush(
                new PointF(0, 0),
                new PointF(0, height),
                GradientRepetitionMode.None,
                new ColorStop(0, Color.FromRgb(230, 235, 245)),
                new ColorStop(0.4f, Color.FromRgb(240, 243, 248)),
                new ColorStop(1, Color.FromRgb(245, 247, 250))
            );
            ctx.Fill(gradientBrush);
        });
    }

    private int DrawTitleCard(IImageProcessingContext ctx, InventoryBuilder builder, int canvasWidth, Font titleFont)
    {
        var serverValue = builder.ServerName;
        var playerValue = builder.PlayerName;

        var fontFamily = CardRenderer.GetFontFamily();
        var largeTitleFont = fontFamily.CreateFont(TitleCardPlayerNameFontSize, FontStyle.Bold);
        var smallFont = fontFamily.CreateFont(TitleCardServerNameFontSize);
        var infoFont = fontFamily.CreateFont(TitleCardHeaderFontSize);

        var playerNameSize = TextMeasurer.MeasureSize(playerValue, new TextOptions(largeTitleFont));
        var serverSize = TextMeasurer.MeasureSize(serverValue, new TextOptions(smallFont));
        var cardWidth = Math.Max(Math.Max((int)playerNameSize.Width + 60, (int)serverSize.Width + 60), TitleCardWidth);
        
        var headerHeight = TitleCardHeaderHeight;
        var contentHeight = headerHeight + 10 + playerNameSize.Height + 6 + serverSize.Height + 10;
        var cardHeight = Math.Max(TitleCardHeight, (int)contentHeight);
        
        var cardX = (canvasWidth - cardWidth) / 2;
        var cardY = MarginY + TitleCardYOffset;

        ctx.DrawRoundedRectangle(cardX + 2, cardY + 2, cardWidth, cardHeight, CardCornerRadius, CardShadowColor);
        ctx.DrawRoundedRectangle(cardX, cardY, cardWidth, cardHeight, CardCornerRadius, CardColor);
        ctx.DrawRoundedRectanglePath(cardX, cardY, cardWidth, cardHeight, CardCornerRadius, 1, CardBorderColor);

        var headerColor = TitleCardHeaderColor;
        ctx.DrawRoundedRectangle(cardX, cardY, cardWidth, headerHeight, CardCornerRadius, headerColor);
        ctx.Fill(headerColor, new RectangleF(cardX, cardY + headerHeight / 2, cardWidth, headerHeight / 2));

        var headerTitle = "玩家背包";
        var headerTitleSize = TextMeasurer.MeasureSize(headerTitle, new TextOptions(infoFont));
        var headerTitleX = cardX + (cardWidth - headerTitleSize.Width) / 2;
        var headerTitleY = cardY + (headerHeight - headerTitleSize.Height) / 2 + 2;
        ctx.DrawText(headerTitle, infoFont, Color.White, new PointF(headerTitleX, headerTitleY));

        var playerNameX = cardX + (cardWidth - playerNameSize.Width) / 2;
        var playerNameY = cardY + headerHeight + 10;
        ctx.DrawText(playerValue, largeTitleFont, Color.FromRgb(50, 55, 70), new PointF(playerNameX, playerNameY));

        var serverX = cardX + (cardWidth - serverSize.Width) / 2;
        var serverY = playerNameY + playerNameSize.Height + 6;
        ctx.DrawText(serverValue, smallFont, Color.FromRgb(130, 140, 160), new PointF(serverX, serverY));

        return cardY + cardHeight;
    }

    private void DrawContent(IImageProcessingContext ctx, InventoryBuilder builder, int canvasWidth)
    {
        var fontFamily = CardRenderer.GetFontFamily();
        var titleFont = fontFamily.CreateFont(TitleFontSize, FontStyle.Bold);
        var regionTitleFont = fontFamily.CreateFont(RegionTitleFontSize, FontStyle.Bold);
        var countFont = fontFamily.CreateFont(ItemCountFontSize);

        var titleCardBottom = DrawTitleCard(ctx, builder, canvasWidth, titleFont);
        var currentY = titleCardBottom + TitleCardBottomGap;

        currentY = DrawRow1(ctx, builder, regionTitleFont, countFont, canvasWidth, currentY);

        currentY += RegionGapY;
        currentY = DrawRow2(ctx, builder, regionTitleFont, countFont, canvasWidth, currentY);

        currentY += RegionGapY;
        currentY = DrawRow3(ctx, builder, regionTitleFont, countFont, canvasWidth, currentY);

        currentY += RegionGapY;
        DrawRow4(ctx, builder, regionTitleFont, countFont, canvasWidth, currentY);

        DrawSignature(ctx, canvasWidth);
    }

    private void DrawSignature(IImageProcessingContext ctx, int canvasWidth)
    {
        var signature = "Generated by Vortex.Bot";
        var fontFamily = CardRenderer.GetFontFamily();
        var signatureFont = fontFamily.CreateFont(18);

        var signatureSize = TextMeasurer.MeasureSize(signature, new TextOptions(signatureFont));
        var signatureX = (canvasWidth - signatureSize.Width) / 2;
        var signatureY = _canvasHeight - 20;

        ctx.DrawText(signature, signatureFont, Color.FromRgb(160, 165, 180), new PointF(signatureX, signatureY));
    }

    private int DrawRow1(IImageProcessingContext ctx, InventoryBuilder builder, Font titleFont, Font countFont, int canvasWidth, int y)
    {
        var mainWidth = 10 * SlotSize + 9 * SlotSpacing;
        var mainHeight = 5 * SlotSize + 4 * SlotSpacing;

        var coinWidth = 2 * SlotSize + 1 * SlotSpacing;
        var coinHeight = 4 * SlotSize + 3 * SlotSpacing;

        var totalWidth = mainWidth + RegionGapX + coinWidth;
        var startX = (canvasWidth - totalWidth) / 2;

        DrawInventoryCard(ctx, startX, y, mainWidth, mainHeight, "背包", titleFont, MainInventoryHeaderColor);
        for (var i = 0; i < 50 && i < builder.Inventory.Count; i++)
        {
            int row = i / 10, col = i % 10;
            DrawSlot(ctx, builder.Inventory[i], startX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
        }

        var coinX = startX + mainWidth + RegionGapX;
        var coinAmmoHeight = coinHeight;

        if (builder.TrashItem != null && !builder.TrashItem.IsEmpty)
        {
            coinAmmoHeight = coinHeight + SlotSize + SlotSpacing + 30;
        }

        DrawInventoryCard(ctx, coinX, y, coinWidth, coinAmmoHeight, "钱币/弹药", titleFont, CoinAmmoHeaderColor);

        for (var i = 0; i < 8 && i + 50 < builder.Inventory.Count; i++)
        {
            int row = i / 2, col = i % 2;
            DrawSlot(ctx, builder.Inventory[i + 50], coinX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
        }


        if (builder.TrashItem != null && !builder.TrashItem.IsEmpty)
        {
            var trashY = y + coinHeight + 15;
            var trashTitleSize = TextMeasurer.MeasureSize("垃圾桶", new TextOptions(titleFont));
            var trashTitleX = coinX + (coinWidth - trashTitleSize.Width) / 2;
            ctx.DrawText("垃圾桶", titleFont, RegionTitleColor, new PointF(trashTitleX, trashY - 18));
            DrawSlot(ctx, builder.TrashItem, coinX + (coinWidth - SlotSize) / 2, trashY, countFont);
        }

        for (var i = 0; i < 50 && i < builder.Inventory.Count; i++)
        {
            int row = i / 10, col = i % 10;
            DrawSlotOverlay(ctx, builder.Inventory[i], startX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
        }
        for (var i = 0; i < 8 && i + 50 < builder.Inventory.Count; i++)
        {
            int row = i / 2, col = i % 2;
            DrawSlotOverlay(ctx, builder.Inventory[i + 50], coinX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
        }
        if (builder.TrashItem != null && !builder.TrashItem.IsEmpty)
        {
            var trashY = y + coinHeight + 15;
            DrawSlotOverlay(ctx, builder.TrashItem, coinX + (coinWidth - SlotSize) / 2, trashY, countFont);
        }

        var rowHeight = Math.Max(mainHeight, coinHeight) + CardHeaderHeight;
        return y + rowHeight;
    }

    private int DrawRow2(IImageProcessingContext ctx, InventoryBuilder builder, Font titleFont, Font countFont, int canvasWidth, int y)
    {
        var storageWidth = 10 * SlotSize + 9 * SlotSpacing;
        var storageHeight = 4 * SlotSize + 3 * SlotSpacing;

        var totalWidth = storageWidth + RegionGapX + storageWidth;
        var startX = (canvasWidth - totalWidth) / 2;

        if (builder.Piggy.Count > 0)
        {
            DrawInventoryCard(ctx, startX, y, storageWidth, storageHeight, "猪猪储钱罐", titleFont, PiggyHeaderColor);
            for (var i = 0; i < builder.Piggy.Count && i < 40; i++)
            {
                int row = i / 10, col = i % 10;
                DrawSlot(ctx, builder.Piggy[i], startX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
            }
        }

        var safeX = startX + storageWidth + RegionGapX;
        if (builder.Safe.Count > 0)
        {
            DrawInventoryCard(ctx, safeX, y, storageWidth, storageHeight, "保险箱", titleFont, SafeHeaderColor);
            for (var i = 0; i < builder.Safe.Count && i < 40; i++)
            {
                int row = i / 10, col = i % 10;
                DrawSlot(ctx, builder.Safe[i], safeX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
            }
        }

        if (builder.Piggy.Count > 0)
        {
            for (var i = 0; i < builder.Piggy.Count && i < 40; i++)
            {
                int row = i / 10, col = i % 10;
                DrawSlotOverlay(ctx, builder.Piggy[i], startX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
            }
        }
        if (builder.Safe.Count > 0)
        {
            for (var i = 0; i < builder.Safe.Count && i < 40; i++)
            {
                int row = i / 10, col = i % 10;
                DrawSlotOverlay(ctx, builder.Safe[i], safeX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
            }
        }

        return y + storageHeight + CardHeaderHeight;
    }

    private int DrawRow3(IImageProcessingContext ctx, InventoryBuilder builder, Font titleFont, Font countFont, int canvasWidth, int y)
    {
        var storageWidth = 10 * SlotSize + 9 * SlotSpacing;
        var storageHeight = 4 * SlotSize + 3 * SlotSpacing;

        var totalWidth = storageWidth + RegionGapX + storageWidth;
        var startX = (canvasWidth - totalWidth) / 2;
        if (builder.VoidVault.Count > 0)
        {
            DrawInventoryCard(ctx, startX, y, storageWidth, storageHeight, "虚空宝库", titleFont, VoidVaultHeaderColor);
            for (var i = 0; i < builder.VoidVault.Count && i < 40; i++)
            {
                int row = i / 10, col = i % 10;
                DrawSlot(ctx, builder.VoidVault[i], startX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
            }
        }

        var forgeX = startX + storageWidth + RegionGapX;
        if (builder.Forge.Count > 0)
        {
            DrawInventoryCard(ctx, forgeX, y, storageWidth, storageHeight, "护卫熔炉", titleFont, ForgeHeaderColor);
            for (var i = 0; i < builder.Forge.Count && i < 40; i++)
            {
                int row = i / 10, col = i % 10;
                DrawSlot(ctx, builder.Forge[i], forgeX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
            }
        }

        if (builder.VoidVault.Count > 0)
        {
            for (var i = 0; i < builder.VoidVault.Count && i < 40; i++)
            {
                int row = i / 10, col = i % 10;
                DrawSlotOverlay(ctx, builder.VoidVault[i], startX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
            }
        }
        if (builder.Forge.Count > 0)
        {
            for (var i = 0; i < builder.Forge.Count && i < 40; i++)
            {
                int row = i / 10, col = i % 10;
                DrawSlotOverlay(ctx, builder.Forge[i], forgeX + col * (SlotSize + SlotSpacing), y + row * (SlotSize + SlotSpacing), countFont);
            }
        }

        return y + storageHeight + CardHeaderHeight;
    }

    private void DrawRow4(IImageProcessingContext ctx, InventoryBuilder builder, Font titleFont, Font countFont, int canvasWidth, int y)
    {
        if (builder.Loadouts.Count == 0) return;

        var loadoutWidth = 3 * TileSlotSize + 2 * TileSlotSpacing;
        var loadoutHeight = 10 * TileSlotSize + 9 * TileSlotSpacing;

        var totalWidth = 3 * loadoutWidth + 2 * TileGapX;
        var startX = (canvasWidth - totalWidth) / 2;
        for (var i = 0; i < builder.Loadouts.Count && i < 3; i++)
        {
            var loadout = builder.Loadouts[i];
            var loadoutX = startX + i * (loadoutWidth + TileGapX);

            DrawInventoryCard(ctx, loadoutX, y, loadoutWidth, loadoutHeight, $"套装 {i + 1}", titleFont, LoadoutHeaderColor);

            var equipItems = builder.MiscEquip.Concat(builder.MiscDye).ToList();
            for (var j = 0; j < equipItems.Count && j < 10; j++)
            {
                DrawSlot(ctx, equipItems[j], loadoutX, y + j * (TileSlotSize + TileSlotSpacing), countFont);
            }

            List<ItemSlot> armorItems2 = [];
            if (loadout.Armor?.Length > 10)
            {
                armorItems2 = loadout.Armor.Skip(10).Take(10).Select(a => new ItemSlot(a)).ToList();
                for (var j = 0; j < armorItems2.Count && j < 10; j++)
                {
                    DrawSlot(ctx, armorItems2[j], loadoutX + TileSlotSize + TileSlotSpacing, y + j * (TileSlotSize + TileSlotSpacing), countFont);
                }
            }

            List<ItemSlot> armorItems1 = [];
            if (loadout.Armor?.Length > 0)
            {
                armorItems1 = loadout.Armor.Take(10).Select(a => new ItemSlot(a)).ToList();
                for (var j = 0; j < armorItems1.Count && j < 10; j++)
                {
                    DrawSlot(ctx, armorItems1[j], loadoutX + 2 * (TileSlotSize + TileSlotSpacing), y + j * (TileSlotSize + TileSlotSpacing), countFont);
                }
            }

            for (var j = 0; j < equipItems.Count && j < 10; j++)
            {
                DrawSlotOverlay(ctx, equipItems[j], loadoutX, y + j * (TileSlotSize + TileSlotSpacing), countFont);
            }
            for (var j = 0; j < armorItems2.Count && j < 10; j++)
            {
                DrawSlotOverlay(ctx, armorItems2[j], loadoutX + TileSlotSize + TileSlotSpacing, y + j * (TileSlotSize + TileSlotSpacing), countFont);
            }
            for (var j = 0; j < armorItems1.Count && j < 10; j++)
            {
                DrawSlotOverlay(ctx, armorItems1[j], loadoutX + 2 * (TileSlotSize + TileSlotSpacing), y + j * (TileSlotSize + TileSlotSpacing), countFont);
            }
        }
    }

    private void DrawInventoryCard(IImageProcessingContext ctx, int x, int y, int width, int height, string title, Font titleFont, Color? headerColor = null)
    {
        var cardWidth = width + CardPadding * 2;
        var cardHeight = height + CardPadding * 2 + CardHeaderHeight;
        var cardX = x - CardPadding;
        var cardY = y - CardPadding - CardHeaderHeight;
        var headerHeight = CardHeaderHeight;

        ctx.DrawRoundedRectangle(cardX + 2, cardY + 2, cardWidth, cardHeight, CardCornerRadius, CardShadowColor);
        ctx.DrawRoundedRectangle(cardX, cardY, cardWidth, cardHeight, CardCornerRadius, CardColor);

        if (headerColor.HasValue)
        {
            ctx.DrawRoundedRectangle(cardX, cardY, cardWidth, headerHeight, CardCornerRadius, headerColor.Value);
            ctx.Fill(headerColor.Value, new RectangleF(cardX, cardY + headerHeight / 2, cardWidth, headerHeight / 2));
        }

        ctx.DrawRoundedRectanglePath(cardX, cardY, cardWidth, cardHeight, CardCornerRadius, 1, CardBorderColor);

        var titleSize = TextMeasurer.MeasureSize(title, new TextOptions(titleFont));
        var titleX = cardX + (cardWidth - titleSize.Width) / 2;
        var titleY = cardY + (headerHeight - titleSize.Height) / 2 + 2;
        var titleTextColor = headerColor.HasValue ? Color.White : RegionTitleColor;
        ctx.DrawText(title, titleFont, titleTextColor, new PointF(titleX, titleY));
    }

    private void DrawSlot(IImageProcessingContext ctx, ItemSlot item, int x, int y, Font countFont)
    {
        var slotColor = item.IsEmpty ? SlotEmptyColor : SlotFilledColor;
        ctx.DrawRoundedRectangle(x, y, SlotSize, SlotSize, 4, slotColor);

        if (item.IsEmpty)
        {
            ctx.DrawRoundedRectanglePath(x + 1, y + 1, SlotSize - 2, SlotSize - 2, 3, 1, Color.FromRgb(225, 228, 232));
        }
        ctx.DrawRoundedRectanglePath(x, y, SlotSize, SlotSize, 4, 1, SlotBorderColor);

        if (!item.IsEmpty)
        {
            DrawItemIcon(ctx, item.NetId, x, y);
        }
    }

    private void DrawSlotOverlay(IImageProcessingContext ctx, ItemSlot item, int x, int y, Font countFont)
    {
        if (!item.IsEmpty && item.Stack > 1)
        {
            var countText = item.Stack.ToString();
            var countSize = TextMeasurer.MeasureSize(countText, new TextOptions(countFont));
            var countX = x + SlotSize - countSize.Width - 4;
            var countY = y + SlotSize - countSize.Height - 4;
            ctx.Fill(ItemCountBackgroundColor, new RectangleF(countX - 2, countY - 1, countSize.Width + 4, countSize.Height + 4));
            ctx.DrawText(countText, countFont, ItemCountColor, new PointF(countX, countY));
        }
    }

    private void DrawItemIcon(IImageProcessingContext ctx, int netId, int x, int y)
    {
        if (netId <= 0) return;

        var itemPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Item", $"{netId}.png");
        if (!File.Exists(itemPath)) return;

        try
        {
            using var itemImage = Image.Load<Rgba32>(itemPath);
            var maxIconSize = SlotSize - (ItemIconPadding * 2);

            float scale = Math.Min((float)maxIconSize / itemImage.Width, (float)maxIconSize / itemImage.Height);
            var drawWidth = (int)(itemImage.Width * scale);
            var drawHeight = (int)(itemImage.Height * scale);
            var drawX = x + (SlotSize - drawWidth) / 2;
            var drawY = y + (SlotSize - drawHeight) / 2;

            if (drawWidth != itemImage.Width || drawHeight != itemImage.Height)
                itemImage.Mutate(i => i.Resize(drawWidth, drawHeight));

            ctx.DrawImage(itemImage, new Point(drawX, drawY), 1f);
        }
        catch { }
    }
}

public static class InventoryGenerateExtensions
{
    public static InventoryBuilder FromPlayerData(PlayerData data, string serverName)
    {
        var builder = InventoryBuilder.Create()
            .SetPlayerName(data.Username)
            .SetServerName(serverName);

        if (data.Inventory?.Length > 0)
            builder.Inventory = [.. data.Inventory.Select(i => new ItemSlot(i))];

        if (data.TrashItem?.Length > 0 && data.TrashItem[0].NetId != 0)
            builder.TrashItem = new ItemSlot(data.TrashItem[0]);

        if (data.Piggy?.Length > 0)
            builder.Piggy = [.. data.Piggy.Select(i => new ItemSlot(i))];

        if (data.Safe?.Length > 0)
            builder.Safe = [.. data.Safe.Select(i => new ItemSlot(i))];

        if (data.VoidVault?.Length > 0)
            builder.VoidVault = [.. data.VoidVault.Select(i => new ItemSlot(i))];

        if (data.Forge?.Length > 0)
            builder.Forge = [.. data.Forge.Select(i => new ItemSlot(i))];

        if (data.MiscEquip?.Length > 0)
            builder.MiscEquip = [.. data.MiscEquip.Select(i => new ItemSlot(i))];

        if (data.MiscDye?.Length > 0)
            builder.MiscDye = [.. data.MiscDye.Select(i => new ItemSlot(i))];

        if (data.Loadout?.Length > 0)
            builder.Loadouts = [.. data.Loadout];

        return builder;
    }
}
