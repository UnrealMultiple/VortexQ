using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Database.Models;

namespace TerrariaBridge.Command;

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
        if (before < TerrariaBridgePlugin.MinimumBalance)
        {
            await args.ReplyAsync($"余额不足，抽货币至少需要 {Green($"{TerrariaBridgePlugin.MinimumBalance} {args.Context.Configuration.Miscellaneous.CurrencyName}")}。");
            return;
        }

        var wins = 0;
        var losses = 0;
        var stoppedForInsufficientBalance = false;
        for (var index = 0; index < times; index++)
        {
            var balance = Currency.GetBalance(args.User.Id);
            if (balance < TerrariaBridgePlugin.MinimumBalance)
            {
                stoppedForInsufficientBalance = true;
                break;
            }

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
        var stoppedMessage = stoppedForInsufficientBalance
            ? $"；余额不足 {Green($"{TerrariaBridgePlugin.MinimumBalance} {args.Context.Configuration.Miscellaneous.CurrencyName}")}，已提前停止"
            : string.Empty;
        await args.ReplyAsync($"货币抽取完成：胜 {Green(wins.ToString())} 次，负 {Green(losses.ToString())} 次；初始余额：{Green(before.ToString())}，当前余额：{Green(after.ToString())}，总变化：{Green($"{after - before:+#;-#;0} {args.Context.Configuration.Miscellaneous.CurrencyName}")}{stoppedMessage}");
    }

    private static string Green(string text) => $"[c/42B268:{text}]";
}