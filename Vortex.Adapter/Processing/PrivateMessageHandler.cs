using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Packets;

namespace Vortex.Adapter.Processing;

public class PrivateMessageHandler(Net.VortexClient client) : RequestHandlerBase<PrivateMessagePacket, PrivateMessagePacketResponse>(client)
{
    public override PrivateMessagePacketResponse Handle(PrivateMessagePacket request)
    {
        Plugin.PendingPrivateMessages.Enqueue(request);
        return CreateSuccessResponse(request, "消息已加入发送队列");
    }
}
