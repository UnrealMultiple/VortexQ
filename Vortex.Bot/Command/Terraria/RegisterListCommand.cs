using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Terraria;

[Command("注册列表", "reglist")]
[HelpText("查看自己的注册列表")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.registerlist")]
public static class RegisterListCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyWithAtAsync("服务器管理器未初始化");
            return;
        }

        if (!serverManager.TryGetUserServer(args.SenderUin, args.GroupUin, out var server) || server == null)
        {
            await args.ReplyWithAtAsync("服务器无效或未切换至一个有效服务器!");
            return;
        }

        var users = TerrariaUser.GetUsersById(args.SenderUin, server.Config.Name);

        if (users.Count == 0)
        {
            await args.ReplyWithAtAsync($"你在 {server.Config.Name} 上没有注册任何角色。");
            return;
        }

        var builder = ListBuilder.Create()
            .SetTitle($"[{server.Config.Name}] 注册列表")
            .SetMemberUin(args.SenderUin);

        for (var i = 0; i < users.Count; i++)
        {
            var user = users[i];
            builder.AddItem($"{i + 1}. {user.Name} (GroupID: {user.GroupId})");
        }

        await args.ReplyImageAsync(builder.Build());
    }
}
