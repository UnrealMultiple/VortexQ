using System.Reflection;
using Vortex.Bot.Attributes;

namespace Vortex.Bot.Utility;

public sealed record CommandPermissionInfo(string Path, IReadOnlyList<string> Permissions);

public static class CommandPermissionCatalog
{
    public static IReadOnlyList<CommandPermissionInfo> Discover(Assembly assembly)
    {
        var entries = new List<CommandPermissionInfo>();

        foreach (var type in assembly.GetTypes()
                     .Where(static type => type.IsClass && !type.IsNested && type.GetCustomAttribute<CommandAttribute>() != null))
        {
            DiscoverType(type, FormatAliases(GetAliases(type)), [], entries);
        }

        return entries
            .GroupBy(static entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void DiscoverType(
        Type type,
        string path,
        IReadOnlyList<string> inheritedPermissions,
        ICollection<CommandPermissionInfo> entries)
    {
        var permissions = CombinePermissions(inheritedPermissions, GetPermissions(type));

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            var methodPermissions = CombinePermissions(permissions, GetPermissions(method));
            var isMain = method.GetCustomAttribute<MainAttribute>() != null;
            var commandPath = isMain ? path : $"{path} {FormatAliases(GetAliases(method))}";
            entries.Add(new CommandPermissionInfo(commandPath, methodPermissions));
        }

        foreach (var nestedType in type.GetNestedTypes(BindingFlags.Public))
        {
            var nestedPath = $"{path} {FormatAliases(GetAliases(nestedType))}";
            DiscoverType(nestedType, nestedPath, permissions, entries);
        }
    }

    private static string[] GetAliases(MemberInfo member)
    {
        var commandAliases = member.GetCustomAttribute<CommandAttribute>()?.Alias;
        if (commandAliases is { Count: > 0 })
            return [.. commandAliases];

        var aliases = member.GetCustomAttributes<AliasAttribute>()
            .SelectMany(static attribute => attribute.Alias)
            .ToArray();
        return aliases.Length > 0 ? aliases : [member.Name.ToLowerInvariant()];
    }

    private static string FormatAliases(IEnumerable<string> aliases) => string.Join(" / ", aliases);

    private static string[] GetPermissions(MemberInfo member) => [.. member
        .GetCustomAttributes<PermissionAttribute>()
        .SelectMany(static attribute => attribute.Permissions)];

    private static string[] CombinePermissions(IEnumerable<string> parent, IEnumerable<string> current) => [.. parent
        .Concat(current)
        .Distinct(StringComparer.OrdinalIgnoreCase)];
}
