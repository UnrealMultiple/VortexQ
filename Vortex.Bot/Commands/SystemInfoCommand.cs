using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Commands;

[Command("系统信息")]
[Alias("sysinfo", "system")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.system.info")]
public static class SystemInfoCommand
{
    [Main]
    public static async Task ShowSystemInfo(CommandArgs args)
    {
        var monitor = args.Context.SystemMonitor;
        
        var builder = ProfileItemBuilder.Create()
            .SetTitle("系统信息")
            .SetMemberUin((uint)args.SenderUin)
            .AddItem("CPU占用率", $"{monitor.CpuUsagePercent:0.0}%")
            .AddItem("内存占用率", $"{monitor.MemoryUsagePercent:0.0}%")
            .AddItem("总内存", $"{monitor.TotalPhysicalMemory / 1024 / 1024} MB")
            .AddItem("占用内存", $"{monitor.UsedPhysicalMemory / 1024 / 1024} MB")
            .AddItem("网络上行", $"{monitor.NetworkUploadKbps:0.0} KB/s")
            .AddItem("网络下行", $"{monitor.NetworkDownloadKbps:0.0} KB/s");
        
        var imageData = builder.Build();
        await args.ReplyImageAsync(imageData);
    }
}
