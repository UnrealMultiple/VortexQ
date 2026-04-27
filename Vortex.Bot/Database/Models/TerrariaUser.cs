using LinqToDB.Mapping;

namespace Vortex.Bot.Database.Models;

[Table("TerrariaUser")]
public class TerrariaUser
{
    [Column("ID")]
    public long Id { get; set; }

    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Column("Server")]
    public string Server { get; set; } = string.Empty;

    [Column("Password")]
    public string Password { get; set; } = string.Empty;

    [Column("GroupID")]
    public long GroupId { get; set; }

    [PrimaryKey, Identity]
    [Column("Index")]
    public int Index { get; set; }

    public static IDataContext<TerrariaUser> DataContext => RecordBase.GetContext<TerrariaUser>("TerrariaUser");

    #region 静态方法

    public static List<TerrariaUser> GetAll() => [.. DataContext.Records];

    public static List<TerrariaUser> GetUsersByServer(string server) => [.. DataContext.Records.Where(f => f.Server == server)];

    public static List<TerrariaUser> GetUsersById(long id) => [.. DataContext.Records.Where(f => f.Id == id)];

    public static List<TerrariaUser> GetUsersById(long id, string server) => [.. DataContext.Records.Where(f => f.Server == server && f.Id == id)];

    public static TerrariaUser? GetUserById(long id, string server, string name) => DataContext.Records
            .FirstOrDefault(f => f.Server == server && f.Name == name && f.Id == id);

    public static TerrariaUser? GetUserByName(string name, string server) => DataContext.Records
            .FirstOrDefault(x => x.Name == name && x.Server == server);

    public static TerrariaUser? GetUserByName(string name) => DataContext.Records
            .FirstOrDefault(x => x.Name == name);

    public static bool HasUser(string server, string name) => DataContext.Records
            .Any(x => x.Name == name && x.Server == server);

    public static bool Exists(long id, string server, string name) => DataContext.Records
            .Any(x => x.Id == id && x.Name == name && x.Server == server);

    public static void Add(long id, long groupId, string server, string name, string password)
    {
        if (Exists(id, server, name))
        {
            throw new InvalidOperationException("此用户已经注册过了!");
        }

        var existingUser = GetUserByName(name, server);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"此名称已经被{existingUser.Id}注册过了!");
        }

        var user = new TerrariaUser
        {
            Id = id,
            Server = server,
            Password = password,
            Name = name,
            GroupId = groupId
        };

        DataContext.Insert(user);
    }

    public static void ResetPassword(long id, string serverName, string name, string password)
    {
        var user = GetUserById(id, serverName, name)
            ?? throw new InvalidOperationException("用户不存在!");

        user.Password = password;
        DataContext.Update(user);
    }

    public static void Remove(string server, string name)
    {
        var user = GetUserByName(name, server) ?? throw new InvalidOperationException($"在{server}上没有找到{name}");
        DataContext.Delete(i => i.Server == server && i.Name == name);
    }

    public static void RemoveByServer(string server)
    {
        DataContext.Delete(i => i.Server == server);
    }

    public static void Reset()
    {
        DataContext.Delete(_ => true);
    }

    #endregion
}
