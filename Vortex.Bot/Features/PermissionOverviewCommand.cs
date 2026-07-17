using System.Reflection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Utility;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Features;

[Command("权限", "permission")]
[HelpText("查看各指令所需权限")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.permission.overview")]
public static class PermissionOverviewCommand
{
    [Main]
    public static async Task Execute(CommandArgs args)
    {
        var assemblies = args.Context.PluginManager.LoadedPlugins
            .Select(static host => host.Plugin.GetType().Assembly)
            .Append(Assembly.GetExecutingAssembly());
        var entries = CommandPermissionCatalog.Discover(assemblies);
        var table = TableBuilder.Create()
            .SetTitle("指令权限列表")
            .SetHeader("指令", "所需权限")
            .SetMemberUin(args.SenderUin)
            .SetLineMaxTextLength(48);

        foreach (var entry in entries)
        {
            table.AddRow(
                $"/{entry.Path}",
                entry.Permissions.Count == 0 ? "无需权限" : string.Join(" + ", entry.Permissions));
        }

        await args.ReplyImageAsync(table.Build());
    }
}
