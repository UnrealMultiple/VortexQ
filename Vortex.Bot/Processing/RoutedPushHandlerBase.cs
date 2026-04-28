using Lagrange.Core;
using Lagrange.Core.Common.Interface;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Models;
using Vortex.Protocol.Interfaces;

namespace Vortex.Bot.Processing;

public abstract class RoutedPushHandlerBase<TRequest>(
    VortexContext vortexContext,
    BotContext botContext,
    VortexSocketService socketService,
    TerrariaServerService? serverService = null)
    where TRequest : INetPacket
{
    protected VortexContext VortexContext { get; } = vortexContext;
    protected BotContext BotContext { get; } = botContext;
    protected VortexSocketService SocketService { get; } = socketService;
    protected TerrariaServerService? ServerService { get; } = serverService;

    public abstract void Handle(TRequest packet, PacketRouteContext context);

    protected Task<bool> SendToClientAsync(Guid clientId, INetPacket packet)
        => SocketService.SendToClientAsync(clientId, packet);

    protected Task<int> BroadcastAsync(INetPacket packet)
        => SocketService.BroadcastAsync(packet);

    protected IEnumerable<TerrariaServer> GetAllServers()
        => ServerService?.GetAllServers() ?? [];

    protected TerrariaServer? GetServer(string name)
        => ServerService?.GetServer(name);

    protected void SendGroupMessage(long groupId, Lagrange.Core.Message.MessageChain message)
        => BotContext.SendGroupMessage(groupId, message);
}
