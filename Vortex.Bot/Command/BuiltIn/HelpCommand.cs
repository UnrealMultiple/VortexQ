using Vortex.Bot.Attributes;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.BuiltIn;

[Command("help", "帮助", "菜单")]
[HelpText("显示帮助菜单")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.help")]
public static class HelpCommand
{
    [Main]
    public static async Task ShowHelp(GroupCommandArgs args)
    {
        var commands = args.Context.CommandManager.GetAllCommandInfos(CommandType.Group, includeSubCommands: false);
        var builder = MenuBuilder.Create()
            .SetMemberUin(args.Member?.Uin ?? 0);
        foreach (var command in commands)
        {
            builder.AddCell(command.Aliases.First(), command.HelpText ?? "");
        }
        await args.ReplyImageAsync(builder.Build());
    }
}
