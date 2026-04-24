using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Command.Terraria;

[Command("重置服务器", "reset")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.terraria.server.reset")]
public static class ServerResetCommand
{
    [Main]
    public static async Task ResetServer(CommandArgs args)
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

        var resetCommands = new List<string>();
        var startArgs = "";

        foreach (var param in args.Params)
        {
            if (param.StartsWith("-"))
            {
                startArgs += param + " ";
            }
            else
            {
                resetCommands.Add(param);
            }
        }

        await args.ReplyAsync($"[{server.Config.Name}] 正在重置服务器...");

        var result = await server.ResetAsync(resetCommands, startArgs.Trim());

        if (result?.Success == true)
        {
            await args.ReplyAsync($"[{server.Config.Name}] 服务器重置命令已发送，正在处理...");
        }
        else
        {
            await args.ReplyAsync($"[{server.Config.Name}] 重置失败: {result?.Message ?? "无法连接服务器"}");
        }
    }
}
