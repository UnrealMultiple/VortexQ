using System.Text;

namespace Vortex.Bot.Command;

internal static class ErrorMessageBuilder
{
    public static string BuildParseError(CommandArgs args, ParseResult result, string commandName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("❌ 指令格式错误");

        var paramInfo = GetParameterInfo(result.Current);
        var fullCommandName = args.CommandPrefix + args.CommandName;
        sb.AppendLine($"最接近的匹配: {fullCommandName}{paramInfo}");

        sb.AppendLine();
        sb.AppendLine("💡 提示:");
        sb.AppendLine("使用 'help' 查看所有可用指令");
        sb.AppendLine("检查参数数量和类型是否正确");

        return sb.ToString().TrimEnd();
    }

    public static string BuildIncompleteCommandHint(Command command, CommandArgs args, string commandName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("❌ 指令不完整，请指定子命令");

        var prefix = args.CommandPrefix + commandName;
        for (var i = 0; i < args.Params.Count; i++)
        {
            prefix += " " + args.Params[i];
        }

        sb.AppendLine($"当前路径: {prefix}");
        sb.AppendLine();
        sb.AppendLine("📋 可用子命令:");

        var availableSubCommands = GetAvailableSubCommands(command, args);
        var hasMainExecutor = command.GetMainCommands().Any(m => m is CommandExecutor);

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

        return sb.ToString().TrimEnd();
    }

    private static string GetParameterInfo(CommandBase commandBase)
    {
        if (commandBase is CommandExecutor executor)
            return executor.ParameterInfo;

        if (commandBase is Command command)
        {
            var firstExecutor = command.GetAllSubCommands()
                .OfType<CommandExecutor>()
                .FirstOrDefault();
            if (firstExecutor != null)
                return firstExecutor.ParameterInfo;
        }

        return "";
    }

    private static List<string> GetAvailableSubCommands(Command command, CommandArgs args)
    {
        var result = new List<string>();

        foreach (var (cmdName, subs) in command.GetNamedCommands())
        {
            if (cmdName == "help") continue;

            foreach (var sub in subs)
            {
                if (sub.CanExecute(args))
                {
                    result.Add(cmdName);
                    break;
                }
            }
        }

        return result;
    }
}
