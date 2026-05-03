# Vortex.Bot

Vortex.Bot 是 VortexQ 系统的 QQ 机器人核心，基于 Lagrange.Core 实现，提供 QQ 群/私聊与 Terraria 服务器的交互能力。

## 项目信息

- **目标框架**: .NET 10.0
- **项目类型**: 控制台应用程序
- **主要功能**: QQ 机器人、Socket 服务器、命令系统、插件系统

## 核心功能

- **QQ 机器人**: 基于 Lagrange.Core 框架，支持扫码登录、密码登录、设备锁验证
- **Socket 服务器**: TCP 服务器，管理与 TShock 适配器的连接
- **命令系统**: 强大的命令解析和执行系统，支持多级命令、参数绑定、权限控制
- **插件系统**: 支持动态加载/卸载/重载插件，热更新无需重启
- **多服务器管理**: 支持同时管理多个 Terraria 服务器
- **图片生成**: 支持生成背包、在线列表、游戏进度等图片
- **数据库支持**: 使用 linq2db 支持 SQLite、MySQL 等数据库

## 核心服务

### CoreLoginService

QQ 登录服务，处理扫码登录、密码登录、验证码、设备锁等登录流程。

**支持的登录方式**:
- 扫码登录（推荐，Uin 设为 0）
- 密码登录（需要账号和密码）
- 设备锁验证
- 短信验证码

### VortexSocketService

Socket 服务器，管理与 Vortex.Adapter 的连接。

**功能**:
- TCP 监听和客户端连接管理
- 客户端认证（Token 验证）
- 数据包路由和处理
- 心跳检测
- 广播和单播消息

### PacketHandlerService

数据包处理器管理，自动注册和路由数据包处理器。

**处理器类型**:
- `IRoutedPacketHandler<TRequest, TResponse>` - 异步处理器接口
- `RoutedRequestHandlerBase<TRequest, TResponse>` - 同步处理器基类
- `RoutedPushHandlerBase<TRequest>` - 推送处理器基类

### TerrariaServerService

Terraria 服务器管理服务，支持多服务器管理。

**功能**:
- 服务器连接管理
- 用户服务器选择记录
- 服务器状态查询
- 命令执行
- 账号管理

### CommandManager

命令管理器，处理 QQ 消息中的命令。

**特性**:
- 自动注册带 `CommandAttribute` 的命令类
- 支持多级命令（如 `/server list`）
- 参数自动绑定和类型转换
- 权限检查
- 支持群组、私聊、服务器三种命令类型

### PluginManager

插件管理器，支持热加载插件。

**功能**:
- 动态加载/卸载/重载插件
- 插件隔离（独立的 AssemblyLoadContext）
- 插件生命周期管理（Initialize/Shutdown）
- 按加载顺序初始化

## 命令系统

### 命令分类

#### 内置命令

| 命令 | 说明 | 权限 |
|------|------|------|
| `help` | 查看帮助文档 | 所有人 |
| `sign` | 每日签到 | 所有人 |
| `signrank` | 签到排行榜 | 所有人 |
| `selfinfo` | 查看个人信息 | 所有人 |
| `systeminfo` | 查看系统信息 | 所有人 |
| `reload` | 重载配置或插件 | 管理员 |

#### Terraria 服务器命令

| 命令 | 说明 | 示例 |
|------|------|------|
| `serverlist` | 查看服务器列表 | `/serverlist` |
| `serverswitch <名称>` | 切换当前服务器 | `/serverswitch Server1` |
| `serverinfo` | 查看服务器信息 | `/serverinfo` |
| `online` / `onlinelist` | 查看在线玩家 | `/online` |
| `inventory [玩家名]` | 查看玩家背包 | `/inventory` 或 `/inv 玩家名` |
| `gameprogress` / `gp` | 查看游戏进度 | `/gp` |
| `register <账号> <密码>` | 注册游戏账号 | `/register test 123456` |
| `resetpassword <账号>` | 重置游戏密码 | `/resetpassword test` |
| `bindcharacter <角色名>` | 绑定游戏角色 | `/bindcharacter Player1` |
| `execute <命令>` | 执行服务器命令 | `/execute /time noon` |
| `kick <玩家名>` | 踢出玩家 | `/kick Player1` |
| `serverrestart` | 重启服务器 | `/serverrestart` |
| `serverreset` | 重置服务器 | `/serverreset` |

#### 管理命令

| 命令 | 说明 | 权限 |
|------|------|------|
| `groupadmin` | 群组管理 | 超级管理员 |
| `accountadmin` | 账号管理 | 超级管理员 |
| `terrariauseradmin` | Terraria 用户管理 | 管理员 |

#### 其他命令

| 命令 | 说明 |
|------|------|
| `wiki <关键词>` | 查询 Wiki |
| `abbreviation <缩写>` | 查询缩写含义 |
| `currency` | 货币系统 |

### 创建自定义命令

