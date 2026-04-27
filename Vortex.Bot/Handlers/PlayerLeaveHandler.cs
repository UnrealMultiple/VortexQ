using Lagrange.Core.Common.Interface;
using Lagrange.Core.Message;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Models;
using Vortex.Bot.Processing;
using Vortex.Protocol.Packets;

namespace Vortex.Bot.Handlers;

public class PlayerLeaveHandler : RoutedPushHandlerBase<PlayerLeavePacket>
{
    public override void Handle(PlayerLeavePacket packet, PacketRouteContext context)
    {
        var servers = Context?.Server?.Services.GetService<TerrariaServerService>();
        if (servers == null) return;
        var message = new MessageBuilder()
        .Text($"玩家 {packet.Player.Name}: 离开服务器...")
        .Build();
        foreach (var server in servers.GetAllServers())
        {
            foreach (var groupid in server.Config.Groups)
            {
                Context?.BotContext.SendGroupMessage(groupid, message);
            }
        }
    }
}
