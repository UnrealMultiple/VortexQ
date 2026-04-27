namespace Vortex.Bot.Command;

internal sealed class CommandInfoExtractor(Command rootCommand, string[] rootAliases, bool includeSubCommands)
{
    private readonly Command _rootCommand = rootCommand;
    private readonly string[] _rootAliases = rootAliases;
    private readonly bool _includeSubCommands = includeSubCommands;

    public IEnumerable<CommandInfo> Extract() => ExtractFromCommand(_rootCommand, _rootAliases, "");

    private IEnumerable<CommandInfo> ExtractFromCommand(Command command, string[] aliases, string parentPath)
    {
        var currentPath = string.IsNullOrEmpty(parentPath) ? aliases[0] : parentPath;

        var mainExecutor = command.GetMainCommands()
            .OfType<CommandExecutor>()
            .FirstOrDefault();

        if (mainExecutor != null)
        {
            yield return CreateCommandInfo(currentPath, aliases, command, mainExecutor);
        }

        if (!_includeSubCommands)
            yield break;

        foreach (var info in ExtractSubCommands(command, currentPath))
        {
            yield return info;
        }
    }

    private IEnumerable<CommandInfo> ExtractSubCommands(Command command, string parentPath)
    {
        var groupedSubCommands = command.GetNamedCommands()
            .SelectMany(kv => kv.Value.Select(cmd => new { Name = kv.Key, Command = cmd }))
            .Where(item => item.Command.GetType().Name != "HelpCommand")
            .GroupBy(item => item.Command)
            .Select(g => new
            {
                Command = g.Key,
                Aliases = g.Select(x => x.Name).Distinct().ToArray()
            });

        foreach (var item in groupedSubCommands)
        {
            var subPath = $"{parentPath} {item.Aliases[0]}";

            if (item.Command is CommandExecutor executor)
            {
                yield return CreateCommandInfo(subPath, item.Aliases, null, executor);
            }
            else if (item.Command is Command subCmd)
            {
                foreach (CommandInfo info in ExtractFromCommand(subCmd, item.Aliases, subPath))
                {
                    yield return info;
                }
            }
        }
    }

    private static CommandInfo CreateCommandInfo(string path, string[] aliases, Command? command, CommandExecutor executor) => new()
    {
        Path = path,
        Aliases = aliases,
        HelpText = command?.HelpText ?? executor.HelpText,
        ParameterInfo = executor.ParameterInfo
    };
}
