using System.Collections.Concurrent;
using Vortex.Plugin.Abstractions;

namespace TerrariaBridge;

public sealed class TerrariaBridgePlugin : PluginBase
{
    private const int MinimumBet = 200;
    private const int CurrencyRewardMinimum = 50;
    private const int CurrencyRewardMaximum = 500;
    private const int MaximumStreakBonusPercent = 5;
    private const string HighlightColor = "42B268";

    private readonly ConcurrentDictionary<long, SemaphoreSlim> _userLocks = new();
    private readonly Random _random = new();
    private readonly object _randomLock = new();
    private PrizeConfiguration? _prizeConfiguration;
    private CurrencyLotteryStateStore _lotteryState = null!;

    public override PluginMetadata Metadata { get; } = new(
        "TerrariaBridge",
        "VortexQ",
        "泰拉瑞亚奖池与游戏内货币抽取",
        new Version(2, 1, 0));

    protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken)
    {
        _prizeConfiguration = PrizeConfiguration.Load(Path.Combine(PluginDirectory, "Prize.json"), Logger);
        _lotteryState = CurrencyLotteryStateStore.Load(Path.Combine(PluginDirectory, "CurrencyLotteryState.json"), Logger);

        Commands.Register(new PluginCommand("抽", PluginCommandScope.Server, DrawPrizeAsync, helpText: "消耗货币抽取物品"));
        Commands.Register(new PluginCommand("抽货币", PluginCommandScope.Server, DrawCurrencyAsync, helpText: "抽取货币，可指定连抽次数"));
        Commands.Register(new PluginCommand("奖池", PluginCommandScope.Group, ShowPrizePoolAsync, helpText: "查看物品奖池"));
        return default;
    }

    private async ValueTask DrawPrizeAsync(IPluginCommandContext context, CancellationToken cancellationToken)
    {
        if (context.BoundUserId <= 0)
        {
            await context.ReplyAsync("请先绑定 QQ 账号后再使用此指令。");
            return;
        }
        var userId = context.BoundUserId;
        if (string.IsNullOrWhiteSpace(context.ServerName))
        {
            await context.ReplyAsync("未识别到当前服务器。");
            return;
        }

        var configuration = _prizeConfiguration;
        if (configuration is null || configuration.Prizes.Count == 0)
        {
            await context.ReplyAsync("奖池配置不可用，请检查 Prize.json。");
            return;
        }

        var times = 1;
        if (context.Arguments.Count > 0 && (!int.TryParse(context.Arguments[0], out times) || times is < 1 or > 100))
        {
            await context.ReplyAsync("连抽次数必须是 1 到 100 之间的整数。");
            return;
        }

        var gate = _userLocks.GetOrAdd(userId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var successfulDraws = 0;
            var failedDraws = 0;
            var totalCost = 0L;
            var rewards = new Dictionary<(int ItemId, string Name), int>();
            var failures = new List<string>();

            for (var index = 0; index < times; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var prize = PickWeightedPrize(configuration.Prizes);
                var quantity = Next(prize.MinimumQuantity, prize.MaximumQuantity);
                var result = await Terraria.GiveItemAsync(
                    context.ServerName,
                    context.PlayerName,
                    prize.ItemId,
                    prize.Name,
                    quantity,
                    cancellationToken);
                if (!result.Success)
                {
                    failedDraws++;
                    if (failures.Count < 3)
                        failures.Add($"{prize.Name}{FormatFailure(result.Message)}");
                    continue;
                }

                Currency.Deduct(userId, configuration.DrawCost);
                totalCost += configuration.DrawCost;
                successfulDraws++;
                rewards.TryGetValue((prize.ItemId, prize.Name), out var currentQuantity);
                rewards[(prize.ItemId, prize.Name)] = currentQuantity + quantity;
            }

            if (successfulDraws == 0)
            {
                await context.ReplyAsync($"抽奖失败，未扣除{CurrencyName}。{string.Join("；", failures)}");
                return;
            }

            var rewardText = string.Join("、", rewards.Select(reward => $"{reward.Key.Name} x{reward.Value}"));
            var balance = Currency.GetBalance(userId);
            var failureText = failedDraws > 0
                ? $"；失败 {failedDraws} 次（未扣费）：{string.Join("、", failures)}"
                : string.Empty;
            await context.ReplyAsync(
                $"抽奖完成：成功 {Green($"{successfulDraws}/{times}")} 次，获得 {Green(rewardText)}；扣除 {Green($"{totalCost} {CurrencyName}")}，当前{CurrencyName}：{Green(balance.ToString())}{failureText}");
        }
        finally
        {
            gate.Release();
        }
    }

    private async ValueTask DrawCurrencyAsync(IPluginCommandContext context, CancellationToken cancellationToken)
    {
        if (context.BoundUserId <= 0)
        {
            await context.ReplyAsync("请先绑定 QQ 账号后再使用此指令。");
            return;
        }
        var userId = context.BoundUserId;

        var times = 1;
        if (context.Arguments.Count > 0 && (!int.TryParse(context.Arguments[0], out times) || times is < 1 or > 100))
        {
            await context.ReplyAsync("连抽次数必须是 1 到 100 之间的整数。");
            return;
        }

        var gate = _userLocks.GetOrAdd(userId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var before = Currency.GetBalance(userId);
            var wins = 0;
            var losses = 0;
            LotteryStreak lastStreak = default;

            for (var index = 0; index < times; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var currentBalance = Currency.GetBalance(userId);
                var bet = Math.Max(currentBalance * 5 / 100, MinimumBet);
                var win = Next(0, 1) == 0;
                lastStreak = _lotteryState.ApplyResult(userId, win);

                var randomAmount = Next(CurrencyRewardMinimum, CurrencyRewardMaximum);
                var streakPercent = Math.Min(lastStreak.CurrentCount, MaximumStreakBonusPercent);
                var streakBonus = Math.Max(currentBalance, 0) * streakPercent / 100;
                var change = bet + randomAmount + streakBonus;

                if (win)
                {
                    Currency.Add(userId, change);
                    wins++;
                }
                else
                {
                    Currency.Deduct(userId, change);
                    losses++;
                }
            }

            _lotteryState.Save();
            var after = Currency.GetBalance(userId);
            var totalChange = after - before;
            var sign = totalChange > 0 ? "+" : string.Empty;
            var streakText = lastStreak.IsWin
                ? $"当前连胜：{Green(lastStreak.CurrentCount.ToString())}"
                : $"当前连败：{Green(lastStreak.CurrentCount.ToString())}";
            await context.ReplyAsync(
                $"连抽 {Green(times.ToString())} 次完成：胜 {Green(wins.ToString())} 次，负 {Green(losses.ToString())} 次；{streakText}。初始{CurrencyName}：{Green(before.ToString())}，最终{CurrencyName}：{Green(after.ToString())}，总变化：{Green($"{sign}{totalChange} {CurrencyName}")}。");
        }
        finally
        {
            gate.Release();
        }
    }

    private async ValueTask ShowPrizePoolAsync(IPluginCommandContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configuration = _prizeConfiguration;
        if (configuration is null || configuration.Prizes.Count == 0)
        {
            await context.ReplyWithAtAsync("奖池配置不可用，请检查 Prize.json。");
            return;
        }

        var rows = configuration.Prizes
            .Select((prize, index) => (IReadOnlyList<PluginTableCell>)new[]
            {
                new PluginTableCell((index + 1).ToString()),
                new PluginTableCell(prize.Name),
                new PluginTableCell($"{prize.Weight}%"),
                new PluginTableCell($"{prize.MinimumQuantity}-{prize.MaximumQuantity}")
            })
            .ToArray();
        var table = new PluginTable(
            $"奖池（每次 {configuration.DrawCost} {CurrencyName}）",
            new PluginTableCell[] { new("序号"), new("奖品"), new("权重"), new("数量") },
            rows)
        {
            MemberUin = context.UserId
        };
        await context.ReplyImageAsync(Images.RenderTable(table));
    }

    private PrizeItem PickWeightedPrize(IReadOnlyList<PrizeItem> prizes)
    {
        var totalWeight = prizes.Sum(static prize => Math.Max(prize.Weight, 0));
        if (totalWeight <= 0) throw new InvalidOperationException("奖池没有可用权重。");

        var target = Next(1, totalWeight);
        foreach (var prize in prizes)
        {
            target -= Math.Max(prize.Weight, 0);
            if (target <= 0) return prize;
        }

        return prizes[^1];
    }

    private int Next(int minimum, int maximum)
    {
        lock (_randomLock)
            return _random.Next(minimum, maximum + 1);
    }

    private static string Green(string text) => $"[c/{HighlightColor}:{text}]";

    private static string FormatFailure(string message) => string.IsNullOrWhiteSpace(message) ? string.Empty : $" 原因：{message}";
}
