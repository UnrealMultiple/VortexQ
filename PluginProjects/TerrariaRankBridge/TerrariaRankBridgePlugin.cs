using Vortex.Plugin.Abstractions;

namespace TerrariaRankBridge;

public sealed class TerrariaRankBridgePlugin : PluginBase
{
    private static readonly PluginColor Gold = new(196, 145, 2);
    private static readonly PluginColor Silver = new(110, 117, 127);
    private static readonly PluginColor Bronze = new(160, 86, 33);

    public override PluginMetadata Metadata { get; } = new(
        "TerrariaRankBridge",
        "VortexQ",
        "泰拉瑞亚在线与死亡排行",
        new Version(2, 0, 0));

    protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken)
    {
        Commands.Register(new PluginCommand("在线排行", PluginCommandScope.Group, ShowOnlineRankAsync, helpText: "查看在线时长排行"));
        Commands.Register(new PluginCommand("死亡排行", PluginCommandScope.Group, ShowDeathRankAsync, helpText: "查看死亡次数排行"));
        return default;
    }

    private ValueTask ShowOnlineRankAsync(IPluginCommandContext context, CancellationToken cancellationToken) =>
        ShowRankAsync(context, true, cancellationToken);

    private ValueTask ShowDeathRankAsync(IPluginCommandContext context, CancellationToken cancellationToken) =>
        ShowRankAsync(context, false, cancellationToken);

    private async ValueTask ShowRankAsync(IPluginCommandContext context, bool online, CancellationToken cancellationToken)
    {
        var serverName = Terraria.GetSelectedServerName(context.UserId, context.GroupId);
        if (string.IsNullOrWhiteSpace(serverName))
        {
            await context.ReplyWithAtAsync("请先使用 /切换服务器 选择服务器。");
            return;
        }

        var result = online
            ? await Terraria.GetOnlineRankAsync(serverName, cancellationToken)
            : await Terraria.GetDeathRankAsync(serverName, cancellationToken);
        if (!result.Success)
        {
            await context.ReplyWithAtAsync(string.IsNullOrWhiteSpace(result.Message) ? "获取排行失败。" : $"获取排行失败：{result.Message}");
            return;
        }

        var entries = result.Entries
            .OrderByDescending(static entry => entry.Value)
            .ThenBy(static entry => entry.Name, StringComparer.Ordinal)
            .Take(100)
            .ToArray();
        if (entries.Length == 0)
        {
            await context.ReplyWithAtAsync("当前没有排行数据。");
            return;
        }

        var rows = entries
            .Select((entry, index) => CreateRow(index + 1, entry, online))
            .ToArray();
        var unit = online ? "在线时长" : "死亡次数";
        var table = new PluginTable(
            $"{serverName} - {unit}排行",
            new PluginTableCell[] { new("排名"), new("玩家"), new(unit) },
            rows)
        {
            MemberUin = context.UserId
        };
        await context.ReplyImageAsync(Images.RenderTable(table));
    }

    private static IReadOnlyList<PluginTableCell> CreateRow(int rank, PluginRankEntry entry, bool online)
    {
        var (label, color, style) = rank switch
        {
            1 => ("★ 1", (PluginColor?)Gold, PluginFontStyle.Bold),
            2 => ("◆ 2", (PluginColor?)Silver, PluginFontStyle.Bold),
            3 => ("▲ 3", (PluginColor?)Bronze, PluginFontStyle.Bold),
            _ => (rank.ToString(), null, PluginFontStyle.Regular)
        };
        var value = online ? FormatOnlineDuration(entry.Value) : $"{entry.Value:N0} 次";
        return new PluginTableCell[]
        {
            new(label, color, style),
            new(entry.Name, color, style),
            new(value, color, style)
        };
    }

    private static string FormatOnlineDuration(long totalSeconds)
    {
        var days = totalSeconds / (24 * 60 * 60);
        var hours = totalSeconds % (24 * 60 * 60) / (60 * 60);
        var minutes = totalSeconds % (60 * 60) / 60;
        var seconds = totalSeconds % 60;

        if (days > 0) return $"{days}天 {hours}小时 {minutes}分钟";
        if (hours > 0) return $"{hours}小时 {minutes}分钟";
        if (minutes > 0) return $"{minutes}分钟 {seconds}秒";
        return $"{seconds}秒";
    }
}
