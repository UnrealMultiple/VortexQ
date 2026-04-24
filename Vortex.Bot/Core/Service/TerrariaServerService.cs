using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Configuration;
using Vortex.Protocol.Packets;

namespace Vortex.Bot.Core.Service;

public class TerrariaServerService
{
    private readonly ILogger<TerrariaServerService> _logger;
    private readonly VortexSocketService _vortexServer;
    private readonly ConcurrentDictionary<string, TerrariaServer> _servers = new();
    private readonly ConcurrentDictionary<(long UserId, long GroupId), string> _userServerSelections = new();

    public TerrariaServerService(
        ILogger<TerrariaServerService> logger,
        IConfiguration configuration,
        VortexSocketService vortexServer)
    {
        _logger = logger;
        _vortexServer = vortexServer;

        var serversConfig = configuration.GetSection("TerrariaServers").Get<TerrariaServerCollection>();
        if (serversConfig?.Servers != null)
        {
            foreach (var config in serversConfig.Servers)
            {
                var server = new TerrariaServer(config, vortexServer, logger);
                _servers[config.Name] = server;
            }
        }

        _logger.LogInformation("[TerrariaServerManager] 已加载 {Count} 个服务器", _servers.Count);
    }

    public IReadOnlyCollection<TerrariaServer> GetAllServers() => _servers.Values.ToList();

    public TerrariaServer? GetServer(string name)
    {
        _servers.TryGetValue(name, out var server);
        return server;
    }

    public IEnumerable<TerrariaServer> GetServersByGroup(long groupId)
    {
        return _servers.Values.Where(s => s.Config.Groups.Contains(groupId));
    }

    public bool TryGetServer(string name, out TerrariaServer? server)
    {
        return _servers.TryGetValue(name, out server);
    }

    public bool TryGetUserServer(long userId, long groupId, out TerrariaServer? server)
    {
        server = null;
        if (_userServerSelections.TryGetValue((userId, groupId), out var serverName))
        {
            return _servers.TryGetValue(serverName, out server);
        }
        return false;
    }

    public void SetUserServer(long userId, long groupId, string serverName)
    {
        _userServerSelections[(userId, groupId)] = serverName;
    }

    public void ClearUserServer(long userId, long groupId)
    {
        _userServerSelections.TryRemove((userId, groupId), out _);
    }

    public async Task<ServerStatusPacketResponse?> GetServerStatusAsync(string serverName, int timeoutMs = 5000)
    {
        if (!TryGetServer(serverName, out var server) || server == null)
            return null;

        var clientId = server.GetOnlineClientId();
        if (clientId == null)
            return null;

        var request = new ServerStatusPacket();
        return await _vortexServer.RequestAsync<ServerStatusPacket, ServerStatusPacketResponse>(clientId.Value, request, timeoutMs);
    }

    public async Task<ServerOnlinePacketResponse?> GetServerOnlineAsync(string serverName, int timeoutMs = 5000)
    {
        if (!TryGetServer(serverName, out var server) || server == null)
            return null;

        var clientId = server.GetOnlineClientId();
        if (clientId == null)
            return null;

        var request = new ServerOnlinePacket();
        return await _vortexServer.RequestAsync<ServerOnlinePacket, ServerOnlinePacketResponse>(clientId.Value, request, timeoutMs);
    }

    public async Task<ExecuteCommandPacketResponse?> ExecuteCommandAsync(string serverName, string command, int timeoutMs = 10000)
    {
        if (!TryGetServer(serverName, out var server) || server == null)
            return null;

        var clientId = server.GetOnlineClientId();
        if (clientId == null)
            return null;

        var request = new ExecuteCommandPacket { Text = command };
        return await _vortexServer.RequestAsync<ExecuteCommandPacket, ExecuteCommandPacketResponse>(clientId.Value, request, timeoutMs);
    }

    public async Task<GameProgressPacketResponse?> GetGameProgressAsync(string serverName, int timeoutMs = 5000)
    {
        if (!TryGetServer(serverName, out var server) || server == null)
            return null;

        var clientId = server.GetOnlineClientId();
        if (clientId == null)
            return null;

        var request = new GameProgressPacket();
        return await _vortexServer.RequestAsync<GameProgressPacket, GameProgressPacketResponse>(clientId.Value, request, timeoutMs);
    }

    public async Task<ServerRestartPacketResponse?> RestartServerAsync(string serverName, string startArgs = "", int timeoutMs = 5000)
    {
        if (!TryGetServer(serverName, out var server) || server == null)
            return null;

        var clientId = server.GetOnlineClientId();
        if (clientId == null)
            return null;

        var request = new ServerRestartPacket { StartArgs = startArgs };
        return await _vortexServer.RequestAsync<ServerRestartPacket, ServerRestartPacketResponse>(clientId.Value, request, timeoutMs);
    }

