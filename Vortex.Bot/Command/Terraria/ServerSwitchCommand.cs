using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Command.Terraria;

[Command("切换", "switch", "use")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.terraria.server.switch")]
public static class ServerSwitchCommand
{
    [Main]
    public static async Task SwitchServer(CommandArgs args)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyAsync("服务器管理器未初始化");
            return;
        }

        if (args.Params.Count == 0)
        {
            await args.ReplyAsync("请输入服务器名称!\n用法: 切换 <服务器名称>");
            return;
        }

        var serverName = args.Params[0];
        var groupId = args is GroupCommandArgs groupArgs ? groupArgs.GroupUin : 0;

        if (!serverManager.TryGetServer(serverName, out var server) || server == null)
        {
            await args.ReplyAsync($"未找到服务器: {serverName}");
            return;
        }

        if (groupId > 0 && !server.Config.Groups.Contains(groupId))
        {
            await args.ReplyAsync("此服务器不属于当前群组!");
            return;
        }

        serverManager.SetUserServer(args.SenderUin, groupId, serverName);
        await args.ReplyAsync($"已切换到服务器: {serverName}");
    }
}
