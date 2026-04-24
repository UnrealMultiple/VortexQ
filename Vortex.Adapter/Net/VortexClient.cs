using System.Net.Sockets;
using Vortex.Adapter.Setting.Configs;
using Vortex.Protocol.Interfaces;
using Vortex.Protocol.Packets;
using Vortex.Protocol.Serialization;

namespace Vortex.Adapter.Net;

public class VortexClient(SocketConfig config) : IDisposable
{
    private readonly string _serverAddress = config.IP;
    private readonly int _serverPort = config.Port;
    private readonly string _clientName = config.ServerName;
    private readonly string _token = config.Token;
    private readonly PacketSerializer _serializer = new();

    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly Guid _clientId = Guid.NewGuid();
    private bool _isConnected;
    private bool _isRunning;

    public event Action<INetPacket>? OnPacketReceived;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public bool IsConnected => _isConnected;
    public Guid ClientId => _clientId;
    public string ClientName => _clientName;

    public void Start()
    {
        _isRunning = true;
        _ = ConnectLoopAsync();
    }

    private async Task ConnectLoopAsync()
    {
        var attemptCount = 0;
        while (_isRunning)
        {
            try
            {
                attemptCount++;
                if (await ConnectAsync())
                {
                    if (await AuthenticateAsync())
                    {
                        var identityPacket = new ClientIdentityPacket
                        {
                            ClientId = _clientId,
                            ClientName = _clientName
                        };
                        await SendPacketAsync(identityPacket);

                        using var cts = new CancellationTokenSource(5000);
                        var response = await ReadPacketAsync(cts.Token);
                        if (response is ClientIdentityResponsePacket identityResponse && identityResponse.Success)
                        {
                            TShockAPI.TShock.Log.ConsoleInfo($"[Vortex.Adapter] 身份注册成功，会话ID: {identityResponse.SessionId}");
                            OnConnected?.Invoke();
                            await ReceiveLoopAsync();
                        }
                        else
                        {
                            TShockAPI.TShock.Log.ConsoleError("[Vortex.Adapter] 身份注册失败");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter]({attemptCount}) 连接服务器失败: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }

            if (_isRunning)
            {
                TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter]({attemptCount}) 未连接至Vortex服务器，{5}秒后重试...");
                await Task.Delay(5000);
            }
        }
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_serverAddress, _serverPort);
            _stream = _tcpClient.GetStream();
            _isConnected = true;
            TShockAPI.TShock.Log.ConsoleInfo($"[Vortex.Adapter] 已连接到服务器 {_serverAddress}:{_serverPort}");
            return true;
        }
        catch (Exception ex)
        {
            TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter] 连接失败: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync()
    {
        if (!_isConnected || _stream == null)
        {
            TShockAPI.TShock.Log.ConsoleError("[Vortex.Adapter] 未连接到服务器，无法认证");
            return false;
        }

        try
        {
            using var cts = new CancellationTokenSource(5000);

            var authPacket = new ClientAuthPacket { Token = _token };
            await SendPacketAsync(authPacket);
            var response = await ReadPacketAsync(cts.Token);

            if (response is ClientAuthResponsePacket authResponse && authResponse.Success)
            {
                TShockAPI.TShock.Log.ConsoleInfo($"[Vortex.Adapter] 认证成功");
                return true;
            }
            else
            {
                TShockAPI.TShock.Log.ConsoleError("[Vortex.Adapter] 认证失败，Token 无效");
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            TShockAPI.TShock.Log.ConsoleError("[Vortex.Adapter] 认证超时");
            return false;
        }
        catch (Exception ex)
        {
            TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter] 认证过程中出错: {ex.Message}");
            return false;
        }
    }

    public async Task SendPacketAsync(INetPacket packet)
    {
        if (_stream == null || !_isConnected)
        {
            TShockAPI.TShock.Log.ConsoleError("[Vortex.Adapter] 无法发送数据包: 未连接");
            return;
        }

        try
        {
            var buffer = _serializer.Serialize(packet);
            await _stream.WriteAsync(buffer);
        }
        catch (Exception ex)
        {
            TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter] 发送数据包失败: {ex.Message}");
        }
    }

    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request, int timeoutMs = 5000)
        where TRequest : IServicePacket
        where TResponse : class, IClientPacket
    {
        var tcs = new TaskCompletionSource<IClientPacket>();
        void handler(INetPacket packet)
        {
            if (packet is TResponse response && response.RequestId == request.RequestId)
            {
                tcs.TrySetResult(response);
                OnPacketReceived -= handler;
            }
        }

        OnPacketReceived += handler;

        await SendPacketAsync(request);

        using var cts = new CancellationTokenSource(timeoutMs);
        await using (cts.Token.Register(() => tcs.TrySetCanceled()))
        {
            try
            {
                var result = await tcs.Task;
                return result as TResponse;
            }
            catch (OperationCanceledException)
            {
                TShockAPI.TShock.Log.ConsoleError("[Vortex.Adapter] 请求超时");
                OnPacketReceived -= handler;
                return null;
            }
        }
    }

    private async Task ReceiveLoopAsync()
    {
        if (_stream == null) return;

        try
        {
            while (_isConnected)
            {
                var packet = await ReadPacketAsync(CancellationToken.None);
                if (packet == null)
                {
                    TShockAPI.TShock.Log.ConsoleInfo("[Vortex.Adapter] 连接已关闭");
                    break;
                }

                try
                {
                    OnPacketReceived?.Invoke(packet);
                }
                catch (Exception handlerEx)
                {
                    TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter] 处理数据包事件时出错: {handlerEx}");
                }
            }
        }
        catch (Exception ex)
        {
            TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter] 接收数据错误: {ex}");
        }
        finally
        {
            Disconnect();
            OnDisconnected?.Invoke();
        }
    }

    private async Task<INetPacket?> ReadPacketAsync(CancellationToken cancellationToken)
    {
        if (_stream == null) return null;

        var lengthBytes = new byte[2];
        int read = await _stream.ReadAsync(lengthBytes.AsMemory(0, 2), cancellationToken);
        if (read < 2)
        {
            TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter] 读取长度失败，只读取了 {read} 字节");
            return null;
        }

        var length = BitConverter.ToInt16(lengthBytes);
        var data = new byte[length];
        data[0] = lengthBytes[0];
        data[1] = lengthBytes[1];

        int totalRead = 2;
        while (totalRead < length)
        {
            read = await _stream.ReadAsync(data.AsMemory(totalRead, length - totalRead), cancellationToken);
            if (read == 0)
            {
                TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter] 连接在读取数据时关闭，已读取 {totalRead}/{length} 字节");
                return null;
            }
            totalRead += read;
        }

        if (totalRead < length)
        {
            TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter] 数据读取不完整: {totalRead}/{length} 字节");
            return null;
        }

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        try
        {
            var packet = _serializer.Deserialize(br);
            return packet;
        }
        catch (Exception ex)
        {
            TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter] 反序列化失败: {ex.Message}");
            TShockAPI.TShock.Log.ConsoleError($"[Vortex.Adapter] 数据内容: {BitConverter.ToString(data)}");
            throw;
        }
    }

    public void Disconnect()
    {
        _isConnected = false;
        _stream?.Close();
        _tcpClient?.Close();
    }

    public void Dispose()
    {
        _isRunning = false;
        Disconnect();
        GC.SuppressFinalize(this);
    }
}
