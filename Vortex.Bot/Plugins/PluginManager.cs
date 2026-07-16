using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Command;

namespace Vortex.Bot.Plugins;

public sealed class PluginManager : IAsyncDisposable
{
    private readonly ILogger<PluginManager> _logger;
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly CommandManager _commands;
    private readonly PluginLoader _loader;
    private readonly ConcurrentDictionary<string, PluginHost> _hosts = new();
    private readonly List<PluginLoadContext> _contexts = [];
    private readonly string _pluginsDirectory;
    private bool _disposed;

    public PluginManager(
        ILogger<PluginManager> logger,
        IServiceProvider services,
        ILoggerFactory loggerFactory,
        CommandManager commands)
    {
        _logger = logger;
        _services = services;
        _loggerFactory = loggerFactory;
        _commands = commands;
        _loader = new PluginLoader(services, loggerFactory.CreateLogger<PluginLoader>());
        _pluginsDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");

        EnsureDirectory();
    }

    public string PluginsDirectory => _pluginsDirectory;
    public IReadOnlyCollection<PluginHost> LoadedPlugins => _hosts.Values.ToList().AsReadOnly();

    public async Task LoadAllAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogLoadingAll(_pluginsDirectory);

        if (!Directory.Exists(_pluginsDirectory))
        {
            _logger.LogDirectoryMissing(_pluginsDirectory);
            return;
        }

        var results = Directory.GetDirectories(_pluginsDirectory)
            .Select(TryLoad)
            .Where(r => r.Success && r.Plugin is not null && r.Info is not null)
            .OrderBy(r => r.Info!.LoadOrder)
            .ToList();

        foreach (var result in results)
        {
            try
            {
                await InitializeHostAsync(result, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogInitFailed(result.Info!.Name, ex);
            }
        }

        _logger.LogLoadCompleted(_hosts.Count);
    }

    public async Task<bool> UnloadAsync(string pluginName, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_hosts.TryRemove(pluginName, out var host))
        {
            _logger.LogNotLoaded(pluginName);
            return false;
        }

        try
        {
            await host.DisposeAsync();
            _logger.LogUnloaded(pluginName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogUnloadFailed(pluginName, ex);
            return false;
        }
    }

    public async Task<bool> ReloadAsync(string pluginName, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_hosts.TryGetValue(pluginName, out var host))
        {
            _logger.LogNotFoundForReload(pluginName);
            return false;
        }

        var directory = host.Info.Directory;

        await UnloadAsync(pluginName, cancellationToken);
        CollectGarbage();

        try
        {
            var result = _loader.Load(directory);
            if (result.Success)
            {
                await InitializeHostAsync(result, cancellationToken);
                _logger.LogReloaded(pluginName);
                return true;
            }

            _logger.LogReloadFailed(pluginName, result.Error?.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogReloadException(pluginName, ex);
            return false;
        }
    }

    public async Task ReloadAllAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogReloadingAll();

        foreach (var pluginName in _hosts.Keys.ToList())
        {
            await UnloadAsync(pluginName, cancellationToken);
        }

        foreach (var context in _contexts)
        {
            context.Unload();
        }
        _contexts.Clear();

        CollectGarbage();
        await LoadAllAsync(cancellationToken);

        _logger.LogAllReloaded();
    }

    public PluginHost? GetPlugin(string pluginName)
    {
        _hosts.TryGetValue(pluginName, out var host);
        return host;
    }

    public PluginInfo? GetPluginInfo(string pluginName)
    {
        return _hosts.TryGetValue(pluginName, out var host) ? host.Info : null;
    }

    public bool IsLoaded(string pluginName) => _hosts.ContainsKey(pluginName);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _logger.LogDisposing();

        foreach (var pluginName in _hosts.Keys.ToList())
        {
            try
            {
                await UnloadAsync(pluginName);
            }
            catch (Exception ex)
            {
                _logger.LogUnloadError(pluginName, ex);
            }
        }

        foreach (var context in _contexts)
        {
            try
            {
                context.Unload();
            }
            catch (Exception ex)
            {
                _logger.LogContextUnloadError(ex);
            }
        }
        _contexts.Clear();

        _disposed = true;
        _logger.LogDisposed();
    }

    private PluginLoadResult TryLoad(string directory)
    {
        try
        {
            var result = _loader.Load(directory);
            if (!result.Success)
            {
                _logger.LogDirectoryLoadFailed(directory, result.Error?.Message);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogDirectoryLoadException(directory, ex);
            return PluginLoadResult.Fail(directory, ex);
        }
    }

    private async Task InitializeHostAsync(PluginLoadResult result, CancellationToken cancellationToken)
    {
        var plugin = result.Plugin!;
        var info = result.Info!;

        if (_hosts.ContainsKey(info.Name))
        {
            _logger.LogAlreadyLoaded(info.Name);
            return;
        }

        var logger = _loggerFactory.CreateLogger(plugin.GetType());
        var host = new PluginHost(plugin, info, _services, logger, _commands);

        if (!_hosts.TryAdd(info.Name, host))
        {
            _logger.LogAddToRegistryFailed(info.Name);
            return;
        }

        await host.InitializeAsync(cancellationToken);
    }

    private void EnsureDirectory()
    {
        if (!Directory.Exists(_pluginsDirectory))
        {
            Directory.CreateDirectory(_pluginsDirectory);
            _logger.LogCreatedDirectory(_pluginsDirectory);
        }
    }

    private static void CollectGarbage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
