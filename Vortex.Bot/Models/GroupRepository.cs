using Vortex.Bot.Database.Models;

namespace Vortex.Bot.Models;

public static class GroupRepository
{
    public static List<Group> GetAll() => [.. Group.DataContext.Records];

    public static Group? GetGroup(string name)
    {
        return string.IsNullOrEmpty(name) ? null : Group.DataContext.Records.FirstOrDefault(g => g.Name == name);
    }

    public static Group GetGroupOrDefault(string name) => GetGroup(name) ?? DefaultGroup.Instance;

    public static bool Exists(string name) => Group.DataContext.Records.Any(g => g.Name == name);

    public static Group Create(string name, string permissions = "", string? parentName = null)
    {
        if (Exists(name))
        {
            throw new InvalidOperationException($"组 '{name}' 已存在");
        }

        var group = new Group
        {
            Name = name,
            LocalPermissions = PermissionSet.Parse(permissions),
            ParentName = parentName ?? DefaultGroup.Instance.Name
        };
        
        Group.DataContext.Insert(group);
        return group;
    }

    public static void Delete(string name) => Group.DataContext.Delete(g => g.Name == name);

    public static void AddPermission(string groupName, string permission)
    {
        var group = GetGroup(groupName) ?? throw new InvalidOperationException($"组 '{groupName}' 不存在");
        group.AddPermission(permission);
        group.Save();
    }

    public static void RemovePermission(string groupName, string permission)
    {
        var group = GetGroup(groupName) ?? throw new InvalidOperationException($"组 '{groupName}' 不存在");
        group.RemovePermission(permission);
        group.Save();
    }

    public static void SetParent(string groupName, string parentName)
    {
        var group = GetGroup(groupName) ?? throw new InvalidOperationException($"组 '{groupName}' 不存在");
        group.ParentName = parentName;
        group.Save();
    }
}

