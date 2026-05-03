using LinqToDB.Mapping;
using Vortex.Bot.Models;

namespace Vortex.Bot.Database.Models;

[Table("GroupList")]
public class Group
{
    [Column]
    [PrimaryKey]
    public string Name { get; set; } = string.Empty;

    [Column("Parent")]
    public string ParentName { get; set; } = string.Empty;

    [Column("Permission")]
    public string PermissionString { get; set; } = string.Empty;

    [NotColumn]
    private PermissionSet? _localPermissions;

    [NotColumn]
    public PermissionSet LocalPermissions
    {
        get => _localPermissions ??= PermissionSet.Parse(PermissionString);
        set
        {
            _localPermissions = value;
            PermissionString = value.ToString();
        }
    }

    [NotColumn]
    private PermissionInheritanceResolver? _resolver;

    private PermissionInheritanceResolver GetResolver()
        => _resolver ??= new PermissionInheritanceResolver(GroupRepository.GetGroup);

    public static IDataContext<Group> DataContext => RecordBase.GetContext<Group>("GroupList");

    public void AddPermission(string permission)
    {
        LocalPermissions.Add(permission);
        PermissionString = LocalPermissions.ToString();
    }

    public void RemovePermission(string permission)
    {
        LocalPermissions.Remove(permission);
        PermissionString = LocalPermissions.ToString();
    }

    public void SetPermissions(IEnumerable<string> permissions)
    {
        LocalPermissions.Set(permissions);
        PermissionString = LocalPermissions.ToString();
    }

    public List<string> GetEffectivePermissions()
    {
        var effective = GetResolver().ResolveEffectivePermissions(this);
        return [.. effective.Permissions];
    }

    public bool HasPermission(string permission)
    {
        return GetResolver().HasPermission(this, permission);
    }

    public void Save() => DataContext.Update(this);
}