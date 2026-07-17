using Microsoft.Extensions.DependencyInjection;
using TerrariaBridge.Config;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database.Models;

namespace TerrariaBridge.Command;

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

        var config = PrizeConfiguration.Instance;
        if (config.Prizes.Count == 0)
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

        var requiredBalance = Math.Max(config.DrawCost, TerrariaBridgePlugin.MinimumBalance);
        if (Currency.GetBalance(args.User.Id) < requiredBalance)
        {
            await args.ReplyAsync($"余额不足，抽奖至少需要 {Green($"{requiredBalance} {args.Context.Configuration.Miscellaneous.CurrencyName}")}。");
            return;
        }

        var successes = 0;
        var totalCost = 0L;
        var rewards = new Dictionary<string, int>();
        string? failureMessage = null;
        var stoppedForInsufficientBalance = false;
        for (var index = 0; index < times; index++)
        {
            if (Currency.GetBalance(args.User.Id) < requiredBalance)
            {
                stoppedForInsufficientBalance = true;
                break;
            }

            var prize = config.PickPrize();
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
        var stoppedMessage = stoppedForInsufficientBalance
            ? $"；余额不足 {Green($"{requiredBalance} {args.Context.Configuration.Miscellaneous.CurrencyName}")}，已提前停止"
            : string.Empty;
        await args.ReplyAsync($"抽奖完成：成功 {Green($"{successes}/{times}")} 次，获得 {rewardText}；扣除 {Green($"{totalCost} {args.Context.Configuration.Miscellaneous.CurrencyName}")}，当前余额：{Green(Currency.GetBalance(args.User.Id).ToString())}{stoppedMessage}");
    }

    private static bool TryGetTimes(ServerCommandArgs args, out int times)
    {
        times = 1;
        return args.Params.Count == 0 || (int.TryParse(args.Params[0], out times) && times is >= 1 and <= 100);
    }

    private static string Green(string text) => $"[c/{HighlightColor}:{text}]";
}