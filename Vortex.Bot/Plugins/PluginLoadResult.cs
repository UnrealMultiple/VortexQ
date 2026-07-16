namespace Vortex.Bot.Plugins;

public readonly record struct PluginLoadResult(
    bool Success,
    string Directory,
    Vortex.Plugin.Abstractions.IPlugin? Plugin = null,
    PluginInfo? Info = null,
    PluginLoadContext? LoadContext = null,
    Exception? Error = null
)
{
    public static PluginLoadResult Ok(string directory, Vortex.Plugin.Abstractions.IPlugin plugin, PluginInfo info, PluginLoadContext loadContext) =>
        new(true, directory, plugin, info, loadContext);

    public static PluginLoadResult Fail(string directory, Exception error) =>
        new(false, directory, Error: error);
}
