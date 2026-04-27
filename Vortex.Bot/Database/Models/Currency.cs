using LinqToDB.Mapping;

namespace Vortex.Bot.Database.Models;

[Table("Currency")]
public class Currency
{
    [Column("QQ")]
    [PrimaryKey]
    public long UserId { get; set; }

    [Column("num")]
    public long Num { get; set; }

    public static IDataContext<Currency> DataContext => RecordBase.GetContext<Currency>("Currency");

    #region 静态方法

    public static Currency? Query(long userId) => DataContext.Records.FirstOrDefault(x => x.UserId == userId);

    public static long GetBalance(long userId) => Query(userId)?.Num ?? 0;

    public static Currency Add(long userId, long amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("增加数量必须大于0", nameof(amount));
        }

        var currency = Query(userId);
        if (currency == null)
        {
            currency = new Currency
            {
                UserId = userId,
                Num = amount
            };
            RecordBase.GetContext<Currency>("Currency").Insert(currency);
        }
        else
        {
            currency.Num += amount;
            RecordBase.GetContext<Currency>("Currency").Update(currency);
        }

        return currency;
    }

    public static Currency Deduct(long userId, long amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("扣除数量必须大于0", nameof(amount));
        }

        var currency = Query(userId) ?? throw new InvalidOperationException("用户没有货币记录，无法扣除！");
        if (currency.Num < amount)
        {
            throw new InvalidOperationException($"余额不足！当前余额: {currency.Num}，需要: {amount}");
        }

        currency.Num -= amount;
        RecordBase.GetContext<Currency>("Currency").Update(currency);

        return currency;
    }

    public static Currency Set(long userId, long amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException("数量不能为负数", nameof(amount));
        }

        var currency = Query(userId);
        if (currency == null)
        {
            currency = new Currency
            {
                UserId = userId,
                Num = amount
            };
            RecordBase.GetContext<Currency>("Currency").Insert(currency);
        }
        else
        {
            currency.Num = amount;
            RecordBase.GetContext<Currency>("Currency").Update(currency);
        }

        return currency;
    }

    public static void Transfer(long fromUserId, long toUserId, long amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("转账数量必须大于0", nameof(amount));
        }

        Deduct(fromUserId, amount);
        Add(toUserId, amount);
    }

    public static List<Currency> GetTop(int top = 10) => [.. DataContext.Records
            .OrderByDescending(x => x.Num)
            .Take(top)];

    #endregion
}
