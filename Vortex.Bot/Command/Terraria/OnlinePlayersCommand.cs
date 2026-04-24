using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Core.Service;

namespace Vortex.Bot.Command.Terraria;

[Command("在线", "online", "players")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.terraria.online")]
public static class OnlinePlayersCommand
{
    [Main]
    public static async Task ShowOnlinePlayers(CommandArgs args)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyAsync("服务器管理器未初始化");
            return;
        }

        var groupId = args is GroupCommandArgs groupArgs ? groupArgs.GroupUin : 0;
        var servers = groupId > 0
            ? [.. serverManager.GetServersByGroup(groupId)]
            : serverManager.GetAllServers().ToList();

        if (servers.Count == 0)
        {
            await args.ReplyAsync("此群未配置任何服务器!");
            return;
        }

        var sb = new StringBuilder();
        foreach (var server in servers)
        {
            var online = await server.GetOnlinePlayersAsync();
            Console.WriteLine(online == null);
            var playerCount = online?.Players?.Count ?? 0;
            var maxCount = online?.MaxCount ?? 0;

            sb.AppendLine($"[{server.Config.Name}] 在线玩家 ({playerCount}/{maxCount})");

            if (online?.Success == true && online.Players != null && online.Players.Count > 0)
            {
                sb.AppendLine(string.Join(", ", online.Players.Select(p => p.Name)));
            }
            else if (online?.Success == false)
            {
                sb.AppendLine("无法连接服务器");
            }
            else
            {
                sb.AppendLine("暂无在线玩家");
            }
            sb.AppendLine();
        }

        await args.ReplyAsync(sb.ToString().Trim());
    }
}