    public async Task<ServerResetPacketResponse?> ResetServerAsync(string serverName, List<string> resetCommands, string startArgs = "", int timeoutMs = 5000)
    {
        if (!TryGetServer(serverName, out var server) || server == null)
            return null;

        var clientId = server.GetOnlineClientId();
        if (clientId == null)
            return null;

        var request = new ServerResetPacket
        {
            ResetCommand = resetCommands,
            StartArgs = startArgs
        };
        return await _vortexServer.RequestAsync<ServerResetPacket, ServerResetPacketResponse>(clientId.Value, request, timeoutMs);
    }

    public void RegisterClientConnection(string serverName, Guid clientId)
    {
        _logger.LogInformation("[TerrariaServerManager] 尝试注册服务器连接: {ServerName}, 可用服务器: {Servers}",
            serverName, string.Join(", ", _servers.Keys));

        if (TryGetServer(serverName, out var server) && server != null)
        {
            server.SetClientConnection(clientId);
            _logger.LogInformation("[TerrariaServerManager] 服务器 {ServerName} 已连接到客户端 {ClientId}", serverName, clientId);
        }
        else
        {
            _logger.LogWarning("[TerrariaServerManager] 未找到服务器: {ServerName}", serverName);
        }
    }

    public void UnregisterClientConnection(Guid clientId)
    {
        foreach (var server in _servers.Values)
        {
            if (server.GetOnlineClientId() == clientId)
            {
                server.ClearClientConnection();
                _logger.LogInformation("[TerrariaServerManager] 服务器 {ServerName} 已断开连接", server.Config.Name);
            }
        }
    }
}

public class TerrariaServer
{
    private readonly ILogger _logger;
    private Guid? _connectedClientId;

    public TerrariaServerEnity Config { get; }
    public VortexSocketService VortexServer { get; }

    public TerrariaServer(TerrariaServerEnity config, VortexSocketService vortexServer, ILogger logger)
    {
        Config = config;
        VortexServer = vortexServer;
        _logger = logger;
    }

    public void SetClientConnection(Guid clientId)
    {
        _connectedClientId = clientId;
    }

    public void ClearClientConnection()
    {
        _connectedClientId = null;
    }

    public Guid? GetOnlineClientId() => _connectedClientId;

    public bool IsOnline => _connectedClientId.HasValue;

    public async Task<ExecuteCommandPacketResponse?> ExecuteCommandAsync(string command)
    {
        if (_connectedClientId == null)
            return null;

        var request = new ExecuteCommandPacket { Text = command };
        return await VortexServer.RequestAsync<ExecuteCommandPacket, ExecuteCommandPacketResponse>(_connectedClientId.Value, request);
    }

    public async Task<ServerStatusPacketResponse?> GetStatusAsync()
    {
        if (_connectedClientId == null)
        {
            _logger.LogDebug("[TerrariaServer] 获取状态失败: 未连接到客户端");
            return null;
        }

        _logger.LogDebug("[TerrariaServer] 正在向客户端 {ClientId} 请求状态", _connectedClientId);
        var request = new ServerStatusPacket();
        var response = await VortexServer.RequestAsync<ServerStatusPacket, ServerStatusPacketResponse>(_connectedClientId.Value, request);

        if (response == null)
        {
            _logger.LogWarning("[TerrariaServer] 获取状态超时或无响应");
        }
        else if (!response.Success)
        {
            _logger.LogWarning("[TerrariaServer] 获取状态失败: {Message}", response.Message);
        }
        else
        {
            _logger.LogDebug("[TerrariaServer] 获取状态成功");
        }

        return response;
    }

    public async Task<ServerOnlinePacketResponse?> GetOnlinePlayersAsync()
    {
        if (_connectedClientId == null)
            return null;

        var request = new ServerOnlinePacket();
        return await VortexServer.RequestAsync<ServerOnlinePacket, ServerOnlinePacketResponse>(_connectedClientId.Value, request);
    }

    public async Task<GameProgressPacketResponse?> GetGameProgressAsync()
    {
        if (_connectedClientId == null)
            return null;

        var request = new GameProgressPacket();
        return await VortexServer.RequestAsync<GameProgressPacket, GameProgressPacketResponse>(_connectedClientId.Value, request);
    }

    public async Task<ServerRestartPacketResponse?> RestartAsync(string startArgs = "")
    {
        if (_connectedClientId == null)
            return null;

        var request = new ServerRestartPacket { StartArgs = startArgs };
        return await VortexServer.RequestAsync<ServerRestartPacket, ServerRestartPacketResponse>(_connectedClientId.Value, request);
    }

    public async Task<ServerResetPacketResponse?> ResetAsync(List<string> resetCommands, string startArgs = "")
    {
        if (_connectedClientId == null)
            return null;

        var request = new ServerResetPacket
        {
            ResetCommand = resetCommands,
            StartArgs = startArgs
        };
        return await VortexServer.RequestAsync<ServerResetPacket, ServerResetPacketResponse>(_connectedClientId.Value, request);
    }
}
