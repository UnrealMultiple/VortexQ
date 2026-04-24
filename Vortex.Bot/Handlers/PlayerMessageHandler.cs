using Lagrange.Core.Common.Interface;
using Lagrange.Core.Message;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Models;
using Vortex.Bot.Processing;
using Vortex.Protocol.Packets;

namespace Vortex.Bot.Handlers;

public class PlayerMessageHandler : RoutedPushHandlerBase<PlayerMessagePacket>
{
    public override void Handle(PlayerMessagePacket packet, PacketRouteContext context)
    {
        if(packet.IsCommand) return;
        var servers = Context?.Server?.Services.GetService<TerrariaServerService>();
        if (servers == null) return;
        var message = new MessageBuilder()
            .Text($"Íæ¼Ò {packet.Player.Name}: {packet.Message}")
            .Build();
        foreach (var server in servers.GetAllServers())
        {
            foreach(var groupid in server.Config.Groups)
            {
                Context?.BotContext.SendGroupMessage(groupid, message);
            }
        }
    }
}
