using LinqToDB.Mapping;
using Vortex.Bot.Database;

namespace Daily.Database;

[Table("Jrrp")]
public class JrrpRecord
{
    [Column("Uin")]
    [PrimaryKey]
    public long UserID { get; set; }

    [Column("Luck")]
    public int Luck { get; set; } = 0;

    [Column("Date")]
    public DateTime Date { get; set; } = DateTime.Now;

    public static IDataContext<JrrpRecord> DataContext => RecordBase.GetContext<JrrpRecord>("Jrrp");

    public static JrrpRecord? GetRecord(long userId) => DataContext.Records.FirstOrDefault(x => x.UserID == userId);

    public static void AddOrUpdateRecord(long userId, int luck)
    {
        var record = GetRecord(userId);
        if (record == null)
        {
            record = new JrrpRecord { UserID = userId, Luck = luck, Date = DateTime.Now };
            DataContext.Insert(record);
        }
        else
        {
            record.Luck = luck;
            record.Date = DateTime.Now;
            DataContext.Update(record);
        }
    }
}
