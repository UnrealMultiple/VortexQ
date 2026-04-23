using LinqToDB.Mapping;
using Vortex.Bot.Database;

namespace Vortex.Bot.Database.Models;

/// <summary>
/// 用户账户实体 - 兼容 XocMat 原有表结构
/// 表名: Account
/// </summary>
[Table("Account")]
public class Account
{
    [Column("ID")]
    [PrimaryKey]
    public long UserId { get; set; }

    [Column("Group")]
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// 所属权限组
    /// </summary>
    [NotColumn]
    public Group Group
    {
        get => Group.GetGroupOrDefault(GroupName);
        set => GroupName = value.Name;
    }

    /// <summary>
    /// 检查账户是否有指定权限
    /// </summary>
    public bool HasPermission(string permission)
    {
        return Group.HasPermission(permission);
    }

    #region 静态方法

    /// <summary>
    /// 获取所有账户
    /// </summary>
    public static List<Account> GetAll()
    {
        return RecordBase.GetContext<Account>("Account").Records.ToList();
    }

    /// <summary>
    /// 根据用户ID获取账户
    /// </summary>
    public static Account? GetByUserId(long userId)
    {
        return RecordBase.GetContext<Account>("Account").Records.FirstOrDefault(a => a.UserId == userId);
    }

    /// <summary>
    /// 获取账户，如果不存在返回默认账户
    /// </summary>
    public static Account GetOrDefault(long userId)
    {
        return GetByUserId(userId) ?? new Account { UserId = userId, Group = DefaultGroup.Instance };
    }

    /// <summary>
    /// 检查账户是否存在
    /// </summary>
    public static bool Exists(long userId)
    {
        return RecordBase.GetContext<Account>("Account").Records.Any(a => a.UserId == userId);
    }

    /// <summary>
    /// 检查用户是否有指定权限
    /// </summary>
    public static bool HasPermission(long userId, string permission)
    {
        return GetOrDefault(userId).HasPermission(permission);
    }

    /// <summary>
    /// 添加账户
    /// </summary>
    public static void Add(long userId, string groupName)
    {
        if (Exists(userId))
        {
            throw new InvalidOperationException($"账户 {userId} 已存在");
        }

        if (!Group.Exists(groupName))
        {
            throw new InvalidOperationException($"组 '{groupName}' 不存在");
        }

        var account = new Account
        {
            UserId = userId,
            GroupName = groupName
        };

        RecordBase.GetContext<Account>("Account").Insert(account);
    }

    /// <summary>
    /// 更新账户
    /// </summary>
    public static void Update(Account account)
    {
        RecordBase.GetContext<Account>("Account").Update(account);
    }

    /// <summary>
    /// 更改账户的组
    /// </summary>
    public static void SetGroup(long userId, string groupName)
    {
        var account = GetByUserId(userId) ?? throw new InvalidOperationException($"账户 {userId} 不存在");

        if (!Group.Exists(groupName))
        {
            throw new InvalidOperationException($"组 '{groupName}' 不存在");
        }

        account.GroupName = groupName;
        Update(account);
    }

    /// <summary>
    /// 删除账户
    /// </summary>
    public static void Delete(long userId)
    {
        RecordBase.GetContext<Account>("Account").Delete(a => a.UserId == userId);
    }

    #endregion
}
