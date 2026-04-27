using Microsoft.Extensions.Logging;

namespace Vortex.Bot.Command;

internal static class CommandHelper
{
    internal static (string[] names, Command tree) Register(Type type)
    {
        if (!(type.IsAbstract && type.IsSealed))
            Console.WriteLine($"Command `{type.FullName}` should be a static class");

        var names = AliasResolver.GetAllAliases(type).ToArray();
        var tree = CommandTreeBuilder.BuildTree(type, names[0], names[0]);

        return (names, tree);
    }

    internal static async Task ExecuteAsync(Command tree, CommandArgs args, string commandName)
    {
        args.Logger.LogDebug("Parsing command, args: [{Args}]", string.Join(", ", args.Params));
        var result = await tree.TryParseAsync(args, 0, commandName);

        if (result.Unmatched == 0)
        {
            args.Logger.LogDebug("Command matched and executed successfully");
            return;
        }

        var errorMessage = ErrorMessageBuilder.BuildParseError(args, result, commandName);
        await args.ReplyWithAtAsync(errorMessage);
    }
}
