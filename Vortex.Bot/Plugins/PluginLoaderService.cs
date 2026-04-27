using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Vortex.Bot.Plugins;

public partial class PluginLoaderService(ILogger<PluginLoaderService> logger, PluginManager pluginManager) : IHostedService
{
    private readonly ILogger<PluginLoaderService> _logger = logger;
    private readonly PluginManager _pluginManager = pluginManager;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInitializingPluginSystem();

        try
        {
            _pluginManager.LoadAllPlugins();
        }
        catch (Exception ex)
        {
            _logger.LogErrorLoadingPlugins(ex.Message);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogStoppingPluginSystem();

        try
        {
            _pluginManager.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogErrorUnloadingPlugins(ex.Message);
        }

        return Task.CompletedTask;
    }
}

public static partial class PluginLoaderServiceLoggerExtension
{
    [LoggerMessage(LogLevel.Information, "Initializing plugin system...")]
    public static partial void LogInitializingPluginSystem(this ILogger<PluginLoaderService> logger);
    [LoggerMessage(LogLevel.Error, "An error occurred while loading plugins: {message}")]
    public static partial void LogErrorLoadingPlugins(this ILogger<PluginLoaderService> logger, string message);
    [LoggerMessage(LogLevel.Information, "Stopping plugin system...")]
    public static partial void LogStoppingPluginSystem(this ILogger<PluginLoaderService> logger);
    [LoggerMessage(LogLevel.Error, "An error occurred while unloading plugins: {message}")]
    public static partial void LogErrorUnloadingPlugins(this ILogger<PluginLoaderService> logger, string message);
}
