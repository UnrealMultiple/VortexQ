using Vortex.Bot.Attributes;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.BuiltIn;

[Command("sign", "签到")]
[HelpText("每日签到获取金币")]
[CommandType(CommandType.Group)]
[Permission("vortex.sign")]
[DefaultCommand]
public static class SignCommand
{
    [Main]
    public static async Task SignIn(GroupCommandArgs args)
    {
        var rand = new Random();
        var reward = rand.Next(10, 100);

        var sign = Sign.DoSignIn(args.SenderUin);
        var currency = Currency.Add(args.SenderUin, reward);

        var builder = ProfileItemBuilder.Create()
            .SetTitle("签到")
            .SetMemberUin(args.SenderUin)
            .SetAvatarSize(150)
            .SetTitleFontSize(50)
            .AddItem("QQ账号", $"{args.SenderUin}")
            .AddItem("昵称", $"{args.SenderDisplayName}")
            .AddItem("连续签到", $"{sign.Date} 天")
            .AddItem("本次获得", $"{reward} 金币")
            .AddItem("金币总数", $"{currency.Num}");

        await args.ReplyImageAsync(builder.Build());
    }
}
