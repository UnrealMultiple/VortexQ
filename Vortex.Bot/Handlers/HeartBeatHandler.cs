using Vortex.Bot.Models;
using Vortex.Bot.Processing;
using Vortex.Protocol.Packets;

namespace Vortex.Bot.Handlers;

public class HeartBeatHandler : RoutedPushHandlerBase<HeartBeatPacket>
{
    public override void Handle(HeartBeatPacket packet, PacketRouteContext context)
    {
       
    }
}
