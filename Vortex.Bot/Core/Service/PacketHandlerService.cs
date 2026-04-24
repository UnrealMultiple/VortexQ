using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Interface;
using Vortex.Bot.Models;
using Vortex.Bot.Processing;
using Vortex.Protocol.Interfaces;

namespace Vortex.Bot.Core.Service;

public class PacketHandlerService(
    ILogger<PacketHandlerService> logger,
    IServiceProvider serviceProvider,
    VortexContext vortexContext)
{
    private readonly ILogger<PacketHandlerService> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly VortexContext _vortexContext = vortexContext;


    private readonly ConcurrentDictionary<byte, Func<INetPacket, PacketRouteContext, Task<IClientPacket?>>> _handlers = new();

    public void RegisterHandler<TRequest, TResponse>(IRoutedPacketHandler<TRequest, TResponse> handler)
        where TRequest : IServicePacket, new()
        where TResponse : IClientPacket, new()
    {
        var instance = new TRequest();
        var packetId = (byte)instance.PacketID;

        _handlers[packetId] = async (packet, context) =>
        {
            if (packet is TRequest request)
            {
                var response = await handler.HandleAsync(request, context);
                response.RequestId = request.RequestId;
                return response;
            }
            return null;
        };

        _logger.LogInformation("[HandlerManager] Registered handler for {PacketType}", typeof(TRequest).Name);
    }

    public void RegisterHandler<TRequest, TResponse>(Func<TRequest, PacketRouteContext, Task<TResponse>> handler)
        where TRequest : IServicePacket, new()
        where TResponse : IClientPacket, new()
    {
        var instance = new TRequest();
        var packetId = (byte)instance.PacketID;

        _handlers[packetId] = async (packet, context) =>
        {
            if (packet is TRequest request)
            {
                var response = await handler(request, context);
                response.RequestId = request.RequestId;
                return response;
            }
            return null;
        };

        _logger.LogInformation("[HandlerManager] Registered handler for {PacketType}", typeof(TRequest).Name);
    }

    public void RegisterHandler<TRequest, TResponse>(RoutedRequestHandlerBase<TRequest, TResponse> handler)
        where TRequest : IServicePacket, new()
        where TResponse : IClientPacket, new()
    {
        var instance = new TRequest();
        var packetId = (byte)instance.PacketID;

        handler.Context = _vortexContext;
        handler.Server = _vortexContext.Server;

        _handlers[packetId] = (packet, context) =>
        {
            if (packet is TRequest request)
            {
                var response = handler.Handle(request, context);
                return Task.FromResult<IClientPacket?>(response);
            }
            return Task.FromResult<IClientPacket?>(null);
        };

        _logger.LogInformation("[HandlerManager] Registered handler for {PacketType}", typeof(TRequest).Name);
    }

    public void RegisterHandler<TRequest>(RoutedPushHandlerBase<TRequest> handler)
        where TRequest : INetPacket, new()
    {
        var instance = new TRequest();
        var packetId = (byte)instance.PacketID;

        handler.Context = _vortexContext;
        handler.Server = _vortexContext.Server;

        _handlers[packetId] = (packet, context) =>
        {
            if (packet is TRequest request)
            {
                handler.Handle(request, context);
            }
            return Task.FromResult<IClientPacket?>(null);
        };

        _logger.LogInformation("[HandlerManager] Registered push handler for {PacketType}", typeof(TRequest).Name);
    }

    public void RegisterHandlersFromAssembly(System.Reflection.Assembly assembly)
    {
        var routedHandlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IRoutedPacketHandler<,>)));

        foreach (var handlerType in routedHandlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRoutedPacketHandler<,>));

            var genericArgs = interfaceType.GetGenericArguments();
            var requestType = genericArgs[0];
            var responseType = genericArgs[1];

            var handlerInstance = Activator.CreateInstance(handlerType)!;

            var method = GetType().GetMethods()
                .Where(m => m.Name == nameof(RegisterHandler) && m.IsGenericMethod)
                .First(m => m.GetParameters().Length == 1 &&
                           m.GetParameters()[0].ParameterType.IsInterface);

            method?.MakeGenericMethod(requestType, responseType).Invoke(this, new[] { handlerInstance });
        }

        var baseHandlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.BaseType?.IsGenericType == true &&
                       t.BaseType.GetGenericTypeDefinition() == typeof(RoutedRequestHandlerBase<,>));

        foreach (var handlerType in baseHandlerTypes)
        {
            var baseType = handlerType.BaseType!;
            var genericArgs = baseType.GetGenericArguments();
            var requestType = genericArgs[0];
            var responseType = genericArgs[1];

            var handlerInstance = _serviceProvider.GetService(handlerType)
                ?? Activator.CreateInstance(handlerType)!;

            var method = GetType().GetMethods()
                .Where(m => m.Name == nameof(RegisterHandler) && m.IsGenericMethod)
                .First(m => m.GetParameters().Length == 1 &&
                           !m.GetParameters()[0].ParameterType.IsInterface &&
                           m.GetParameters()[0].ParameterType.IsClass &&
                           m.GetGenericArguments().Length == 2);

            method?.MakeGenericMethod(requestType, responseType).Invoke(this, [handlerInstance]);
        }

        var pushHandlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.BaseType?.IsGenericType == true &&
                       t.BaseType.GetGenericTypeDefinition() == typeof(RoutedPushHandlerBase<>));

        foreach (var handlerType in pushHandlerTypes)
        {
            var baseType = handlerType.BaseType!;
            var genericArgs = baseType.GetGenericArguments();
            var requestType = genericArgs[0];

            var handlerInstance = _serviceProvider.GetService(handlerType)
                ?? Activator.CreateInstance(handlerType)!;

            var method = GetType().GetMethods()
                .Where(m => m.Name == nameof(RegisterHandler) && m.IsGenericMethod)
                .First(m => m.GetParameters().Length == 1 &&
                           !m.GetParameters()[0].ParameterType.IsInterface &&
                           m.GetParameters()[0].ParameterType.IsClass &&
                           m.GetGenericArguments().Length == 1 &&
                           m.GetParameters()[0].ParameterType.Name == typeof(RoutedPushHandlerBase<>).Name);

            method?.MakeGenericMethod(requestType).Invoke(this, [handlerInstance]);
        }
    }

    public async Task<IClientPacket?> ProcessAsync(INetPacket packet, PacketRouteContext context)
    {
        var packetId = (byte)packet.PacketID;

        if (_handlers.TryGetValue(packetId, out var handler))
        {
            try
            {
                return await handler(packet, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HandlerManager] Error processing packet {PacketID}", packet.PacketID);
                return null;
            }
        }
        return null;
    }


    public bool HasHandler(byte packetId) => _handlers.ContainsKey(packetId);

    public int HandlerCount => _handlers.Count;
}
