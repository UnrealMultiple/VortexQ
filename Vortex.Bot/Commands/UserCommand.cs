using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Database.Models;

namespace Vortex.Bot.Commands;

[Command("user")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.user")]
public static class UserCommand
{
    [Command("register", "reg", "注册")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.user.register")]
    public static class RegisterCmd
    {
        [Main]
        public static async Task Execute(GroupCommandArgs args)
        {
            if (args.Account != null)
            {
                await args.ReplyAsync("你已经注册过了！");
                return;
            }

            Account.Add(args.SenderUin, "default");

            await args.ReplyAsync($"注册成功！\nQQ: {args.SenderUin}\n默认权限组: default");
        }

        [Main]
        public static async Task Execute(PrivateCommandArgs args)
        {
            if (args.Account != null)
            {
                await args.ReplyAsync("你已经注册过了！");
                return;
            }

            Account.Add(args.SenderUin, "default");

            await args.ReplyAsync($"注册成功！\nQQ: {args.SenderUin}\n默认权限组: default");
        }
    }

    [Command("info")]
    [CommandType(CommandType.Group)]
    [Permission("vortex.user.info")]
    public static class InfoCmd
    {
        [Main]
        public static async Task Execute(GroupCommandArgs args, [Param("目标QQ号")] long targetUin)
        {
            var targetAccount = Account.GetByUserId(targetUin);
            if (targetAccount == null)
            {
                await args.ReplyAsync("该用户不存在！");
                return;
            }

            var currency = Currency.Query(targetUin);
            var sign = Sign.Query(targetUin);

            var reply = $"用户信息\n" +
                        $"QQ: {targetUin}\n" +
                        $"权限组: {targetAccount.Group.Name}\n" +
                        $"金币: {currency?.Num ?? 0}\n" +
                        $"连续签到: {sign?.Date ?? 0} 天";

            await args.ReplyAsync(reply);
        }
    }
}
