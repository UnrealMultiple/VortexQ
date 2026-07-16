using Microsoft.Extensions.Logging;

namespace Vortex.Bot.Plugins;

public static partial class PluginHostLoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "[{Plugin}] Initializing v{Version} by {Author}")]
    public static partial void LogHostInitializing(this ILogger logger, string plugin, Version version, string author);

    [LoggerMessage(LogLevel.Information, "[{Plugin}] Initialized")]
    public static partial void LogHostInitialized(this ILogger logger, string plugin);

    [LoggerMessage(LogLevel.Error, "[{Plugin}] Initialization failed")]
    public static partial void LogHostInitFailed(this ILogger logger, string plugin, Exception ex);

    [LoggerMessage(LogLevel.Information, "[{Plugin}] Shutting down")]
    public static partial void LogHostShuttingDown(this ILogger logger, string plugin);

    [LoggerMessage(LogLevel.Information, "[{Plugin}] Shut down")]
    public static partial void LogHostShutDown(this ILogger logger, string plugin);

    [LoggerMessage(LogLevel.Error, "[{Plugin}] Shutdown error")]
    public static partial void LogHostShutdownError(this ILogger logger, string plugin, Exception ex);
}

public static partial class PluginLoaderLoggerExtensions
{
    [LoggerMessage(LogLevel.Debug, "Loading plugin from: {Directory}")]
    public static partial void LogLoadingFrom(this ILogger logger, string directory);

    [LoggerMessage(LogLevel.Error, "Failed to load plugin from: {Directory}")]
    public static partial void LogLoadFailed(this ILogger logger, string directory, Exception ex);
}

public static partial class PluginManagerLoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Loading all plugins from: {Directory}")]
    public static partial void LogLoadingAll(this ILogger logger, string directory);
    [LoggerMessage(LogLevel.Warning, "Plugins directory does not exist: {Directory}")]
    public static partial void LogDirectoryMissing(this ILogger logger, string directory);
    [LoggerMessage(LogLevel.Error, "Failed to initialize plugin: {PluginName}")]
    public static partial void LogInitFailed(this ILogger logger, string pluginName, Exception ex);
    [LoggerMessage(LogLevel.Information, "Plugin loading completed. Loaded {Count} plugins")]
    public static partial void LogLoadCompleted(this ILogger logger, int count);
    [LoggerMessage(LogLevel.Warning, "Plugin {PluginName} is not loaded")]
    public static partial void LogNotLoaded(this ILogger logger, string pluginName);
    [LoggerMessage(LogLevel.Information, "Plugin [{PluginName}] unloaded")]
    public static partial void LogUnloaded(this ILogger logger, string pluginName);
    [LoggerMessage(LogLevel.Error, "Failed to unload plugin: {PluginName}")]
    public static partial void LogUnloadFailed(this ILogger logger, string pluginName, Exception ex);
    [LoggerMessage(LogLevel.Warning, "Plugin {PluginName} not found for reload")]
    public static partial void LogNotFoundForReload(this ILogger logger, string pluginName);
    [LoggerMessage(LogLevel.Information, "Plugin [{PluginName}] reloaded")]
    public static partial void LogReloaded(this ILogger logger, string pluginName);
    [LoggerMessage(LogLevel.Error, "Failed to reload plugin [{PluginName}]: {Error}")]
    public static partial void LogReloadFailed(this ILogger logger, string pluginName, string? error);
    [LoggerMessage(LogLevel.Error, "Failed to reload plugin: {PluginName}")]
    public static partial void LogReloadException(this ILogger logger, string pluginName, Exception ex);
    [LoggerMessage(LogLevel.Information, "Reloading all plugins...")]
    public static partial void LogReloadingAll(this ILogger logger);
    [LoggerMessage(LogLevel.Information, "All plugins reloaded")]
    public static partial void LogAllReloaded(this ILogger logger);
    [LoggerMessage(LogLevel.Warning, "Failed to load plugin from {Directory}: {Error}")]
    public static partial void LogDirectoryLoadFailed(this ILogger logger, string directory, string? error);
    [LoggerMessage(LogLevel.Error, "Failed to load plugin from directory: {Directory}")]
    public static partial void LogDirectoryLoadException(this ILogger logger, string directory, Exception ex);
    [LoggerMessage(LogLevel.Warning, "Plugin {PluginName} is already loaded, skipping")]
    public static partial void LogAlreadyLoaded(this ILogger logger, string pluginName);
    [LoggerMessage(LogLevel.Warning, "Failed to add plugin {PluginName} to registry")]
    public static partial void LogAddToRegistryFailed(this ILogger logger, string pluginName);
    [LoggerMessage(LogLevel.Information, "Created plugins directory: {Directory}")]
    public static partial void LogCreatedDirectory(this ILogger logger, string directory);
    [LoggerMessage(LogLevel.Information, "Disposing plugin manager...")]
    public static partial void LogDisposing(this ILogger logger);
    [LoggerMessage(LogLevel.Error, "Error unloading plugin: {PluginName}")]
    public static partial void LogUnloadError(this ILogger logger, string pluginName, Exception ex);
    [LoggerMessage(LogLevel.Information, "Plugin manager disposed")]
    public static partial void LogDisposed(this ILogger logger);
}

public static partial class PluginLoaderServiceLoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Starting plugin system...")]
    public static partial void LogStarting(this ILogger logger);
    [LoggerMessage(LogLevel.Error, "Failed to load plugins during startup")]
    public static partial void LogStartupFailed(this ILogger logger, Exception ex);
    [LoggerMessage(LogLevel.Information, "Stopping plugin system...")]
    public static partial void LogStopping(this ILogger logger);
    [LoggerMessage(LogLevel.Error, "Error during plugin system shutdown")]
    public static partial void LogShutdownError(this ILogger logger, Exception ex);
}
