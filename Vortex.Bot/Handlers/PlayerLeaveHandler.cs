using Lagrange.Core;
using Lagrange.Core.Message;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Models;
using Vortex.Bot.Processing;
using Vortex.Protocol.Packets;

namespace Vortex.Bot.Handlers;

public class PlayerLeaveHandler(
    VortexContext vortexContext,
    BotContext botContext,
    VortexSocketService socketService,
    TerrariaServerService serverService)
    : RoutedPushHandlerBase<PlayerLeavePacket>(vortexContext, botContext, socketService, serverService)
{
    public override void Handle(PlayerLeavePacket packet, PacketRouteContext context)
    {
        var message = new MessageBuilder()
            .Text($"[{context.ClientName}] 玩家 {packet.Player.Name} 离开服务器")
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
