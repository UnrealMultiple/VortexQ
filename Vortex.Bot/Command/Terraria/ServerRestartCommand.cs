using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Command.Terraria;

[Command("重启服务器", "restart")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.terraria.server.restart")]
public static class ServerRestartCommand
{
    [Main]
    public static async Task RestartServer(CommandArgs args)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyAsync("服务器管理器未初始化");
            return;
        }

        var groupId = args is GroupCommandArgs groupArgs ? groupArgs.GroupUin : 0;

        if (!serverManager.TryGetUserServer(args.SenderUin, groupId, out var server) || server == null)
        {
            await args.ReplyAsync("请先使用 '切换服务器 <名称>' 选择要操作的服务器!");
            return;
        }

        var startArgs = args.Params.Count > 0 ? string.Join(" ", args.Params) : "";
        var result = await server.RestartAsync(startArgs);

        if (result?.Success == true)
        {
            await args.ReplyAsync($"[{server.Config.Name}] 正在重启服务器，请稍后...");
        }
        else
        {
            await args.ReplyAsync($"[{server.Config.Name}] 重启失败: {result?.Message ?? "无法连接服务器"}");
        }
    }
}
