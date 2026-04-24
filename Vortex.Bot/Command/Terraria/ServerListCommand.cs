using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Terraria;

[Command("服务器列表", "servers", "svlist")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.terraria.server.list")]
public static class ServerListCommand
{
    [Main]
    public static async Task ShowServerList(CommandArgs args)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyAsync("服务器管理器未初始化");
            return;
        }

        var groupId = args is GroupCommandArgs groupArgs ? groupArgs.GroupUin : 0;
        var servers = groupId > 0
            ? serverManager.GetServersByGroup(groupId).ToList()
            : serverManager.GetAllServers().ToList();

        if (servers.Count == 0)
        {
            await args.ReplyAsync("此群未配置任何服务器!");
            return;
        }

        var tableBuilder = new TableBuilder()
            .SetHeader("服务器名称", "IP", "端口", "版本", "说明", "状态", "世界", "种子", "大小")
            .SetTitle("服务器列表")
            .SetMemberUin((uint)args.SenderUin);

        foreach (var server in servers)
        {
            var status = await server.GetStatusAsync();
            var isOnline = status != null && status.Success;

            tableBuilder.AddRow(
                server.Config.Name,
                server.Config.IP,
                server.Config.DisplayPort.ToString(),
                server.Config.Version,
                server.Config.Describe,
                !isOnline ? "离线" : $"运行:{status!.RunTime:dd\\.hh\\:mm\\:ss}",
                !isOnline ? "-" : status.WorldName,
                !isOnline ? "-" : status.WorldSeed,
                !isOnline ? "-" : $"{status.WorldWidth}x{status.WorldHeight}"
            );
        }

        await args.ReplyImageAsync(tableBuilder.Builder());
    }
}
