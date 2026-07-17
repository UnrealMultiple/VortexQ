using Vortex.Protocol.Enums;
using Vortex.Protocol.Interfaces;

namespace Vortex.Protocol.Packets;

public class PrivateMessagePacket : IServicePacket
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public PacketType PacketID => PacketType.PrivateMessage;

    public string Text { get; set; } = string.Empty;
    public byte[] Color { get; set; } = Array.Empty<byte>();
    public string Name { get; set; } = string.Empty;
    public int PlayerIndex { get; set; } = -1;
}

public class PrivateMessagePacketResponse : IClientPacket
{
    public Guid RequestId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PacketType PacketID => PacketType.PrivateMessageResponse;
}
