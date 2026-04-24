using Vortex.Bot.Models;
using Vortex.Protocol.Interfaces;

namespace Vortex.Bot.Interface;

public interface IRoutedPacketHandler<in TRequest, TResponse>
    where TRequest : IServicePacket
    where TResponse : IClientPacket, new()
{
    Task<TResponse> HandleAsync(TRequest request, PacketRouteContext context);
}
