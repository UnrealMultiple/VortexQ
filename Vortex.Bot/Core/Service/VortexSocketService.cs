using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using Vortex.Bot.Configuration;
using Vortex.Bot.Models;
using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Packets;
using Vortex.Protocol.Serialization;

namespace Vortex.Bot.Core.Service;

public sealed class VortexSocketService(
    ILogger<VortexSocketService> logger,
    IConfiguration configuration,
    IServiceProvider serviceProvider,
    ClientConnectionService connectionManager,
    PacketHandlerService handlerManager) : BackgroundService
{
    private readonly ILogger<VortexSocketService> _logger = logger;
    private readonly SocketConfiguration _config = configuration.GetSection("Core:Socket").Get<SocketConfiguration>() ?? new();
    private readonly PacketSerializer _serializer = new();
    private readonly ClientConnectionService _connectionManager = connectionManager;
    private readonly PacketHandlerService _handlerManager = handlerManager;

    private TcpListener? _listener;

    public ClientConnectionService Connections => _connectionManager;
    public PacketHandlerService Handlers => _handlerManager;
    public bool IsRunning { get; private set; }
    public IServiceProvider Services => serviceProvider;

    public event Action<ClientConnection>? OnClientConnected
    {
        add => _connectionManager.OnClientConnected += value;
        remove => _connectionManager.OnClientConnected -= value;
    }

    public event Action<ClientConnection>? OnClientDisconnected
    {
        add => _connectionManager.OnClientDisconnected += value;
        remove => _connectionManager.OnClientDisconnected -= value;
    }

    public event Action<ClientConnection, INetPacket>? OnPacketReceived;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogServerDisabled();
            return;
        }

        _handlerManager.RegisterHandlersFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
        _listener = new TcpListener(IPAddress.Any, _config.Port);
        _listener.Start();
        IsRunning = true;

        _logger.LogServerStarted(_config.Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(client, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogServerStopping();
        }
        finally
        {
            IsRunning = false;
            _listener?.Stop();
            await _connectionManager.DisconnectAllAsync();
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var endpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        _logger.LogClientConnected(endpoint);

        using var stream = tcpClient.GetStream();
        ClientConnection? client = null;

        try
        {
            var authPacket = await ReadPacketAsync(stream, cancellationToken);
            if (authPacket is not ClientAuthPacket auth)
            {
                _logger.LogAuthPacketMissing(endpoint);
                await SendResponseAsync(stream, new ClientAuthResponsePacket { Success = false, Message = "Auth required first" }, cancellationToken);
                return;
            }

            if (auth.Token != _config.Token)
            {
                _logger.LogAuthFailed(endpoint);
                await SendResponseAsync(stream, new ClientAuthResponsePacket { Success = false, Message = "Invalid token" }, cancellationToken);
                return;
            }
            await SendResponseAsync(stream, new ClientAuthResponsePacket { Success = true, Message = "Auth success" }, cancellationToken);
            _logger.LogAuthSuccess(endpoint);

            var identityPacket = await ReadPacketAsync(stream, cancellationToken);
            if (identityPacket is not ClientIdentityPacket identity)
            {
                _logger.LogIdentityPacketMissing(endpoint);
                return;
            }

            client = _connectionManager.RegisterClient(identity, tcpClient, endpoint);
            await SendResponseAsync(stream, CreateIdentityResponse(client), cancellationToken);
            _logger.LogClientRegistered(client.ClientName, client.ClientId);

            RegisterToServerManager(client);

            await ProcessClientPacketsAsync(stream, client, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogClientError(client?.ClientId, ex.Message, ex);
        }
        finally
        {
            if (client != null)
            {
                UnregisterFromServerManager(client.ClientId);
                await _connectionManager.RemoveClientAsync(client.ClientId);
            }
            tcpClient.Close();
        }
    }

    private async Task ProcessClientPacketsAsync(NetworkStream stream, ClientConnection client, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var packet = await ReadPacketAsync(stream, cancellationToken);
            if (packet == null) break;

            var context = CreateRouteContext(client);
            _logger.LogPacketReceived(packet.PacketID, client.ClientName);
            OnPacketReceived?.Invoke(client, packet);

            var response = await _handlerManager.ProcessAsync(packet, context);

            if (response != null)
            {
                await SendResponseAsync(stream, response, cancellationToken);
                _logger.LogPacketSent(response.PacketID, client.ClientName);
            }
        }
    }

    private PacketRouteContext CreateRouteContext(ClientConnection client)
    {
        var serverService = Services.GetService<TerrariaServerService>();
        return new PacketRouteContext
        {
            SenderClientId = client.ClientId,
            SenderSessionId = client.SessionId,
            SenderConnection = client,
            ClientName = client.ClientName,
            Server = serverService?.GetServer(client.ClientName)
        };
    }

    private void RegisterToServerManager(ClientConnection client)
    {
        try
        {
            var serverManager = Services.GetService<TerrariaServerService>();
            serverManager?.RegisterClientConnection(client.ClientName, client.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogRegisterClientError(ex);
        }
    }

    private void UnregisterFromServerManager(Guid clientId)
    {
        try
        {
            var serverManager = Services.GetService<TerrariaServerService>();
            serverManager?.UnregisterClientConnection(clientId);
        }
        catch (Exception ex)
        {
            _logger.LogUnregisterClientError(ex.Message);
        }
    }

    private async Task<INetPacket?> ReadPacketAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var lengthBytes = new byte[2];
        var read = await stream.ReadAsync(lengthBytes.AsMemory(0, 2), cancellationToken);
        if (read < 2) return null;

        var length = BitConverter.ToInt16(lengthBytes);
        var data = new byte[length];
        data[0] = lengthBytes[0];
        data[1] = lengthBytes[1];

        var totalRead = 2;
        while (totalRead < length)
        {
            read = await stream.ReadAsync(data.AsMemory(totalRead, length - totalRead), cancellationToken);
            if (read == 0) break;
            totalRead += read;
        }

        if (totalRead < length)
        {
            _logger.LogIncompleteDataRead(totalRead, length);
            return null;
        }

        _logger.LogDataReceived(BitConverter.ToString(data));

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);
        try
        {
            return _serializer.Deserialize(br);
        }
        catch (Exception ex)
        {
            _logger.LogDeserializationError(ex);
            throw;
        }
    }

    private static ClientIdentityResponsePacket CreateIdentityResponse(ClientConnection client)
    {
        return new ClientIdentityResponsePacket
        {
            RequestId = Guid.NewGuid(),
            Success = true,
            Message = "Registered successfully",
            ClientId = client.ClientId,
            SessionId = client.SessionId
        };
    }

    private async Task SendResponseAsync(NetworkStream stream, INetPacket packet, CancellationToken cancellationToken)
    {
        var buffer = _serializer.Serialize(packet);
        await stream.WriteAsync(buffer, cancellationToken);
    }

    public async Task<bool> SendToClientAsync(Guid clientId, INetPacket packet)
    {
        var client = _connectionManager.GetClient(clientId);
        if (client == null)
        {
            _logger.LogClientNotFound(clientId);
            return false;
        }

        try
        {
            var buffer = _serializer.Serialize(packet);
            await client.Stream.WriteAsync(buffer);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogSendToClientError(clientId, ex);
            return false;
        }
    }

    public Task<bool> SendToSessionAsync(int sessionId, INetPacket packet)
    {
        var client = _connectionManager.GetClientBySession(sessionId);
        return client != null ? SendToClientAsync(client.ClientId, packet) : Task.FromResult(false);
    }

    public async Task<int> BroadcastAsync(INetPacket packet)
    {
        var clients = _connectionManager.GetAllClients();
        var tasks = clients.Select(c => SendToClientAsync(c.ClientId, packet)).ToArray();
        var results = await Task.WhenAll(tasks);
        return results.Count(r => r);
    }

    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(Guid clientId, TRequest request, int timeoutMs = 1000)
        where TRequest : IServicePacket
        where TResponse : class, IClientPacket
    {
        if (!_connectionManager.IsOnline(clientId))
        {
            _logger.LogClientOffline(clientId);
            return null;
        }

        var tcs = new TaskCompletionSource<IClientPacket>();
        void handler(ClientConnection conn, INetPacket packet)
        {
            if (conn.ClientId == clientId && packet is TResponse response && response.RequestId == request.RequestId)
            {
                tcs.TrySetResult(response);
                OnPacketReceived -= handler;
            }
        }

        OnPacketReceived += handler;

        try
        {
            if (!await SendToClientAsync(clientId, request))
            {
                OnPacketReceived -= handler;
                return null;
            }

            using var cts = new CancellationTokenSource(timeoutMs);
            await using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                var result = await tcs.Task;
                return result as TResponse;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogRequestTimeout(clientId);
            OnPacketReceived -= handler;
            return null;
        }
    }
}

