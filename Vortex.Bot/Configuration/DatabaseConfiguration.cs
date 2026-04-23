namespace Vortex.Bot.Configuration;

public class DatabaseConfiguration
{
    /// <summary>
    /// 数据库文件路径（相对于应用程序目录）
    /// </summary>
    public string DbPath { get; set; } = "Data/vortex.db";

    /// <summary>
    /// 数据库类型
    /// </summary>
    public string SqlType { get; set; } = "Sqlite";
}
