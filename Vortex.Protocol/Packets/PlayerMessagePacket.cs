using Vortex.Protocol.Enums;
using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Models;

namespace Vortex.Protocol.Packets;

public class PlayerMessagePacket : IServerPushPacket
{
    public PacketType PacketID => PacketType.PlayerMessage;

    public string Message { get; set; } = string.Empty;

    public bool IsCommand { get; set; }

    public Player Player { get; set; } = new();
}
