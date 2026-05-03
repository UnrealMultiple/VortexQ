using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.User;

[Command("user", "用户")]
[HelpText("管理 Terraria 用户数据")]
[CommandType(CommandType.Group)]
[Permission("vortex.user.admin")]
public static class UserCommand
{
    [Command("list", "列表")]
    [HelpText("列出所有 Terraria 用户")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.user.admin.list")]
    [DefaultCommand]
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
                await args.ReplyWithAtAsync("请先使用 '切换 <名称>' 选择要操作的服务器!");
                return;
            }

            var users = TerrariaUser.GetUsersByServer(server.Config.Name);
            if (users.Count == 0)
            {
                await args.ReplyWithAtAsync($"[{server.Config.Name}] 暂无注册用户");
                return;
            }

            var builder = TableBuilder.Create()
                .SetHeader("QQ号", "角色名", "服务器", "组ID")
                .SetTitle($"[{server.Config.Name}] 用户列表")
                .SetMemberUin(args.SenderUin);

            var displayCount = Math.Min(users.Count, 30);
            for (var i = 0; i < displayCount; i++)
            {
                var user = users[i];
                builder.AddRow(user.Id.ToString(), user.Name, user.Server, user.GroupId.ToString());
            }

            await args.ReplyImageAsync(builder.Build());

            if (users.Count > 30)
            {
                await args.ReplyWithAtAsync($"...还有 {users.Count - 30} 个用户未显示");
            }
        }
    }

    [Command("find", "查找")]
    [HelpText("根据条件查找用户")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.user.admin.find")]
    [DefaultCommand]
    public static class FindCmd
    {
        [Main]
        public static async Task Execute(GroupCommandArgs args, [Param("角色名或QQ号")] string keyword)
        {
            var allUsers = TerrariaUser.GetAll();
            List<TerrariaUser> matchedUsers;

            if (long.TryParse(keyword, out var userId))
            {
                matchedUsers = allUsers.Where(u => u.Id == userId).ToList();
            }
            else
            {
                matchedUsers = allUsers.Where(u => u.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (matchedUsers.Count == 0)
            {
                await args.ReplyWithAtAsync($"未找到匹配 '{keyword}' 的用户");
                return;
            }

            var builder = TableBuilder.Create()
                .SetHeader("QQ号", "角色名", "服务器", "组ID")
                .SetTitle($"查找结果")
                .SetMemberUin(args.SenderUin);

            foreach (var user in matchedUsers.Take(30))
            {
                builder.AddRow(user.Id.ToString(), user.Name, user.Server, user.GroupId.ToString());
            }

            await args.ReplyImageAsync(builder.Build());

            if (matchedUsers.Count > 30)
            {
                await args.ReplyWithAtAsync($"...还有 {matchedUsers.Count - 30} 个结果未显示");
            }
        }
    }

    [Command("info", "信息")]
    [HelpText("查看指定用户的详细信息")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.user.admin.info")]
    [DefaultCommand]
    public static class InfoCmd
    {
        [Main]
        public static async Task Execute(GroupCommandArgs args, [Param("角色名")] string characterName)
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
            var user = TerrariaUser.GetUserByName(characterName, server.Config.Name);

            if (user == null)
            {
                await args.ReplyWithAtAsync($"未找到角色 '{characterName}'");
                return;
            }

            var builder = ProfileItemBuilder.Create()
                .SetMemberUin(args.SenderUin)
                .SetAvatarSize(150)
                .SetTitleFontSize(50)
                .SetTitle("角色信息")
                .AddItem("QQ号", user.Id.ToString())
                .AddItem("角色名", user.Name)
                .AddItem("服务器", user.Server)
                .AddItem("组ID", user.GroupId.ToString())
                .AddItem("索引", user.Index.ToString());

            await args.ReplyImageAsync(builder.Build());
        }
    }

    [Command("del", "删除")]
    [HelpText("删除指定用户")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.user.admin.del")]
    public static class DelCmd
    {
        [Main]
        public static async Task Execute(GroupCommandArgs args, [Param("角色名")] string characterName)
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

            try
            {
                TerrariaUser.Remove(server.Config.Name, characterName);
                await args.ReplyWithAtAsync($"[{server.Config.Name}] 角色 '{characterName}' 已从数据库移除!");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"删除失败: {ex.Message}");
            }
        }
    }


    [Command("count", "统计")]
    [HelpText("统计用户数量")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.user.admin.count")]
    [DefaultCommand]
    public static class CountCmd
    {
        [Main]
        public static async Task Execute(GroupCommandArgs args, [Param("服务器名称(可选)")] string? serverName = null)
        {
            var allUsers = TerrariaUser.GetAll();

            if (!string.IsNullOrEmpty(serverName))
            {
                var serverUsers = allUsers.Where(u => u.Server == serverName).ToList();
                await args.ReplyWithAtAsync($"[{serverName}] 共有 {serverUsers.Count} 个注册用户");
            }
            else
            {
                var serverGroups = allUsers.GroupBy(u => u.Server)
                    .Select(g => new { Server = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                if (serverGroups.Count == 0)
                {
                    await args.ReplyWithAtAsync("当前没有任何注册用户");
                    return;
                }

                var builder = ListBuilder.Create()
                    .SetTitle("用户统计")
                    .SetMemberUin(args.SenderUin);

                foreach (var group in serverGroups)
                {
                    builder.AddItem($"[{group.Server}] {group.Count} 人");
                }

                builder.AddItem("");
                builder.AddItem($"总计: {allUsers.Count} 人");

                await args.ReplyImageAsync(builder.Build());
            }
        }
    }
}
