using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Packets;
using TShockAPI;

namespace Vortex.Adapter.Processing;

public class ExecuteCommandHandler(Net.VortexClient client) : RequestHandlerBase<ExecuteCommandPacket, ExecuteCommandPacketResponse>(client)
{
    public override ExecuteCommandPacketResponse Handle(ExecuteCommandPacket request)
    {
        var player = new OneBotPlayer("VortexBot");
        try
        {
            var success = Commands.HandleCommand(player, request.Text) && !player.HasCommandError;
            return new ExecuteCommandPacketResponse
            {
                RequestId = request.RequestId,
                Success = success,
                Message = success ? "执行成功" : string.Join("\n", player.CommandOutput),
                Params = player.CommandOutput
            };
        }
        catch (Exception ex)
        {
            return new ExecuteCommandPacketResponse
            {
                RequestId = request.RequestId,
                Success = false,
                Message = ex.Message,
                Params = player.CommandOutput
            };
        }
    }
}
