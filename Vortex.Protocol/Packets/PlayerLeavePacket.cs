using Vortex.Protocol.Enums;
using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Models;

namespace Vortex.Protocol.Packets;

public class PlayerLeavePacket : IServerPushPacket
{
    public PacketType PacketID => PacketType.PlayerLeave;

    public Player Player { get; set; } = new();
}
