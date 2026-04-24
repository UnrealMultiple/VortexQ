using Vortex.Bot.Core.Service;
using Vortex.Bot.Models;
using Vortex.Protocol.Interfaces;

namespace Vortex.Bot.Processing;

public abstract class RoutedPushHandlerBase<TRequest>
    where TRequest : INetPacket
{
    public VortexContext? Context { get; set; }

    public VortexSocketService? Server { get; set; }

    public abstract void Handle(TRequest packet, PacketRouteContext context);

    protected async Task<bool> SendToClientAsync(Guid clientId, INetPacket packet)
    {
        if (Server == null) return false;
        return await Server.SendToClientAsync(clientId, packet);
    }

    protected async Task<int> BroadcastAsync(INetPacket packet)
    {
        if (Server == null) return 0;
        return await Server.BroadcastAsync(packet);
    }
}
