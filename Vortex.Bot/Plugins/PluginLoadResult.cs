namespace Vortex.Bot.Plugins;

public readonly record struct PluginLoadResult(
    bool Success,
    string Directory,
    IPlugin? Plugin = null,
    PluginInfo? Info = null,
    Exception? Error = null
)
{
    public static PluginLoadResult Ok(string directory, IPlugin plugin, PluginInfo info) =>
        new(true, directory, plugin, info);

    public static PluginLoadResult Fail(string directory, Exception error) =>
        new(false, directory, Error: error);
}
