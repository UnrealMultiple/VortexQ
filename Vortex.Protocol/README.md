# Vortex.Protocol

Vortex.Protocol 是 VortexQ 系统的通信协议库，定义了 Bot 与 Adapter 之间的数据包格式和通信规范。

## 项目信息

- **目标框架**: .NET 9.0
- **项目类型**: 类库
- **主要用途**: 为 Vortex.Bot 和 Vortex.Adapter 提供通信协议支持

## 核心组件

### 接口定义

| 接口 | 说明 | 属性 |
|------|------|------|
| `INetPacket` | 基础网络数据包接口 | `PacketType PacketID` |
| `IServicePacket` | 服务请求数据包 | `Guid RequestId` |
| `IClientPacket` | 客户端响应数据包 | `Guid RequestId`, `bool Success`, `string Message` |
| `IServerPushPacket` | 服务器推送数据包（单向） | 无 |

### 数据包类型

#### 认证相关

| 数据包 | 类型 | 说明 |
|--------|------|------|
| `ClientAuthPacket` | Request | 客户端认证请求，包含 Token |
| `ClientAuthResponsePacket` | Response | 认证响应 |
| `ClientIdentityPacket` | Request | 客户端身份注册，包含 ClientId 和 ClientName |
| `ClientIdentityResponsePacket` | Response | 身份注册响应，包含 SessionId |

#### 服务器管理

| 数据包 | 类型 | 说明 |
|--------|------|------|
| `ServerStatusPacket` | Request | 获取服务器状态 |
| `ServerStatusPacketResponse` | Response | 服务器状态响应 |
| `ServerRestartPacket` | Request | 重启服务器，包含启动参数 |
| `ServerRestartPacketResponse` | Response | 重启响应 |
| `ServerResetPacket` | Request | 重置服务器，包含重置命令 |
| `ServerResetPacketResponse` | Response | 重置响应 |
| `ExecuteCommandPacket` | Request | 执行服务器命令 |
| `ExecuteCommandPacketResponse` | Response | 命令执行结果 |

#### 玩家管理

| 数据包 | 类型 | 说明 |
|--------|------|------|
| `PlayerJoinPacket` | Push | 玩家加入游戏（服务器推送） |
| `PlayerLeavePacket` | Push | 玩家离开游戏（服务器推送） |
| `PlayerMessagePacket` | Push | 玩家发送消息（服务器推送） |
| `PlayerInventoryPacket` | Request | 查询玩家背包 |
| `PlayerInventoryPacketResponse` | Response | 背包数据响应 |
| `PlayerExportPacket` | Request | 导出玩家数据 |
| `PlayerExportPacketResponse` | Response | 导出结果 |

#### 账号管理

| 数据包 | 类型 | 说明 |
|--------|------|------|
| `AccountRegistrationPacket` | Request | 注册游戏账号 |
| `AccountRegistrationPacketResponse` | Response | 注册结果 |
| `AccountQueryPacket` | Request | 查询账号信息 |
| `AccountQueryPacketResponse` | Response | 账号信息 |
| `PasswordResetPacket` | Request | 重置玩家密码 |
| `PasswordResetPacketResponse` | Response | 重置结果 |

#### 数据统计

| 数据包 | 类型 | 说明 |
|--------|------|------|
| `GameProgressPacket` | Request | 获取游戏进度 |
| `GameProgressPacketResponse` | Response | 游戏进度数据 |
| `DeathRankPacket` | Request | 获取死亡排行 |
| `DeathRankPacketResponse` | Response | 死亡排行数据 |
| `OnlineRankPacket` | Request | 获取在线时长排行 |
| `OnlineRankPacketResponse` | Response | 在线排行数据 |
| `ServerOnlinePacket` | Request | 获取在线玩家列表 |
| `ServerOnlinePacketResponse` | Response | 在线玩家数据 |
| `BossDamagePacket` | Request | 获取 BOSS 伤害统计 |
| `BossDamagePacketResponse` | Response | BOSS 伤害数据 |

