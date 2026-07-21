using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Events.EventArgs;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entities;
using Microsoft.Extensions.Logging;
using Vortex.Bot;
using Vortex.Bot.Plugins;

namespace Reply;

[Plugin(Name = "Reply", Author = "少司命", Description = "关键词自动回复")]
public sealed class ReplyPlugin : PluginBase
{
    protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken)
    {
        ReplyAdapter.Logger = msg => Logger.LogInformation(msg);

        // 注册动态变量处理器（静态变量如 $uin $card $groupid 由消息上下文提供）
        ReplyAdapter.RegisterAsyncHandler("time", (name, param, chain, ctx) =>
            Task.FromResult(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

        // 注册内容处理器
        ReplyAdapter.RegisterContentHandler("image", async (type, content, bot, msg, chain, builder) =>
        {
            var bytes = FileReader.ReadFileBuffer(content);
            if (bytes.Length > 0)
                builder.Image(bytes);
        });

        ReplyAdapter.RegisterContentHandler("at", (type, content, bot, msg, chain, builder) =>
        {
            if (uint.TryParse(content, out var uin))
                builder.Mention(uin, null);
            return Task.CompletedTask;
        });

        ReplyAdapter.RegisterContentHandler("select", (type, content, bot, msg, chain, builder) =>
        {
            foreach (var mention in ReplyAdapter.GetMentions(chain))
                builder.Mention(mention.Uin, null);
            return Task.CompletedTask;
        });

        ReplyAdapter.RegisterContentHandler("forward", (type, content, bot, msg, chain, builder) =>
        {
            builder.Reply(msg); // 引用原消息，替代合并转发
            return Task.CompletedTask;
        });

        // 订阅群消息（使用 AsyncHandler 避免 .Result 死锁）
        Vortex.BotContext.EventInvoker.RegisterEvent<BotMessageEvent>(HandleGroupMessage);

        Logger.LogInformation("[Reply] 插件已初始化，已订阅群消息");
        return default;
    }

    protected override ValueTask OnShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("[Reply] 插件正在关闭");
        return default;
    }

    private async Task HandleGroupMessage(BotContext bot, BotMessageEvent e)
    {
        if (e.Message.Type != MessageType.Group) return;
        if (e.Message.Contact is not BotGroupMember member) return;
        if (member.Uin == bot.BotUin) return;

        var text = ReplyAdapter.GetText(e.Message.Entities);
        Logger.LogDebug("[Reply] 收到群消息: {Text}", text);

        try
        {
            var context = new Dictionary<string, string>
            {
                ["uin"] = member.Uin.ToString(),
                ["card"] = member.MemberCard ?? member.Nickname ?? "",
                ["groupid"] = member.Group.GroupUin.ToString()
            };

            var response = await ReplyAdapter.ProcessMessageAsync(e.Message.Entities, bot, e.Message, context);
            if (response == null) return;

            Logger.LogInformation("[Reply] 匹配成功，准备发送回复");
            await bot.SendGroupMessage(member.Group.GroupUin, response.Build());
            Logger.LogInformation("[Reply] 回复已发送");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Reply] 处理消息时出错");
        }
    }
}

