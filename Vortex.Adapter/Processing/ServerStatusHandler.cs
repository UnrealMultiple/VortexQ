using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Models;
using Vortex.Protocol.Packets;
using Terraria;
using Terraria.IO;
using TerrariaApi.Server;

namespace Vortex.Adapter.Processing;

public class ServerStatusHandler(Net.VortexClient client) : RequestHandlerBase<ServerStatusPacket, ServerStatusPacketResponse>(client)
{
    public override ServerStatusPacketResponse Handle(ServerStatusPacket request)
    {
        var plugins = new List<Vortex.Protocol.Models.Plugin>();
        try
        {
            plugins = [.. ServerApi.Plugins
                .Where(x => x.Plugin != null)
                .Select(x => new Vortex.Protocol.Models.Plugin
                {
                    Name = x.Plugin.Name ?? "Unknown",
                    Author = x.Plugin.Author ?? "Unknown",
                    Description = x.Plugin.Description ?? "",
                })];
        }
        catch (Exception ex)
        {
            TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter] 获取插件列表失败: {ex.Message}");
        }

        return new ServerStatusPacketResponse
        {
            RequestId = request.RequestId,
            Success = true,
            Message = "获取成功",
            WorldName = Main.worldName ?? "Unknown",
            WorldID = Main.worldID,
            WorldMode = Main.GameMode,
            WorldSeed = Main.ActiveWorldFileData?.SeedText ?? "",
            WorldWidth = Main.maxTilesX,
            WorldHeight = Main.maxTilesY,
            RunTime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime,
            TShockPath = Environment.CurrentDirectory,
            Plugins = plugins
        };
    }
}
