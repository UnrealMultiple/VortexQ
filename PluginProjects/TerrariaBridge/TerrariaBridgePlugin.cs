using Vortex.Bot.Plugins;

namespace TerrariaBridge;

[Plugin(Name = "TerrariaBridge", Author = "VortexQ", Description = "泰拉瑞亚奖池与游戏内货币抽取", Major = 2, Minor = 1)]
public sealed class TerrariaBridgePlugin : PluginBase
{
    public const long MinimumBalance = 200;
    protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken)
    {
        return default;
    }

    protected override ValueTask OnShutdownAsync(CancellationToken cancellationToken) => default;
}
