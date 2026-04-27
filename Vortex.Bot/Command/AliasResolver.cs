using System.Reflection;
using Vortex.Bot.Attributes;

namespace Vortex.Bot.Command;

internal static class AliasResolver
{
    public static IEnumerable<string> GetAliases(MemberInfo member)
    {
        var commandAttr = member.GetCustomAttribute<CommandAttribute>();
        if (commandAttr != null && commandAttr.Alias.Count > 0)
        {
            foreach (string alias in commandAttr.Alias)
                yield return alias;
            yield break;
        }

        var aliasAttrs = member.GetCustomAttributes<AliasAttribute>().SelectMany(a => a.Alias);
        var hasAlias = false;
        foreach (string alias in aliasAttrs)
        {
            hasAlias = true;
            yield return alias;
        }

        if (hasAlias)
            yield break;

        yield return member.Name.ToLowerInvariant();
    }

    public static IEnumerable<string> GetAllAliases(MemberInfo member)
    {
        var commandAliases = member.GetCustomAttributes<CommandAttribute>().SelectMany(a => a.Alias);
        var aliasAttrs = member.GetCustomAttributes<AliasAttribute>().SelectMany(a => a.Alias);

        var found = false;

        foreach (string alias in commandAliases)
        {
            found = true;
            yield return alias;
        }

        foreach (string alias in aliasAttrs)
        {
            found = true;
            yield return alias;
        }

        if (found)
            yield break;

        yield return member.Name.ToLowerInvariant();
    }
}
