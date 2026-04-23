using System.Text;
using LinqToDB.Mapping;
using Vortex.Bot.Database;

namespace Vortex.Bot.Database.Models;

/// <summary>
/// 权限组实体 - 兼容 XocMat 原有表结构
/// 表名: GroupList
/// </summary>
[Table("GroupList")]
public class Group
{
    [Column]
    [PrimaryKey]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 权限字符串（逗号分隔，以!开头的为否定权限）
    /// </summary>
    [Column]
    public string Permission
    {
        get
        {
            var all = new List<string>(_permissions);
            foreach (var perm in _negatedPermissions)
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
                foreach (var perm in value.Split(','))
                {
                    AddPermission(perm.Trim());
                }
            }
        }
    }

    /// <summary>
    /// 父组名称
    /// </summary>
    [Column]
    public string parent { get; set; } = string.Empty;

    [NotColumn]
    private List<string> _permissions = [];

    [NotColumn]
    private List<string> _negatedPermissions = [];

    /// <summary>
    /// 当前组的权限列表（不包括继承的）
    /// </summary>
    [NotColumn]
    public IReadOnlyList<string> Permissions => _permissions.AsReadOnly();

    /// <summary>
    /// 当前组的否定权限列表
    /// </summary>
    [NotColumn]
    public IReadOnlyList<string> NegatedPermissions => _negatedPermissions.AsReadOnly();

    /// <summary>
    /// 父组
    /// </summary>
    [NotColumn]
    public Group? Parent
    {
        get => string.IsNullOrEmpty(parent) ? null : GetGroup(parent);
        set => parent = value?.Name ?? string.Empty;
    }

    /// <summary>
    /// 添加权限
    /// </summary>
    public void AddPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return;

        if (permission.StartsWith("!"))
        {
            // 否定权限
            var actualPerm = permission[1..];
            if (!_negatedPermissions.Contains(actualPerm))
            {
                _negatedPermissions.Add(actualPerm);
                _permissions.Remove(actualPerm); // 移除冲突的正向权限
            }
        }
        else
        {
            // 正向权限
            if (!_permissions.Contains(permission))
            {
                _permissions.Add(permission);
                _negatedPermissions.Remove(permission); // 移除冲突的否定权限
            }
        }
    }

    /// <summary>
    /// 移除权限
    /// </summary>
    public void RemovePermission(string permission)
    {
        if (permission.StartsWith("!"))
        {
            _negatedPermissions.Remove(permission[1..]);
        }
        else
        {
            _permissions.Remove(permission);
        }
    }

    /// <summary>
    /// 设置权限列表
    /// </summary>
    public void SetPermissions(IEnumerable<string> permissions)
    {
        _permissions.Clear();
        _negatedPermissions.Clear();
        foreach (var perm in permissions)
        {
            AddPermission(perm);
        }
    }

    /// <summary>
    /// 获取所有有效权限（包括继承的，已解决冲突）
    /// </summary>
    public List<string> GetTotalPermissions()
    {
        var allPerms = new HashSet<string>();
        var negatedPerms = new HashSet<string>();
        var traversed = new HashSet<string>(); // 防止循环继承

        Group? current = this;
        while (current != null)
        {
            if (traversed.Contains(current.Name))
            {
                throw new InvalidOperationException($"检测到循环继承: {current.Name}");
            }
            traversed.Add(current.Name);

            // 先添加正向权限
            foreach (var perm in current._permissions)
            {
                if (!negatedPerms.Contains(perm))
                {
                    allPerms.Add(perm);
                }
            }

            // 再处理否定权限
            foreach (var perm in current._negatedPermissions)
            {
                allPerms.Remove(perm);
                negatedPerms.Add(perm);
            }

            current = current.Parent;
        }

        return [.. allPerms];
    }

    /// <summary>
    /// 检查是否有指定权限（支持通配符 *）
    /// </summary>
    public bool HasPermission(string permission)
    {
        if (string.IsNullOrEmpty(permission))
            return true;

        // 检查精确权限
        if (HasExactPermission(permission, out bool negated))
        {
            return !negated;
        }

        // 检查通配符权限
        var nodes = permission.Split('.');
        for (int i = nodes.Length - 1; i >= 0; i--)
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

    /// <summary>
    /// 检查是否有精确权限（不包括通配符）
    /// </summary>
    private bool HasExactPermission(string permission, out bool negated)
    {
        negated = false;
        var traversed = new HashSet<string>();

        Group? current = this;
        while (current != null)
        {
            if (traversed.Contains(current.Name))
            {
                throw new InvalidOperationException($"检测到循环继承: {current.Name}");
            }
            traversed.Add(current.Name);

            // 先检查否定权限
            if (current._negatedPermissions.Contains(permission))
            {
                negated = true;
                return false;
            }

            // 再检查正向权限
            if (current._permissions.Contains(permission))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    #region 静态方法

    /// <summary>
    /// 获取所有组
    /// </summary>
    public static List<Group> GetAll()
    {
        return RecordBase.GetContext<Group>("GroupList").Records.ToList();
    }

    /// <summary>
    /// 根据名称获取组
    /// </summary>
    public static Group? GetGroup(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        return RecordBase.GetContext<Group>("GroupList").Records.FirstOrDefault(g => g.Name == name);
    }

    /// <summary>
    /// 获取组，如果不存在返回默认组
    /// </summary>
    public static Group GetGroupOrDefault(string name)
    {
        return GetGroup(name) ?? DefaultGroup.Instance;
    }

    /// <summary>
    /// 检查组是否存在
    /// </summary>
    public static bool Exists(string name)
    {
        return RecordBase.GetContext<Group>("GroupList").Records.Any(g => g.Name == name);
    }

    /// <summary>
    /// 添加组
    /// </summary>
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

        RecordBase.GetContext<Group>("GroupList").Insert(group);
    }

    /// <summary>
    /// 更新组
    /// </summary>
    public static void Update(Group group)
    {
        RecordBase.GetContext<Group>("GroupList").Update(group);
    }

    /// <summary>
    /// 删除组
    /// </summary>
    public static void Delete(string name)
    {
        RecordBase.GetContext<Group>("GroupList").Delete(g => g.Name == name);
    }

    /// <summary>
    /// 添加权限到组
    /// </summary>
    public static void AddPermission(string groupName, string permission)
    {
        var group = GetGroup(groupName) ?? throw new InvalidOperationException($"组 '{groupName}' 不存在");
        group.AddPermission(permission);
        Update(group);
    }

    /// <summary>
    /// 从组移除权限
    /// </summary>
    public static void RemovePermission(string groupName, string permission)
    {
        var group = GetGroup(groupName) ?? throw new InvalidOperationException($"组 '{groupName}' 不存在");
        group.RemovePermission(permission);
        Update(group);
    }

    /// <summary>
    /// 更改组的父组
    /// </summary>
    public static void SetParent(string groupName, string parentName)
    {
        var group = GetGroup(groupName) ?? throw new InvalidOperationException($"组 '{groupName}' 不存在");
        group.parent = parentName;
        Update(group);
    }

    #endregion
}
