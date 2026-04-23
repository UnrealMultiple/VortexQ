using LinqToDB;
using LinqToDB.Mapping;
using Vortex.Bot.Database;

namespace Vortex.Bot.Database.Models;

/// <summary>
/// 消息记录实体 - 兼容 XocMat 原有表结构
/// 表名: MessageRecord
/// </summary>
[Table("MessageRecord")]
public class MessageRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column(nameof(TypeInt))]
    public int TypeInt { get; set; }

    [Column(nameof(SequenceLong))]
    public long SequenceLong { get; set; }

    [Column(nameof(ClientSequenceLong))]
    public long ClientSequenceLong { get; set; }

    [Column(nameof(MessageIdLong))]
    public long MessageIdLong { get; set; }

    /// <summary>
    /// 消息时间
    /// </summary>
    public DateTimeOffset Time { get; set; }

    [Column(nameof(FromUinLong))]
    public long FromUinLong { get; set; }

    [Column(nameof(ToUinLong))]
    public long ToUinLong { get; set; }

    /// <summary>
    /// 消息内容（序列化后的数据）
    /// </summary>
    [Column("Entities", DataType = DataType.VarBinary)]
    public byte[] Entities { get; set; } = [];

    #region 静态方法

    /// <summary>
    /// 根据消息ID查询记录
    /// </summary>
    public static MessageRecord? Query(ulong messageId)
    {
        return RecordBase.GetContext<MessageRecord>("MessageRecord")
            .Records.FirstOrDefault(x => (ulong)x.MessageIdLong == messageId);
    }

    /// <summary>
    /// 插入消息记录
    /// </summary>
    public static void Insert(MessageRecord record)
    {
        var context = RecordBase.GetContext<MessageRecord>("MessageRecord");

        // 清理旧记录（保留最近1000条）
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

    /// <summary>
    /// 计算消息哈希
    /// </summary>
    public static int CalcMessageHash(ulong msgId, uint seq)
    {
        return ((ushort)seq << 16) | (ushort)msgId;
    }

    /// <summary>
    /// 获取所有记录
    /// </summary>
    public static List<MessageRecord> GetAll()
    {
        return RecordBase.GetContext<MessageRecord>("MessageRecord").Records.ToList();
    }

    /// <summary>
    /// 清理所有记录
    /// </summary>
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

/// <summary>
/// 消息类型枚举
/// </summary>
public enum MessageType
{
    Friend = 0,
    Group = 1,
    Temp = 2,
}
