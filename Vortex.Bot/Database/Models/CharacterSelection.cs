using LinqToDB.Mapping;

namespace Vortex.Bot.Database.Models;

[Table("CharacterSelection")]
public class CharacterSelection
{
    [Column("UserID")]
    public long UserId { get; set; }

    [Column("GroupID")]
    public long GroupId { get; set; }

    [Column("ServerName")]
    public string ServerName { get; set; } = string.Empty;

    [Column("UpdateTime")]
    public DateTime UpdateTime { get; set; } = DateTime.Now;

    [PrimaryKey, Identity]
    [Column("Index")]
    public int Index { get; set; }

    public static IDataContext<CharacterSelection> DataContext => RecordBase.GetContext<CharacterSelection>("CharacterSelection");

    #region 静态方法
    public static List<CharacterSelection> GetAll()
    {
        return [.. DataContext.Records];
    }

    public static CharacterSelection? GetSelection(long userId, long groupId) => DataContext.Records
            .FirstOrDefault(f => f.UserId == userId && f.GroupId == groupId);

    public static void SetSelection(long userId, long groupId, string serverName)
    {
        var existing = DataContext.Records
            .FirstOrDefault(f => f.UserId == userId && f.GroupId == groupId);

        if (existing != null)
        {
            existing.ServerName = serverName;
            existing.UpdateTime = DateTime.Now;
            DataContext.Update(existing);
        }
        else
        {
            var selection = new CharacterSelection
            {
                UserId = userId,
                GroupId = groupId,
                ServerName = serverName,
                UpdateTime = DateTime.Now
            };
            DataContext.Insert(selection);
        }
    }

    public static void ClearSelection(long userId, long groupId) => DataContext.Delete(f => f.UserId == userId && f.GroupId == groupId);

    public static void ClearAllSelections(long userId) => DataContext.Delete(f => f.UserId == userId);

    public static void ClearByServer(string serverName) => DataContext.Delete(f => f.ServerName == serverName);

    #endregion
}
