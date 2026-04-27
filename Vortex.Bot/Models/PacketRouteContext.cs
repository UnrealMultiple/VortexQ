namespace Vortex.Bot.Models;

public class PacketRouteContext
{
    public Guid SenderClientId { get; set; }

    public int SenderSessionId { get; set; }

    public ClientConnection SenderConnection { get; set; } = null!;

    public DateTime ReceiveTime { get; set; } = DateTime.Now;
}
