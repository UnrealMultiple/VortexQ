using Microsoft.Extensions.Logging;
using Vortex.Plugin.Abstractions;

namespace Vortex.Bot.Plugins;

public sealed class PluginHost(
    IPlugin plugin,
    PluginInfo info,
    ILogger logger,
    PluginCommandRegistry commandRegistry,
    PluginCurrencyService currency,
    PluginTerrariaService terraria,
    PluginImageService images,
    string currencyName) : IAsyncDisposable
{
    private readonly IPlugin _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
    private readonly PluginInfo _info = info ?? throw new ArgumentNullException(nameof(info));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly PluginCommandRegistry _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
    private readonly PluginCurrencyService _currency = currency ?? throw new ArgumentNullException(nameof(currency));
    private readonly PluginTerrariaService _terraria = terraria ?? throw new ArgumentNullException(nameof(terraria));
    private readonly PluginImageService _images = images ?? throw new ArgumentNullException(nameof(images));
    private readonly string _currencyName = currencyName;
    private bool _disposed;
    private bool _initialized;

    public IPlugin Plugin => _plugin;
    public PluginInfo Info => _info;
    public bool IsInitialized => _initialized;

    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_initialized) return;

        _logger.LogHostInitializing(_info.Name, _info.Version, _info.Author);
        var context = new PluginContextAdapter(
            _info.Directory,
            _currencyName,
            new PluginLoggerAdapter(_logger),
            _commandRegistry.CreateScope(_info.Name),
            _currency,
            _terraria,
            _images);

        try
        {
            await _plugin.InitializeAsync(context, cancellationToken);
            _initialized = true;
            _info.IsInitialized = true;
            _logger.LogHostInitialized(_info.Name);
        }
        catch (Exception ex)
        {
            _commandRegistry.Unregister(_info.Name);
            _logger.LogHostInitFailed(_info.Name, ex);
            throw;
        }
    }

    public async ValueTask ShutdownAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!_initialized) return;

        _logger.LogHostShuttingDown(_info.Name);
        try
        {
            await _plugin.ShutdownAsync(cancellationToken);
            _initialized = false;
            _info.IsInitialized = false;
            _logger.LogHostShutDown(_info.Name);
        }
        catch (Exception ex)
        {
            _logger.LogHostShutdownError(_info.Name, ex);
            throw;
        }
        finally
        {
            _commandRegistry.Unregister(_info.Name);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        try
        {
            if (_initialized)
                await ShutdownAsync();
        }
        finally
        {
            await _plugin.DisposeAsync();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, nameof(PluginHost));
}
