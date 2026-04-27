using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Command.Terraria;

[Command("ban", "封禁")]
[HelpText("封禁/解封服务器玩家")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.ban")]
public static class BanPlayerCommand
{
    [Alias("add")]
    [Flexible(1)]
    public static async Task Add(GroupCommandArgs args, [Param("玩家名称")] string playerName, [Param("原因(可选)")] string reason = "")
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyWithAtAsync("服务器管理器未初始化");
            return;
        }

        if (!serverManager.TryGetUserServer(args.SenderUin, args.GroupUin, out var server) || server == null)
        {
            await args.ReplyWithAtAsync("请先使用 '切换 <名称>' 选择要操作的服务器!");
            return;
        }

        var banReason = string.IsNullOrEmpty(reason) ? "Banned by admin" : reason;
        var result = await server.ExecuteCommandAsync($"/ban add {playerName} \"{banReason}\"");

        if (result?.Success == true)
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 已封禁玩家 {playerName}\n原因: {banReason}");
        }
        else
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 封禁玩家失败: {result?.Message ?? "无法连接服务器"}");
        }
    }

    [Alias("del", "remove")]
    public static async Task Del(GroupCommandArgs args, [Param("玩家名称")] string playerName)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyWithAtAsync("服务器管理器未初始化");
            return;
        }

        if (!serverManager.TryGetUserServer(args.SenderUin, args.GroupUin, out var server) || server == null)
        {
            await args.ReplyWithAtAsync("请先使用 '切换 <名称>' 选择要操作的服务器!");
            return;
        }

        var result = await server.ExecuteCommandAsync($"/ban del {playerName}");

        if (result?.Success == true)
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 已解封玩家 {playerName}");
        }
        else
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 解封玩家失败: {result?.Message ?? "无法连接服务器"}");
        }
    }
}
