namespace Vortex.Bot.Plugins;

public sealed record PluginInfo(
    string Name,
    string Author,
    string Description,
    Version Version,
    int LoadOrder,
    string Directory,
    DateTime LoadTime
)
{
    public bool IsInitialized { get; internal set; }

    public override string ToString() => $"{Name} v{Version} by {Author}";
}
