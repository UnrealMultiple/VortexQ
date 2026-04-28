using Lagrange.Core;
using Lagrange.Core.Message;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Models;
using Vortex.Bot.Processing;
using Vortex.Protocol.Packets;

namespace Vortex.Bot.Handlers;

public class PlayerMessageHandler(
    VortexContext vortexContext,
    BotContext botContext,
    VortexSocketService socketService,
    TerrariaServerService serverService,
    Command.CommandManager commandManager)
    : RoutedPushHandlerBase<PlayerMessagePacket>(vortexContext, botContext, socketService, serverService)
{
    private readonly Command.CommandManager _commandManager = commandManager;

    public override void Handle(PlayerMessagePacket packet, PacketRouteContext context)
    {
        if (packet.IsCommand)
        {
            _ = _commandManager.ExecuteServerAsync(packet.Message, VortexContext, packet.Player, context.SenderSessionId);
            return;
        }

        var message = new MessageBuilder()
            .Text($"[{context.ClientName}] {packet.Player.Name}: {packet.Message}")
            .Build();

        if (context.Server?.Config.ForwardGroups != null)
        {
            foreach (var groupId in context.Server.Config.ForwardGroups)
            {
                SendGroupMessage(groupId, message);
            }
        }
    }
}
