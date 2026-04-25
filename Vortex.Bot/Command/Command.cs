using System.Reflection;
using System.Text;

namespace Vortex.Bot.Command;

internal sealed class Command : CommandBase
{
    private readonly Dictionary<string, List<CommandBase>> _dict = [];
    private readonly List<CommandBase> _main = [];
    private readonly string _infoPrefix;
    private readonly string _name;

    public Command(MemberInfo type, string name, string infoPrefix) : base(type)
    {
        _infoPrefix = infoPrefix;
        _name = name;
        Info = infoPrefix;
    }

    #region 内部集合访问器（供 Help 系统使用）

    internal IReadOnlyDictionary<string, List<CommandBase>> GetNamedCommands() => _dict;

    internal IEnumerable<CommandBase> GetMainCommands() => _main;

    internal IEnumerable<Command> GetNestedCommands() => _main.OfType<Command>();

    #endregion

    public void PostBuildTree()
    {
        _main.Add(new HelpCommand(this, _infoPrefix));
        var firstExecutor = GetAllSubCommands()
            .OfType<CommandExecutor>()
            .FirstOrDefault();

        if (firstExecutor != null)
        {
            var paramInfo = firstExecutor.GetParamInfo();
            if (!string.IsNullOrEmpty(paramInfo))
            {
                var hasMainCommand = _main.Any(sub => sub is CommandExecutor);
                if (hasMainCommand)
                {
                    Info = $"{_infoPrefix}{paramInfo}";
                }
                else
                {
                    Info = $"{_infoPrefix} <...>{paramInfo}";
                }
            }
        }
    }

    public void Add(string? cmd, CommandBase sub)
    {
        if (string.IsNullOrEmpty(cmd))
        {
            _main.Add(sub);
        }
        else if (_dict.TryGetValue(cmd, out var lst))
        {
            lst.Add(sub);
        }
        else
        {
            _dict.Add(cmd, [sub]);
        }
    }

    public override async Task<ParseResult> TryParseAsync(CommandArgs args, int current, string commandName)
    {
        var permResult = await CheckPermissionAsync(args);
        if (permResult.Result != PermissionResult.Granted)
        {
            await args.ReplyAsync(permResult.DenyMessage ?? "你没有权限执行此指令。");
            return GetResult(0);
        }

        var most = GetResult(args.Params.Count - current + 1);
        if (current < args.Params.Count && args.Params[current] == "help")
        {
            if (_dict.TryGetValue("help", out var helpSubs))
            {
                foreach (var sub in helpSubs)
                {
                    var res = await sub.TryParseAsync(args, current + 1, commandName);
                    if (res.Unmatched == 0)
                        return res;

                    if (res.Unmatched < most.Unmatched)
                        most = res;
                }
            }

            foreach (var sub in _main)
            {
                if (sub is HelpCommand)
                {
                    var res = await sub.TryParseAsync(args, current, commandName);
                    if (res.Unmatched == 0)
                        return res;
                }
            }

            return most;
        }

        if (current < args.Params.Count && _dict.TryGetValue(args.Params[current], out var subs))
        {
            foreach (var sub in subs)
            {
                var res = await sub.TryParseAsync(args, current + 1, commandName);
                if (res.Unmatched == 0)
                    return res;

                if (res.Unmatched < most.Unmatched)
                    most = res;
            }
        }

        foreach (var sub in _main)
        {
            var res = await sub.TryParseAsync(args, current, commandName);
            if (res.Unmatched == 0)
                return res;

            if (res.Unmatched < most.Unmatched)
                most = res;
        }
        if (most.Unmatched > 0 && current >= args.Params.Count && _dict.Count > 0)
        {
            await ShowSubCommandsHint(args, commandName);
            return GetResult(0);
        }

        return most;
    }

