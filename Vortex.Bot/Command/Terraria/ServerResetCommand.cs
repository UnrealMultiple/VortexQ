using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Command.Terraria;

[Command("重置服务器", "reset")]
[HelpText("重置游戏服务器地图")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.server.reset")]
public static class ServerResetCommand
{
    [Main]
    public static async Task ResetServer(GroupCommandArgs args)
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

        var resetCommands = new List<string>();
        var startArgs = "";

        foreach (var param in args.Params)
        {
            if (param.StartsWith('-'))
            {
                startArgs += param + " ";
            }
            else
            {
                resetCommands.Add(param);
            }
        }

        await args.ReplyWithAtAsync($"[{server.Config.Name}] 正在重置服务器...");

        var result = await server.ResetAsync(resetCommands, startArgs.Trim());

        if (result?.Success == true)
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 服务器重置命令已发送，正在处理...");
        }
        else
        {
            await args.ReplyWithAtAsync($"[{server.Config.Name}] 重置失败: {result?.Message ?? "无法连接服务器"}");
        }
    }
}
