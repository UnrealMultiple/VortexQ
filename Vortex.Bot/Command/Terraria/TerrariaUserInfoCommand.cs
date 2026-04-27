using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Terraria;

[Command("玩家信息", "ui", "userinfo")]
[HelpText("查看玩家详细信息")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.userinfo")]
public static class TerrariaUserInfoCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args, [Param("角色名称")] string characterName)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyWithAtAsync("服务器管理器未初始化");
            return;
        }

        if (!serverManager.TryGetUserServer(args.SenderUin, args.GroupUin, out var server) || server == null)
        {
            await args.ReplyWithAtAsync("服务器不存在或未切换至一个服务器!");
            return;
        }

        var result = await server.QueryAccountAsync(characterName);

        if (result?.Success == true && result.Accounts.Count > 0)
        {
            var account = result.Accounts.FirstOrDefault(a => a.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));
            if (account == null)
            {
                await args.ReplyWithAtAsync($"[{server.Config.Name}] 未找到玩家: {characterName}");
                return;
            }

            var builder = ProfileItemBuilder.Create()
                .SetTitle($"[{server.Config.Name}] 玩家信息")
                .SetMemberUin(args.SenderUin)
                .SetAvatarSize(150)
                .SetTitleFontSize(50)
                .AddItem("角色名", account.Name)
                .AddItem("ID", account.ID.ToString())
                .AddItem("权限组", account.Group)
                .AddItem("注册时间", account.RegisterTime)
                .AddItem("最后登录", account.LastLoginTime)
                .AddItem("IP地址", account.IP);

            await args.ReplyImageAsync(builder.Build());
        }
        else
        {
            await args.ReplyWithAtAsync($"查询失败: {result?.Message ?? "无法连接服务器或玩家不存在"}");
        }
    }
}
