using Lagrange.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Bot.Command;
using Vortex.Bot.Configuration;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database;
using Vortex.Bot.Models;
using Vortex.Bot.Plugins;
using Vortex.Bot.Utility;

namespace Vortex.Bot;

public partial class VortexContext(
    BotContext botContext,
    IDatabaseService database,
    CommandManager commandManager,
    IOptions<CoreConfiguration> configuration,
    ILogger<VortexContext> logger,
    PluginManager pluginManager) : IHostedService
{
    private readonly ILogger<VortexContext> _logger = logger;
    public BotContext BotContext { get; } = botContext;
    public IDatabaseService Database { get; } = database;
    public CommandManager CommandManager { get; } = commandManager;
    public ILogger<VortexContext> Logger => _logger;
    public CoreConfiguration Configuration { get; } = configuration.Value;
    public PluginManager PluginManager { get; } = pluginManager;
    public SystemMonitor SystemMonitor { get; private set; } = null!;

    public VortexSocketService? Server { get; set; }

    public static string Path => Environment.CurrentDirectory;
    public static string SavePath => System.IO.Path.Combine(Path, "Config");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogVortexContextStarted();

        DefaultGroup.Initialize();
        _logger.LogDefaultGroupInitialized();

        SystemMonitor = new SystemMonitor();
        _logger.LogSystemMonitorStarted();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogVortexContextStopping();

        SystemMonitor?.Dispose();

        if (Database is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return Task.CompletedTask;
    }
}

public static partial class VortexContextLoggerExtension
{
    [LoggerMessage(LogLevel.Information, "VortexContext started")]
    public static partial void LogVortexContextStarted(this ILogger<VortexContext> logger);
    [LoggerMessage(LogLevel.Information, "DefaultGroup initialized")]
    public static partial void LogDefaultGroupInitialized(this ILogger<VortexContext> logger);
    [LoggerMessage(LogLevel.Information, "SystemMonitor started")]
    public static partial void LogSystemMonitorStarted(this ILogger<VortexContext> logger);
    [LoggerMessage(LogLevel.Information, "VortexContext stopping")]
    public static partial void LogVortexContextStopping(this ILogger<VortexContext> logger);
}
