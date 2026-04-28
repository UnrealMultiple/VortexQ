using Lagrange.Core;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Models;
using Vortex.Bot.Processing;
using Vortex.Protocol.Packets;

namespace Vortex.Bot.Handlers;

public class HeartBeatHandler(
    VortexContext vortexContext,
    BotContext botContext,
    VortexSocketService socketService)
    : RoutedPushHandlerBase<HeartBeatPacket>(vortexContext, botContext, socketService)
{
    public override void Handle(HeartBeatPacket packet, PacketRouteContext context)
    {
    }
}
