using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Command.Terraria;

[Command("切换", "switch", "use")]
[HelpText("切换操作的服务器")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.server.switch")]
public static class ServerSwitchCommand
{
    [Main]
    public static async Task SwitchServer(GroupCommandArgs args, [Param("服务器名称")] string serverName)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyWithAtAsync("服务器管理器未初始化");
            return;
        }

        if (args.Params.Count == 0)
        {
            await args.ReplyWithAtAsync("请输入服务器名称!\n用法: 切换 <服务器名称>");
            return;
        }

        if (!serverManager.TryGetServer(serverName, out var server) || server == null)
        {
            await args.ReplyWithAtAsync($"未找到服务器: {serverName}");
            return;
        }

        if (args.GroupUin > 0 && !server.Config.Groups.Contains(args.GroupUin))
        {
            await args.ReplyWithAtAsync("此服务器不属于当前群组!");
            return;
        }

        serverManager.SetUserServer(args.SenderUin, args.GroupUin, serverName);
        await args.ReplyWithAtAsync($"已切换到服务器: {serverName}");
    }
}
