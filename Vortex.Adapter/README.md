# Vortex.Adapter

Vortex.Adapter 是 VortexQ 系统的 TShock 适配插件，作为 Terraria 服务器与 Vortex.Bot 之间的桥梁，实现游戏事件转发和远程管理功能。

## 项目信息

- **目标框架**: .NET 9.0
- **项目类型**: TShock 插件
- **TShock 版本**: 6.1.0+
- **主要功能**: Socket 客户端、事件转发、命令执行、数据统计

## 核心功能

- **Socket 客户端**: 连接到 Vortex.Bot 的 Socket 服务器
- **事件转发**: 将游戏内事件（玩家加入/离开/消息等）实时转发给 Bot
- **命令执行**: 接收并执行 Bot 发送的服务器命令
- **数据统计**: 收集在线时长、死亡次数、BOSS 伤害等数据
- **进服限制**: 支持阻止未注册玩家进入服务器
- **自动重连**: 断线后自动重连，保证服务稳定性
- **心跳检测**: 定期发送心跳包保持连接

## 安装

### 方式一：使用预编译版本

1. 下载最新版本的 `Vortex.Adapter.dll`
2. 将 DLL 文件复制到 TShock 的 `ServerPlugins` 目录
3. 启动服务器，插件会自动生成配置文件

### 方式二：自行编译

```bash
# 克隆仓库
git clone <repository-url>
cd VortexQ/Vortex.Adapter

# 编译
dotnet build -c Release

# 复制到插件目录
copy bin/Release/net9.0/Vortex.Adapter.dll <TShock-Path>/ServerPlugins/
```

## 配置说明

配置文件位于 `tshock/Vortex.Adapter.json`，首次启动会自动生成。

### 完整配置示例

```json
{
  "阻止未注册进入": false,
  "阻止语句": [
    "未注册禁止进入服务器！",
    "请先联系管理员注册账号"
  ],
  "Socket": {
    "服务器地址": "127.0.0.1",
    "服务器名称": "Server1",
    "端口": 6000,
    "验证令牌": "your-secret-token",
    "心跳包间隔": 60000,
    "重连间隔": 5000,
    "空指令注册": [
      "购买",
      "抽",
      "签到"
    ]
  },
  "重置设置": {
    "删除地图": true,
    "删除日志": true,
    "执行命令": [
      "/skill reset",
      "/deal reset",
      "/礼包 重置",
      "/level reset",
      "/clearallplayersplus"
    ],
    "删除表": [
      "boss数据统计",
      "economics",
      "economicsskill",
      "learnt",
      "OnlineDuration",
      "BotOnlineDuration",
      "BotDeath",
      "onlybaniplist",
      "permabuff",
      "permabuffs",
      "regions",
      "user",
      "Death",
      "rememberedpos",
      "research",
      "stronger",
      "synctable",
      "tscharacter",
      "users",
      "warps",
      "weapons",
      "使用日志"
    ]
  }
}
```

### 配置项说明

#### 进服限制

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `阻止未注册进入` | bool | 是否阻止未注册 TShock 账号的玩家进入 |
| `阻止语句` | string[] | 阻止进入时显示的消息 |

#### Socket 配置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `服务器地址` | string | 127.0.0.1 | Vortex.Bot 的 IP 地址 |
| `服务器名称` | string | TerrariaServer | 服务器唯一标识，必须与 Bot 配置一致 |
| `端口` | int | 6000 | Vortex.Bot 的 Socket 端口 |
| `验证令牌` | string | - | 认证令牌，必须与 Bot 配置一致 |
| `心跳包间隔` | int | 60000 | 心跳包发送间隔（毫秒） |
| `重连间隔` | int | 5000 | 断线后重连间隔（毫秒） |
| `空指令注册` | string[] | - | 注册为无操作命令，用于拦截特定指令 |

#### 重置设置

