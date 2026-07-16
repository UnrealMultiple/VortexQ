using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Plugins;
using Vortex.Bot.Utility.Images;
using Vortex.Protocol.Packets;

namespace TerrariaRankBridge;

[Plugin(Name = "TerrariaRankBridge", Author = "VortexQ", Description = "泰拉瑞亚在线与死亡排行", Major = 2)]
public sealed class TerrariaRankBridgePlugin : PluginBase
{
    protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken) => default;
    protected override ValueTask OnShutdownAsync(CancellationToken cancellationToken) => default;
}

[Command("在线排行")]
[CommandType(CommandType.Group)]
public static class OnlineRankCommand
{
    [Main]
    public static Task Execute(GroupCommandArgs args) => RankCommands.ShowAsync(args, true);
}

[Command("死亡排行")]
[CommandType(CommandType.Group)]
public static class DeathRankCommand
{
    [Main]
    public static Task Execute(GroupCommandArgs args) => RankCommands.ShowAsync(args, false);
}

internal static class RankCommands
{
    public static async Task ShowAsync(GroupCommandArgs args, bool online)
    {
        var servers = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (servers == null || !servers.TryGetUserServer(args.SenderUin, args.GroupUin, out var server) || server?.GetOnlineClientId() is not { } clientId)
        {
            await args.ReplyWithAtAsync("请先选择并连接服务器。");
            return;
        }

        if (online)
        {
            var response = await args.Context.Server!.RequestAsync<OnlineRankPacket, OnlineRankPacketResponse>(clientId, new OnlineRankPacket());
            await ReplyAsync(args, response?.Success == true ? response.OnlineRank : null, "在线时长", static value => FormatDuration(value));
        }
        else
        {
            var response = await args.Context.Server!.RequestAsync<DeathRankPacket, DeathRankPacketResponse>(clientId, new DeathRankPacket());
            await ReplyAsync(args, response?.Success == true ? response.Rank : null, "死亡次数", static value => value.ToString());
        }
    }

    private static async Task ReplyAsync(GroupCommandArgs args, Dictionary<string, int>? rank, string title, Func<int, string> format)
    {
        if (rank == null || rank.Count == 0)
        {
            await args.ReplyWithAtAsync("当前没有排行数据。");
            return;
        }

        var table = TableBuilder.Create().SetTitle(title).SetHeader("排名", "玩家", title).SetMemberUin(args.SenderUin);
        foreach (var entry in rank.OrderByDescending(static item => item.Value).ThenBy(static item => item.Key).Take(100).Select((item, index) => (item, index)))
            table.AddRow((entry.index + 1).ToString(), entry.item.Key, format(entry.item.Value));

        await args.ReplyImageAsync(table.Build());
    }

    private static string FormatDuration(int totalSeconds)
    {
        var duration = TimeSpan.FromSeconds(totalSeconds);
        return $"{(int)duration.TotalDays}天{duration.Hours}小时{duration.Minutes}分钟{duration.Seconds}秒";
    }
}
