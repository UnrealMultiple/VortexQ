using Daily.Database;
using Lagrange.Core.Message;

using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Extension;
using Vortex.Bot.Utility;

namespace Daily;

[Command("jrrp", "今日人品")]
[CommandType(CommandType.Group)]
[Permission("vortex.jrrp")]
public static class JrrpCommand
{
    private static readonly Random _random = new();
    private const string Url = "https://oiapi.net/API/Yun/";

    [Main]
    public static async ValueTask ExecuteAsync(GroupCommandArgs args)
    {
        int jrrp;
        if (JrrpRecord.GetRecord(args.SenderUin) is { } record && record.Date.Date == DateTime.Now.Date)
        {
            jrrp = record.Luck;
        }
        else
        {
            jrrp = _random.Next(1, 101);
            JrrpRecord.AddOrUpdateRecord(args.SenderUin, jrrp);
        }

        var imageBuffer = await HttpUtility.GetByteAsync(Url, new Dictionary<string, string>
        {
            { "let", $"{args.SenderUin}{jrrp}" },
        });

        // 构建转发消息（MultiMsgEntity 内部会自动预处理子消息中的图片等实体）
        var nickname = args.Member?.Nickname ?? "";
        var now = DateTime.Now;
        var msg = MessageBuilder.Create().MultiMsg([
            BotMessage.CreateCustomGroup(args.GroupUin, args.SenderUin, nickname, now,
                MessageBuilder.Create().Image(imageBuffer).Build()),
            BotMessage.CreateCustomGroup(args.GroupUin, args.SenderUin, nickname, now,
                MessageBuilder.Create().Text($"今日人品：{jrrp}").Build())
        ]).Build();
        var res = await args.ReplyAsync(msg);
        Console.WriteLine(res.Type);
    }
}
