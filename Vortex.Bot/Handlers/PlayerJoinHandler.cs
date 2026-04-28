using Lagrange.Core;
using Lagrange.Core.Message;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Models;
using Vortex.Bot.Processing;
using Vortex.Protocol.Packets;

namespace Vortex.Bot.Handlers;

public class PlayerJoinHandler(
    VortexContext vortexContext,
    BotContext botContext,
    VortexSocketService socketService,
    TerrariaServerService serverService)
    : RoutedPushHandlerBase<PlayerJoinPacket>(vortexContext, botContext, socketService, serverService)
{
    public override void Handle(PlayerJoinPacket packet, PacketRouteContext context)
    {
        var message = new MessageBuilder()
            .Text($"[{context.ClientName}] 玩家 {packet.Player.Name} 加入服务器")
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
