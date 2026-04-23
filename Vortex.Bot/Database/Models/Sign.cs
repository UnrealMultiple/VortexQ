using LinqToDB.Mapping;
using Vortex.Bot.Database;

namespace Vortex.Bot.Database.Models;

/// <summary>
/// 签到记录实体 - 兼容 XocMat 原有表结构
/// 表名: Sign
/// </summary>
[Table("Sign")]
public class Sign
{
    [Column("QQ")]
    [PrimaryKey]
    public long UserId { get; set; }

    /// <summary>
    /// 最后签到日期 (yyyyMMdd 格式)
    /// </summary>
    [Column("LastDate")]
    public string LastDate { get; set; } = string.Empty;

    /// <summary>
    /// 累计签到天数
    /// </summary>
    [Column("date")]
    public long Date { get; set; }

    #region 静态方法

    /// <summary>
    /// 查询用户签到信息
    /// </summary>
    public static Sign? Query(long userId)
    {
        return RecordBase.GetContext<Sign>("Sign").Records.FirstOrDefault(x => x.UserId == userId);
    }

    /// <summary>
    /// 获取所有签到记录
    /// </summary>
    public static List<Sign> GetAll()
    {
        return RecordBase.GetContext<Sign>("Sign").Records.ToList();
    }

    /// <summary>
    /// 签到
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>签到后的信息</returns>
    /// <exception cref="InvalidOperationException">今天已经签到过</exception>
    public static Sign DoSignIn(long userId)
    {
        var signInfo = Query(userId);
        var today = DateTime.Now.ToString("yyyyMMdd");

        if (signInfo == null)
        {
            // 首次签到
            signInfo = new Sign
            {
                UserId = userId,
                Date = 1,
                LastDate = today
            };
            RecordBase.GetContext<Sign>("Sign").Insert(signInfo);
        }
        else
        {
            if (signInfo.LastDate == today)
            {
                throw new InvalidOperationException("今天已经签到过了！");
            }

            // 更新签到信息
            signInfo.Date += 1;
            signInfo.LastDate = today;
            RecordBase.GetContext<Sign>("Sign").Update(signInfo);
        }

        return signInfo;
    }

    /// <summary>
    /// 检查今天是否已签到
    /// </summary>
    public static bool HasSignedToday(long userId)
    {
        var signInfo = Query(userId);
        if (signInfo == null) return false;

        return signInfo.LastDate == DateTime.Now.ToString("yyyyMMdd");
    }

    /// <summary>
    /// 获取连续签到天数
    /// </summary>
    public static int GetConsecutiveDays(long userId)
    {
        var signInfo = Query(userId);
        if (signInfo == null) return 0;

        // 检查昨天是否签到
        var yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
        if (signInfo.LastDate == yesterday || signInfo.LastDate == DateTime.Now.ToString("yyyyMMdd"))
        {
            return 1; // 简化处理，实际应该计算连续天数
        }

        return 0;
    }

    #endregion
}
