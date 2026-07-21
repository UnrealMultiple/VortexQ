using Microsoft.Extensions.Logging;
using Vortex.Bot.Plugins;

namespace GitHook;

[Plugin(Name = "GitHook", Author = "少司命", Description = "用于管理github仓库")]
public sealed class GitHookPlugin : PluginBase
{
    private HttpServer? _httpServer;

    protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken)
    {
        _httpServer = new HttpServer(Vortex);
        _httpServer.Logger += OnServerLog;
        _ = _httpServer.Start();
        return default;
    }

    protected override ValueTask OnShutdownAsync(CancellationToken cancellationToken)
    {
        if (_httpServer != null)
        {
            _httpServer.Logger -= OnServerLog;
            _httpServer.Dispose();
        }
        return default;
    }

    private void OnServerLog(string log, LogLevel level) => Logger.Log(level, log);
}
