using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Commands;

[Command("help", "帮助", "菜单")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.help")]
public static class HelpCommand
{
    [Main]
    public static async Task ShowHelp(GroupCommandArgs args)
    {
        var commands = args.Context.CommandManager.GetAllCommands(CommandType.Group);
        var builder = MenuBuilder.Create()
            .SetMemberUin(args.Member?.Uin ?? 0);
        foreach (var command in commands)
        {
            builder.AddCell(command, "hhhhhh");
        }
        await args.ReplyImageAsync(builder.Build());
    }
}
