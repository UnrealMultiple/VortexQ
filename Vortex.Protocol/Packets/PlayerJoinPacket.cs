using Vortex.Protocol.Enums;
using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Models;

namespace Vortex.Protocol.Packets;

public class PlayerJoinPacket : IServerPushPacket
{
    public PacketType PacketID => PacketType.PlayerJoin;

    public Player Player { get; set; } = new();
}
