using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Command.Terraria;

[Command("重启服务器", "restart")]
[HelpText("重启游戏服务器")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.server.restart")]
public static class ServerRestartCommand
{
    [Main]
    public static async Task RestartServer(GroupCommandArgs args)
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

        var startArgs = args.Params.Count > 0 ? string.Join(" ", args.Params) : "";
        var result = await server.RestartAsync(startArgs);

        if (result?.Success == true)
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 正在重启服务器，请稍后...");
        }
        else
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 重启失败: {result?.Message ?? "无法连接服务器"}");
        }
    }
}