用于 `serverreset` 命令执行服务器重置操作：

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `删除地图` | bool | 是否删除世界文件 |
| `删除日志` | bool | 是否删除日志文件 |
| `执行命令` | string[] | 重置前执行的 TShock 命令 |
| `删除表` | string[] | 重置时要清空的数据库表 |

## 数据包处理

### 支持的数据包

| 数据包 | 类型 | 说明 |
|--------|------|------|
| `ExecuteCommandPacket` | Request | 执行服务器命令 |
| `ServerStatusPacket` | Request | 获取服务器状态 |
| `ServerRestartPacket` | Request | 重启服务器 |
| `ServerResetPacket` | Request | 重置服务器 |
| `AccountRegistrationPacket` | Request | 注册 TShock 账号 |
| `AccountQueryPacket` | Request | 查询账号信息 |
| `PasswordResetPacket` | Request | 重置密码 |
| `PlayerInventoryPacket` | Request | 查询玩家背包 |
| `PlayerExportPacket` | Request | 导出玩家数据 |
| `BroadcastMessagePacket` | Request | 广播消息 |
| `PrivateMessagePacket` | Request | 私聊玩家 |
| `GameProgressPacket` | Request | 获取游戏进度 |
| `DeathRankPacket` | Request | 获取死亡排行 |
| `OnlineRankPacket` | Request | 获取在线排行 |
| `ServerOnlinePacket` | Request | 获取在线玩家 |
| `BossDamagePacket` | Request | 获取 BOSS 伤害统计 |
| `WorldMapPacket` | Request | 生成世界地图 |
| `MapImagePacket` | Request | 获取地图图片 |
| `WorldFilePacket` | Request | 上传/下载世界文件 |
| `ConnectionStatusPacket` | Request | 查询连接状态 |

### 推送数据包

| 数据包 | 说明 |
|--------|------|
| `PlayerJoinPacket` | 玩家加入游戏时推送 |
| `PlayerLeavePacket` | 玩家离开游戏时推送 |
| `PlayerMessagePacket` | 玩家发送消息时推送 |
| `HeartBeatPacket` | 定期心跳包 |

## 事件监听

插件监听以下游戏事件并转发给 Bot：

| 事件 | 说明 | 触发时机 |
|------|------|----------|
| `NetGreetPlayer` | 玩家加入 | 玩家成功进入游戏 |
| `ServerLeave` | 玩家离开 | 玩家断开连接 |
| `ServerChat` | 玩家聊天 | 玩家发送聊天消息 |
| `NpcSpawn` | NPC 生成 | BOSS 生成时 |
| `NpcStrike` | NPC 受击 | BOSS 受到伤害时 |
| `NpcKilled` | NPC 死亡 | BOSS 被击杀时 |
| `KillMe` | 玩家死亡 | 玩家死亡时 |
| `GamePostInitialize` | 游戏初始化 | 服务器启动完成 |

## 数据统计

### 在线时长统计

- 每分钟更新一次在线玩家时长
- 数据存储在内存中，可通过 `OnlineRankPacket` 查询

### 死亡统计

- 记录玩家死亡次数
- 数据存储在内存中，可通过 `DeathRankPacket` 查询

### BOSS 伤害统计

- 记录 BOSS 战中每个玩家的伤害输出
- 从 BOSS 生成开始统计，到 BOSS 死亡结束
- 包含伤害值、BOSS 名称、最大生命值等信息
- 可通过 `BossDamagePacket` 查询

## 开发指南

### 添加新的数据包处理器

1. 在 `Processing` 目录创建新的处理器类：

```csharp
public class MyPacketHandler : RequestHandlerBase<MyPacket, MyPacketResponse>
{
    public MyPacketHandler(VortexClient client) : base(client) { }

    public override MyPacketResponse Handle(MyPacket request)
    {
        // 处理逻辑
        return CreateResponse(request, success: true, message: "处理成功");
    }
}
```

