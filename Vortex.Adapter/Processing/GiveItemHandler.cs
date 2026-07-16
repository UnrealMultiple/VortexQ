using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Packets;

namespace Vortex.Adapter.Processing;

public sealed class GiveItemHandler(Net.VortexClient client) : RequestHandlerBase<GiveItemPacket, GiveItemPacketResponse>(client)
{
    public override GiveItemPacketResponse Handle(GiveItemPacket request)
    {
        if (string.IsNullOrWhiteSpace(request.PlayerName))
            return CreateFailureResponse(request, "玩家名称不能为空");

        if (request.MessageOnly)
        {
            try
            {
                var player = OnlinePlayerLookup.Find(request.PlayerIndex, request.PlayerName);
                if (player == null)
                    return CreateFailureResponse(request, "目标玩家不在线");

                var color = request.Color.Length >= 3 ? request.Color : [255, 255, 255];
                player.SendMessage(request.Notification, color[0], color[1], color[2]);
                return CreateSuccessResponse(request, "消息发送成功");
            }
            catch (Exception ex)
            {
                return CreateFailureResponse(request, ex.Message);
            }
        }

        if (request.ItemId <= 0 || request.Quantity <= 0)
            return CreateFailureResponse(request, "物品 ID 和数量必须大于 0");

        try
        {
            var player = OnlinePlayerLookup.Find(request.PlayerIndex, request.PlayerName);
            if (player == null)
                return CreateFailureResponse(request, "目标玩家不在线");

            if (!player.GiveItemCheck(request.ItemId, request.ItemId.ToString(), request.Quantity, request.Prefix))
                return CreateFailureResponse(request, "服务器拒绝发放该物品");

            return CreateSuccessResponse(request, "物品发放成功");
        }
        catch (Exception ex)
        {
            return CreateFailureResponse(request, ex.Message);
        }
    }
}
