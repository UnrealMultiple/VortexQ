using System.Reflection;
using Vortex.Bot.Attributes;

namespace Vortex.Bot.Command;

internal static class CommandTreeBuilder
{
    public static Command BuildTree(Type type, string name, string prefix)
    {
        var skipHelp = type.GetCustomAttribute<SkipHelpAttribute>() != null;
        var result = new Command(type, name, prefix, skipHelp);

        foreach (var nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
        {
            var aliases = AliasResolver.GetAliases(nestedType).ToArray();
            var displayName = aliases[0];
            var subCommand = BuildTree(nestedType, displayName, $"{prefix} {displayName}");
            foreach (var alias in aliases)
                result.Add(alias, subCommand);
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            var aliases = AliasResolver.GetAliases(method).ToArray();
            var isMain = method.GetCustomAttribute<MainAttribute>() != null;
            var flexibleAttr = method.GetCustomAttribute<FlexibleAttribute>();
            var isFlexible = flexibleAttr != null;

            var executorType = isFlexible ? ExecutorType.Flexible : ExecutorType.Normal;
            var minArgs = isFlexible ? flexibleAttr!.MinArgs : 0;

            var displayName = aliases[0];
            var executorName = isMain ? "" : displayName;

            if (isMain)
            {
                var mainExecutor = new CommandExecutor(method, prefix, executorName, executorType, minArgs);
                result.Add(null, mainExecutor);
            }
            else
            {
                var executor = new CommandExecutor(method, prefix, executorName, executorType, minArgs);
                foreach (var alias in aliases)
                    result.Add(alias, executor);
            }
        }

        result.PostBuildTree();
        return result;
    }
}
