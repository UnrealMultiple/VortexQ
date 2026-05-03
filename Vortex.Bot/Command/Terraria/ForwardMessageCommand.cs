using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database.Models;

namespace Vortex.Bot.Command.Terraria;

[Command("消息转发")]
[HelpText("开启或关闭服务器消息转发到当前群")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.forwardmessage")]
public static class ForwardMessageCommand
{
    [Main]
    public async static Task ExecuteAsync(GroupCommandArgs args, string status)
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
        switch (status)
        {
            case "开启":
                GroupForwardMessage.AddGroupToServer(server.Config.Name, args.GroupUin);
                await args.ReplyWithAtAsync("已开启消息转发");
            break;
                case "关闭":
                GroupForwardMessage.RemoveGroupFromServer(server.Config.Name, args.GroupUin);
                await args.ReplyWithAtAsync("已关闭消息转发"); 
            break;
                default: 
                await args.ReplyWithAtAsync("参数错误! 请输入 '开启' 或 '关闭'"); 
           break;
        }
    }
}
