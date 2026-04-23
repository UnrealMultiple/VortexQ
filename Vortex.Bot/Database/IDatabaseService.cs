using System.Data;

namespace Vortex.Bot.Database;

/// <summary>
/// 数据库服务接口
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// 数据库连接
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    SqlType SqlType { get; }

    /// <summary>
    /// 获取指定实体的数据上下文
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="tableName">表名</param>
    /// <returns>数据上下文</returns>
    IDataContext<T> GetContext<T>(string tableName) where T : class, new();
}

/// <summary>
/// 数据上下文接口
/// </summary>
public interface IDataContext<T> where T : class, new()
{
    /// <summary>
    /// 查询所有记录
    /// </summary>
    IQueryable<T> Records { get; }

    /// <summary>
    /// 插入记录
    /// </summary>
    int Insert(T entity);

    /// <summary>
    /// 更新记录
    /// </summary>
    int Update(T entity);

    /// <summary>
    /// 删除记录
    /// </summary>
    int Delete(Func<T, bool> predicate);
}

/// <summary>
/// 数据库类型
/// </summary>
public enum SqlType
{
    Unknown,
    Sqlite,
    Mysql
}
