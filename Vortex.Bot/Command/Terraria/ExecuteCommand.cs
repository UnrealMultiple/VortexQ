using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Command.Terraria;

[Command("执行", "exec", "cmd")]
[HelpText("在服务器执行命令")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.execute")]
[SkipHelp]
public static class ExecuteCommand
{
    [Main]
    [Flexible]
    public static async Task ExecuteServerCommand(GroupCommandArgs args)
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

        if (args.Params.Count == 0)
        {
            await args.ReplyWithAtAsync("请输入要执行的命令!\n用法: 执行 <命令>");
            return;
        }

        var command = "/" + string.Join(" ", args.Params);
        var result = await server.ExecuteCommandAsync(command);

        if (result?.Success == true)
        {
            var output = result.Params != null && result.Params.Count > 0
                ? string.Join("\n", result.Params)
                : "命令执行成功(无输出)";
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 执行结果:\n{output}");
        }
        else
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 执行失败: {result?.Message ?? "无法连接服务器"}");
        }
    }
}
