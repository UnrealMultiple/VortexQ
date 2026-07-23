using Vortex.Bot.Plugins;

namespace Daily;

[Plugin(Author = "Vortex", Name = "Daily", Description = "A plugin that provides daily features.", Major = 1, Minor = 0, Patch = 0)]
public class DailyPlugin : PluginBase
{
    protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnShutdownAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
