# VortexQ

VortexQ 是一个用于 Terraria 游戏服务器的 QQ 机器人管理系统，让玩家可以通过 QQ 群/私聊与游戏服务器进行交互。

## 系统架构

```
┌─────────────┐      TCP Socket      ┌─────────────┐
│ Vortex.Bot  │ ◄──────────────────► │Vortex.Adapter│
│  (QQ 机器人) │   Vortex.Protocol    │ (TShock插件) │
└──────┬──────┘                      └──────┬──────┘
       │                                    │
       ▼                                    ▼
┌─────────────┐                      ┌─────────────┐
│ Lagrange.Core│                      │   TShock    │
│  (QQ 协议)   │                      │ (游戏服务器) │
└─────────────┘                      └─────────────┘
```

- **Vortex.Bot**: QQ 机器人核心，处理 QQ 消息和命令
- **Vortex.Adapter**: TShock 插件，连接游戏服务器
- **Vortex.Protocol**: 通信协议库

## 快速开始

### 环境要求

- .NET 9.0 SDK 或更高版本
- Terraria 服务器（TShock 5.2+）
- QQ 账号

### 部署步骤

#### 1. 部署 Vortex.Bot

```bash
cd Vortex.Bot
dotnet build
dotnet run
```

首次运行会生成默认配置文件 `appsettings.jsonc`，请按以下步骤配置：

**配置 QQ 登录**（二选一）：

方式 A - 扫码登录（推荐）：
```json
{
  "Login": {
    "Uin": 0
  }
}
```

方式 B - 密码登录：
```json
{
  "Login": {
    "Uin": 123456789,
    "Password": "your-password"
  }
}
```

**配置 Socket 服务器**：
```json
{
  "Core": {
    "Socket": {
      "Enabled": true,
      "Port": 6000,
      "Token": "your-secret-token"
    }
  }
}
```

**配置数据库**（默认 SQLite）：
```json
{
  "Core": {
    "Database": {
      "Type": "SQLite",
      "ConnectionString": "Data Source=vortex.db"
    }
  }
}
```

**配置 Terraria 服务器**：
```json
{
  "TerrariaServers": {
    "Servers": [
      {
        "Name": "Server1",
        "Groups": [123456789],
        "DisplayName": "服务器1"
      }
    ]
  }
}
```

#### 2. 部署 Vortex.Adapter

1. 编译项目：
```bash
cd Vortex.Adapter
dotnet build
```

2. 将生成的 `Vortex.Adapter.dll` 复制到 TShock 的 `ServerPlugins` 目录

3. 启动 Terraria 服务器，插件会自动生成配置文件

4. 编辑 `tshock/Vortex.Adapter.json`：
```json
{
  "阻止未注册进入": false,
  "Socket": {
    "服务器地址": "127.0.0.1",
    "服务器名称": "Server1",
    "端口": 6000,
    "验证令牌": "your-secret-token",
    "心跳包间隔": 60000,
    "重连间隔": 5000
  }
}
```

> **注意**：`服务器名称` 必须与 Vortex.Bot 配置中的服务器名称一致

### 验证连接

启动 Vortex.Bot 和 Terraria 服务器后，观察日志确认连接成功：

**Vortex.Bot 日志**：
```
Vortex Socket Server started on port 6000
Client connected from 127.0.0.1:xxxxx
Authentication succeeded
Client registered: Server1 (xxxxxxxx-xxxx...)
```

**Vortex.Adapter 日志**：
```
[Vortex.Adapter] 已连接到服务器 127.0.0.1:6000
[Vortex.Adapter] 认证成功
[Vortex.Adapter] 身份注册成功
```

## 使用指南

### 基础命令

在 QQ 群或私聊中使用以下命令：

| 命令 | 说明 | 示例 |
|------|------|------|
| `/help` | 查看帮助 | `/help` |
| `/serverlist` | 查看服务器列表 | `/serverlist` |
| `/serverswitch <名称>` | 切换服务器 | `/serverswitch Server1` |
| `/online` | 查看在线玩家 | `/online` |
| `/inventory [玩家名]` | 查看背包 | `/inventory` 或 `/inventory 玩家名` |
| `/gameprogress` | 查看游戏进度 | `/gameprogress` |
| `/register <账号> <密码>` | 注册游戏账号 | `/register testuser 123456` |
| `/resetpassword <账号>` | 重置密码 | `/resetpassword testuser` |

### 管理员命令

| 命令 | 说明 | 权限 |
|------|------|------|
| `/execute <命令>` | 执行服务器命令 | 管理员 |
| `/kick <玩家名>` | 踢出玩家 | 管理员 |
| `/serverrestart` | 重启服务器 | 超级管理员 |
| `/serverreset` | 重置服务器 | 超级管理员 |

### 签到系统

| 命令 | 说明 |
|------|------|
| `/sign` | 每日签到 |
| `/signrank` | 签到排行榜 |

## 项目文档

各项目的详细说明请查看对应目录的 README：

- [Vortex.Protocol](Vortex.Protocol/) - 通信协议文档
- [Vortex.Bot](Vortex.Bot/) - QQ 机器人文档
- [Vortex.Adapter](Vortex.Adapter/) - TShock 插件文档

## 常见问题

### Q: Bot 无法连接到 QQ？
A: 检查 `appsettings.jsonc` 中的登录配置，确保 Uin 正确。首次登录可能需要扫码验证。

### Q: Adapter 无法连接到 Bot？
A: 检查以下几点：
1. Vortex.Bot 的 Socket 服务器已启动
2. 两边的 `Token` 配置一致
3. `服务器名称` 配置一致
4. 防火墙允许对应端口通信

### Q: 命令没有响应？
A: 检查命令前缀配置，默认前缀为 `/`。可在配置中修改或禁用前缀。

## 技术栈

- **Vortex.Bot**: .NET 10, Lagrange.Core, SixLabors.ImageSharp
- **Vortex.Adapter**: .NET 9, TShock
- **Vortex.Protocol**: .NET 9

## 作者

- **少司命**

## 致谢

- [Lagrange.Core](https://github.com/LagrangeDev/Lagrange.Core) - QQ 协议实现
- [TShock](https://github.com/Pryaxis/TShock) - Terraria 服务器管理工具
