using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Terraria;

[Command("tuser", "游戏用户")]
[HelpText("管理游戏用户")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.useradmin")]
public static class TerrariaUserAdminCommand
{
    [Command("del", "删除")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.terraria.useradmin.del")]
    public static class DelCmd
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
                await args.ReplyWithAtAsync("未切换服务器或服务器无效!");
                return;
            }

            try
            {
                TerrariaUser.Remove(server.Config.Name, characterName);
                await args.ReplyWithAtAsync($"[{server.Config.Name}] 角色 {characterName} 已从数据库移除!");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"移除失败: {ex.Message}");
            }
        }
    }

    [Command("list", "列表")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.terraria.useradmin.list")]
    public static class ListCmd
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
                await args.ReplyWithAtAsync("未切换服务器或服务器无效!");
                return;
            }

            var users = TerrariaUser.GetUsersByServer(server.Config.Name);

            if (users.Count == 0)
            {
                await args.ReplyWithAtAsync($"[{server.Config.Name}] 暂无注册用户");
                return;
            }

            var builder = ListBuilder.Create()
                .SetTitle($"[{server.Config.Name}] 用户列表")
                .SetMemberUin(args.SenderUin);

            var displayCount = Math.Min(users.Count, 20);
            for (var i = 0; i < displayCount; i++)
            {
                var user = users[i];
                builder.AddItem($"ID:{user.Id} 名称:{user.Name} 组ID:{user.GroupId}");
            }

            if (users.Count > 20)
            {
                builder.AddItem($"");
                builder.AddItem($"...还有 {users.Count - 20} 个用户");
            }

            await args.ReplyImageAsync(builder.Build());
        }
    }
}
