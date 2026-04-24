using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Terraria;

[Command("服务器信息", "serverinfo", "svinfo")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.terraria.server.info")]
public static class ServerInfoCommand
{
    [Main]
    public static async Task ShowServerInfo(CommandArgs args)
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

        var status = await server.GetStatusAsync();

        if (status?.Success == true)
        {
            var tableBuilder = new TableBuilder()
                .SetHeader("插件名称", "说明", "作者")
                .SetTitle($"[{server.Config.Name}] 插件列表")
                .SetMemberUin((uint)args.SenderUin);

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

            await args.ReplyImageAsync(tableBuilder.Builder());
        }
        else
        {
            await args.ReplyAsync($"[{server.Config.Name}] 获取信息失败: {status?.Message ?? "无法连接服务器"}");
        }
    }
}
