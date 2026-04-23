using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vortex.Bot.Plugins;

public sealed class PluginManager : IDisposable
{
    private readonly ILogger<PluginManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly VortexContext _vortexContext;
    private readonly string _pluginsDirectory;
    private readonly ConcurrentDictionary<string, PluginInfo> _plugins = new();
    private readonly List<PluginLoadContext> _loadContexts = [];
    private bool _disposed;

    public IReadOnlyCollection<PluginInfo> LoadedPlugins => _plugins.Values.ToList().AsReadOnly();
    public string PluginsDirectory => _pluginsDirectory;

    public PluginManager(
        ILogger<PluginManager> logger,
        IServiceProvider serviceProvider,
        VortexContext vortexContext)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _vortexContext = vortexContext;
        _pluginsDirectory = Path.Combine(VortexContext.Path, "Plugins");
        EnsurePluginsDirectory();
    }

    private void EnsurePluginsDirectory()
    {
        if (!Directory.Exists(_pluginsDirectory))
        {
            Directory.CreateDirectory(_pluginsDirectory);
            _logger.LogInformation("Created plugins directory: {Path}", _pluginsDirectory);
        }
    }

    public void LoadAllPlugins()
    {
        _logger.LogInformation("Loading plugins...");

        var pluginDirs = Directory.GetDirectories(_pluginsDirectory);
        var loadedPlugins = new List<(PluginInfo Info, int Order)>();

        foreach (var pluginDir in pluginDirs)
        {
            try
            {
                var plugins = LoadPluginFromDirectory(pluginDir);
                loadedPlugins.AddRange(plugins.Select(p => (p, p.Plugin.LoadOrder)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin: {Directory}", pluginDir);
            }
        }

        InitializePlugins(loadedPlugins);
        _logger.LogInformation("Plugin loading completed, loaded {Count} plugins", _plugins.Count);
    }

    private List<PluginInfo> LoadPluginFromDirectory(string pluginDirectory)
    {
        var dirName = Path.GetFileName(pluginDirectory);
        _logger.LogDebug("Loading plugin directory: {Directory}", dirName);

        var loadContext = new PluginLoadContext(pluginDirectory, $"PluginContext_{dirName}_{Guid.NewGuid():N}");
        _loadContexts.Add(loadContext);

        loadContext.LoadAssemblies();

        if (loadContext.LoadedAssemblies.Count == 0)
        {
            _logger.LogWarning("No valid DLL files found in plugin directory: {Directory}", dirName);
            return [];
        }

        var plugins = loadContext.CreatePluginInstances(_serviceProvider);
        var result = new List<PluginInfo>();

        foreach (var plugin in plugins)
        {
            if (_plugins.ContainsKey(plugin.Name))
            {
                _logger.LogWarning("Plugin {PluginName} already exists, skipping", plugin.Name);
                continue;
            }

            var pluginContext = new PluginContext(
                _loggerFactory.CreateLogger(plugin.GetType()),
                _vortexContext,
                pluginDirectory
            );

            plugin.Context = pluginContext;
            var pluginInfo = new PluginInfo(plugin, pluginDirectory, loadContext);
            _plugins[plugin.Name] = pluginInfo;
            result.Add(pluginInfo);

            _logger.LogInformation("Plugin [{PluginName}] v{Version} by {Author} loaded",
                plugin.Name, plugin.Version, plugin.Author);
        }

        return result;
    }

    private void InitializePlugins(List<(PluginInfo Info, int Order)> plugins)
    {
        var sortedPlugins = plugins
            .OrderBy(static p => p.Order)
            .Select(static p => p.Info)
            .ToList();

        foreach (var pluginInfo in sortedPlugins)
        {
            try
            {
                if (pluginInfo.Plugin is PluginBase pluginBase)
                    pluginBase.OnInitialize();
                else
                    pluginInfo.Plugin.Initialize();

                pluginInfo.IsInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize plugin [{PluginName}]", pluginInfo.Plugin.Name);
            }
        }
    }

    public bool UnloadPlugin(string pluginName)
    {
        if (!_plugins.TryRemove(pluginName, out var pluginInfo))
        {
            _logger.LogWarning("Plugin {PluginName} not found, cannot unload", pluginName);
            return false;
        }

        try
        {
            if (pluginInfo.Plugin is PluginBase pluginBase)
                pluginBase.OnShutdown();
            else
                pluginInfo.Plugin.Shutdown();

            pluginInfo.Plugin.Dispose();
            _logger.LogInformation("Plugin [{PluginName}] unloaded", pluginName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading plugin [{PluginName}]", pluginName);
            return false;
        }
    }

    public bool ReloadPlugin(string pluginName)
    {
        if (!_plugins.TryGetValue(pluginName, out var pluginInfo))
        {
            _logger.LogWarning("Plugin {PluginName} not found, cannot reload", pluginName);
            return false;
        }

        var pluginDir = pluginInfo.Directory;
        UnloadPlugin(pluginName);

        CollectGarbage();

        try
        {
            var plugins = LoadPluginFromDirectory(pluginDir);

            if (_plugins.TryGetValue(pluginName, out var newPluginInfo) && !newPluginInfo.IsInitialized)
            {
                if (newPluginInfo.Plugin is PluginBase newPluginBase)
                    newPluginBase.OnInitialize();
                else
                    newPluginInfo.Plugin.Initialize();

                newPluginInfo.IsInitialized = true;
            }

            _logger.LogInformation("Plugin [{PluginName}] reloaded", pluginName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload plugin [{PluginName}]", pluginName);
            return false;
        }
    }

    public void ReloadAllPlugins()
    {
        _logger.LogInformation("Reloading all plugins...");

        foreach (var pluginName in _plugins.Keys.ToList())
            UnloadPlugin(pluginName);

        foreach (var context in _loadContexts)
            context.UnloadPlugins();

        _loadContexts.Clear();
        _plugins.Clear();

        CollectGarbage();
        LoadAllPlugins();
        _logger.LogInformation("All plugins reloaded");
    }

    public PluginInfo? GetPluginInfo(string pluginName)
    {
        _plugins.TryGetValue(pluginName, out var pluginInfo);
        return pluginInfo;
    }

    public bool IsPluginLoaded(string pluginName) => _plugins.ContainsKey(pluginName);

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Unloading all plugins...");

        foreach (var pluginName in _plugins.Keys.ToList())
            UnloadPlugin(pluginName);

        foreach (var context in _loadContexts)
        {
            try
            {
                context.UnloadPlugins();
                context.Unload();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unload plugin context");
            }
        }

        _loadContexts.Clear();
        _plugins.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
        _logger.LogInformation("All plugins unloaded");
    }

    private static void CollectGarbage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private ILoggerFactory _loggerFactory => _serviceProvider.GetRequiredService<ILoggerFactory>();
}
