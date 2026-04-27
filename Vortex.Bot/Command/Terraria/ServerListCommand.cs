using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Terraria;

[Command("服务器列表", "servers", "svlist")]
[HelpText("查看所有服务器列表")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.server.list")]
public static class ServerListCommand
{
    [Main]
    public static async Task ShowServerList(GroupCommandArgs args)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyWithAtAsync("服务器管理器未初始化");
            return;
        }

        var servers = args.GroupUin > 0
            ? [.. serverManager.GetServersByGroup(args.GroupUin)]
            : serverManager.GetAllServers().ToList();

        if (servers.Count == 0)
        {
            await args.ReplyWithAtAsync("此群未配置任何服务器!");
            return;
        }

        var tableBuilder = new TableBuilder()
            .SetHeader("服务器名称", "IP", "端口", "版本", "说明", "状态", "世界", "种子", "大小")
            .SetTitle("服务器列表")
            .SetMemberUin(args.SenderUin);

        foreach (var server in servers)
        {
            var status = await server.GetStatusAsync();
            bool isOnline = status != null && status.Success;

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

        await args.ReplyImageAsync(tableBuilder.Build());
    }
}