public static partial class VortexSocketServiceLoggerExtension
{
    [LoggerMessage(LogLevel.Information, "Vortex Socket Server started on port {port}")]
    public static partial void LogServerStarted(this ILogger<VortexSocketService> logger, int port);

    [LoggerMessage(LogLevel.Information, "Vortex Socket Server is disabled in configuration")]
    public static partial void LogServerDisabled(this ILogger<VortexSocketService> logger);

    [LoggerMessage(LogLevel.Information, "Vortex Socket Server is stopping...")]
    public static partial void LogServerStopping(this ILogger<VortexSocketService> logger);

    [LoggerMessage(LogLevel.Information, "Client connected from {endpoint}")]
    public static partial void LogClientConnected(this ILogger<VortexSocketService> logger, string endpoint);

    [LoggerMessage(LogLevel.Warning, "Auth packet missing from {endpoint}")]
    public static partial void LogAuthPacketMissing(this ILogger<VortexSocketService> logger, string endpoint);

    [LoggerMessage(LogLevel.Warning, "Authentication failed for {endpoint}")]
    public static partial void LogAuthFailed(this ILogger<VortexSocketService> logger, string endpoint);

    [LoggerMessage(LogLevel.Information, "Authentication succeeded for {endpoint}")]
    public static partial void LogAuthSuccess(this ILogger<VortexSocketService> logger, string endpoint);

