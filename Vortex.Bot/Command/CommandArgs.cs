using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Events.EventArgs;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entities;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Database;
using Vortex.Bot.Database.Models;

namespace Vortex.Bot.Command;

public abstract class CommandArgs(VortexContext context, List<string> @params, BotMessageEvent? messageEvent)
{
    public VortexContext Context { get; init; } = context;
    public BotContext BotContext => Context.BotContext;
    public IDatabaseService Database => Context.Database;
    public ILogger Logger => Context.Logger;
    public List<string> Params { get; init; } = @params;
    public BotMessageEvent? MessageEvent { get; init; } = messageEvent;
    public string CommandName { get; set; } = string.Empty;
    public string CommandPrefix { get; set; } = string.Empty;
    public BotMessage? Message => MessageEvent?.Message;
    public MessageChain? MessageChain => Message?.Entities;
    public long SenderUin => Message?.Contact.Uin ?? 0;
#pragma warning disable CS8618
    public Account Account { get; private set; }
#pragma warning restore CS8618
    public Group Group { get; private set; } = DefaultGroup.Instance;
    public bool IsSuperAdmin { get; private set; }
    protected void InitializeAccount()
    {
        if (SenderUin > 0)
        {
            IsSuperAdmin = Context.Configuration.SuperAdmins.Contains(SenderUin);
            Account = Account.GetOrDefault(SenderUin);
            Group = Account.Group;
        }
    }

    public bool HasPermission(string permission)
    {
        if (IsSuperAdmin)
            return true;

        return Group.HasPermission(permission);
    }

    public abstract Task ReplyAsync(string message);

    public abstract Task ReplyAsync(MessageChain chain);

    public abstract Task ReplyImageAsync(byte[] imageData);

    public string GetTextContent()
    {
        if (MessageChain == null) return string.Empty;
        return string.Join("", MessageChain.OfType<TextEntity>().Select(t => t.Text));
    }
}

public class GroupCommandArgs : CommandArgs
{
    public long GroupUin { get; init; }

    public BotGroupMember? Member => Message?.Contact as BotGroupMember;
    public string? SenderDisplayName => Member?.MemberCard ?? Member?.Nickname;

    public GroupCommandArgs(VortexContext context, List<string> @params, BotMessageEvent messageEvent)
        : base(context, @params, messageEvent)
    {
        GroupUin = Message?.Contact is BotGroupMember groupMember
            ? groupMember.Group.GroupUin
            : 0;
        InitializeAccount();
    }

    public override async Task ReplyAsync(string message)
    {
        var builder = new MessageBuilder();
        builder.Text(message);
        await BotContext.SendGroupMessage(GroupUin, builder.Build());
    }

    public override async Task ReplyAsync(MessageChain chain)
    {
        await BotContext.SendGroupMessage(GroupUin, chain);
    }

    public override async Task ReplyImageAsync(byte[] imageData)
    {
        var builder = new MessageBuilder();
        builder.Image(imageData);
        await BotContext.SendGroupMessage(GroupUin, builder.Build());
    }

    public async Task ReplyWithAtAsync(string message)
    {
        var builder = new MessageBuilder();
        builder.Mention(SenderUin, SenderDisplayName ?? "");
        builder.Text(" " + message);
        await BotContext.SendGroupMessage(GroupUin, builder.Build());
    }
}

public class PrivateCommandArgs : CommandArgs
{
    public long FriendUin => SenderUin;

    public BotFriend? Friend => Message?.Contact as BotFriend;

    public string? FriendNickname => Friend?.Nickname;

    public PrivateCommandArgs(VortexContext context, List<string> @params, BotMessageEvent messageEvent)
        : base(context, @params, messageEvent)
    {
        InitializeAccount();
    }

    public override async Task ReplyAsync(string message)
    {
        var builder = new MessageBuilder();
        builder.Text(message);
        await BotContext.SendFriendMessage(FriendUin, builder.Build());
    }

    public override async Task ReplyAsync(MessageChain chain)
    {
        await BotContext.SendFriendMessage(FriendUin, chain);
    }

    public override async Task ReplyImageAsync(byte[] imageData)
    {
        var builder = new MessageBuilder();
        builder.Image(imageData);
        await BotContext.SendFriendMessage(FriendUin, builder.Build());
    }
}

public class ServerCommandArgs : CommandArgs
{
    public string ExecutorName { get; init; }
    public bool HasServerPermission { get; init; }

    public ServerCommandArgs(VortexContext context, List<string> @params, string executorName, bool hasServerPermission)
        : base(context, @params, null)
    {
        ExecutorName = executorName;
        HasServerPermission = hasServerPermission;
    }

    public new bool HasPermission(string permission)
    {
        if (HasServerPermission)
            return true;

        return base.HasPermission(permission);
    }

    public override Task ReplyAsync(string message)
    {
        Logger.LogInformation($"[{ExecutorName}] {message}");
        return Task.CompletedTask;
    }

    public override Task ReplyAsync(MessageChain chain)
    {
        var text = string.Join("", chain.OfType<TextEntity>().Select(t => t.Text));
        Logger.LogInformation($"[{ExecutorName}] {text}");
        return Task.CompletedTask;
    }

    public override Task ReplyImageAsync(byte[] imageData)
    {
        Logger.LogInformation($"[{ExecutorName}] [Image] {imageData.Length} bytes");
        return Task.CompletedTask;
    }
}
