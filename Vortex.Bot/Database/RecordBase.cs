namespace Vortex.Bot.Database;

public static class RecordBase
{
    private static string? _connectionString;

    public static void Initialize(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static IDataContext<T> GetContext<T>(string tableName) where T : class, new()
    {
        return string.IsNullOrEmpty(_connectionString)
            ? throw new InvalidOperationException("数据库连接未初始化，请先调用 RecordBase.Initialize()")
            : (IDataContext<T>)new DataContext<T>(tableName, _connectionString);
    }
}
