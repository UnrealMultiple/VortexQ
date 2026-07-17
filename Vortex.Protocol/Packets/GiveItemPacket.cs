using Vortex.Protocol.Enums;
using Vortex.Protocol.Interfaces;

namespace Vortex.Protocol.Packets;

public sealed class GiveItemPacket : IServicePacket
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public PacketType PacketID => PacketType.GiveItem;

    public string PlayerName { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public int Prefix { get; set; }
    public bool MessageOnly { get; set; }
    public string Notification { get; set; } = string.Empty;
    public int PlayerIndex { get; set; } = -1;
    public byte[] Color { get; set; } = [255, 255, 255];
}

public sealed class GiveItemPacketResponse : IClientPacket
{
    public Guid RequestId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PacketType PacketID => PacketType.GiveItemResponse;
}