    [LoggerMessage(LogLevel.Warning, "Identity packet missing from {endpoint}")]
    public static partial void LogIdentityPacketMissing(this ILogger<VortexSocketService> logger, string endpoint);

    [LoggerMessage(LogLevel.Error, "Error with client {clientId}: {message}")]
    public static partial void LogClientError(this ILogger<VortexSocketService> logger, Guid? clientId, string message, Exception ex);

    [LoggerMessage(LogLevel.Error, "Error unregistering client: {message}")]
    public static partial void LogUnregisterClientError(this ILogger<VortexSocketService> logger, string message);

    [LoggerMessage(LogLevel.Warning, "Incomplete data read: expected {expected} bytes but got {actual} bytes")]
    public static partial void LogIncompleteDataRead(this ILogger<VortexSocketService> logger, int actual, int expected);

    [LoggerMessage(LogLevel.Debug, "Data received: {data}")]
    public static partial void LogDataReceived(this ILogger<VortexSocketService> logger, string data);

    [LoggerMessage(LogLevel.Error, "[VortexServer] Deserialization failed")]
    public static partial void LogDeserializationError(this ILogger<VortexSocketService> logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "[VortexServer] Client registered: {ClientName} ({ClientId})")]
    public static partial void LogClientRegistered(this ILogger<VortexSocketService> logger, string clientName, Guid clientId);

    [LoggerMessage(LogLevel.Error, "[VortexServer] Failed to register client to TerrariaServerManager")]
    public static partial void LogRegisterClientError(this ILogger<VortexSocketService> logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "[VortexServer] Received {PacketID} from {ClientName}")]
    public static partial void LogPacketReceived(this ILogger<VortexSocketService> logger, Protocol.Enums.PacketType packetID, string clientName);

    [LoggerMessage(LogLevel.Information, "[VortexServer] Sent {PacketID} to {ClientName}")]
    public static partial void LogPacketSent(this ILogger<VortexSocketService> logger, Protocol.Enums.PacketType packetID, string clientName);

    [LoggerMessage(LogLevel.Warning, "[VortexServer] Client {ClientId} not found")]
    public static partial void LogClientNotFound(this ILogger<VortexSocketService> logger, Guid clientId);

    [LoggerMessage(LogLevel.Error, "[VortexServer] Failed to send to {ClientId}")]
    public static partial void LogSendToClientError(this ILogger<VortexSocketService> logger, Guid clientId, Exception ex);

    [LoggerMessage(LogLevel.Warning, "[VortexServer] Client {ClientId} is offline")]
    public static partial void LogClientOffline(this ILogger<VortexSocketService> logger, Guid clientId);

    [LoggerMessage(LogLevel.Warning, "[VortexServer] Request timeout for {ClientId}")]
    public static partial void LogRequestTimeout(this ILogger<VortexSocketService> logger, Guid clientId);
}
