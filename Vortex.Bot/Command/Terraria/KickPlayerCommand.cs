using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Command.Terraria;

[Command("踢出玩家", "kickplayer")]
[HelpText("踢出服务器玩家")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.kick")]
public static class KickPlayerCommand
{
    [Main]
    [Flexible(1)]
    public static async Task Execute(GroupCommandArgs args, [Param("玩家名称")] string playerName, [Param("原因(可选)")] string reason = "")
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

        var kickReason = string.IsNullOrEmpty(reason) ? "Kicked by admin" : reason;
        var result = await server.ExecuteCommandAsync($"/kick {playerName} \"{kickReason}\"");

        if (result?.Success == true)
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 已踢出玩家 {playerName}\n原因: {kickReason}");
        }
        else
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 踢出玩家失败: {result?.Message ?? "无法连接服务器"}");
        }
    }
}
