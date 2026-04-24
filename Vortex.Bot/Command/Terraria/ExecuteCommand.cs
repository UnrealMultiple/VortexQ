using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Command.Terraria;

[Command("执行", "exec", "cmd")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.terraria.execute")]
public static class ExecuteCommand
{
    [Main]
    public static async Task ExecuteServerCommand(CommandArgs args)
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

        if (args.Params.Count == 0)
        {
            await args.ReplyAsync("请输入要执行的命令!\n用法: 执行 <命令>");
            return;
        }

        var command = "/" + string.Join(" ", args.Params);
        var result = await server.ExecuteCommandAsync(command);

        if (result?.Success == true)
        {
            var output = result.Params != null && result.Params.Count > 0
                ? string.Join("\n", result.Params)
                : "命令执行成功(无输出)";
            await args.ReplyAsync($"[{server.Config.Name}] 执行结果:\n{output}");
        }
        else
        {
            await args.ReplyAsync($"[{server.Config.Name}] 执行失败: {result?.Message ?? "无法连接服务器"}");
        }
    }
}
