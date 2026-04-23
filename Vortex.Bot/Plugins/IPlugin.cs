using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Vortex.Bot.Plugins;

public interface IPlugin : IDisposable
{
    string Name { get; }
    string Author { get; }
    string Description { get; }
    Version Version { get; }
    int LoadOrder { get; }
    Assembly Assembly { get; }
    PluginContext Context { get; set; }
    void Initialize();
    void Shutdown();
}

public class PluginContext(ILogger logger, VortexContext vortexContext, string pluginPath)
{
    public ILogger Logger { get; } = logger;
    public VortexContext VortexContext { get; } = vortexContext;
    public string PluginPath { get; } = pluginPath;
}
