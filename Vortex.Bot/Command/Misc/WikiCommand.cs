using System.Web;
using Vortex.Bot.Attributes;

namespace Vortex.Bot.Command.Misc;

[Command("wiki")]
[HelpText("泰拉瑞亚Wiki搜索")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.misc.wiki")]
public static class WikiCommand
{
    [Main]
    [Flexible]
    public static async Task Execute(CommandArgs args, [Param("搜索内容(可选)")] string? searchTerm = null)
    {
        var baseUrl = "https://terraria.wiki.gg/zh/index.php";
        var url = string.IsNullOrEmpty(searchTerm) ? baseUrl : $"{baseUrl}?search={HttpUtility.UrlEncode(searchTerm)}";
        await args.ReplyWithAtAsync(url);
    }
}
