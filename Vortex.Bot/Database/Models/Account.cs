using LinqToDB.Mapping;
using Vortex.Bot.Database;

namespace Vortex.Bot.Database.Models;

[Table("Account")]
public class Account
{
    [Column("ID")]
    [PrimaryKey]
    public long UserId { get; set; }

    [Column("Group")]
    public string GroupName { get; set; } = string.Empty;

    [NotColumn]
    public Group Group
    {
        get => Group.GetGroupOrDefault(GroupName);
        set => GroupName = value.Name;
    }

    public bool HasPermission(string permission)
    {
        return Group.HasPermission(permission);
    }

    #region 静态方法

    public static List<Account> GetAll()
    {
        return [.. RecordBase.GetContext<Account>("Account").Records];
    }

    public static Account? GetByUserId(long userId)
    {
        return RecordBase.GetContext<Account>("Account").Records.FirstOrDefault(a => a.UserId == userId);
    }

    public static Account GetOrDefault(long userId)
    {
        return GetByUserId(userId) ?? new Account { UserId = userId, Group = DefaultGroup.Instance };
    }

    public static bool Exists(long userId)
    {
        return RecordBase.GetContext<Account>("Account").Records.Any(a => a.UserId == userId);
    }

    public static bool HasPermission(long userId, string permission)
    {
        return GetOrDefault(userId).HasPermission(permission);
    }

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

    public static void Update(Account account)
    {
        RecordBase.GetContext<Account>("Account").Update(account);
    }

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

    public static void Delete(long userId)
    {
        RecordBase.GetContext<Account>("Account").Delete(a => a.UserId == userId);
    }

    #endregion
}
