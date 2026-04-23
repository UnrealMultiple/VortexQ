using Lagrange.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Bot.Command;
using Vortex.Bot.Configuration;
using Vortex.Bot.Database;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility;

namespace Vortex.Bot;

public class VortexContext(
    BotContext botContext,
    IDatabaseService database,
    CommandManager commandManager,
    IOptions<CoreConfiguration> configuration,
    ILogger<VortexContext> logger) : IHostedService
{
    private readonly ILogger<VortexContext> _logger = logger;
    public BotContext BotContext { get; } = botContext;
    public IDatabaseService Database { get; } = database;
    public CommandManager CommandManager { get; } = commandManager;
    public ILogger<VortexContext> Logger => _logger;
    public CoreConfiguration Configuration { get; } = configuration.Value;
    public SystemMonitor SystemMonitor { get; private set; } = null!;

    public static string Path => Environment.CurrentDirectory;
    public static string SavePath => System.IO.Path.Combine(Path, "Config");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("VortexContext 已启动");

        DefaultGroup.Initialize();
        _logger.LogInformation("默认权限组已初始化");

        SystemMonitor = new SystemMonitor();
        _logger.LogInformation("系统监控已启动");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("VortexContext 正在停止...");

        SystemMonitor?.Dispose();

        if (Database is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return Task.CompletedTask;
    }
}