```csharp
[Command("mycommand", "mc")]      // 命令名和别名
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.mycommand")]  // 所需权限
[DefaultCommand]                  // 默认组自动拥有此权限
public class MyCommand : CommandBase
{
    [Main]  // 主执行方法
    public async Task Execute(CommandArgs args)
    {
        // 获取参数
        var param1 = args.Parameters.ElementAtOrDefault(0);
        
        // 发送消息
        await args.Context.SendMessage("命令执行成功");
    }
}
```

### 带子命令的命令

```csharp
[Command("currency", "金币")]
[CommandType(CommandType.Group | CommandType.Friend)]
[HelpText("金币系统")]
public static class CurrencyCommand
{
    // 查询子命令 - 需要额外权限
    [Command("query", "查询")]
    [Permission("vortex.currency.query")]
    [DefaultCommand]  // 默认组自动拥有查询权限
    public static class QueryCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args) { }
    }
    
    // 增加金币 - 仅管理员可用，不标记 DefaultCommand
    [Command("add", "增加")]
    [Permission("vortex.currency.admin")]
    public static class AddCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, long userId, long amount) { }
    }
}
```

### 特性说明

| 特性 | 作用目标 | 说明 |
|------|----------|------|
| `[Command]` | Class | 定义命令名和别名 |
| `[DefaultCommand]` | Class | **默认组权限标记**，带有此特性的命令，其权限会自动分配给默认组 |
| `[Main]` | Method | 标记为主执行方法 |
| `[CommandType]` | Class | 指定命令支持的类型（Group/Friend/Server） |
| `[Permission]` | Class | 指定所需权限 |
| `[HelpText]` | Class | 命令帮助描述 |
| `[Param]` | Parameter | 参数描述，用于帮助生成 |

### 权限系统说明

系统使用基于组的权限管理：

- **默认组 (default)**: 所有用户默认所属的组
- **权限继承**: 子组可以继承父组的权限
- **DefaultCommand 作用**: 标记了 `[DefaultCommand]` 的命令，其 `[Permission]` 指定的权限会自动加入默认组的权限列表

```csharp
// 示例：签到命令 - 所有用户默认可用
[Command("sign", "签到")]
[Permission("vortex.sign")]
[DefaultCommand]  // 默认组自动拥有 vortex.sign 权限
public static class SignCommand { }

// 示例：管理员命令 - 仅管理员可用
[Command("serverrestart")]
[Permission("vortex.admin.restart")]
// 不标记 [DefaultCommand]，默认组没有此权限
public static class ServerRestartCommand { }
```

## 配置说明

### 配置文件结构

```jsonc
{
  // 核心配置
  "Core": {
    // 服务器配置
    "Server": {
      "Socket": {
        "Enabled": true,        // 是否启用 Socket 服务器
        "Port": 6000,           // 监听端口
        "Token": "your-token"   // 认证令牌
      }
    },
    
    // 登录配置
    "Login": {
      "Uin": 0,                 // QQ 号，0 表示扫码登录
      "Password": "",           // 密码（可选）
      "CompatibleQrCode": false // 兼容模式二维码
    },
    
    // 数据库配置
    "Database": {
      "Type": "SQLite",         // 数据库类型: SQLite, MySQL
      "ConnectionString": "Data Source=vortex.db"
    },
    
    // 命令配置
    "Command": {
      "EnablePrefix": true,     // 是否启用前缀
      "Prefix": "/",            // 命令前缀
      "EnableAt": true          // 是否支持 @机器人
    },
    
    // 邮件配置（用于密码重置等）
    "Mail": {
      "SmtpServer": "",
      "Port": 587,
      "Username": "",
      "Password": ""
    },
    
    // 超级管理员 QQ 号列表
    "SuperAdmins": [123456789]
  },
  
  // Terraria 服务器配置
  "TerrariaServers": {
    "Servers": [
      {
        "Name": "Server1",              // 服务器名称（唯一标识）
        "DisplayName": "服务器1",        // 显示名称
        "Groups": [123456789],          // 绑定的 QQ 群号
        "Description": "主服务器"         // 描述
      }
    ]
  }
}
```

## 插件开发

### 插件接口

```csharp
public interface IPlugin : IDisposable
{
    string Name { get; }           // 插件名称
    Version Version { get; }       // 版本
    string Author { get; }         // 作者
    int LoadOrder { get; }         // 加载顺序（越小越先加载）
    PluginContext Context { get; set; }
    
    void Initialize();             // 初始化
    void Shutdown();               // 关闭
}
```

### 插件基类

```csharp
public abstract class PluginBase : IPlugin
{
    public abstract string Name { get; }
    public abstract Version Version { get; }
    public abstract string Author { get; }
    public virtual int LoadOrder => 100;
    public PluginContext Context { get; set; } = null!;
    
    public virtual void Initialize() { }
    public virtual void OnInitialize() { }
    public virtual void Shutdown() { }
    public virtual void OnShutdown() { }
    public virtual void Dispose() { }
}
```

### 完整插件示例

