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
        var miscConfig = args.Context.Configuration.Miscellaneous;
        var (min, max) = ParseRange(miscConfig.SginCurrencyAcquisitionRange);
        var rand = new Random();
        var reward = rand.Next(min, max + 1);
        var currencyName = miscConfig.CurrencyName;

        try
        {
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
                .AddItem("本次获得", $"{reward} {currencyName}")
                .AddItem($"{currencyName}总数", $"{currency.Num}");
            await args.ReplyImageAsync(builder.Build());
        }
        catch (InvalidOperationException)
        {
            await args.ReplyWithAtAsync("你今天已经签到过了哦！");
        }
    }

    private static (int min, int max) ParseRange(string range)
    {
        var parts = range.Split('-');
        if (parts.Length == 2 &&
            int.TryParse(parts[0].Trim(), out var min) &&
            int.TryParse(parts[1].Trim(), out var max))
        {
            return (min, max);
        }
        return (100, 300);
    }
}
