namespace Vortex.Bot.Plugins;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PluginAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = "Unknown";
    public string Description { get; set; } = string.Empty;
    public int Major { get; set; } = 1;
    public int Minor { get; set; } = 0;
    public int Patch { get; set; } = 0;
    public int LoadOrder { get; set; } = 100;

    public Version Version => new(Major, Minor, Patch);
}

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
