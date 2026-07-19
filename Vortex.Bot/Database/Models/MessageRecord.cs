using Lagrange.Core.Message;
using LinqToDB;
using LinqToDB.Mapping;
using Vortex.Bot.Utility;

namespace Vortex.Bot.Database.Models;

[Table("MessageRecord")]
public class MessageRecord
{
    [PrimaryKey, Identity]
    public int Id { get; set; }

    [Column(nameof(TypeInt))]
    public int TypeInt { get; set; }

    [Column(nameof(SequenceLong))]
    public long SequenceLong { get; set; }

    [Column(nameof(ClientSequenceLong))]
    public long ClientSequenceLong { get; set; }

    [Column(nameof(MessageIdLong))]
    public long MessageIdLong { get; set; }

    public DateTimeOffset Time { get; set; }

    [Column(nameof(FromUinLong))]
    public long FromUinLong { get; set; }

    [Column(nameof(ToUinLong))]
    public long ToUinLong { get; set; }

    [Column("Entities", DataType = DataType.VarBinary)]
    public byte[] Entities { get; set; } = [];

    #region 静态方法

    public static MessageRecord? Query(ulong messageId)
    {
        return RecordBase.GetContext<MessageRecord>("MessageRecord")
            .Records.FirstOrDefault(x => (ulong)x.MessageIdLong == messageId);
    }

    public MessageChain? DeserializeEntities()
    {
        return MessageChainSerializer.Deserialize(Entities);
    }

    public static void Insert(MessageRecord record)
    {
        var context = RecordBase.GetContext<MessageRecord>("MessageRecord");
        var count = context.Records.Count();
        if (count > 1000)
        {
            var toDelete = context.Records.OrderBy(x => x.Id).Take(100).ToList();
            foreach (var item in toDelete)
            {
                context.Delete(x => x.Id == item.Id);
            }
        }

        context.Insert(record);
    }

    public static int CalcMessageHash(ulong msgId, uint seq)
    {
        return ((ushort)seq << 16) | (ushort)msgId;
    }

    public static List<MessageRecord> GetAll()
    {
        return [.. RecordBase.GetContext<MessageRecord>("MessageRecord").Records];
    }

    public static void Clear()
    {
        var context = RecordBase.GetContext<MessageRecord>("MessageRecord");
        var all = context.Records.ToList();
        foreach (var item in all)
        {
            context.Delete(x => x.Id == item.Id);
        }
    }

    #endregion
}

public enum MessageType
{
    Friend = 0,
    Group = 1,
    Temp = 2,
}
