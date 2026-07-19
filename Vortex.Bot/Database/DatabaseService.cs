using LinqToDB;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Vortex.Bot.Database;

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
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        ConnectionString = $"Data Source={dbPath}";
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
        GC.SuppressFinalize(this);
    }
}

public class DataContext<T> : DataConnection, IDataContext<T> where T : class, new()
{
    public IQueryable<T> Records => this.GetTable<T>();

    public DataContext(string tableName, string connectionString)
        : base(new DataOptions().UseConnectionString(ProviderName.SQLiteMS, connectionString))
    {
        MappingSchema.AddScalarType(typeof(string), new LinqToDB.SqlQuery.SqlDataType(DataType.NVarChar, 255));
        this.CreateTable<T>(tableName, tableOptions: TableOptions.CreateIfNotExists);
    }

    int IDataContext<T>.Insert(T entity)
    {
        return Convert.ToInt32(this.InsertWithIdentity(entity));
    }

    int IDataContext<T>.Update(T entity)
    {
        return this.Update(entity);
    }

    int IDataContext<T>.Delete(Func<T, bool> predicate)
    {
        var items = this.GetTable<T>().Where(predicate).ToList();
        var count = 0;
        foreach (var item in items)
        {
            count += this.Delete(item);
        }
        return count;
    }
}
