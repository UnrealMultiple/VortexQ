using System.Data;

namespace Vortex.Bot.Database;

public interface IDatabaseService
{
    IDbConnection Connection { get; }

    string ConnectionString { get; }

    SqlType SqlType { get; }

    IDataContext<T> GetContext<T>(string tableName) where T : class, new();
}

public interface IDataContext<T> where T : class, new()
{

    IQueryable<T> Records { get; }

    int Insert(T entity);

    int Update(T entity);

    int Delete(Func<T, bool> predicate);
}

public enum SqlType
{
    Unknown,
    Sqlite,
    Mysql
}
