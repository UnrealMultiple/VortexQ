using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;
using Vortex.Bot.Models;
using Vortex.Protocol.Packets;

namespace Vortex.Bot.Core.Service;

public partial class ClientConnectionService(ILogger<ClientConnectionService> logger)
{
    private readonly ILogger<ClientConnectionService> _logger = logger;
    private readonly ConcurrentDictionary<Guid, ClientConnection> _clientsById = new();
    private readonly ConcurrentDictionary<int, Guid> _sessionToClientId = new();
    private int _nextSessionId = 0;

    public event Action<ClientConnection>? OnClientConnected;
    public event Action<ClientConnection>? OnClientDisconnected;

    public ClientConnection RegisterClient(ClientIdentityPacket packet, TcpClient tcpClient, string endpoint)
    {
        var sessionId = Interlocked.Increment(ref _nextSessionId);

        var client = new ClientConnection
        {
            ClientId = packet.ClientId,
            ClientName = packet.ClientName,
            TcpClient = tcpClient,
            SessionId = sessionId,
            Endpoint = endpoint,
            IsAuthenticated = true
        };

        _clientsById[packet.ClientId] = client;
        _sessionToClientId[sessionId] = packet.ClientId;

        _logger.LogClientRegistered(packet.ClientName, packet.ClientId, sessionId, endpoint);

        OnClientConnected?.Invoke(client);
        return client;
    }

    public async Task<bool> RemoveClientAsync(Guid clientId)
    {
        if (!_clientsById.TryRemove(clientId, out var client))
            return false;

        _sessionToClientId.TryRemove(client.SessionId, out _);

        _logger.LogClientRemoved(client.ClientName, client.ClientId);

        try
        {
            client.TcpClient.Close();
        }
        catch { }

        OnClientDisconnected?.Invoke(client);
        return await Task.FromResult(true);
    }

    public ClientConnection? GetClient(Guid clientId)
    {
        _clientsById.TryGetValue(clientId, out var client);
        return client;
    }

    public ClientConnection? GetClientBySession(int sessionId)
    {
        return _sessionToClientId.TryGetValue(sessionId, out var clientId) ? GetClient(clientId) : null;
    }

    public ClientConnection? GetClientByName(string clientName)
    {
        return _clientsById.Values.FirstOrDefault(c =>
            c.ClientName.Equals(clientName, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyCollection<ClientConnection> GetAllClients() => [.. _clientsById.Values];
    public int Count => _clientsById.Count;
    public bool IsOnline(Guid clientId) => _clientsById.ContainsKey(clientId);


    public async Task DisconnectAllAsync()
    {
        var clientIds = _clientsById.Keys.ToList();
        foreach (var clientId in clientIds)
        {
            await RemoveClientAsync(clientId);
        }
    }
}

public static partial class ClientConnectionServiceLoggerExtension
{
    [LoggerMessage(LogLevel.Information, "Client connected: {clientName} (ID: {clientId}, Session: {sessionId}, Endpoint: {endpoint})")]
    public static partial void LogClientRegistered(this ILogger<ClientConnectionService> logger, string clientName, Guid clientId, int sessionId, string endpoint);
    [LoggerMessage(LogLevel.Information, "Client disconnected: {clientName} (ID: {clientId})")]
    public static partial void LogClientRemoved(this ILogger<ClientConnectionService> logger, string clientName, Guid clientId);
}
