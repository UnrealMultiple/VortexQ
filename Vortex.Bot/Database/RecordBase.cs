namespace Vortex.Bot.Database;

/// <summary>
/// 数据库实体基类
/// </summary>
public static class RecordBase
{
    private static string? _connectionString;

    /// <summary>
    /// 初始化数据库连接
    /// </summary>
    public static void Initialize(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// 获取数据上下文
    /// </summary>
    public static IDataContext<T> GetContext<T>(string tableName) where T : class, new()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new InvalidOperationException("数据库连接未初始化，请先调用 RecordBase.Initialize()");
        }

        return new DataContext<T>(tableName, _connectionString);
    }
}
