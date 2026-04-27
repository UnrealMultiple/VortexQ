using Vortex.Bot.Attributes;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Admin;

[Command("account", "账户")]
[HelpText("管理用户账户")]
[CommandType(CommandType.Group)]
[Permission("vortex.admin.account")]
public static class AccountAdminCommand
{
    [Command("add", "添加")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.admin.account.add")]
    public static class AddCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("QQ号")] long userId, [Param("权限组")] string groupName)
        {
            try
            {
                Account.Add(userId, groupName);
                await args.ReplyWithAtAsync($"账户 {userId} 已添加到组 {groupName}");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"添加失败: {ex.Message}");
            }
        }
    }

    [Command("del", "删除")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.admin.account.del")]
    public static class DelCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("QQ号")] long userId)
        {
            try
            {
                Account.Delete(userId);
                await args.ReplyWithAtAsync($"账户 {userId} 删除成功!");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"删除失败: {ex.Message}");
            }
        }
    }

    [Command("group", "组")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.admin.account.group")]
    public static class GroupCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("QQ号")] long userId, [Param("新权限组")] string groupName)
        {
            try
            {
                Account.SetGroup(userId, groupName);
                await args.ReplyWithAtAsync($"账户 {userId} 的组已更改为 {groupName}");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"更改失败: {ex.Message}");
            }
        }
    }

    [Command("list", "列表")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.admin.account.list")]
    public static class ListCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args)
        {
            var accounts = Account.GetAll();

            if (accounts.Count == 0)
            {
                await args.ReplyWithAtAsync("当前没有任何账户!");
                return;
            }

            var builder = TableBuilder.Create()
                .SetHeader("QQ号", "权限组")
                .SetTitle("账户列表")
                .SetMemberUin(args.SenderUin);

            foreach (var account in accounts)
            {
                builder.AddRow(account.UserId.ToString(), account.GroupName);
            }

            await args.ReplyImageAsync(builder.Build());
        }
    }
}
