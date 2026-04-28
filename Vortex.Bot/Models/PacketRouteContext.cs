using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Models;

public sealed class PacketRouteContext
{
    public required Guid SenderClientId { get; init; }
    public required int SenderSessionId { get; init; }
    public required ClientConnection SenderConnection { get; init; }
    public DateTime ReceiveTime { get; init; } = DateTime.Now;
    public required string ClientName { get; init; }
    public required TerrariaServer? Server { get; init; }
}
