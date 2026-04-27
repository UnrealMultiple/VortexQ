using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Terraria;

[Command("服务器信息", "serverinfo", "svinfo")]
[HelpText("查看服务器信息")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.server.info")]
public static class ServerInfoCommand
{
    [Main]
    public static async Task ShowServerInfo(GroupCommandArgs args)
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

        var status = await server.GetStatusAsync();

        if (status?.Success == true)
        {
            var tableBuilder = new TableBuilder()
                .SetHeader("插件名称", "说明", "作者")
                .SetTitle($"{server.Config.Name} 插件列表")
                .SetMemberUin(args.SenderUin);

            if (status.Plugins != null)
            {
                foreach (var plugin in status.Plugins)
                {
                    tableBuilder.AddRow(
                        plugin.Name ?? "Unknown",
                        plugin.Description ?? "No description",
                        plugin.Author ?? "Unknown"
                    );
                }
            }

            await args.ReplyImageAsync(tableBuilder.Build());
        }
        else
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 获取信息失败: {status?.Message ?? "无法连接服务器"}");
        }
    }
}
