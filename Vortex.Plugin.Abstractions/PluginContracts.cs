namespace Vortex.Plugin.Abstractions;

public static class PluginApi
{
    public const int MajorVersion = 1;
}

[Flags]
public enum PluginCommandScope
{
    Group = 1,
    Friend = 2,
    Server = 4
}

public enum PluginLogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error
}

public enum PluginFontStyle
{
    Regular,
    Bold
}

public readonly struct PluginColor(byte red, byte green, byte blue)
{
    public byte Red { get; } = red;
    public byte Green { get; } = green;
    public byte Blue { get; } = blue;
}

public sealed class PluginMetadata
{
    public PluginMetadata(string name, string author, string description, Version version, int loadOrder = 100)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Author = author ?? throw new ArgumentNullException(nameof(author));
        Description = description ?? string.Empty;
        Version = version ?? throw new ArgumentNullException(nameof(version));
        LoadOrder = loadOrder;
    }

    public string Name { get; }
    public string Author { get; }
    public string Description { get; }
    public Version Version { get; }
    public int LoadOrder { get; }
}

public interface IPlugin : IAsyncDisposable
{
    PluginMetadata Metadata { get; }
    ValueTask InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default);
    ValueTask ShutdownAsync(CancellationToken cancellationToken = default);
}

public abstract class PluginBase : IPlugin
{
    private bool _disposed;

    protected IPluginContext Context { get; private set; } = null!;
    protected IPluginLogger Logger => Context.Logger;
    protected IPluginCommandRegistry Commands => Context.Commands;
    protected IPluginCurrencyService Currency => Context.Currency;
    protected ITerrariaPluginService Terraria => Context.Terraria;
    protected IPluginImageService Images => Context.Images;
    protected string PluginDirectory => Context.PluginDirectory;
    protected string CurrencyName => Context.CurrencyName;

    public abstract PluginMetadata Metadata { get; }

    public async ValueTask InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(GetType().Name);
        Context = context ?? throw new ArgumentNullException(nameof(context));
        await OnInitializeAsync(cancellationToken);
    }

    public virtual ValueTask ShutdownAsync(CancellationToken cancellationToken = default) => OnShutdownAsync(cancellationToken);

    protected virtual ValueTask OnInitializeAsync(CancellationToken cancellationToken) => default;
    protected virtual ValueTask OnShutdownAsync(CancellationToken cancellationToken) => default;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await OnDisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask OnDisposeAsync() => default;
}

public interface IPluginContext
{
    string PluginDirectory { get; }
    string CurrencyName { get; }
    IPluginLogger Logger { get; }
    IPluginCommandRegistry Commands { get; }
    IPluginCurrencyService Currency { get; }
    ITerrariaPluginService Terraria { get; }
    IPluginImageService Images { get; }
}

public interface IPluginLogger
{
    void Log(PluginLogLevel level, string message, Exception? exception = null);
}

public interface IPluginCommandRegistry
{
    void Register(PluginCommand command);
}

public sealed class PluginCommand
{
    public PluginCommand(
        string name,
        PluginCommandScope scopes,
        Func<IPluginCommandContext, CancellationToken, ValueTask> handler,
        IEnumerable<string>? aliases = null,
        string? requiredPermission = null,
        string? helpText = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Scopes = scopes;
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        Aliases = (aliases ?? Array.Empty<string>()).Where(static name => !string.IsNullOrWhiteSpace(name)).ToArray();
        RequiredPermission = requiredPermission ?? string.Empty;
        HelpText = helpText ?? string.Empty;
    }

    public string Name { get; }
    public IReadOnlyList<string> Aliases { get; }
    public PluginCommandScope Scopes { get; }
    public string RequiredPermission { get; }
    public string HelpText { get; }
    public Func<IPluginCommandContext, CancellationToken, ValueTask> Handler { get; }
}

public interface IPluginCommandContext
{
    PluginCommandScope Scope { get; }
    string CommandName { get; }
    IReadOnlyList<string> Arguments { get; }
    long UserId { get; }
    long GroupId { get; }
    string SenderName { get; }
    string PlayerName { get; }
    long BoundUserId { get; }
    string ServerName { get; }
    bool HasPermission(string permission);
    Task ReplyAsync(string message);
    Task ReplyWithAtAsync(string message);
    Task ReplyImageAsync(byte[] imageData);
}

public interface IPluginCurrencyService
{
    long GetBalance(long userId);
    long Add(long userId, long amount);
    long Deduct(long userId, long amount);
    long Set(long userId, long amount);
    void Transfer(long fromUserId, long toUserId, long amount);
}

public interface ITerrariaPluginService
{
    string? GetSelectedServerName(long userId, long groupId);
    Task<PluginOperationResult> ExecuteCommandAsync(string serverName, string command, CancellationToken cancellationToken = default);
    Task<PluginOperationResult> GiveItemAsync(string serverName, string playerName, int itemId, string itemName, int stack, CancellationToken cancellationToken = default);
    Task<PluginRankResult> GetOnlineRankAsync(string serverName, CancellationToken cancellationToken = default);
    Task<PluginRankResult> GetDeathRankAsync(string serverName, CancellationToken cancellationToken = default);
}

public sealed class PluginOperationResult
{
    public PluginOperationResult(bool success, string message = "")
    {
        Success = success;
        Message = message ?? string.Empty;
    }

    public bool Success { get; }
    public string Message { get; }
}

public sealed class PluginRankResult
{
    public PluginRankResult(bool success, IReadOnlyList<PluginRankEntry>? entries = null, string message = "")
    {
        Success = success;
        Entries = entries ?? Array.Empty<PluginRankEntry>();
        Message = message ?? string.Empty;
    }

    public bool Success { get; }
    public IReadOnlyList<PluginRankEntry> Entries { get; }
    public string Message { get; }
}

public sealed class PluginRankEntry
{
    public PluginRankEntry(string name, long value)
    {
        Name = name ?? string.Empty;
        Value = value;
    }

    public string Name { get; }
    public long Value { get; }
}

public interface IPluginImageService
{
    byte[] RenderTable(PluginTable table);
}

public sealed class PluginTable
{
    public PluginTable(string title, IReadOnlyList<PluginTableCell> header, IReadOnlyList<IReadOnlyList<PluginTableCell>> rows)
    {
        Title = title ?? string.Empty;
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    public string Title { get; }
    public IReadOnlyList<PluginTableCell> Header { get; }
    public IReadOnlyList<IReadOnlyList<PluginTableCell>> Rows { get; }
    public long MemberUin { get; set; }
}

public sealed class PluginTableCell
{
    public PluginTableCell(string text, PluginColor? color = null, PluginFontStyle fontStyle = PluginFontStyle.Regular)
    {
        Text = text ?? string.Empty;
        Color = color;
        FontStyle = fontStyle;
    }

    public string Text { get; }
    public PluginColor? Color { get; }
    public PluginFontStyle FontStyle { get; }
}
