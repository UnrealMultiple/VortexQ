using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Database.Models;

namespace Vortex.Bot.Commands;

[Command("sign", "签到")]
[CommandType(CommandType.Group)]
[Permission("vortex.sign")]
public static class SignCommand
{
    [Main]
    public static async Task SignIn(GroupCommandArgs args)
    {
        var account = args.Account;
        if (account == null)
        {
            await args.ReplyAsync("请先注册账号！");
            return;
        }

        var rand = new Random();
        var reward = rand.Next(10, 100);

        var sign = Sign.DoSignIn(args.SenderUin);
        var currency = Currency.Add(args.SenderUin, reward);

        var reply = $"签到成功！\n" +
                    $"QQ: {args.SenderUin}\n" +
                    $"昵称: {args.SenderDisplayName}\n" +
                    $"连续签到: {sign.Date} 天\n" +
                    $"本次获得: {reward} 金币\n" +
                    $"金币总数: {currency.Num}";

        await args.ReplyAsync(reply);
    }
}
