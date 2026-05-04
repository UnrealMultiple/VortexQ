using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Terraria;

[Command("在线", "online", "players")]
[HelpText("查看在线玩家")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.online")]
[DefaultCommand]
public static class OnlinePlayersCommand
{
    [Main]
    public static async Task ShowOnlinePlayers(GroupCommandArgs args)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyWithAtAsync("服务器管理器未初始化");
            return;
        }

        var groupId = args.GroupUin;
        var servers = serverManager.GetServersByGroup(groupId);

        if (!servers.Any())
        {
            await args.ReplyWithAtAsync("此群未配置任何服务器!");
            return;
        }
        var builder = ServerOnlineBuilder.Create();

        foreach (var server in servers)
        {
            var online = await server.GetOnlinePlayersAsync();
            var playerCount = online?.Players?.Count ?? 0;
            var maxCount = online?.MaxCount ?? 0;

            if (online?.Success == true && online.Players != null)
            {
                builder.AddSection(server.Config.Name, playerCount, maxCount, online.Players);
            }
            else
            {
                builder.AddSection(server.Config.Name, 0, maxCount, []);
            }
        }

        try
        {
            var imageData = builder.Build();
            await args.ReplyImageAsync(imageData);
        }
        catch (Exception ex)
        {
            args.Logger.LogError(ex, "生成在线玩家图片失败");
            await args.ReplyWithAtAsync("生成在线玩家图片失败，请稍后重试");
        }
    }
}
