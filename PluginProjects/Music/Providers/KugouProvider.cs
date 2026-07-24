using KuGou.Net.Clients;
using KuGou.Net.Infrastructure;
using KuGou.Net.Protocol.Session;
using Microsoft.Extensions.Logging;
using Music.Abstractions;
using Music.Kugou;
using Music.Models;

namespace Music.Providers;

public sealed class KugouProvider : IAuthenticatableProvider
{
    private readonly KuGouServiceProvider _serviceProvider;
    private readonly LoginClient _loginClient;
    private readonly SearchClient _searchClient;
    private readonly SongClient _songClient;
    private readonly PlaylistClient _playlistClient;
    private readonly AlbumClient _albumClient;
    private readonly KgSessionManager _sessionManager;
    private readonly ILogger _logger;
    private readonly System.Timers.Timer _tokenRefreshTimer;

    public string Name => "酷狗音乐";
    public MusicSource Source => MusicSource.Kugou;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_sessionManager.Session.Token);

    public event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

    public KugouProvider(ILogger logger)
    {
        _logger = logger;

        _serviceProvider = new KuGouServiceProvider
        {
            SessionPersistence = new FileSessionPersistence(),
            LoggerFactory = new PluginLoggerFactory(logger)
        };

        _loginClient = _serviceProvider.GetService<LoginClient>();
        _searchClient = _serviceProvider.GetService<SearchClient>();
        _songClient = _serviceProvider.GetService<SongClient>();
        _playlistClient = _serviceProvider.GetService<PlaylistClient>();
        _albumClient = _serviceProvider.GetService<AlbumClient>();
        _sessionManager = _serviceProvider.GetService<KgSessionManager>();

        // 如果已有持久化的 Token，尝试恢复
        if (IsAuthenticated)
        {
            logger.LogInformation("[Kugou] 已加载持久化的会话, UserID: {UserId}", _sessionManager.Session.UserId);
        }

        _tokenRefreshTimer = new System.Timers.Timer(600_000); // 10 分钟
        _tokenRefreshTimer.Elapsed += async (_, _) => await TryRefreshTokenAsync();
        if (IsAuthenticated)
            _tokenRefreshTimer.Start();
    }

    // ======================== 搜索 ========================

    public async Task<IReadOnlyList<SongInfo>> SearchAsync(string keyword, int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _searchClient.SearchAsync(keyword, 1, "song", limit);
            return results.Select(MapSongInfo).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kugou] 搜索歌曲失败: {Keyword}", keyword);
            return [];
        }
    }

    // ======================== 播放 URL ========================

    public async Task<string> GetPlayUrlAsync(string songId, CancellationToken cancellationToken = default)
    {
        try
        {
            var playInfo = await _songClient.GetPlayInfoAsync(songId);
            if (playInfo is { IsSuccess: true, Urls.Count: > 0 })
                return playInfo.Urls[0];

            _logger.LogWarning("[Kugou] 获取播放URL失败: hash={Hash}, status={Status}", songId, playInfo?.Status);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kugou] 获取播放URL异常: {Hash}", songId);
            return string.Empty;
        }
    }

    // ======================== 歌曲详情 ========================

    public async Task<SongInfo?> GetSongDetailAsync(string songId, CancellationToken cancellationToken = default)
    {
        var results = await SearchAsync(songId, 1, cancellationToken);
        return results.FirstOrDefault();
    }

    // ======================== 歌单 ========================

    public async Task<PlaylistInfo?> GetPlaylistAsync(string playlistId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var info = await _playlistClient.GetInfoAsync(playlistId);
            if (info is null) return null;

            var songs = await _playlistClient.GetSongsAsync(playlistId);
            var songInfos = songs?.Songs?.Select(s => new SongInfo
            {
                Id = s.Hash,
                Name = s.Name,
                Artists = s.Singers?.Select(sg => sg.Name).ToList() ?? [],
                Album = s.Album?.Name ?? "",
                AlbumCover = s.Cover ?? "",
                Duration = s.DurationMs / 1000,
                Source = MusicSource.Kugou
            }).ToList() ?? [];

            return new PlaylistInfo
            {
                Id = playlistId,
                Name = info.Name,
                Cover = info.PicUrl ?? "",
                Creator = info.CreatorName ?? "",
                Description = info.Intro ?? "",
                Songs = songInfos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kugou] 获取歌单失败: {Id}", playlistId);
            return null;
        }
    }

    // ======================== 二维码登录 ========================

    public async Task<KugouQrCodeResult?> GetQrCodeAsync()
    {
        try
        {
            var qrCode = await _loginClient.GetQrCodeAsync();
            if (qrCode is null || string.IsNullOrEmpty(qrCode.Qrcode))
            {
                _logger.LogWarning("[Kugou] 获取二维码失败");
                return null;
            }

            var key = qrCode.Qrcode;
            var qrUrl = $"https://h5.kugou.com/apps/loginQRCode/html/index.html?appid=3116&{key}";

            // 从 API 返回的 qrcode_img 中解码 base64 图片
            byte[]? qrBytes = null;
            if (!string.IsNullOrEmpty(qrCode.QrcodeImg) &&
                qrCode.QrcodeImg.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var commaIdx = qrCode.QrcodeImg.IndexOf(',');
                if (commaIdx > 0)
                {
                    try
                    {
                        var base64 = qrCode.QrcodeImg.Substring(commaIdx + 1).Trim();
                        qrBytes = Convert.FromBase64String(base64);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Kugou] 解码二维码图片失败");
                    }
                }
            }

            return new KugouQrCodeResult
            {
                QrCodeKey = key,
                QrCodeUrl = qrUrl,
                QrCodeImageUrl = qrCode.QrcodeImg,
                QrCodeImageBytes = qrBytes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kugou] 获取二维码异常");
            return null;
        }
    }

    public async Task<QrLoginResult?> CheckQrStatusAsync(string qrCode)
    {
        try
        {
            var status = await _loginClient.CheckQrStatusAsync(qrCode);
            if (status is null) return null;

            return new QrLoginResult
            {
                Status = (int)status.QrStatus,
                UserId = status.UserId.ToString(),
                Nickname = status.Nickname,
                Avatar = status.Pic,
                Token = status.Token
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kugou] 检查二维码状态异常");
            return null;
        }
    }

    public void ApplyQrLogin(QrLoginResult result)
    {
        if (result?.Status != 4 || string.IsNullOrEmpty(result.Token)) return;

        // sessionManager 已在 CheckQrStatusAsync 内部自动保存，此处触发事件和计时器
        _tokenRefreshTimer.Start();
        AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs { IsAuthenticated = true });
        _logger.LogInformation("[Kugou] 二维码登录成功! Nickname: {Nickname}", result.Nickname);
    }

    // ======================== 手机验证码登录 ========================

    public async Task<bool> SendSmsCodeAsync(string mobile)
    {
        try
        {
            var result = await _loginClient.SendCodeAsync(mobile);
            if (result?.Status == 1) return true;

            _logger.LogWarning("[Kugou] 发送验证码失败: status={Status}, error={Error}",
                result?.Status, result?.ErrorCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kugou] 发送验证码异常: {Mobile}", mobile);
            return false;
        }
    }

    public async Task<(bool Success, string? Nickname)> LoginByMobileAsync(string mobile, string code)
    {
        try
        {
            var result = await _loginClient.LoginByMobileAsync(mobile, code);
            if (result?.Status == 1)
            {
                // sessionManager 已在 LoginByMobileAsync 内部自动保存
                _tokenRefreshTimer.Start();
                AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs { IsAuthenticated = true });
                _logger.LogInformation("[Kugou] 手机登录成功! UserID: {UserId}", result.UserId);
                return (true, null);
            }

            _logger.LogWarning("[Kugou] 手机登录失败: status={Status}, error={Error}",
                result?.Status, result?.ErrorCode);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kugou] 手机登录异常");
            return (false, null);
        }
    }

    // ======================== Token 刷新 ========================

    public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated) return false;

        try
        {
            var result = await _loginClient.RefreshSessionAsync();
            if (result.Status != 1) return false;

            _logger.LogInformation("[Kugou] Token 刷新成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kugou] Token 刷新失败");
            return false;
        }
    }

    public void Dispose()
    {
        _tokenRefreshTimer.Stop();
        _tokenRefreshTimer.Dispose();
        _serviceProvider.Dispose();
    }

    // ======================== 私有方法 ========================

    private async Task TryRefreshTokenAsync()
    {
        try
        {
            await RefreshTokenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kugou] 定时刷新 Token 失败");
        }
    }

    private SongInfo MapSongInfo(KuGou.Net.Abstractions.Models.SongInfo song)
    {
        return new SongInfo
        {
            Id = song.Hash,
            Name = song.Name,
            Artists = string.IsNullOrEmpty(song.Singer) ? [] : [song.Singer],
            Album = song.AlbumName,
            AlbumCover = song.Cover?.Replace("{size}", "400") ?? "",
            Duration = song.Duration,
            Source = MusicSource.Kugou
        };
    }
}
