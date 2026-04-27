using LinqToDB.Mapping;

namespace Vortex.Bot.Database.Models;

[Table("GroupList")]
public class Group
{
    [Column]
    [PrimaryKey]
    public string Name { get; set; } = string.Empty;

    [Column]
    public string Permission
    {
        get
        {
            List<string> all = new List<string>(_permissions);
            foreach (string perm in _negatedPermissions)
            {
                all.Add("!" + perm);
            }
            return string.Join(",", all);
        }
        set
        {
            _permissions.Clear();
            _negatedPermissions.Clear();
            if (!string.IsNullOrEmpty(value))
            {
                foreach (string perm in value.Split(','))
                {
                    AddPermission(perm.Trim());
                }
            }
        }
    }

    [Column]
    public string parent { get; set; } = string.Empty;

    [NotColumn]
    private List<string> _permissions = [];

    [NotColumn]
    private List<string> _negatedPermissions = [];

    [NotColumn]
    public IReadOnlyList<string> Permissions => _permissions.AsReadOnly();

    [NotColumn]
    public IReadOnlyList<string> NegatedPermissions => _negatedPermissions.AsReadOnly();

    [NotColumn]
    public Group? Parent
    {
        get => string.IsNullOrEmpty(parent) ? null : GetGroup(parent);
        set => parent = value?.Name ?? string.Empty;
    }

    public static IDataContext<Group> DataContext => RecordBase.GetContext<Group>("GroupList");

    public void AddPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return;

        if (permission.StartsWith("!"))
        {
            var actualPerm = permission[1..];
            if (!_negatedPermissions.Contains(actualPerm))
            {
                _negatedPermissions.Add(actualPerm);
                _permissions.Remove(actualPerm);
            }
        }
        else
        {
            if (!_permissions.Contains(permission))
            {
                _permissions.Add(permission);
                _negatedPermissions.Remove(permission);
            }
        }
    }

    public void RemovePermission(string permission)
    {
        if (permission.StartsWith('!'))
        {
            _negatedPermissions.Remove(permission[1..]);
        }
        else
        {
            _permissions.Remove(permission);
        }
    }

    public void SetPermissions(IEnumerable<string> permissions)
    {
        _permissions.Clear();
        _negatedPermissions.Clear();
        foreach (var perm in permissions)
        {
            AddPermission(perm);
        }
    }

    public List<string> GetTotalPermissions()
    {
        var allPerms = new HashSet<string>();
        var negatedPerms = new HashSet<string>();
        var traversed = new HashSet<string>();

        var current = this;
        while (current != null)
        {
            if (traversed.Contains(current.Name))
            {
                throw new InvalidOperationException($"检测到循环继承: {current.Name}");
            }
            traversed.Add(current.Name);

            foreach (var perm in current._permissions)
            {
                if (!negatedPerms.Contains(perm))
                {
                    allPerms.Add(perm);
                }
            }

            foreach (var perm in current._negatedPermissions)
            {
                allPerms.Remove(perm);
                negatedPerms.Add(perm);
            }

            current = current.Parent;
        }

        return [.. allPerms];
    }

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrEmpty(permission))
            return true;

        if (HasExactPermission(permission, out bool negated))
        {
            return !negated;
        }

        var nodes = permission.Split('.');
        for (var i = nodes.Length - 1; i >= 0; i--)
        {
            nodes[i] = "*";
            var wildcardPerm = string.Join(".", nodes, 0, i + 1);
            if (HasExactPermission(wildcardPerm, out negated))
            {
                return !negated;
            }
        }

        return false;
    }

    private bool HasExactPermission(string permission, out bool negated)
    {
        negated = false;
        var traversed = new HashSet<string>();

        var current = this;
        while (current != null)
        {
            if (traversed.Contains(current.Name))
            {
                throw new InvalidOperationException($"检测到循环继承: {current.Name}");
            }
            traversed.Add(current.Name);

            if (current._negatedPermissions.Contains(permission))
            {
                negated = true;
                return false;
            }

            if (current._permissions.Contains(permission))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    #region 静态方法

    public static List<Group> GetAll() => [.. DataContext.Records];

    public static Group? GetGroup(string name)
    {
        return string.IsNullOrEmpty(name) ? null : DataContext.Records.FirstOrDefault(g => g.Name == name);
    }

    public static Group GetGroupOrDefault(string name) => GetGroup(name) ?? DefaultGroup.Instance;

    public static bool Exists(string name) => DataContext.Records.Any(g => g.Name == name);

    public static void Add(string name, string permissions = "", string? parentName = null)
    {
        if (Exists(name))
        {
            throw new InvalidOperationException($"组 '{name}' 已存在");
        }

        var group = new Group
        {
            Name = name,
            Permission = permissions,
            parent = parentName ?? DefaultGroup.Instance.Name
        };

        DataContext.Insert(group);
    }


    public static void Update(Group group) => DataContext.Update(group);

    public static void Delete(string name) => DataContext.Delete(g => g.Name == name);

    public static void AddPermission(string groupName, string permission)
    {
        var group = GetGroup(groupName) ?? throw new InvalidOperationException($"组 '{groupName}' 不存在");
        group.AddPermission(permission);
        Update(group);
    }

    public static void RemovePermission(string groupName, string permission)
    {
        var group = GetGroup(groupName) ?? throw new InvalidOperationException($"组 '{groupName}' 不存在");
        group.RemovePermission(permission);
        Update(group);
    }

    public static void SetParent(string groupName, string parentName)
    {
        var group = GetGroup(groupName) ?? throw new InvalidOperationException($"组 '{groupName}' 不存在");
        group.parent = parentName;
        Update(group);
    }

    #endregion
}
