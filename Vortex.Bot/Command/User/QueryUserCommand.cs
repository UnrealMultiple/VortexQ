using Lagrange.Core.Common.Interface;
using Lagrange.Core.Message.Entities;
using Vortex.Bot.Attributes;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.User;

[Command("查", "query")]
[HelpText("查询用户信息")]
[CommandType(CommandType.Group)]
[Permission("vortex.user.query")]
public static class QueryUserCommand
{
    [Main]
    [Flexible]
    public static async Task Execute(GroupCommandArgs args, [Param("QQ号(可选)")] long? targetUserId = null)
    {
        long userId;
        var mention = args.MessageChain?.OfType<MentionEntity>().FirstOrDefault();
        if (mention != null)
        {
            userId = mention.Uin;
        }
        else if (targetUserId.HasValue)
        {
            userId = targetUserId.Value;
        }
        else if (args.Params.Count == 0)
        {
            userId = args.SenderUin;
        }
        else
        {
            await args.ReplyWithAtAsync("请@要查询的成员或输入QQ号");
            return;
        }

        var target = (await args.BotContext.FetchMembers(args.GroupUin)).FirstOrDefault(m => m.Uin == userId);
        var account = Account.GetOrDefault(userId);
        var currency = Currency.Query(userId);
        var sign = Sign.Query(userId);

        var builder = ProfileItemBuilder.Create()
            .SetMemberUin(userId)
            .SetAvatarSize(150)
            .SetTitleFontSize(50)
            .SetTitle("个人信息")
            .AddItem("QQ", userId.ToString())
            .AddItem("昵称", target?.Nickname ?? "未知")
            .AddItem("权限组", account.Group.Name)
            .AddItem("金币", (currency?.Num ?? 0).ToString())
            .AddItem("连续签到", $"{sign?.Date ?? 0} 天");

        await args.ReplyImageAsync(builder.Build());
    }
}
