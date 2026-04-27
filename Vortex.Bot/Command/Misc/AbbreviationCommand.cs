using System.Text.Json.Nodes;
using Vortex.Bot.Attributes;
using Vortex.Bot.Utility;

namespace Vortex.Bot.Command.Misc;

[Command("缩写", "abbr")]
[HelpText("查询网络缩写含义")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.misc.abbreviation")]
public static class AbbreviationCommand
{
    [Main]
    [Flexible]
    public static async Task Execute(CommandArgs args, [Param("缩写")] string? abbreviation = null)
    {
        if (string.IsNullOrEmpty(abbreviation))
        {
            await args.ReplyWithAtAsync("请输入要查询的缩写");
            return;
        }

        try
        {
            string url = $"https://oiapi.net/API/Nbnhhsh?text={Uri.EscapeDataString(abbreviation)}";
            var result = await HttpUtility.GetStringAsync(url);

            var data = JsonNode.Parse(result);
            var trans = data?["data"]?[0]?["trans"]?.AsArray();

            if (trans != null && trans.Count != 0)
            {
                var meanings = trans.Select(t => t?.ToString()).Where(s => !string.IsNullOrEmpty(s));
                await args.ReplyWithAtAsync($"缩写 `{abbreviation}` 可能为:\n{string.Join(", ", meanings)}");
            }
            else
            {
                await args.ReplyWithAtAsync("也许该缩写没有被收录!");
            }
        }
        catch (Exception ex)
        {
            await args.ReplyWithAtAsync($"查询失败: {ex.Message}");
        }
    }
}
