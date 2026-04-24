using System.Reflection;
using Vortex.Adapter.Net;
using Vortex.Protocol.Enums;
using Vortex.Protocol.Interfaces;
using TShockAPI;

namespace Vortex.Adapter.Processing;

public sealed class PacketHandlerManager
{
    private readonly VortexClient _client;
    private readonly Dictionary<PacketType, HandlerInfo> _handlers = new();
    private readonly struct HandlerInfo
    {
        public readonly Func<VortexClient, IRequestHandler> Factory;

        public HandlerInfo(Func<VortexClient, IRequestHandler> factory)
        {
            Factory = factory;
        }
    }

    public PacketHandlerManager(VortexClient client)
    {
        _client = client;
        RegisterHandlersFromAssembly();
    }

    private void RegisterHandlersFromAssembly()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && IsRequestHandler(t));

        foreach (var handlerType in handlerTypes)
        {
            RegisterHandlerType(handlerType);
        }

        TShock.Log.ConsoleInfo($"[Vortex.Adapter] 已自动注册 {_handlers.Count} 个数据包处理器");
    }

    private static bool IsRequestHandler(Type type)
    {
        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RequestHandlerBase<,>))
            {
                return true;
            }
            type = type.BaseType!;
        }
        return false;
    }

    private void RegisterHandlerType(Type handlerType)
    {
        try
        {
            var packetType = GetPacketTypeFromHandlerType(handlerType);
            if (packetType == default)
            {
                TShock.Log.ConsoleError($"[Vortex.Adapter] 无法获取处理器 {handlerType.Name} 的 PacketType");
                return;
            }
            var factory = CreateFactory(handlerType);

            _handlers[packetType] = new HandlerInfo(factory);
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[Vortex.Adapter] 注册处理器 {handlerType.Name} 失败: {ex.Message}");
        }
    }

    private static PacketType GetPacketTypeFromHandlerType(Type handlerType)
    {
        try
        {
            var packetTypeProperty = handlerType.GetProperty("PacketType");
            if (packetTypeProperty != null)
            {
                var tempClient = new VortexClient(new Setting.Configs.SocketConfig());
                var handler = (IRequestHandler)Activator.CreateInstance(handlerType, tempClient)!;
                var packetType = handler.PacketType;
                TShock.Log.ConsoleInfo($"[Vortex.Adapter] 处理器 {handlerType.Name} 的 PacketType: {packetType}");
                return packetType;
            }
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[Vortex.Adapter] 获取处理器 {handlerType.Name} 的 PacketType 失败: {ex.Message}");
        }
        return default;
    }

    private static Func<VortexClient, IRequestHandler> CreateFactory(Type handlerType)
    {
        var constructor = handlerType.GetConstructor([typeof(VortexClient)])
            ?? throw new InvalidOperationException($"处理器 {handlerType.Name} 缺少 VortexClient 构造函数");

        return client => (IRequestHandler)constructor.Invoke([client]);
    }

    public async Task<bool> HandlePacketAsync(INetPacket packet)
    {
        if (!_handlers.TryGetValue(packet.PacketID, out var handlerInfo))
        {
            TShock.Log.ConsoleWarn($"[Vortex.Adapter] 未找到数据包 {packet.PacketID} 的处理器");
            return false;
        }

        if (packet is not IServicePacket servicePacket)
        {
            TShock.Log.ConsoleError($"[Vortex.Adapter] 数据包 {packet.PacketID} 不是 IServicePacket 类型");
            return false;
        }

        try
        {
            var handler = handlerInfo.Factory(_client);
            var response = handler.Handle(servicePacket);
            if (response != null)
            {
                await _client.SendPacketAsync(response);
            }

            return true;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[Vortex.Adapter] 处理数据包 {packet.PacketID} 时出错: {ex}");
            return false;
        }
    }
    public int HandlerCount => _handlers.Count;
}
