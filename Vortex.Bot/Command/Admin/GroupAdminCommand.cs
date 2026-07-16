using Vortex.Bot.Attributes;
using Vortex.Bot.Models;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Admin;

[Command("group", "权限组")]
[HelpText("管理权限组")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.admin.group")]
public static class GroupAdminCommand
{
    [Command("add", "添加")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.admin.group.add")]
    public static class AddCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("组名")] string groupName)
        {
            try
            {
                GroupRepository.Create(groupName);
                await args.ReplyWithAtAsync($"组 {groupName} 添加成功!");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"添加失败: {ex.Message}");
            }
        }
    }

    [Command("del", "删除")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.admin.group.del")]
    public static class DelCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("组名")] string groupName)
        {
            try
            {
                GroupRepository.Delete(groupName);
                await args.ReplyWithAtAsync($"组 {groupName} 删除成功!");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"删除失败: {ex.Message}");
            }
        }
    }

    [Command("addperm", "添加权限")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.admin.group.addperm")]
    public static class AddPermCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("组名")] string groupName, [Param("权限")] string permission)
        {
            try
            {
                GroupRepository.AddPermission(groupName, permission);
                await args.ReplyWithAtAsync($"权限 {permission} 已添加到组 {groupName}");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"添加权限失败: {ex.Message}");
            }
        }
    }

    [Command("delperm", "删除权限")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.admin.group.delperm")]
    public static class DelPermCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("组名")] string groupName, [Param("权限")] string permission)
        {
            try
            {
                GroupRepository.RemovePermission(groupName, permission);
                await args.ReplyWithAtAsync($"权限 {permission} 已从组 {groupName} 删除");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"删除权限失败: {ex.Message}");
            }
        }
    }

    [Command("parent", "父组")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.admin.group.parent")]
    public static class ParentCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("组名")] string groupName, [Param("父组名")] string parentGroupName)
        {
            try
            {
                GroupRepository.SetParent(groupName, parentGroupName);
                await args.ReplyWithAtAsync($"组 {groupName} 的父组已更改为 {parentGroupName}");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"设置父组失败: {ex.Message}");
            }
        }
    }

    [Command("list", "列表")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.admin.group.list")]
    public static class ListCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args)
        {
            var groups = GroupRepository.GetAll();

            if (groups.Count == 0)
            {
                await args.ReplyWithAtAsync("还没有添加任何权限组!");
                return;
            }

            var builder = TableBuilder.Create()
                .SetHeader("组名", "父组", "权限")
                .SetTitle("权限组列表")
                .SetMemberUin(args.SenderUin);

            foreach (var group in groups)
            {
                var perms = string.Join(", ", group.LocalPermissions.Permissions);
                if (string.IsNullOrEmpty(perms))
                    perms = "无";
                var parent = string.IsNullOrEmpty(group.ParentName) ? "无" : group.ParentName;
                builder.AddRow(group.Name, parent, perms);
            }

            await args.ReplyImageAsync(builder.Build());
        }
    }
}
