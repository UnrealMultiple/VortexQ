using Vortex.Bot.Plugins;

namespace Decompile;

[Plugin(Name = "Decompile", Author = "VortexQ", Description = "反编译插件", Major = 1, Minor = 0)]
public class DecompilePlugin : PluginBase
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
