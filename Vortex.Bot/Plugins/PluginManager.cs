using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Vortex.Bot.Plugins;

public sealed partial class PluginManager : IDisposable
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
            _logger.LogPluginsDirectoryCreated(_pluginsDirectory);
        }
    }

    public void LoadAllPlugins()
    {
        _logger.LogLoadingPlugins();

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
                _logger.LogFailedToLoadPlugin(pluginDir, ex);
            }
        }

        InitializePlugins(loadedPlugins);
        _logger.LogPluginsLoaded(_plugins.Count);
    }

    private List<PluginInfo> LoadPluginFromDirectory(string pluginDirectory)
    {
        var dirName = Path.GetFileName(pluginDirectory);
        _logger.LogLoadingPluginDirectory(dirName);

        var loadContext = new PluginLoadContext(pluginDirectory, $"PluginContext_{dirName}_{Guid.NewGuid():N}");
        _loadContexts.Add(loadContext);

        loadContext.LoadAssemblies();

        if (loadContext.LoadedAssemblies.Count == 0)
        {
            _logger.LogNoValidDllFiles(dirName);
            return [];
        }

        var plugins = loadContext.CreatePluginInstances(_serviceProvider);
        var result = new List<PluginInfo>();

        foreach (var plugin in plugins)
        {
            if (_plugins.ContainsKey(plugin.Name))
            {
                _logger.LogPluginAlreadyExists(plugin.Name);
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

            _logger.LogPluginLoaded(plugin.Name, plugin.Version.ToString(), plugin.Author);
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
                _logger.LogFailedToInitializePlugin(pluginInfo.Plugin.Name, ex);
            }
        }
    }

    public bool UnloadPlugin(string pluginName)
    {
        if (!_plugins.TryRemove(pluginName, out var pluginInfo))
        {
            _logger.LogPluginNotFoundForUnload(pluginName);
            return false;
        }

        try
        {
            if (pluginInfo.Plugin is PluginBase pluginBase)
                pluginBase.OnShutdown();
            else
                pluginInfo.Plugin.Shutdown();

            pluginInfo.Plugin.Dispose();
            _logger.LogPluginUnloaded(pluginName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogErrorUnloadingPlugin(pluginName, ex);
            return false;
        }
    }

    public bool ReloadPlugin(string pluginName)
    {
        if (!_plugins.TryGetValue(pluginName, out var pluginInfo))
        {
            _logger.LogPluginNotFoundForReload(pluginName);
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

            _logger.LogPluginReloaded(pluginName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogFailedToReloadPlugin(pluginName, ex);
            return false;
        }
    }

    public void ReloadAllPlugins()
    {
        _logger.LogReloadingAllPlugins();

        foreach (var pluginName in _plugins.Keys.ToList())
            UnloadPlugin(pluginName);

        foreach (var context in _loadContexts)
            context.UnloadPlugins();

        _loadContexts.Clear();
        _plugins.Clear();

        CollectGarbage();
        LoadAllPlugins();
        _logger.LogAllPluginsReloaded();
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

        _logger.LogUnloadingAllPlugins();

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
                _logger.LogFailedToUnloadPluginContext(ex);
            }
        }

        _loadContexts.Clear();
        _plugins.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
        _logger.LogAllPluginsUnloaded();
    }

    private static void CollectGarbage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private ILoggerFactory _loggerFactory => _serviceProvider.GetRequiredService<ILoggerFactory>();
}

public static partial class PluginManagerLoggerExtension
{
    [LoggerMessage(LogLevel.Information, "Created plugins directory: {Path}")]
    public static partial void LogPluginsDirectoryCreated(this ILogger<PluginManager> logger, string path);

    [LoggerMessage(LogLevel.Information, "Loading plugins...")]
    public static partial void LogLoadingPlugins(this ILogger<PluginManager> logger);
        
    [LoggerMessage(LogLevel.Error, "Failed to load plugin: {Directory}")]
    public static partial void LogFailedToLoadPlugin(this ILogger<PluginManager> logger, string directory, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Loading plugin directory: {Directory}")]
    public static partial void LogLoadingPluginDirectory(this ILogger<PluginManager> logger, string directory);

    [LoggerMessage(LogLevel.Warning, "No valid DLL files found in plugin directory: {Directory}")]
    public static partial void LogNoValidDllFiles(this ILogger<PluginManager> logger, string directory);

    [LoggerMessage(LogLevel.Warning, "Plugin {PluginName} already exists, skipping")]
    public static partial void LogPluginAlreadyExists(this ILogger<PluginManager> logger, string pluginName);

    [LoggerMessage(LogLevel.Information, "Plugin [{PluginName}] v{Version} by {Author} loaded")]
    public static partial void LogPluginLoaded(this ILogger<PluginManager> logger, string pluginName, string version, string author);

    [LoggerMessage(LogLevel.Error, "Failed to initialize plugin [{PluginName}]")]
    public static partial void LogFailedToInitializePlugin(this ILogger<PluginManager> logger, string pluginName, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Plugin {PluginName} not found, cannot unload")]
    public static partial void LogPluginNotFoundForUnload(this ILogger<PluginManager> logger, string pluginName);

    [LoggerMessage(LogLevel.Information, "Plugin [{PluginName}] unloaded")]
    public static partial void LogPluginUnloaded(this ILogger<PluginManager> logger, string pluginName);

    [LoggerMessage(LogLevel.Error, "Error unloading plugin [{PluginName}]")]
    public static partial void LogErrorUnloadingPlugin(this ILogger<PluginManager> logger, string pluginName, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Plugin {PluginName} not found, cannot reload")]
    public static partial void LogPluginNotFoundForReload(this ILogger<PluginManager> logger, string pluginName);

    [LoggerMessage(LogLevel.Information, "Plugin [{PluginName}] reloaded")]
    public static partial void LogPluginReloaded(this ILogger<PluginManager> logger, string pluginName);

    [LoggerMessage(LogLevel.Error, "Failed to reload plugin [{PluginName}]")]
    public static partial void LogFailedToReloadPlugin(this ILogger<PluginManager> logger, string pluginName, Exception ex);

    [LoggerMessage(LogLevel.Information, "Reloading all plugins...")]
    public static partial void LogReloadingAllPlugins(this ILogger<PluginManager> logger);

    [LoggerMessage(LogLevel.Information, "All plugins reloaded")]
    public static partial void LogAllPluginsReloaded(this ILogger<PluginManager> logger);

    [LoggerMessage(LogLevel.Information, "Unloading all plugins...")]
    public static partial void LogUnloadingAllPlugins(this ILogger<PluginManager> logger);

    [LoggerMessage(LogLevel.Error, "Failed to unload plugin context")]
    public static partial void LogFailedToUnloadPluginContext(this ILogger<PluginManager> logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "All plugins unloaded")]
    public static partial void LogAllPluginsUnloaded(this ILogger<PluginManager> logger);

    [LoggerMessage(LogLevel.Information, "Plugin loading completed, loaded {Count} plugins")]
    public static partial void LogPluginsLoaded(this ILogger<PluginManager> logger, int count);
}