using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Events.EventArgs;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Models;
using Vortex.Bot.Command.Verification;
using Vortex.Protocol.Packets;

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
    public Account Account { get; protected set; }
#pragma warning restore CS8618
    public Group Group { get; protected set; } = DefaultGroup.Instance;
    public bool IsSuperAdmin { get; protected set; }
    protected void InitializeAccount()
    {
        if (SenderUin > 0)
        {
            IsSuperAdmin = Context.Configuration.SuperAdmins.Contains(SenderUin);
            Account = Account.GetOrDefault(SenderUin);
            Group = Account.Group;
        }
    }

    public bool HasPermission(string permission) => IsSuperAdmin || Group.HasPermission(permission);

    public abstract Task ReplyAsync(string message);

    public abstract Task ReplyAsync(MessageChain chain);

    public abstract Task ReplyImageAsync(byte[] imageData);

    public abstract Task ReplyWithAtAsync(string message);

    public string GetTextContent() => MessageChain == null ? string.Empty : string.Join("", MessageChain.OfType<TextEntity>().Select(t => t.Text));

    public TypedVerificationManager.TypedVerification CreateVerification(
        string actionType,
        string actionName,
        int timeoutSeconds = 60,
        object? data = null,
        string? verifyKey = null)
    {
        long groupId = this is GroupCommandArgs groupArgs ? groupArgs.GroupUin : 0;
        return TypedVerificationManager.Create(SenderUin, groupId, actionType, actionName, timeoutSeconds, data, verifyKey);
    }

    public TypedVerificationManager.VerificationResult Verify(string actionType, string? verifyKey = null)
    {
        long groupId = this is GroupCommandArgs groupArgs ? groupArgs.GroupUin : 0;
        return TypedVerificationManager.Verify(SenderUin, groupId, actionType, verifyKey);
    }

    public TypedVerificationManager.VerificationResult CancelVerification(string actionType)
    {
        long groupId = this is GroupCommandArgs groupArgs ? groupArgs.GroupUin : 0;
        return TypedVerificationManager.Cancel(SenderUin, groupId, actionType);
    }

    public TypedVerificationManager.TypedVerification? GetPendingVerification(string actionType, string? verifyKey = null)
    {
        long groupId = this is GroupCommandArgs groupArgs ? groupArgs.GroupUin : 0;
        return TypedVerificationManager.GetPending(SenderUin, groupId, actionType, verifyKey);
    }

    public Task StartVerificationTimeoutAsync(string actionType, Func<TypedVerificationManager.TypedVerification, Task> onTimeout, string? verifyKey = null)
    {
        long groupId = this is GroupCommandArgs groupArgs ? groupArgs.GroupUin : 0;
        return TypedVerificationManager.StartTimeoutMonitorAsync(SenderUin, groupId, actionType, onTimeout, verifyKey);
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

    public override async Task ReplyWithAtAsync(string message)
    {
        var builder = new MessageBuilder();
        builder.Reply(Message!);
        builder.Text(message);
        await ReplyAsync(builder.Build());
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

    public override async Task ReplyWithAtAsync(string message)
    {
        var builder = new MessageBuilder();
        builder.Reply(Message!);
        builder.Text(message);
        await ReplyAsync(builder.Build());
    }
}

public class ServerCommandArgs : CommandArgs
{
    public Protocol.Models.Player Player { get; init; }

    public int SessionId { get; init; }

    public VortexSocketService? VortexSocketService => Context.Server;

    public ClientConnection? SenderConnection => VortexSocketService?.Connections.GetClientBySession(SessionId);

    public TerrariaUser? User => TerrariaUser.GetUserByName(Player.Name, SenderConnection?.ClientName ?? "");

    public TerrariaServerService? TerrariaServerService => Context.Server?.Services.GetService<TerrariaServerService>();

    public TerrariaServer? Server => TerrariaServerService?.GetServer(SenderConnection?.ClientName ?? "");

    public ServerCommandArgs(VortexContext context, List<string> @params, Protocol.Models.Player player, int sessionId) : base(context, @params, null)
    {
        Player = player;
        SessionId = sessionId;
        Account = Account.GetOrDefault(User?.Id ?? 0);
        Group = Account.Group;
        IsSuperAdmin = Context.Configuration.SuperAdmins.Contains(User?.Id ?? 0);
    }

    public new bool HasPermission(string permission)
    {
        return base.HasPermission(permission);
    }

    public override async Task ReplyAsync(string message)
    {
#pragma warning disable CS8602
        _ = await VortexSocketService?.SendToSessionAsync(SessionId, new PrivateMessagePacket()
        {
            Text = message,
            Name = Player.Name,
            Color = [255, 255, 255]
        });
#pragma warning restore CS8602
    }

    public override async Task ReplyAsync(MessageChain chain)
    {
        var text = string.Join("", chain.OfType<TextEntity>().Select(t => t.Text));
        await ReplyAsync(text);
    }

    public override async Task ReplyImageAsync(byte[] imageData)
    {

    }

    public override async Task ReplyWithAtAsync(string message)
    {
        await ReplyAsync(message);
    }
}
