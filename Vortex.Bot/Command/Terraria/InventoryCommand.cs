using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Terraria;

[Command("背包", "inventory", "inv", "bag")]
[HelpText("查看玩家背包")]
[CommandType(CommandType.Group)]
[Permission("vortex.terraria.inventory")]
[DefaultCommand]
public static class InventoryCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args, [Param("玩家名称")] string playerName)
    {
        var serverManager = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (serverManager == null)
        {
            await args.ReplyWithAtAsync("服务器管理器未初始化");
            return;
        }

        if (!serverManager.TryGetUserServer(args.SenderUin, args.GroupUin, out var server) || server == null)
        {
            await args.ReplyWithAtAsync("服务器不存在或未切换至一个服务器!");
            return;
        }

        var result = await server.QueryPlayerInventoryAsync(playerName);

        if (result?.Success == true && result.PlayerData != null)
        {
            try
            {
                var user = TerrariaUser.GetUserByName(result.PlayerData.Username, server.Config.Name);
                var avatarUin = user?.Id ?? args.SenderUin;
                var builder = InventoryGenerateExtensions.FromPlayerData(result.PlayerData, server.Config.Name, avatarUin);
                await args.ReplyImageAsync(builder.Build());
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"生成背包图片失败: {ex.Message}");
            }
        }
        else
        {
            await args.ReplyWithAtAsync($"查询失败: {result?.Message ?? "无法连接服务器或玩家不存在"}");
        }
    }
}