#### 消息通信

| 数据包 | 类型 | 说明 |
|--------|------|------|
| `BroadcastMessagePacket` | Request | 向服务器广播消息 |
| `BroadcastMessagePacketResponse` | Response | 广播结果 |
| `PrivateMessagePacket` | Request | 向玩家发送私聊 |
| `PrivateMessagePacketResponse` | Response | 发送结果 |

#### 地图相关

| 数据包 | 类型 | 说明 |
|--------|------|------|
| `WorldMapPacket` | Request | 生成世界地图 |
| `WorldMapPacketResponse` | Response | 地图数据 |
| `MapImagePacket` | Request | 获取地图图片 |
| `MapImagePacketResponse` | Response | 图片数据 |
| `WorldFilePacket` | Request | 上传/下载世界文件 |
| `WorldFilePacketResponse` | Response | 文件传输结果 |

#### 其他

| 数据包 | 类型 | 说明 |
|--------|------|------|
| `HeartBeatPacket` | Push | 心跳包，保持连接 |
| `ConnectionStatusPacket` | Request | 查询连接状态 |
| `ConnectionStatusPacketResponse` | Response | 连接状态 |

### 序列化系统

#### PacketSerializer

核心序列化器，负责数据包的序列化和反序列化：

```csharp
var serializer = new PacketSerializer();

// 序列化
byte[] data = serializer.Serialize(packet);

// 反序列化
using var ms = new MemoryStream(data);
using var br = new BinaryReader(ms);
var packet = serializer.Deserialize(br);
```

#### 支持的字段类型

- 基础类型：`int`, `long`, `short`, `byte`, `bool`, `float`, `double`
- 字符串：`string`
- 特殊类型：`Guid`, `DateTime`, `TimeSpan`, `byte[]`
- 集合类型：数组、`List<T>`, `Dictionary<K,V>`, `IEnumerable<T>`
- 可空类型：`Nullable<T>`
- 自定义类：任何实现了属性的类

#### 序列化特性

```csharp
public class MyPacket : INetPacket
{
    public PacketType PacketID => PacketType.MyPacket;
    
    // 自动序列化
    public string Name { get; set; }
    
    // 忽略此字段
    [Ignore]
    public string TempData { get; set; }
    
    // 使用自定义序列化器
    [Serializer(typeof(MyCustomSerializer))]
    public MyComplexType Data { get; set; }
}

// 类级别默认序列化器
[DefaultSerializer(typeof(MyClassSerializer))]
public class MyComplexType { }
```

### 处理器系统

#### PacketProcessor

用于处理请求-响应模式的数据包：

```csharp
var processor = new PacketProcessor();

// 注册处理器
processor.Register<MyRequestPacket, MyResponsePacket>(request =>
{
    return new MyResponsePacket
    {
        Success = true,
        Message = "处理成功"
    };
});

// 处理数据包
var response = processor.Process(packet);

// 异步请求
var response = await processor.RequestAsync<MyRequestPacket, MyResponsePacket>(
    request,
    async pkt => await SendAsync(pkt),
    timeoutMs: 5000
);
```

#### RequestHandlerBase

处理器基类，支持依赖注入：

```csharp
public class MyRequestHandler : RequestHandlerBase<MyRequestPacket, MyResponsePacket>
{
    private readonly ILogger<MyRequestHandler> _logger;
    
    public MyRequestHandler(ILogger<MyRequestHandler> logger)
    {
        _logger = logger;
    }
    
    public override MyResponsePacket Handle(MyRequestPacket request)
    {
        _logger.LogInformation("处理请求: {RequestId}", request.RequestId);
        
        return CreateResponse(request, success: true, message: "成功");
    }
}
```

### 模型定义

#### Player

```csharp
public class Player
{
    public int Index { get; set; }      // 玩家索引
    public string Name { get; set; }    // 玩家名称
    public string Group { get; set; }   // 权限组
    public string Prefix { get; set; }  // 前缀
    public bool IsLogin { get; set; }   // 是否登录
}
```

