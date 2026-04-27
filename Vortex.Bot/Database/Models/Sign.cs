using LinqToDB.Mapping;

namespace Vortex.Bot.Database.Models;

[Table("Sign")]
public class Sign
{
    [Column("QQ")]
    [PrimaryKey]
    public long UserId { get; set; }

    [Column("LastDate")]
    public string LastDate { get; set; } = string.Empty;

    [Column("date")]
    public long Date { get; set; }

    public static IDataContext<Sign> DataContext => RecordBase.GetContext<Sign>("Sign");

    #region 静态方法

    public static Sign? Query(long userId) => DataContext.Records.FirstOrDefault(x => x.UserId == userId);

    public static List<Sign> GetAll() => [.. DataContext.Records];

    public static Sign DoSignIn(long userId)
    {
        var signInfo = Query(userId);
        var today = DateTime.Now.ToString("yyyyMMdd");

        if (signInfo == null)
        {
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

            signInfo.Date += 1;
            signInfo.LastDate = today;
            RecordBase.GetContext<Sign>("Sign").Update(signInfo);
        }

        return signInfo;
    }

    public static bool HasSignedToday(long userId)
    {
        var signInfo = Query(userId);
        return signInfo != null && signInfo.LastDate == DateTime.Now.ToString("yyyyMMdd");
    }


    public static int GetConsecutiveDays(long userId)
    {
        var signInfo = Query(userId);
        if (signInfo == null) return 0;

        var yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
        return signInfo.LastDate == yesterday || signInfo.LastDate == DateTime.Now.ToString("yyyyMMdd") ? 1 : 0;
    }

    #endregion
}
