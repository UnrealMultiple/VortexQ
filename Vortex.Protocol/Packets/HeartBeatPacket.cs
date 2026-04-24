using Vortex.Protocol.Enums;
using Vortex.Protocol.Interfaces;

namespace Vortex.Protocol.Packets;

public class HeartBeatPacket : IServerPushPacket
{
    public PacketType PacketID => PacketType.HeartBeat;
}
