using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Vortex.Bot.Plugins;

public sealed class PluginLoaderService(ILogger<PluginLoaderService> logger, PluginManager manager) : IHostedService, IAsyncDisposable
{
    private readonly ILogger<PluginLoaderService> _logger = logger;
    private readonly PluginManager _manager = manager;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogStarting();

        try
        {
            await _manager.LoadAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogStartupFailed(ex);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogStopping();

        try
        {
            await _manager.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogShutdownError(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _manager.DisposeAsync();
    }
}