```csharp
public class MyPlugin : PluginBase
{
    public override string Name => "MyPlugin";
    public override Version Version => new(1, 0, 0);
    public override string Author => "YourName";
    public override int LoadOrder => 10;
    
    private ILogger<MyPlugin> _logger = null!;
    private VortexContext _vortex = null!;
    
    public override void OnInitialize()
    {
        _logger = Context.GetLogger<MyPlugin>();
        _vortex = Context.VortexContext;
        
        _logger.LogInformation("MyPlugin 已加载");
        
        // 注册命令
        _vortex.CommandManager.Register(typeof(MyPluginCommand));
        
        // 订阅事件
        CommandEvents.OnCommandExecuting += OnCommandExecuting;
    }
    
    public override void OnShutdown()
    {
        CommandEvents.OnCommandExecuting -= OnCommandExecuting;
        _logger.LogInformation("MyPlugin 已卸载");
    }
    
    private async Task<bool> OnCommandExecuting(CommandArgs args, string cmdName)
    {
        // 拦截命令
        if (cmdName == "blocked")
        {
            await args.Context.SendMessage("此命令已被插件拦截");
            return true; // 返回 true 表示已处理，阻止原命令执行
        }
        return false;
    }
}
```

### 插件目录结构

```
Plugins/
└── MyPlugin/
    ├── MyPlugin.dll          # 插件主程序
    ├── Dependency1.dll       # 依赖库
    └── config.json           # 插件配置文件
```

## 数据库模型

### 主要数据表

| 表名 | 说明 |
|------|------|
| `Sign` | 签到记录 |
| `Currency` | 货币记录 |
| `TerrariaUser` | Terraria 用户绑定 |
| `CharacterSelection` | 用户服务器选择 |
| `GroupForwardMessage` | 群组消息转发配置 |
| `MessageRecord` | 消息记录 |

### 使用数据库

```csharp
// 查询
var users = await database.QueryAsync<TerrariaUser>(
    u => u.QQ == userId
);

// 插入
await database.InsertAsync(new Sign
{
    UserId = userId,
    GroupId = groupId,
    SignTime = DateTime.Now
});

// 更新
await database.UpdateAsync(user);

// 删除
await database.DeleteAsync(user);
```

## 图片生成

### 支持生成的图片类型

- **InventoryGenerate** - 玩家背包图片
- **ServerOnlineGenerate** - 在线玩家列表
- **ProgressGenerate** - 游戏进度
- **TableGenerate** - 数据表格
- **MenuGenerate** - 菜单界面
- **ProfileCard** - 个人信息卡片

### 使用示例

```csharp
// 生成背包图片
var generator = new InventoryGenerate(items, playerName);
var image = await generator.GenerateAsync();
await image.SaveAsync("inventory.png");
```

## 项目结构

```
Vortex.Bot/
├── Core/
│   └── Service/
│       ├── CoreLoginService.cs      # QQ 登录服务
│       ├── VortexSocketService.cs   # Socket 服务器
│       ├── PacketHandlerService.cs  # 数据包处理
│       ├── TerrariaServerService.cs # 服务器管理
│       └── ...
├── Command/
│   ├── CommandManager.cs            # 命令管理器
│   ├── CommandBase.cs               # 命令基类
│   ├── CommandArgs.cs               # 命令参数
│   ├── CommandHelper.cs             # 命令辅助
│   ├── BuiltIn/                     # 内置命令
│   ├── Terraria/                    # Terraria 命令
│   ├── Admin/                       # 管理命令
│   └── ...
├── Plugins/
│   ├── PluginManager.cs             # 插件管理器
│   ├── PluginBase.cs                # 插件基类
│   └── IPlugin.cs                   # 插件接口
├── Database/
│   ├── DatabaseService.cs           # 数据库服务
│   └── Models/                      # 数据模型
├── Configuration/
│   └── CoreConfiguration.cs         # 配置类
├── Handlers/
│   └── ...                          # 推送数据包处理器
└── Program.cs                       # 入口
```

## 依赖项

| 包名 | 版本 | 用途 |
|------|------|------|
| Lagrange.Core | - | QQ 协议实现 |
| Microsoft.Extensions.Hosting | 10.0.7 | 主机框架 |
| linq2db | 6.2.1 | ORM |
| Microsoft.Data.Sqlite | 10.0.7 | SQLite 支持 |
| SixLabors.ImageSharp | 3.1.12 | 图片处理 |
| SixLabors.ImageSharp.Drawing | 2.1.7 | 图片绘制 |
| SixLabors.Fonts | 2.1.3 | 字体渲染 |
| Net.Codecrete.QrCodeGenerator | 2.1.0 | 二维码生成 |

## 运行

```bash
# 开发模式
dotnet run

# 发布
dotnet publish -c Release -o ./publish

# 运行发布版本
./publish/Vortex.Bot.exe
```

## 调试

使用 `appsettings.Development.jsonc` 配置开发环境：

```jsonc
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Vortex.Bot": "Debug"
    }
  }
}
```
