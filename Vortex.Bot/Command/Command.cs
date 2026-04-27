using System.Reflection;

namespace Vortex.Bot.Command;

internal sealed class Command : CommandBase
{
    private readonly Dictionary<string, List<CommandBase>> _namedCommands = [];
    private readonly List<CommandBase> _mainCommands = [];
    private readonly string _infoPrefix;
    private readonly string _name;
    private readonly bool _skipHelp;

    public Command(MemberInfo type, string name, string infoPrefix, bool skipHelp = false) : base(type)
    {
        _infoPrefix = infoPrefix;
        _name = name;
        _skipHelp = skipHelp;
        Info = infoPrefix;
    }

    public void PostBuildTree()
    {
        _mainCommands.Add(new HelpCommand(this, _infoPrefix));

        var firstExecutor = GetAllSubCommands()
            .OfType<CommandExecutor>()
            .FirstOrDefault();

        if (firstExecutor != null)
        {
            var paramInfo = firstExecutor.ParameterInfo;
            if (!string.IsNullOrEmpty(paramInfo))
            {
                var hasMainCommand = _mainCommands.Any(sub => sub is CommandExecutor);
                Info = hasMainCommand
                    ? $"{_infoPrefix}{paramInfo}"
                    : $"{_infoPrefix} <...>{paramInfo}";
            }
        }
    }

    public void Add(string? cmd, CommandBase sub)
    {
        if (string.IsNullOrEmpty(cmd))
        {
            _mainCommands.Add(sub);
        }
        else if (_namedCommands.TryGetValue(cmd, out List<CommandBase>? list))
        {
            list.Add(sub);
        }
        else
        {
            _namedCommands.Add(cmd, [sub]);
        }
    }

    public override async Task<ParseResult> TryParseAsync(CommandArgs args, int current, string commandName)
    {
        var permResult = await CheckPermissionAsync(args);
        if (permResult.Result != PermissionResult.Granted)
        {
            await args.ReplyWithAtAsync(permResult.DenyMessage ?? "你没有权限执行此指令。");
            return CreateResult(0);
        }

        var bestMatch = CreateResult(args.Params.Count - current + 1);

        if (current < args.Params.Count && args.Params[current] == "help" && !_skipHelp)
        {
            var helpResult = await TryParseHelpAsync(args, current, commandName);
            if (helpResult.Unmatched == 0)
                return helpResult;

            if (helpResult.Unmatched < bestMatch.Unmatched)
                bestMatch = helpResult;
        }

        if (current < args.Params.Count && _namedCommands.TryGetValue(args.Params[current], out List<CommandBase>? subs))
        {
            foreach (var sub in subs)
            {
                var res = await sub.TryParseAsync(args, current + 1, commandName);
                if (res.Unmatched == 0)
                    return res;

                if (res.Unmatched < bestMatch.Unmatched)
                    bestMatch = res;
            }
        }

        foreach (var sub in _mainCommands)
        {
            var res = await sub.TryParseAsync(args, current, commandName);
            if (res.Unmatched == 0)
                return res;

            if (res.Unmatched < bestMatch.Unmatched)
                bestMatch = res;
        }

        if (bestMatch.Unmatched > 0 && current >= args.Params.Count && _namedCommands.Count > 0)
        {
            await ShowSubCommandsHint(args, commandName);
            return CreateResult(0);
        }

        return bestMatch;
    }

    private async Task<ParseResult> TryParseHelpAsync(CommandArgs args, int current, string commandName)
    {
        var bestMatch = CreateResult(args.Params.Count - current + 1);

        if (_namedCommands.TryGetValue("help", out var helpSubs))
        {
            foreach (var sub in helpSubs)
            {
                var res = await sub.TryParseAsync(args, current + 1, commandName);
                if (res.Unmatched == 0)
                    return res;

                if (res.Unmatched < bestMatch.Unmatched)
                    bestMatch = res;
            }
        }

        foreach (var sub in _mainCommands)
        {
            if (sub is HelpCommand)
            {
                var res = await sub.TryParseAsync(args, current, commandName);
                if (res.Unmatched == 0)
                    return res;

                if (res.Unmatched < bestMatch.Unmatched)
                    bestMatch = res;
            }
        }

        return bestMatch;
    }

