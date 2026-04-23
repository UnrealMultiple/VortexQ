namespace Vortex.Bot.Database.Models;

public class DefaultGroup : Group
{
    /// <summary>
    /// 默认权限列表
    /// </summary>
    public static readonly List<string> DefaultPermissions =
    [
        "user.help",
        "user.info",
        "user.register",
        "user.status",
        "user.ping",
        "user.time",
        "vortex.help",
        "vortex.selfinfo",
        "vortex.sign",
        "vortex.user",
        "vortex.user.register",
        "vortex.user.info"
    ];

    /// <summary>
    /// 默认组名称
    /// </summary>
    public const string DefaultGroupName = "default";

    /// <summary>
    /// 单例实例
    /// </summary>
    public static readonly DefaultGroup Instance = new();

    private DefaultGroup() : base()
    {
        Name = DefaultGroupName;
        // 设置默认权限
        SetPermissions(DefaultPermissions);
    }

    /// <summary>
    /// 初始化默认组到数据库
    /// </summary>
    public static void Initialize()
    {
        if (!Group.Exists(DefaultGroupName))
        {
            try
            {
                Group.Add(DefaultGroupName, string.Join(",", DefaultPermissions), "");
            }
            catch
            {
                // 已存在则忽略
            }
        }
    }
}
