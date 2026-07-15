namespace Vortex.Bot.Command;

internal sealed class CommandInfoExtractor(Command rootCommand, string[] rootAliases, bool includeSubCommands)
{
    private readonly Command _rootCommand = rootCommand;
    private readonly string[] _rootAliases = rootAliases;
    private readonly bool _includeSubCommands = includeSubCommands;

    public IEnumerable<CommandInfo> Extract() => ExtractFromCommand(_rootCommand, _rootAliases, "", []);

    private IEnumerable<CommandInfo> ExtractFromCommand(
        Command command,
        string[] aliases,
        string parentPath,
        IReadOnlyList<string> inheritedPermissions)
    {
        var currentPath = string.IsNullOrEmpty(parentPath) ? aliases[0] : parentPath;
        var commandPermissions = CombinePermissions(inheritedPermissions, command.RequiredPermissions);

        var mainExecutor = command.GetMainCommands()
            .OfType<CommandExecutor>()
            .FirstOrDefault();

        if (mainExecutor != null)
        {
            yield return CreateCommandInfo(currentPath, aliases, command, mainExecutor, commandPermissions);
        }

        if (!_includeSubCommands)
            yield break;

        foreach (var info in ExtractSubCommands(command, currentPath, commandPermissions))
        {
            yield return info;
        }
    }

    private IEnumerable<CommandInfo> ExtractSubCommands(
        Command command,
        string parentPath,
        IReadOnlyList<string> inheritedPermissions)
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
                yield return CreateCommandInfo(subPath, item.Aliases, null, executor, inheritedPermissions);
            }
            else if (item.Command is Command subCmd)
            {
                foreach (CommandInfo info in ExtractFromCommand(subCmd, item.Aliases, subPath, inheritedPermissions))
                {
                    yield return info;
                }
            }
        }
    }

    private static CommandInfo CreateCommandInfo(
        string path,
        string[] aliases,
        Command? command,
        CommandExecutor executor,
        IReadOnlyList<string> inheritedPermissions) => new()
    {
        Path = path,
        Aliases = aliases,
        HelpText = command?.HelpText ?? executor.HelpText,
        ParameterInfo = executor.ParameterInfo,
        RequiredPermissions = [.. CombinePermissions(inheritedPermissions, executor.RequiredPermissions)]
    };

    private static IReadOnlyList<string> CombinePermissions(
        IReadOnlyList<string> inheritedPermissions,
        IReadOnlyList<string> permissions) =>
        [.. inheritedPermissions.Concat(permissions).Distinct(StringComparer.Ordinal).OrderBy(permission => permission, StringComparer.Ordinal)];
}
