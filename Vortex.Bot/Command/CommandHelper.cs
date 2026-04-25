using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Attributes;

namespace Vortex.Bot.Command;

internal static class CommandHelper
{
    private static IEnumerable<string> GetAlias(MemberInfo info)
    {
        var commandAttr = info.GetCustomAttribute<CommandAttribute>();
        if (commandAttr != null && commandAttr.Alias.Count > 0)
        {
            foreach (var a in commandAttr.Alias)
                yield return a;
            yield break;
        }

        var aliasAttrs = info.GetCustomAttributes<AliasAttribute>().SelectMany(a => a.Alias);
        var hasAlias = false;
        foreach (var a in aliasAttrs)
        {
            hasAlias = true;
            yield return a;
        }
        if (hasAlias)
            yield break;
        yield return info.Name.ToLowerInvariant();
    }

    private static Command BuildTree(Type type, string name, string prefix)
    {
        var result = new Command(type, name, prefix);

        foreach (var t in type.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
        {
            var aliases = GetAlias(t).ToArray();
            var displayName = aliases[0];
            var sub = BuildTree(t, displayName, $"{prefix} {displayName}");
            foreach (var alias in aliases)
                result.Add(alias, sub);
        }

        foreach (var func in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            var aliases = GetAlias(func).ToArray();
            var isMain = func.GetCustomAttribute<MainAttribute>() != null;
            var isFlexible = isMain || func.GetCustomAttribute<FlexibleAttribute>() != null;
            var displayName = aliases[0];
            var executorName = isMain ? "" : displayName;
            CommandBase sub = new CommandExecutor(func, prefix, executorName, isFlexible);

            if (isMain)
                result.Add(null, sub);
            else
                foreach (var alias in aliases)
                    result.Add(alias, sub);
        }

        result.PostBuildTree();
        return result;
    }

    private static async Task ParseCommandAsync(Command tree, CommandArgs args, string commandName)
    {
        args.Logger.LogDebug("Parsing command, args: [{Args}]", string.Join(", ", args.Params));
        var result = await tree.TryParseAsync(args, 0, commandName);
        if (result.Unmatched == 0)
        {
            args.Logger.LogDebug("Command matched and executed successfully");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("❌ 指令格式错误");
        var paramInfo = GetParamInfoFromResult(result.Current);
        var fullCommandName = args.CommandPrefix + args.CommandName;
        sb.AppendLine($"最接近的匹配: {fullCommandName}{paramInfo}");

        sb.AppendLine();
        sb.AppendLine("💡 提示:");
        sb.AppendLine("使用 'help' 查看所有可用指令");
        sb.AppendLine("检查参数数量和类型是否正确");
        await args.ReplyAsync(sb.ToString().TrimEnd());
    }

    private static string GetParamInfoFromResult(CommandBase commandBase)
    {
        if (commandBase is CommandExecutor executor)
        {
            return executor.GetParamInfo();
        }

        if (commandBase is Command command)
        {
            var firstExecutor = command.GetAllSubCommands()
                .OfType<CommandExecutor>()
                .FirstOrDefault();
            if (firstExecutor != null)
            {
                return firstExecutor.GetParamInfo();
            }
        }

        return "";
    }

    private static IEnumerable<string> GetCommandAlias(MemberInfo info)
    {
        var commandAliases = info.GetCustomAttributes<CommandAttribute>().SelectMany(a => a.Alias);
        var aliasAttrs = info.GetCustomAttributes<AliasAttribute>().SelectMany(a => a.Alias);
        
        var flag = false;

        foreach (var a in commandAliases)
        {
            flag = true;
            yield return a;
        }

        foreach (var a in aliasAttrs)
        {
            flag = true;
            yield return a;
        }

        if (flag)
            yield break;

        yield return info.Name.ToLowerInvariant();
    }

    internal static (string[] names, Command tree) Register(Type type)
    {
        if (!(type.IsAbstract && type.IsSealed))
            Console.WriteLine($"Command `{type.FullName}` should be a static class");

        var names = GetCommandAlias(type).ToArray();
        var tree = BuildTree(type, names[0], names[0]);

        return (names, tree);
    }

    internal static async Task ExecuteAsync(Command tree, CommandArgs args, string commandName)
    {
        await ParseCommandAsync(tree, args, commandName);
    }
}
