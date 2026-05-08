using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Command;
using Vortex.Bot.Configuration;

namespace Vortex.Bot.Plugins;

public sealed class PluginHost(IPlugin plugin, PluginInfo info, IServiceProvider services, ILogger logger, CommandManager? commands = null) : IAsyncDisposable
{
    private readonly IPlugin _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
    private readonly PluginInfo _info = info ?? throw new ArgumentNullException(nameof(info));
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly CommandManager? _commands = commands;
    private readonly List<Type> _configs = [];
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

        try
        {
            var vortex = _services.GetRequiredService<VortexContext>();
            var context = new PluginContext(_services, _logger, _info.Directory, vortex);

            if (_plugin is PluginBase basePlugin)
            {
                await basePlugin.InitializeAsync(context, cancellationToken);
            }
            else
            {
                await _plugin.InitializeAsync(context, cancellationToken);
                await AutoRegisterCommandsAsync();
                await AutoLoadConfigsAsync();
            }

            _initialized = true;
            _info.IsInitialized = true;
            _logger.LogHostInitialized(_info.Name);
        }
        catch (Exception ex)
        {
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
            await UnloadConfigsAsync();
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
    }

    private async ValueTask AutoRegisterCommandsAsync()
    {
        if (_commands is null) return;

        try
        {
            _commands.AutoRegister(_plugin.GetType().Assembly);
            _logger.LogHostCommandsRegistered(_info.Name);
        }
        catch (Exception ex)
        {
            _logger.LogHostCommandRegFailed(_info.Name, ex);
        }
    }

    private async ValueTask AutoLoadConfigsAsync()
    {
        try
        {
            _configs.Clear();
            var baseType = typeof(JsonConfigBase<>);

            foreach (var type in _plugin.GetType().Assembly.GetExportedTypes())
            {
                if (type.IsAbstract || type.IsInterface) continue;

                var current = type.BaseType;
                while (current is not null)
                {
                    if (current.IsGenericType && current.GetGenericTypeDefinition() == baseType)
                    {
                        _configs.Add(type);
                        break;
                    }
                    current = current.BaseType;
                }
            }

            foreach (var configType in _configs)
            {
                try
                {
                    var loadMethod = configType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
                    var fileName = loadMethod?.Invoke(null, null) as string;
                    _logger.LogHostConfigLoaded(_info.Name, fileName ?? configType.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogHostConfigLoadFailed(_info.Name, configType.Name, ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogHostConfigLoadingFailed(_info.Name, ex);
        }
    }

    private async ValueTask UnloadConfigsAsync()
    {
        foreach (var configType in _configs)
        {
            try
            {
                var unloadMethod = configType.GetMethod("Unload", BindingFlags.Public | BindingFlags.Static);
                unloadMethod?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                _logger.LogHostConfigUnloadFailed(_info.Name, configType.Name, ex);
            }
        }
        _configs.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            if (_initialized)
                await ShutdownAsync();
        }
        catch (Exception ex)
        {
            _logger.LogHostDisposalError(_info.Name, ex);
        }
        finally
        {
            await _plugin.DisposeAsync();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PluginHost));
    }
}
