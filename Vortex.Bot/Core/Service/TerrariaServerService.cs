using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Vortex.Bot.Configuration;
using Vortex.Bot.Database.Models;
using Vortex.Protocol.Packets;

namespace Vortex.Bot.Core.Service;

public partial class TerrariaServerService
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

        _logger.LogServersLoaded(_servers.Count);
        LoadUserServerSelectionsFromDatabase();
    }

    private void LoadUserServerSelectionsFromDatabase()
    {
        try
        {
            var selections = CharacterSelection.GetAll();
            foreach (var selection in selections)
            {
                if (_servers.ContainsKey(selection.ServerName))
                {
                    _userServerSelections[(selection.UserId, selection.GroupId)] = selection.ServerName;
                }
            }
            _logger.LogUserServerSelectionsLoaded(_userServerSelections.Count);
        }
        catch (Exception ex)
        {
            _logger.LogFailedToLoadUserServerSelections(ex.Message);
        }
    }

    public IReadOnlyCollection<TerrariaServer> GetAllServers() => [.. _servers.Values];

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
        return _userServerSelections.TryGetValue((userId, groupId), out var serverName) && _servers.TryGetValue(serverName, out server);
    }

    public void SetUserServer(long userId, long groupId, string serverName)
    {
        _userServerSelections[(userId, groupId)] = serverName;

        try
        {
            CharacterSelection.SetSelection(userId, groupId, serverName);
            _logger.LogUserServerSelected(userId, groupId, serverName);
        }
        catch (Exception ex)
        {
            _logger.LogFailedToSaveUserServerSelection(ex.Message);
        }
    }

    public void ClearUserServer(long userId, long groupId)
    {
        _userServerSelections.TryRemove((userId, groupId), out _);

        try
        {
            CharacterSelection.ClearSelection(userId, groupId);
            _logger.LogUserServerCleared(userId, groupId);
        }
        catch (Exception ex)
        {
            _logger.LogFailedToClearUserServerSelection(ex.Message);
        }
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

    public async Task<AccountRegistrationPacketResponse?> RegisterAccountAsync(string serverName, string name, string password, string group, int timeoutMs = 10000)
    {
        if (!TryGetServer(serverName, out var server) || server == null)
            return null;

        var clientId = server.GetOnlineClientId();
        if (clientId == null)
            return null;

        var request = new AccountRegistrationPacket
        {
            Name = name,
            Password = password,
            Group = group
        };
        return await _vortexServer.RequestAsync<AccountRegistrationPacket, AccountRegistrationPacketResponse>(clientId.Value, request, timeoutMs);
    }

    public async Task<AccountQueryPacketResponse?> QueryAccountAsync(string serverName, string targetName, int timeoutMs = 5000)
    {
        if (!TryGetServer(serverName, out var server) || server == null)
            return null;

        var clientId = server.GetOnlineClientId();
        if (clientId == null)
            return null;

        var request = new AccountQueryPacket
        {
            Target = targetName
        };
        return await _vortexServer.RequestAsync<AccountQueryPacket, AccountQueryPacketResponse>(clientId.Value, request, timeoutMs);
    }

    public void RegisterClientConnection(string serverName, Guid clientId)
    {
        _logger.LogRegisteringServerConnection(serverName, string.Join(", ", _servers.Keys));

        if (TryGetServer(serverName, out var server) && server != null)
        {
            server.SetClientConnection(clientId);
            _logger.LogServerConnected(serverName, clientId);
        }
        else
        {
            _logger.LogServerNotFound(serverName);
        }
    }

    public void UnregisterClientConnection(Guid clientId)
    {
        foreach (var server in _servers.Values)
        {
            if (server.GetOnlineClientId() == clientId)
            {
                server.ClearClientConnection();
                _logger.LogServerDisconnected(server.Config.Name);
            }
        }
    }
}

public static partial class TerrariaServerServiceLoggerExtension
{
    [LoggerMessage(LogLevel.Information, "[TerrariaServerManager] 已加载 {Count} 个服务器")]
    public static partial void LogServersLoaded(this ILogger<TerrariaServerService> logger, int count);

    [LoggerMessage(LogLevel.Information, "[TerrariaServerManager] 已从数据库加载 {Count} 个用户服务器选择")]
    public static partial void LogUserServerSelectionsLoaded(this ILogger<TerrariaServerService> logger, int count);

    [LoggerMessage(LogLevel.Warning, "[TerrariaServerManager] 从数据库加载用户服务器选择失败: {Error}")]
    public static partial void LogFailedToLoadUserServerSelections(this ILogger<TerrariaServerService> logger, string error);

    [LoggerMessage(LogLevel.Debug, "[TerrariaServerManager] 用户 {UserId} 在群组 {GroupId} 选择了服务器 {ServerName}")]
    public static partial void LogUserServerSelected(this ILogger<TerrariaServerService> logger, long userId, long groupId, string serverName);