    private async Task ShowSubCommandsHint(CommandArgs args, string commandName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("❌ 指令不完整，请指定子命令");

        var prefix = args.CommandPrefix + commandName;
        for (int i = 0; i < args.Params.Count; i++)
        {
            prefix += " " + args.Params[i];
        }

        sb.AppendLine($"当前路径: {prefix}");
        sb.AppendLine();
        sb.AppendLine("📋 可用子命令:");
        var availableSubCommands = new List<string>();

        foreach (var (cmdName, subs) in _dict)
        {
            if (cmdName == "help") continue;

            foreach (var sub in subs)
            {
                if (sub.CanExec(args))
                {
                    availableSubCommands.Add(cmdName);
                    break;
                }
            }
        }

        var hasMainExecutor = _main.Any(m => m is CommandExecutor);

        if (availableSubCommands.Count == 0 && !hasMainExecutor)
        {
            sb.AppendLine("  (无可用子命令)");
        }
        else
        {
            foreach (var cmdName in availableSubCommands.OrderBy(n => n))
            {
                sb.AppendLine($"  • {cmdName}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("💡 提示:");
        sb.AppendLine($"使用 '{prefix} help' 查看详细帮助");
        sb.AppendLine("检查命令拼写是否正确");

        await args.ReplyAsync(sb.ToString().TrimEnd());
    }

    public string GetName() => _name;

    public IEnumerable<CommandBase> GetAllSubCommands() => _dict.Values.SelectMany(subs => subs).Concat(_main).Distinct();

    public IEnumerable<(string Path, CommandBase Command)> GetAllCommandsWithPath(string parentPath)
    {
        foreach (var (cmdName, subs) in _dict)
        {
            var currentPath = string.IsNullOrEmpty(parentPath) ? cmdName : $"{parentPath} {cmdName}";
            foreach (var sub in subs)
            {
                yield return (currentPath, sub);
                if (sub is Command cmd)
                {
                    foreach (var nested in cmd.GetAllCommandsWithPath(currentPath))
                    {
                        yield return nested;
                    }
                }
            }
        }

        foreach (var sub in _main.Where(sub => sub is not HelpCommand))
        {
            yield return (parentPath, sub);
            if (sub is Command cmd)
            {
                foreach (var nested in cmd.GetAllCommandsWithPath(parentPath))
                {
                    yield return nested;
                }
            }
        }
    }

    #region Help 系统

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
            if (!ValidateHelpCommand(args, current, out var errorResult))
                return errorResult;
            var commandPath = BuildCommandPath(args, current);

            var targetCommand = FindTargetCommand(args, current);
            var helpText = BuildHelpText(targetCommand ?? _rootCommand, commandPath);
            await args.ReplyAsync(helpText);

            return GetResult(0);
        }

        private bool ValidateHelpCommand(CommandArgs args, int current, out ParseResult errorResult)
        {
            errorResult = GetResult(0);

            if (current != args.Params.Count - 1)
            {
                errorResult = GetResult(Math.Abs(args.Params.Count - 1 - current));
                return false;
            }
            if (args.Params[current] != "help")
            {
                errorResult = GetResult(1);
                return false;
            }

            return true;
        }

        private static string BuildCommandPath(CommandArgs args, int current)
        {
            var path = args.CommandPrefix + args.CommandName;

            for (int i = 0; i < current; i++)
            {
                path += " " + args.Params[i];
            }

            return path;
        }

        private Command? FindTargetCommand(CommandArgs args, int current)
        {
            if (current == 0) return null;

            var target = _rootCommand;
            for (int i = 0; i < current && target != null; i++)
            {
                var cmdName = args.Params[i];
                var sub = target.GetNamedCommands()
                    .Where(kv => kv.Key.Equals(cmdName, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(kv => kv.Value)
                    .FirstOrDefault();

                if (sub is Command cmd)
                {
                    target = cmd;
                }
                else
                {
                    return null;
                }
            }

            return target;
        }
        private static string BuildHelpText(Command command, string commandPath)
        {
            var builder = new HelpTreeBuilder(commandPath);
            return builder.Build(command);
        }
    }

    #endregion
}
