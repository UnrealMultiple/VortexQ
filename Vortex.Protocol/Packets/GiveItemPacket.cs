using Vortex.Protocol.Enums;
using Vortex.Protocol.Interfaces;

namespace Vortex.Protocol.Packets;

public class GiveItemPacket : IServicePacket
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public PacketType PacketID => PacketType.GiveItem;
    public string PlayerName { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Stack { get; set; }
}

public class GiveItemPacketResponse : IClientPacket
{
    public Guid RequestId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PacketType PacketID => PacketType.GiveItemResponse;
}
