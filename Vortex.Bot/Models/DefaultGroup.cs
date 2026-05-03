using System.Reflection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Database.Models;

namespace Vortex.Bot.Models;

public sealed class DefaultGroup : Group
{
    public const string DefaultGroupName = "default";

    public static readonly DefaultGroup Instance = new();

    private static List<string>? _cachedDefaultPermissions;

    private DefaultGroup()
    {
        Name = DefaultGroupName;
        ParentName = string.Empty;
    }

    public static IReadOnlyList<string> DefaultPermissions
    {
        get
        {
            _cachedDefaultPermissions ??= [.. Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsDefined(typeof(PermissionAttribute), inherit: true)
                     && t.IsDefined(typeof(DefaultCommandAttribute), inherit: true))
            .SelectMany(t => t.GetCustomAttributes<PermissionAttribute>(inherit: true))
            .SelectMany(p => p.Permissions)];
            return _cachedDefaultPermissions;
        }
    }

    public static void Initialize()
    {
        EnsureDefaultGroupExists();
    }

    private static void EnsureDefaultGroupExists()
    {
        if (GroupRepository.Exists(DefaultGroupName))
            return;
        var permissions = string.Join(",", DefaultPermissions);
        GroupRepository.Create(DefaultGroupName, permissions, string.Empty);

    }

    public new bool HasPermission(string permission)
    {
        return DefaultPermissions.Contains(permission);
    }
}
