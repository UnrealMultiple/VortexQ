using System.Reflection;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Attributes;

namespace Vortex.Bot.Command;

internal static class CommandHelper
{
    private static IEnumerable<string> GetAlias(MemberInfo info)
    {
        var alias = info.GetCustomAttributes<AliasAttribute>().SelectMany(a => a.Alias);
        var flag = false;

        foreach (var a in alias)
        {
            flag = true;
            yield return a;
        }

        if (flag)
            yield break;

        yield return info.Name.ToLowerInvariant();
    }

    private static string AliasToString(string[] alias)
    {
        return alias.Length == 1 ? alias[0] + " " : $"({string.Join('|', alias)}) ";
    }

    private static Command BuildTree(Type type, string name, string prefix)
    {
        var result = new Command(type, name, prefix);

        foreach (var t in type.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
        {
            var al = GetAlias(t).ToArray();
            var sub = BuildTree(t, al[0], prefix + AliasToString(al));
            foreach (var alias in al)
                result.Add(alias, sub);
        }

        foreach (var func in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            var al = GetAlias(func).ToArray();
            var isFlexible = func.GetCustomAttribute<FlexibleAttribute>() != null;
            var isMain = func.GetCustomAttribute<MainAttribute>() != null;

            var infoPrefix = isMain ? prefix : prefix + AliasToString(al);
            CommandBase sub = new CommandExecutor(func, infoPrefix, isFlexible);

            if (isMain)
                result.Add(null, sub);
            else
                foreach (var alias in al)
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

        await args.ReplyAsync($"Best match: {result.Current}\nUse subcommand 'help' for more usage");
    }

    private static IEnumerable<string> GetCommandAlias(MemberInfo info)
    {
        var alias = info.GetCustomAttributes<CommandAttribute>().SelectMany(a => a.Alias);
        var flag = false;

        foreach (var a in alias)
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
        var tree = BuildTree(type, names[0], AliasToString(names));

        return (names, tree);
    }

    internal static async Task ExecuteAsync(Command tree, CommandArgs args, string commandName)
    {
        await ParseCommandAsync(tree, args, commandName);
    }
}
