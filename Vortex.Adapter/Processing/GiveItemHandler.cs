using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Packets;
using TShockAPI;

namespace Vortex.Adapter.Processing;

public sealed class GiveItemHandler(Net.VortexClient client) : RequestHandlerBase<GiveItemPacket, GiveItemPacketResponse>(client)
{
    public override GiveItemPacketResponse Handle(GiveItemPacket request)
    {
        if (request.ItemId <= 0 || request.Stack <= 0)
            return CreateFailureResponse(request, "物品 ID 或数量无效。");

        var players = TSPlayer.FindByNameOrID($"tsn:{request.PlayerName}")
            .Where(static player => player.Active)
            .ToArray();
        if (players.Length == 0)
            return CreateFailureResponse(request, "目标玩家不在线。");
        if (players.Length > 1)
            return CreateFailureResponse(request, "找到多个同名玩家，无法发放物品。");

        try
        {
            var success = players[0].GiveItemCheck(request.ItemId, request.ItemName, request.Stack, 0);
            return success
                ? CreateSuccessResponse(request, "物品发放成功。")
                : CreateFailureResponse(request, "物品被服务器规则拒绝。");
        }
        catch (Exception ex)
        {
            return CreateFailureResponse(request, ex.Message);
        }
    }
}
