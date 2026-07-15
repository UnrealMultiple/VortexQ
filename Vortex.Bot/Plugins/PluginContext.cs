using Microsoft.Extensions.Logging;

namespace Vortex.Bot.Plugins;

public sealed record PluginContext(
    IServiceProvider Services,
    ILogger Logger,
    string PluginDirectory,
    VortexContext Vortex
) : IPluginContext
{
    public VortexContext VortexContext => Vortex;
}
