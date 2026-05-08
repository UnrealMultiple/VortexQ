using Microsoft.Extensions.Logging;

namespace Vortex.Bot.Plugins;

public interface IPlugin : IAsyncDisposable
{
    string Name { get; }
    string Author { get; }
    string Description { get; }
    Version Version { get; }
    int LoadOrder { get; }

    ValueTask InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default);
    ValueTask ShutdownAsync(CancellationToken cancellationToken = default);
}

public interface IPluginContext
{
    IServiceProvider Services { get; }
    ILogger Logger { get; }
    string PluginDirectory { get; }
    VortexContext Vortex { get; }
}
