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

        BuildCommandNode(command, _rootPath, "", true);
        return _sb.ToString().TrimEnd();
    }

    private void BuildCommandNode(Command command, string currentPath, string indent, bool isLast)
    {
        var children = GetVisibleChildren(command);
        if (children.Count == 0)
        {
            BuildLeafNode(command, currentPath, indent, isLast);
            return;
        }

        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var isLastChild = i == children.Count - 1;
            BuildChildNode(child, currentPath, indent, isLastChild);
        }
    }

    private void BuildLeafNode(Command command, string currentPath, string indent, bool isLast)
    {
        var mainExecutor = command.GetMainCommands()
            .OfType<CommandExecutor>()
            .FirstOrDefault();

        if (mainExecutor == null) return;

        var branch = isLast ? "└── " : "├── ";
        var paramInfo = mainExecutor.GetParamInfo();
        _sb.AppendLine($"{indent}{branch}{currentPath}{paramInfo}");
    }

    private void BuildChildNode(HelpNode child, string parentPath, string indent, bool isLast)
    {
        var branch = isLast ? "└── " : "├── ";
        var childIndent = isLast ? "    " : "│   ";

        if (child.IsLeaf)
        {
            _sb.AppendLine($"{indent}{branch}{child.DisplayName}{child.ParamInfo}");
        }
        else
        {
            _sb.AppendLine($"{indent}{branch}{child.DisplayName}{child.ParamInfo}");
            BuildCommandNode(child.Command!, child.FullPath, indent + childIndent, isLast);
        }
    }

    private List<HelpNode> GetVisibleChildren(Command command)
    {
        var children = new List<HelpNode>();

        var namedChildren = GetNamedSubCommands(command);
        children.AddRange(namedChildren);

        var nestedChildren = GetNestedSubCommands(command);
        children.AddRange(nestedChildren);

        return children;
    }

    private List<HelpNode> GetNamedSubCommands(Command command)
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
            var displayName = string.Join(" / ", item.Names);
            var firstName = item.Names.First();

            if (item.Command is CommandExecutor executor)
            {
                nodes.Add(new HelpNode(
                    displayName,
                    firstName,
                    executor.GetParamInfo()
                ));
            }
            else if (item.Command is Command subCmd)
            {
                var mainExecutor = subCmd.GetMainCommands()
                    .OfType<CommandExecutor>()
                    .FirstOrDefault();
                var paramInfo = mainExecutor?.GetParamInfo() ?? "";

                nodes.Add(new HelpNode(
                    displayName,
                    firstName,
                    paramInfo,
                    subCmd
                ));
            }
        }

        return nodes;
    }

    private List<HelpNode> GetNestedSubCommands(Command command)
    {
        var nodes = new List<HelpNode>();

        var nestedCommands = command.GetNestedCommands();

        foreach (var subCmd in nestedCommands)
        {
            nodes.Add(new HelpNode(
                "",
                "",
                "",
                subCmd
            ));
        }

        return nodes;
    }
}

internal sealed class HelpNode(string displayName, string fullPath, string paramInfo, Command? command = null)
{
    public string DisplayName { get; } = displayName;

    public string FullPath { get; } = fullPath;

    public string ParamInfo { get; } = paramInfo;

    public bool IsLeaf => Command == null;

    public Command? Command { get; } = command;
}
