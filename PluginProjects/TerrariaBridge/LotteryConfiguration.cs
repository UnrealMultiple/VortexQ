using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Vortex.Plugin.Abstractions;

namespace TerrariaBridge;

internal sealed class PrizeConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    [JsonPropertyName("抽奖费用")]
    public long DrawCost { get; set; }

    [JsonPropertyName("奖池内容")]
    public List<PrizeItem> Prizes { get; set; } = new();

    public static PrizeConfiguration? Load(string path, IPluginLogger logger)
    {
        try
        {
            var configuration = JsonSerializer.Deserialize<PrizeConfiguration>(File.ReadAllText(path), JsonOptions);
            if (configuration is null || configuration.DrawCost < 0 || configuration.Prizes.Count == 0)
                throw new InvalidOperationException("奖池内容为空或抽奖费用无效。");
            return configuration;
        }
        catch (Exception ex)
        {
            logger.Log(PluginLogLevel.Error, $"无法读取奖池配置：{path}", ex);
            return null;
        }
    }
}

internal sealed class PrizeItem
{
    [JsonPropertyName("奖品名称")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("奖品ID")]
    public int ItemId { get; set; }

    [JsonPropertyName("中奖概率")]
    public int Weight { get; set; }

    [JsonPropertyName("最大数量")]
    public int MaximumQuantity { get; set; }

    [JsonPropertyName("最小数量")]
    public int MinimumQuantity { get; set; }
}

internal sealed class CurrencyLotteryStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private static readonly UTF8Encoding Utf8WithBom = new(true);
    private readonly object _syncRoot = new();
    private readonly string _path;
    private CurrencyLotteryStateDocument _state;

    private CurrencyLotteryStateStore(string path, CurrencyLotteryStateDocument state)
    {
        _path = path;
        _state = state;
    }

    public static CurrencyLotteryStateStore Load(string path, IPluginLogger logger)
    {
        try
        {
            var state = ReadState(File.ReadAllText(path));
            var store = new CurrencyLotteryStateStore(path, state);
            store.Save();
            return store;
        }
        catch (Exception ex)
        {
            logger.Log(PluginLogLevel.Warning, $"无法读取货币抽取状态，将创建新的状态文件：{path}", ex);
            var store = new CurrencyLotteryStateStore(path, new CurrencyLotteryStateDocument());
            store.Save();
            return store;
        }
    }

    public LotteryStreak ApplyResult(long userId, bool win)
    {
        lock (_syncRoot)
        {
            var key = userId.ToString();
            if (!_state.UserStates.TryGetValue(key, out var state))
            {
                state = new CurrencyLotteryUserState();
                _state.UserStates[key] = state;
            }

            if (win)
            {
                state.WinStreak++;
                state.LossStreak = 0;
                return new LotteryStreak(true, state.WinStreak);
            }

            state.LossStreak++;
            state.WinStreak = 0;
            return new LotteryStreak(false, state.LossStreak);
        }
    }

    public void Save()
    {
        lock (_syncRoot)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var file = new CurrencyLotteryStateFile
            {
                Users = _state.UserStates.ToDictionary(
                    static pair => pair.Key,
                    static pair => new CurrencyLotteryStateFileUser
                    {
                        WinStreak = pair.Value.WinStreak,
                        LossStreak = pair.Value.LossStreak
                    })
            };
            File.WriteAllText(_path, JsonSerializer.Serialize(file, JsonOptions), Utf8WithBom);
        }
    }

    private static CurrencyLotteryStateDocument ReadState(string json)
    {
        using var document = JsonDocument.Parse(json);
        var state = new CurrencyLotteryStateDocument();
        var root = document.RootElement;
        if (!root.TryGetProperty("users", out var users) && !root.TryGetProperty("用户状态", out users))
            return state;
        if (users.ValueKind != JsonValueKind.Object)
            return state;

        foreach (var user in users.EnumerateObject())
        {
            if (user.Value.ValueKind != JsonValueKind.Object)
                continue;

            state.UserStates[user.Name] = new CurrencyLotteryUserState
            {
                WinStreak = ReadInt(user.Value, "winStreak", "连胜次数"),
                LossStreak = ReadInt(user.Value, "lossStreak", "连败次数")
            };
        }

        return state;
    }

    private static int ReadInt(JsonElement element, string currentName, string legacyName)
    {
        if (element.TryGetProperty(currentName, out var current) && current.TryGetInt32(out var currentValue))
            return currentValue;
        if (element.TryGetProperty(legacyName, out var legacy) && legacy.TryGetInt32(out var legacyValue))
            return legacyValue;
        return 0;
    }
}

internal sealed class CurrencyLotteryStateDocument
{
    public Dictionary<string, CurrencyLotteryUserState> UserStates { get; set; } = new();
}

internal sealed class CurrencyLotteryUserState
{
    public int WinStreak { get; set; }
    public int LossStreak { get; set; }
}

internal sealed class CurrencyLotteryStateFile
{
    [JsonPropertyName("users")]
    public Dictionary<string, CurrencyLotteryStateFileUser> Users { get; set; } = new();
}

internal sealed class CurrencyLotteryStateFileUser
{
    [JsonPropertyName("winStreak")]
    public int WinStreak { get; set; }

    [JsonPropertyName("lossStreak")]
    public int LossStreak { get; set; }
}

internal readonly record struct LotteryStreak(bool IsWin, int CurrentCount);