    private async Task ShowSubCommandsHint(CommandArgs args, string commandName)
    {
        var message = ErrorMessageBuilder.BuildIncompleteCommandHint(this, args, commandName);
        await args.ReplyWithAtAsync(message);
    }

    public string GetName() => _name;

    public IReadOnlyDictionary<string, List<CommandBase>> GetNamedCommands() => _namedCommands;

    public IEnumerable<CommandBase> GetMainCommands() => _mainCommands;

    public IEnumerable<Command> GetNestedCommands() => _mainCommands.OfType<Command>();

    public IEnumerable<CommandBase> GetAllSubCommands() =>
        _namedCommands.Values.SelectMany(subs => subs).Concat(_mainCommands).Distinct();

    public IEnumerable<(string Path, CommandBase Command)> GetAllCommandsWithPath(string parentPath)
    {
        foreach ((var cmdName, var subs) in _namedCommands)
        {
            string currentPath = string.IsNullOrEmpty(parentPath) ? cmdName : $"{parentPath} {cmdName}";
            foreach (var sub in subs)
            {
                yield return (currentPath, sub);
                if (sub is Command cmd)
                {
                    foreach ((string Path, CommandBase Command) nested in cmd.GetAllCommandsWithPath(currentPath))
                        yield return nested;
                }
            }
        }

        foreach (var sub in _mainCommands.Where(sub => sub is not HelpCommand))
        {
            yield return (parentPath, sub);
            if (sub is Command cmd)
            {
                foreach ((string Path, CommandBase Command) nested in cmd.GetAllCommandsWithPath(parentPath))
                    yield return nested;
            }
        }
    }

    private sealed class HelpCommand : CommandBase
    {
        private readonly Command _rootCommand;

        public HelpCommand(Command rootCommand, string infoPrefix)
        {
            _rootCommand = rootCommand;
            Permissions = [];
            Info = infoPrefix + "help";
        }

        public override async Task<ParseResult> TryParseAsync(CommandArgs args, int current, string commandName)
        {
            if (!ValidateHelpCommand(args, current, out ParseResult errorResult))
                return errorResult;

            var targetCommand = FindTargetCommand(args, current);
            var commandPath = BuildCommandPath(args, current);
            var helpText = BuildHelpText(targetCommand ?? _rootCommand, commandPath);

            await args.ReplyWithAtAsync(helpText);
            return CreateResult(0);
        }

        private bool ValidateHelpCommand(CommandArgs args, int current, out ParseResult errorResult)
        {
            errorResult = CreateResult(0);

            if (current != args.Params.Count - 1)
            {
                errorResult = CreateResult(Math.Abs(args.Params.Count - 1 - current));
                return false;
            }

            if (args.Params[current] != "help")
            {
                errorResult = CreateResult(1);
                return false;
            }

            return true;
        }

        private static string BuildCommandPath(CommandArgs args, int current)
        {
            var path = args.CommandPrefix + args.CommandName;

            for (var i = 0; i < current; i++)
                path += " " + args.Params[i];

            return path;
        }

        private Command? FindTargetCommand(CommandArgs args, int current)
        {
            if (current == 0) return null;

            var target = _rootCommand;
            for (var i = 0; i < current && target != null; i++)
            {
                var cmdName = args.Params[i];
                CommandBase? sub = target.GetNamedCommands()
                    .Where(kv => kv.Key.Equals(cmdName, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(kv => kv.Value)
                    .FirstOrDefault();

                if (sub is Command cmd)
                    target = cmd;
                else
                    return null;
            }

            return target;
        }

        private static string BuildHelpText(Command command, string commandPath)
        {
            var builder = new HelpTreeBuilder(commandPath);
            return builder.Build(command);
        }
    }
}