#### Item

```csharp
public class Item
{
    public int Id { get; set; }         // 物品ID
    public int Stack { get; set; }      // 数量
    public byte Prefix { get; set; }    // 前缀
}
```

#### Account

```csharp
public class Account
{
    public string Name { get; set; }           // 账号名
    public string Group { get; set; }          // 权限组
    public DateTime RegisterTime { get; set; } // 注册时间
}
```

#### KillNpc (BOSS伤害统计)

```csharp
public class KillNpc
{
    public int Id { get; set; }             // NPC ID
    public string Name { get; set; }        // NPC 名称
    public int MaxLife { get; set; }        // 最大生命值
    public bool IsAlive { get; set; }       // 是否存活
    public DateTime KillTime { get; set; }  // 击杀时间
    public List<PlayerStrike> Strikes { get; set; } // 伤害记录
}
```

## 数据包格式

### 二进制格式

```
+--------+--------+--------+------------------+
| Length | Packet | Packet |     Payload      |
| 2 bytes| Type   |  ID    |    (Variable)    |
|        | 1 byte | 1 byte |                  |
+--------+--------+--------+------------------+
```

- **Length**: 整个数据包的长度（包括 Length 字段本身）
- **Packet Type**: 保留字段
- **Packet ID**: 数据包类型标识（PacketType 枚举值）
- **Payload**: 序列化后的数据包内容

## 使用示例

### 创建自定义数据包

```csharp
// 1. 在 PacketType 枚举中添加新类型
public enum PacketType : byte
{
    // ... 现有类型
    MyCustomPacket,
    MyCustomPacketResponse
}

// 2. 定义请求数据包
public class MyCustomPacket : IServicePacket
{
    public PacketType PacketID => PacketType.MyCustomPacket;
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public string Data { get; set; }
}

// 3. 定义响应数据包
public class MyCustomPacketResponse : IClientPacket
{
    public PacketType PacketID => PacketType.MyCustomPacketResponse;
    public Guid RequestId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public string Result { get; set; }
}
```

### 序列化自定义类型

```csharp
// 实现 IFieldSerializer 接口
public class MyTypeSerializer : IFieldSerializer
{
    public void Write(BinaryWriter writer, object value)
    {
        var myType = (MyType)value;
        writer.Write(myType.Property1);
        writer.Write(myType.Property2);
    }
    
    public object Read(BinaryReader reader)
    {
        return new MyType
        {
            Property1 = reader.ReadString(),
            Property2 = reader.ReadInt32()
        };
    }
}
```

## 项目结构

```
Vortex.Protocol/
├── Enums/
│   └── PacketType.cs           # 数据包类型枚举
├── Interfaces/
│   ├── INetPacket.cs           # 基础数据包接口
│   ├── IClientPacket.cs        # 客户端响应接口
│   ├── IServerPushPacket.cs    # 服务器推送接口
│   └── IServicePacket.cs       # 服务请求接口
├── Models/
│   ├── Player.cs               # 玩家模型
│   ├── Item.cs                 # 物品模型
│   ├── Account.cs              # 账号模型
│   ├── KillNpc.cs              # BOSS伤害模型
│   └── ...                     # 其他模型
├── Packets/
│   ├── ClientAuthPacket.cs     # 认证数据包
│   ├── PlayerJoinPacket.cs     # 玩家加入
│   ├── PlayerMessagePacket.cs  # 玩家消息
│   └── ...                     # 其他数据包
├── Serialization/
│   ├── PacketSerializer.cs     # 核心序列化器
│   ├── IFieldSerializer.cs     # 字段序列化接口
│   ├── SerializerAttributes.cs # 序列化特性
│   └── Serializers/            # 各类序列化器实现
└── Processing/
    ├── PacketProcessor.cs      # 数据包处理器
    └── RequestHandlerBase.cs   # 处理器基类
```

## 依赖关系

本项目无外部依赖，仅使用 .NET 基础类库。

## 版本历史

- **v1.0.0** - 初始版本，定义完整通信协议