    [LoggerMessage(LogLevel.Warning, "[TerrariaServerManager] 保存用户服务器选择到数据库失败: {Error}")]
    public static partial void LogFailedToSaveUserServerSelection(this ILogger<TerrariaServerService> logger, string error);

    [LoggerMessage(LogLevel.Debug, "[TerrariaServerManager] 用户 {UserId} 在群组 {GroupId} 的服务器选择已清除")]
    public static partial void LogUserServerCleared(this ILogger<TerrariaServerService> logger, long userId, long groupId);

    [LoggerMessage(LogLevel.Warning, "[TerrariaServerManager] 从数据库清除用户服务器选择失败: {Error}")]
    public static partial void LogFailedToClearUserServerSelection(this ILogger<TerrariaServerService> logger, string error);

    [LoggerMessage(LogLevel.Information, "[TerrariaServerManager] 尝试注册服务器连接: {ServerName}, 可用服务器: {Servers}")]
    public static partial void LogRegisteringServerConnection(this ILogger<TerrariaServerService> logger, string serverName, string servers);

    [LoggerMessage(LogLevel.Information, "[TerrariaServerManager] 服务器 {ServerName} 已连接到客户端 {ClientId}")]
    public static partial void LogServerConnected(this ILogger<TerrariaServerService> logger, string serverName, Guid clientId);

    [LoggerMessage(LogLevel.Warning, "[TerrariaServerManager] 未找到服务器: {ServerName}")]
    public static partial void LogServerNotFound(this ILogger<TerrariaServerService> logger, string serverName);

    [LoggerMessage(LogLevel.Information, "[TerrariaServerManager] 服务器 {ServerName} 已断开连接")]
    public static partial void LogServerDisconnected(this ILogger<TerrariaServerService> logger, string serverName);
}

public partial class TerrariaServer(TerrariaServerEnity config, VortexSocketService vortexServer, ILogger logger)
{
    private readonly ILogger _logger = logger;
    private Guid? _connectedClientId;

    public TerrariaServerEnity Config { get; } = config;
    public VortexSocketService VortexServer { get; } = vortexServer;

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
            _logger.LogGetStatusFailedNotConnected();
            return null;
        }

        _logger.LogRequestingStatus(_connectedClientId);
        var request = new ServerStatusPacket();
        var response = await VortexServer.RequestAsync<ServerStatusPacket, ServerStatusPacketResponse>(_connectedClientId.Value, request);

        if (response == null)
        {
            _logger.LogGetStatusTimeout(_connectedClientId);
        }
        else if (!response.Success)
        {
            _logger.LogGetStatusFailed(response.Message);
        }
        else
        {
            _logger.LogGetStatusSuccess();
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

    public async Task<AccountRegistrationPacketResponse?> RegisterAccountAsync(string name, string password, string group)
    {
        if (_connectedClientId == null)
            return null;

        var request = new AccountRegistrationPacket
        {
            Name = name,
            Password = password,
            Group = group
        };
        return await VortexServer.RequestAsync<AccountRegistrationPacket, AccountRegistrationPacketResponse>(_connectedClientId.Value, request);
    }

    public async Task<AccountQueryPacketResponse?> QueryAccountAsync(string targetName)
    {
        if (_connectedClientId == null)
            return null;

        var request = new AccountQueryPacket
        {
            Target = targetName
        };
        return await VortexServer.RequestAsync<AccountQueryPacket, AccountQueryPacketResponse>(_connectedClientId.Value, request);
    }

    public async Task<PlayerInventoryPacketResponse?> QueryPlayerInventoryAsync(string playerName)
    {
        if (_connectedClientId == null)
            return null;

        var request = new PlayerInventoryPacket
        {
            Name = playerName
        };
        return await VortexServer.RequestAsync<PlayerInventoryPacket, PlayerInventoryPacketResponse>(_connectedClientId.Value, request);
    }
}

public static partial class TerrariaServerLoggerExtension
{
    [LoggerMessage(LogLevel.Warning, "[TerrariaServer] 无法获取服务器状态，未连接到客户端")]
    public static partial void LogGetStatusFailedNotConnected(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "[TerrariaServer] 请求服务器状态，客户端ID: {ClientId}")]
    public static partial void LogRequestingStatus(this ILogger logger, Guid? clientId);

    [LoggerMessage(LogLevel.Warning, "[TerrariaServer] 获取服务器状态超时，客户端ID: {ClientId}")]
    public static partial void LogGetStatusTimeout(this ILogger logger, Guid? clientId);

    [LoggerMessage(LogLevel.Warning, "[TerrariaServer] 获取服务器状态失败: {Message}")]
    public static partial void LogGetStatusFailed(this ILogger logger, string message);
    [LoggerMessage(LogLevel.Information, "[TerrariaServer] 获取服务器状态成功")]
    public static partial void LogGetStatusSuccess(this ILogger logger);
}
