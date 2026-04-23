using LinqToDB.Mapping;
using Vortex.Bot.Database;

namespace Vortex.Bot.Database.Models;

/// <summary>
/// 货币/积分实体 - 兼容 XocMat 原有表结构
/// 表名: Currency
/// </summary>
[Table("Currency")]
public class Currency
{
    [Column("QQ")]
    [PrimaryKey]
    public long UserId { get; set; }

    /// <summary>
    /// 货币数量
    /// </summary>
    [Column("num")]
    public long Num { get; set; }

    #region 静态方法

    /// <summary>
    /// 查询用户货币
    /// </summary>
    public static Currency? Query(long userId)
    {
        return RecordBase.GetContext<Currency>("Currency").Records.FirstOrDefault(x => x.UserId == userId);
    }

    /// <summary>
    /// 获取用户货币数量，不存在返回0
    /// </summary>
    public static long GetBalance(long userId)
    {
        return Query(userId)?.Num ?? 0;
    }

    /// <summary>
    /// 增加货币
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="amount">数量</param>
    /// <returns>更新后的货币信息</returns>
    public static Currency Add(long userId, long amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("增加数量必须大于0", nameof(amount));
        }

        var currency = Query(userId);
        if (currency == null)
        {
            // 首次添加
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

    /// <summary>
    /// 扣除货币
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="amount">数量</param>
    /// <returns>更新后的货币信息</returns>
    /// <exception cref="InvalidOperationException">余额不足或用户不存在</exception>
    public static Currency Deduct(long userId, long amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("扣除数量必须大于0", nameof(amount));
        }

        var currency = Query(userId);
        if (currency == null)
        {
            throw new InvalidOperationException("用户没有货币记录，无法扣除！");
        }

        if (currency.Num < amount)
        {
            throw new InvalidOperationException($"余额不足！当前余额: {currency.Num}，需要: {amount}");
        }

        currency.Num -= amount;
        RecordBase.GetContext<Currency>("Currency").Update(currency);

        return currency;
    }

    /// <summary>
    /// 设置货币数量
    /// </summary>
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

    /// <summary>
    /// 转账
    /// </summary>
    /// <param name="fromUserId">转出用户</param>
    /// <param name="toUserId">转入用户</param>
    /// <param name="amount">数量</param>
    public static void Transfer(long fromUserId, long toUserId, long amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("转账数量必须大于0", nameof(amount));
        }

        // 扣除转出方
        Deduct(fromUserId, amount);

        // 增加转入方
        Add(toUserId, amount);
    }

    /// <summary>
    /// 获取排行榜
    /// </summary>
    /// <param name="top">前N名</param>
    public static List<Currency> GetTop(int top = 10)
    {
        return RecordBase.GetContext<Currency>("Currency").Records
            .OrderByDescending(x => x.Num)
            .Take(top)
            .ToList();
    }

    #endregion
}
