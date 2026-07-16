using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility.Images;
using Vortex.Plugin.Abstractions;

namespace Vortex.Bot.Plugins;

public sealed class PluginCommandRegistry
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, RegisteredPluginCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public IPluginCommandRegistry CreateScope(string pluginName) => new PluginCommandRegistryScope(this, pluginName);

    public bool TryGet(string name, PluginCommandScope scope, out PluginCommand command)
    {
        lock (_syncRoot)
        {
            if (_commands.TryGetValue(name, out var registration) && registration.Command.Scopes.HasFlag(scope))
            {
                command = registration.Command;
                return true;
            }
        }

        command = null!;
        return false;
    }

    public void Unregister(string pluginName)
    {
        lock (_syncRoot)
        {
            foreach (var name in _commands
                         .Where(entry => string.Equals(entry.Value.PluginName, pluginName, StringComparison.OrdinalIgnoreCase))
                         .Select(entry => entry.Key)
                         .ToArray())
            {
                _commands.Remove(name);
            }
        }
    }

    private void Register(string pluginName, PluginCommand command)
    {
        var names = new[] { command.Name }.Concat(command.Aliases).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (names.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("插件指令名称不能为空。", nameof(command));

        lock (_syncRoot)
        {
            var conflicts = names.Where(_commands.ContainsKey).ToArray();
            if (conflicts.Length > 0)
                throw new InvalidOperationException($"插件指令已被占用: {string.Join(", ", conflicts)}");

            foreach (var name in names)
                _commands[name] = new RegisteredPluginCommand(pluginName, command);
        }
    }

    private sealed class PluginCommandRegistryScope(PluginCommandRegistry registry, string pluginName) : IPluginCommandRegistry
    {
        public void Register(PluginCommand command) => registry.Register(pluginName, command);
    }

    private sealed record RegisteredPluginCommand(string PluginName, PluginCommand Command);
}

public sealed class PluginCurrencyService : IPluginCurrencyService
{
    public long GetBalance(long userId) => Currency.GetBalance(userId);
    public long Add(long userId, long amount) => Currency.Add(userId, amount).Num;
    public long Deduct(long userId, long amount) => Currency.Deduct(userId, amount).Num;
    public long Set(long userId, long amount) => Currency.Set(userId, amount).Num;
    public void Transfer(long fromUserId, long toUserId, long amount) => Currency.Transfer(fromUserId, toUserId, amount);
}

public sealed class PluginTerrariaService(TerrariaServerService servers) : ITerrariaPluginService
{
    public string? GetSelectedServerName(long userId, long groupId) =>
        servers.TryGetUserServer(userId, groupId, out var server) ? server?.Config.Name : null;

    public async Task<PluginOperationResult> ExecuteCommandAsync(string serverName, string command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await servers.ExecuteCommandAsync(serverName, command);
        return response is null
            ? new PluginOperationResult(false, "服务器未连接或请求超时。")
            : new PluginOperationResult(response.Success, response.Message);
    }

    public async Task<PluginOperationResult> GiveItemAsync(string serverName, string playerName, int itemId, string itemName, int stack, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await servers.GiveItemAsync(serverName, playerName, itemId, itemName, stack);
        return response is null
            ? new PluginOperationResult(false, "服务器未连接或请求超时。")
            : new PluginOperationResult(response.Success, response.Message);
    }

    public async Task<PluginRankResult> GetOnlineRankAsync(string serverName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await servers.GetOnlineRankAsync(serverName);
        return response is null
            ? new PluginRankResult(false, message: "服务器未连接或请求超时。")
            : new PluginRankResult(response.Success,
                response.OnlineRank.Select(entry => new PluginRankEntry(entry.Key, entry.Value)).ToArray(),
                response.Message);
    }

    public async Task<PluginRankResult> GetDeathRankAsync(string serverName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await servers.GetDeathRankAsync(serverName);
        return response is null
            ? new PluginRankResult(false, message: "服务器未连接或请求超时。")
            : new PluginRankResult(response.Success,
                response.Rank.Select(entry => new PluginRankEntry(entry.Key, entry.Value)).ToArray(),
                response.Message);
    }
}

public sealed class PluginImageService : IPluginImageService
{
    public byte[] RenderTable(PluginTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (table.Header.Count == 0)
            throw new ArgumentException("表格至少需要一列。", nameof(table));

        var builder = TableBuilder.Create()
            .SetTitle(table.Title)
            .SetMemberUin(table.MemberUin);
        builder.SetHeader(table.Header.Select(ToTableCell).ToArray());

        foreach (var row in table.Rows)
        {
            if (row.Count != table.Header.Count)
                throw new ArgumentException("表格行列数必须与表头一致。", nameof(table));
            builder.AddRow(row.Select(ToTableCell).ToArray());
        }

        return builder.Build();
    }

    private static TableCell ToTableCell(PluginTableCell cell)
    {
        var style = cell.FontStyle == PluginFontStyle.Bold ? FontStyle.Bold : FontStyle.Regular;
        return cell.Color is { } color
            ? new TableCell(cell.Text, Color.FromRgb(color.Red, color.Green, color.Blue), style)
            : new TableCell(cell.Text) { FontStyle = style };
    }
}

internal sealed class PluginContextAdapter(
    string pluginDirectory,
    string currencyName,
    IPluginLogger logger,
    IPluginCommandRegistry commands,
    IPluginCurrencyService currency,
    ITerrariaPluginService terraria,
    IPluginImageService images) : IPluginContext
{
    public string PluginDirectory { get; } = pluginDirectory;
    public string CurrencyName { get; } = currencyName;
    public IPluginLogger Logger { get; } = logger;
    public IPluginCommandRegistry Commands { get; } = commands;
    public IPluginCurrencyService Currency { get; } = currency;
    public ITerrariaPluginService Terraria { get; } = terraria;
    public IPluginImageService Images { get; } = images;
}

internal sealed class PluginLoggerAdapter(ILogger logger) : IPluginLogger
{
    public void Log(PluginLogLevel level, string message, Exception? exception = null)
    {
        var hostLevel = level switch
        {
            PluginLogLevel.Trace => LogLevel.Trace,
            PluginLogLevel.Debug => LogLevel.Debug,
            PluginLogLevel.Information => LogLevel.Information,
            PluginLogLevel.Warning => LogLevel.Warning,
            PluginLogLevel.Error => LogLevel.Error,
            _ => LogLevel.Information
        };
        logger.Log(hostLevel, exception, "{Message}", message);
    }
}