2. 确保数据包类型已在 `Vortex.Protocol` 中定义

3. 重新编译插件

### 自定义事件处理

```csharp
// 在 Plugin.cs 中注册新的事件处理
ServerApi.Hooks.MyEvent.Register(this, OnMyEvent);

private void OnMyEvent(MyEventArgs args)
{
    // 发送自定义数据包到 Bot
    Client?.SendPacketAsync(new MyCustomPacket
    {
        Data = args.Data
    });
}
```

## 项目结构

```
Vortex.Adapter/
├── Attributes/
│   ├── CommandMatch.cs         # 命令匹配特性
│   └── ProgressMatch.cs        # 进度匹配特性
├── Converter/
│   └── MessageTypeConverter.cs # 消息类型转换
├── DB/
│   ├── PlayerDeath.cs          # 死亡统计
│   └── PlayerOnline.cs         # 在线统计
├── Enumerates/
│   ├── ImageType.cs            # 图片类型
│   └── ProgressType.cs         # 进度类型
├── Extension/
│   └── MethodExt.cs            # 扩展方法
├── Net/
│   └── VortexClient.cs         # Socket 客户端
├── Processing/
│   ├── PacketHandlerManager.cs # 处理器管理
│   ├── RequestHandlerBase.cs   # 处理器基类
│   └── ...                     # 各类处理器
├── Setting/
│   ├── Config.cs               # 配置管理
│   └── Configs/
│       ├── ResetConfig.cs      # 重置配置
│       └── SocketConfig.cs     # Socket 配置
├── i18n/                       # 国际化文件
├── Plugin.cs                   # 插件主类
├── PacketHandler.cs            # 数据包处理入口
├── Utils.cs                    # 工具类
└── manifest.json               # 插件清单
```

## 核心类说明

### Plugin

插件主类，继承自 `TerrariaPlugin`，负责：
- 插件初始化和清理
- 事件注册和注销
- 定时器管理
- 客户端生命周期管理

### VortexClient

Socket 客户端类，负责：
- TCP 连接管理
- 自动重连
- 数据包序列化/反序列化
- 请求-响应模式支持

### PacketHandlerManager

数据包处理器管理器，负责：
- 自动注册处理器
- 数据包路由
- 处理器实例创建

### RequestHandlerBase<TRequest, TResponse>

处理器基类，提供：
- 统一的处理接口
- 响应创建辅助方法
- 客户端访问

## 日志输出

插件使用 TShock 的日志系统输出信息：

```
[Vortex.Adapter] 已连接到服务器 127.0.0.1:6000
[Vortex.Adapter] 认证成功
[Vortex.Adapter] 身份注册成功，会话ID: 1
[Vortex.Adapter] 已自动注册 20 个数据包处理器
```

## 故障排除

### 无法连接到 Bot

1. 检查 Vortex.Bot 是否已启动
2. 检查 `服务器地址` 和 `端口` 配置是否正确
3. 检查 `验证令牌` 是否与 Bot 配置一致
4. 检查防火墙是否允许连接

### 服务器名称不匹配

确保 `服务器名称` 配置与 Vortex.Bot 中配置的 `Name` 完全一致（区分大小写）。

### 频繁断线重连

1. 检查网络稳定性
2. 调整 `心跳包间隔` 为更小的值
3. 检查 Vortex.Bot 是否正常运行

## 依赖项

| 包名 | 版本 | 用途 |
|------|------|------|
| TShock | 6.1.0 | Terraria 服务器框架 |
| SixLabors.ImageSharp | 3.1.12 | 图片处理 |
| Vortex.Protocol | - | 通信协议 |

## 版本历史

### v1.0.0
- 初始版本
- 实现基础通信功能
- 支持玩家事件转发
- 支持服务器管理命令

## 作者

- **少司命**

## 反馈

- 优先提交 Issue 到项目仓库
- TShock 官方群：816771079
