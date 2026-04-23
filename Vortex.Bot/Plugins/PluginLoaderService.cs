using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Vortex.Bot.Plugins;

public class PluginLoaderService(ILogger<PluginLoaderService> logger, PluginManager pluginManager) : IHostedService
{
    private readonly ILogger<PluginLoaderService> _logger = logger;
    private readonly PluginManager _pluginManager = pluginManager;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing plugin system...");

        try
        {
            _pluginManager.LoadAllPlugins();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading plugins");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping plugin system...");

        try
        {
            _pluginManager.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading plugins");
        }

        return Task.CompletedTask;
    }
}
