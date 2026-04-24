using Vortex.Bot.Core.Service;
using Vortex.Bot.Models;
using Vortex.Protocol.Interfaces;

namespace Vortex.Bot.Processing;

public abstract class RoutedRequestHandlerBase<TRequest, TResponse>
    where TRequest : IServicePacket
    where TResponse : IClientPacket, new()
{
    public VortexContext? Context { get; set; }

    public VortexSocketService? Server { get; set; }

    public abstract TResponse Handle(TRequest request, PacketRouteContext context);

    protected static TResponse CreateSuccessResponse(TRequest request, string message = "Success")
    {
        return new TResponse
        {
            RequestId = request.RequestId,
            Success = true,
            Message = message
        };
    }

    protected static TResponse CreateFailureResponse(TRequest request, string message)
    {
        return new TResponse
        {
            RequestId = request.RequestId,
            Success = false,
            Message = message
        };
    }

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
