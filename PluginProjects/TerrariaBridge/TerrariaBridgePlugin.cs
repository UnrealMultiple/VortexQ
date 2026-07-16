using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Plugins;
using Vortex.Bot.Utility.Images;

namespace TerrariaBridge;

[Plugin(Name = "TerrariaBridge", Author = "VortexQ", Description = "泰拉瑞亚奖池与游戏内货币抽取", Major = 2, Minor = 1)]
public sealed class TerrariaBridgePlugin : PluginBase
{
    protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken)
    {
        TerrariaBridgeState.Load(Path.Combine(PluginDirectory, "Prize.json"));
        return default;
    }

    protected override ValueTask OnShutdownAsync(CancellationToken cancellationToken) => default;
}

[Command("抽")]
[CommandType(CommandType.Server)]
public static class DrawPrizeCommand
{
    private const string HighlightColor = "42B268";

    [Main]
    [Flexible]
    public static async Task Execute(ServerCommandArgs args)
    {
        if (args.User?.Id is not > 0 || args.Server == null)
        {
            await args.ReplyAsync("请先绑定 QQ 账号并连接服务器。");
            return;
        }

        if (!TryGetTimes(args, out var times))
        {
            await args.ReplyAsync("抽取次数必须是 1 到 100 之间的整数。");
            return;
        }

        var config = TerrariaBridgeState.Configuration;
        if (config == null || config.Prizes.Count == 0)
        {
            await args.ReplyAsync("奖池配置不可用，请检查 Prize.json。");
            return;
        }

        var service = args.Context.Server?.Services.GetService<TerrariaServerService>();
        if (service == null)
        {
            await args.ReplyAsync("服务器服务尚未初始化。");
            return;
        }

        var successes = 0;
        var totalCost = 0L;
        var rewards = new Dictionary<string, int>();
        string? failureMessage = null;
        for (var index = 0; index < times; index++)
        {
            var prize = TerrariaBridgeState.PickPrize(config.Prizes);
            var quantity = Random.Shared.Next(prize.MinimumQuantity, prize.MaximumQuantity + 1);
            var response = await service.GiveItemAsync(args.Server.Config.Name, args.Player.Name, prize.ItemId, quantity);
            if (response?.Success != true)
            {
                failureMessage = response?.Message ?? "服务器未响应";
                continue;
            }

            Currency.Deduct(args.User.Id, config.DrawCost);
            totalCost += config.DrawCost;
            successes++;
            rewards[prize.Name] = rewards.GetValueOrDefault(prize.Name) + quantity;
        }

        if (successes == 0)
        {
            await args.ReplyAsync($"抽奖失败：{failureMessage ?? "无法发放物品"}，未扣除货币。");
            return;
        }

        var rewardText = string.Join("、", rewards.Select(static pair => Green($"{pair.Key} x{pair.Value}")));
        await args.ReplyAsync($"抽奖完成：成功 {Green($"{successes}/{times}")} 次，获得 {rewardText}；扣除 {Green($"{totalCost} {args.Context.Configuration.Miscellaneous.CurrencyName}")}，当前余额：{Green(Currency.GetBalance(args.User.Id).ToString())}");
    }

    private static bool TryGetTimes(ServerCommandArgs args, out int times)
    {
        times = 1;
        return args.Params.Count == 0 || (int.TryParse(args.Params[0], out times) && times is >= 1 and <= 100);
    }

    private static string Green(string text) => $"[c/{HighlightColor}:{text}]";
}

[Command("抽货币")]
[CommandType(CommandType.Server)]
public static class DrawCurrencyCommand
{
    [Main]
    [Flexible]
    public static async Task Execute(ServerCommandArgs args)
    {
        if (args.User?.Id is not > 0)
        {
            await args.ReplyAsync("请先绑定 QQ 账号。");
            return;
        }

        var times = 1;
        if (args.Params.Count > 0 && (!int.TryParse(args.Params[0], out times) || times is < 1 or > 100))
        {
            await args.ReplyAsync("抽取次数必须是 1 到 100 之间的整数。");
            return;
        }

        var before = Currency.GetBalance(args.User.Id);
        var wins = 0;
        var losses = 0;
        for (var index = 0; index < times; index++)
        {
            var balance = Currency.GetBalance(args.User.Id);
            var amount = Math.Max(Math.Max(balance, 0) * 5 / 100, 200) + Random.Shared.Next(50, 501);
            if (Random.Shared.Next(2) == 0)
            {
                Currency.Add(args.User.Id, amount);
                wins++;
            }
            else
            {
                Currency.Deduct(args.User.Id, amount);
                losses++;
            }
        }

        var after = Currency.GetBalance(args.User.Id);
        await args.ReplyAsync($"货币抽取完成：胜 {Green(wins.ToString())} 次，负 {Green(losses.ToString())} 次；初始余额：{Green(before.ToString())}，当前余额：{Green(after.ToString())}，总变化：{Green($"{after - before:+#;-#;0} {args.Context.Configuration.Miscellaneous.CurrencyName}")}");
    }

    private static string Green(string text) => $"[c/42B268:{text}]";
}

[Command("奖池")]
[CommandType(CommandType.Group)]
public static class PrizePoolCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args)
    {
        var config = TerrariaBridgeState.Configuration;
        if (config == null || config.Prizes.Count == 0)
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

internal static class TerrariaBridgeState
{
    public static PrizeConfiguration? Configuration { get; private set; }

    public static void Load(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var drawCost = root.EnumerateObject().Select(static property => property.Value).FirstOrDefault(static value => value.ValueKind == JsonValueKind.Number).GetInt64();
            var prizes = root.EnumerateObject().Select(static property => property.Value).FirstOrDefault(static value => value.ValueKind == JsonValueKind.Array)
                .EnumerateArray().Select(ParsePrize).Where(static prize => prize != null).Select(static prize => prize!).ToList();
            Configuration = drawCost >= 0 && prizes.Count > 0 ? new PrizeConfiguration(drawCost, prizes) : null;
        }
        catch
        {
            Configuration = null;
        }
    }

    public static PrizeItem PickPrize(IReadOnlyList<PrizeItem> prizes)
    {
        var totalWeight = prizes.Sum(static prize => Math.Max(prize.Weight, 0));
        var target = Random.Shared.Next(1, totalWeight + 1);
        foreach (var prize in prizes)
        {
            target -= Math.Max(prize.Weight, 0);
            if (target <= 0) return prize;
        }
        return prizes[^1];
    }

    private static PrizeItem? ParsePrize(JsonElement element)
    {
        var name = element.EnumerateObject().Select(static property => property.Value).FirstOrDefault(static value => value.ValueKind == JsonValueKind.String).GetString();
        var values = element.EnumerateObject().Select(static property => property.Value).Where(static value => value.ValueKind == JsonValueKind.Number).Select(static value => value.GetInt32()).ToArray();
        return name != null && values.Length >= 4 ? new PrizeItem(name, values[0], values[1], values[2], values[3]) : null;
    }
}

internal sealed record PrizeConfiguration(long DrawCost, List<PrizeItem> Prizes);
internal sealed record PrizeItem(string Name, int ItemId, int Weight, int MaximumQuantity, int MinimumQuantity);
