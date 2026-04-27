using System.Text;

namespace Vortex.Bot.Command;

internal sealed class HelpTreeBuilder(string rootPath)
{
    private readonly StringBuilder _sb = new();
    private readonly string _rootPath = rootPath;

    public string Build(Command command)
    {
        _sb.Clear();
        _sb.AppendLine("📋 可用指令:");
        _sb.AppendLine();

        var mainExecutor = command.GetMainCommands()
            .OfType<CommandExecutor>()
            .FirstOrDefault();

        if (mainExecutor != null)
        {
            var helpText = command.HelpText ?? mainExecutor.HelpText;
            var node = new HelpNode(
                _rootPath,
                mainExecutor.ParameterInfo,
                helpText
            );
            BuildCommandLine(node, "", true);
        }

        var children = GetVisibleChildren(command, _rootPath);

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var isLast = i == children.Count - 1;
            BuildCommandLine(child, "", isLast);
        }

        return _sb.ToString().TrimEnd();
    }

    private void BuildCommandLine(HelpNode node, string indent, bool isLast)
    {
        var branch = isLast ? "└── " : "├── ";

        var line = $"{indent}{branch}{node.FullPath}{node.ParamInfo}";

        if (!string.IsNullOrEmpty(node.HelpText))
        {
            line += $" - {node.HelpText}";
        }

        _sb.AppendLine(line);

        if (node.SubCommands.Count > 0)
        {
            var childIndent = indent + (isLast ? "    " : "│   ");

            for (var i = 0; i < node.SubCommands.Count; i++)
            {
                var child = node.SubCommands[i];
                var isLastChild = i == node.SubCommands.Count - 1;
                BuildCommandLine(child, childIndent, isLastChild);
            }
        }
    }

    private List<HelpNode> GetVisibleChildren(Command command, string parentPath)
    {
        var nodes = new List<HelpNode>();

        var grouped = command.GetNamedCommands()
            .SelectMany(kv => kv.Value.Select(cmd => new { Name = kv.Key, Command = cmd }))
            .Where(item => item.Command.GetType().Name != "HelpCommand")
            .GroupBy(item => item.Command)
            .Select(g => new
            {
                Command = g.Key,
                Names = g.Select(x => x.Name).Distinct().ToList()
            });

        foreach (var item in grouped)
        {
            var firstName = item.Names.First();
            var fullPath = string.IsNullOrEmpty(parentPath) ? firstName : $"{parentPath} {firstName}";

            if (item.Command is CommandExecutor executor)
            {
                nodes.Add(new HelpNode(
                    fullPath,
                    executor.ParameterInfo,
                    executor.HelpText
                ));
            }
            else if (item.Command is Command subCmd)
            {
                var mainExecutor = subCmd.GetMainCommands()
                    .OfType<CommandExecutor>()
                    .FirstOrDefault();
                var paramInfo = mainExecutor?.ParameterInfo ?? "";
                var helpText = subCmd.HelpText ?? mainExecutor?.HelpText;

                var subChildren = GetVisibleChildren(subCmd, fullPath);

                nodes.Add(new HelpNode(
                    fullPath,
                    paramInfo,
                    helpText,
                    subChildren
                ));
            }
        }

        return nodes;
    }
}

internal sealed class HelpNode(string fullPath, string paramInfo, string? helpText = null, List<HelpNode>? subCommands = null)
{
    public string FullPath { get; } = fullPath;
    public string ParamInfo { get; } = paramInfo;
    public string? HelpText { get; } = helpText;
    public List<HelpNode> SubCommands { get; } = subCommands ?? [];
}
