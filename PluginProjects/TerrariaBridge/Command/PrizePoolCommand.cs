using TerrariaBridge.Config;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Utility.Images;

namespace TerrariaBridge.Command;

[Command("奖池")]
[CommandType(CommandType.Group)]
public static class PrizePoolCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args)
    {
        var config = PrizeConfiguration.Instance;
        if (config.Prizes.Count == 0)
        {
            await args.ReplyWithAtAsync("奖池配置不可用，请检查 Prize.json。");
            return;
        }

        var table = TableBuilder.Create()
            .SetTitle($"奖池（每次 {config.DrawCost} {args.Context.Configuration.Miscellaneous.CurrencyName}）")
            .SetHeader("序号", "奖品", "权重", "数量")
            .SetMemberUin(args.SenderUin);

        foreach (var prize in config.Prizes.Select((prize, index) => (prize, index)))
            table.AddRow((prize.index + 1).ToString(), prize.prize.Name, prize.prize.Weight.ToString(), $"{prize.prize.MinimumQuantity}-{prize.prize.MaximumQuantity}");

        await args.ReplyImageAsync(table.Build());
    }
}