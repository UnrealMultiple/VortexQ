using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Packets;
using TShockAPI;

namespace Vortex.Adapter.Processing;

public class ExecuteCommandHandler(Net.VortexClient client) : RequestHandlerBase<ExecuteCommandPacket, ExecuteCommandPacketResponse>(client)
{
    public override ExecuteCommandPacketResponse Handle(ExecuteCommandPacket request)
    {
        var player = new OneBotPlayer("VortexBot");
        Commands.HandleCommand(player, request.Text);
        return new ExecuteCommandPacketResponse
        {
            RequestId = request.RequestId,
            Success = true,
            Message = "执行成功",
            Params = player.CommandOutput
        };
    }
}
