using System.Data;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SQLite;
using Microsoft.Data.Sqlite;

namespace Vortex.Bot.Database;

/// <summary>
/// 数据库服务实现
/// </summary>
public class DatabaseService : IDatabaseService, IDisposable
{
    private readonly string _dbPath;
    private IDbConnection? _connection;

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                _connection = new SqliteConnection(ConnectionString);
                _connection.Open();
            }
            return _connection;
        }
    }

    public string ConnectionString { get; }

    public SqlType SqlType => SqlType.Sqlite;

    public DatabaseService(string dbPath)
    {
        _dbPath = dbPath;

        // 确保目录存在
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        ConnectionString = $"Data Source={dbPath}";

        // 初始化 RecordBase
        RecordBase.Initialize(ConnectionString);
    }

    public IDataContext<T> GetContext<T>(string tableName) where T : class, new()
    {
        return new DataContext<T>(tableName, ConnectionString);
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }
}

/// <summary>
/// 数据上下文实现
/// </summary>
public class DataContext<T> : DataConnection, IDataContext<T> where T : class, new()
{
    public IQueryable<T> Records => this.GetTable<T>();

    public DataContext(string tableName, string connectionString)
        : base(ProviderName.SQLiteMS, connectionString)
    {
        // 配置字符串类型映射
        MappingSchema.AddScalarType(typeof(string), new LinqToDB.SqlQuery.SqlDataType(DataType.NVarChar, 255));

        // 自动创建表（如果不存在）
        this.CreateTable<T>(tableName, tableOptions: TableOptions.CreateIfNotExists);
    }

    int IDataContext<T>.Insert(T entity)
    {
        return this.Insert(entity);
    }

    int IDataContext<T>.Update(T entity)
    {
        return this.Update(entity);
    }

    int IDataContext<T>.Delete(Func<T, bool> predicate)
    {
        var items = this.GetTable<T>().Where(predicate).ToList();
        int count = 0;
        foreach (var item in items)
        {
            count += this.Delete(item);
        }
        return count;
    }
}
