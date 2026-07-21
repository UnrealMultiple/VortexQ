using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Music.Abstractions;
using Music.Common;
using Music.Models;
using Music.QQ.Enums;
using Music.QQ.Internal;
using Music.QQ.Internal.MusicToken;
using Music.QQ.Internal.Playlists;
using Music.QQ.Internal.QuerSong;
using Music.QQ.Internal.Search;
using Music.QQ.Internal.Search.Song;

namespace Music.Providers;

public sealed class QQMusicProvider : IAuthenticatableProvider, IMusicProvider
{
    private readonly HttpClient _client;
    private readonly CookieContainer _cookieContainer;
    private readonly System.Timers.Timer _tokenRefreshTimer;
    private TokenInfo? _tokenInfo;
    private readonly ILogger _logger;

    public string Name => "QQ音乐";
    public MusicSource Source => MusicSource.QQMusic;
    public bool IsAuthenticated => _tokenInfo is not null;

    public event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

    public QQMusicProvider(ILogger logger)
    {
        _logger = logger;
        _cookieContainer = new CookieContainer();
        _client = HttpClientFactory.CreateWithCookies(_cookieContainer);
        _client.DefaultRequestHeaders.Add("Referer", "y.qq.com");
        _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        _tokenRefreshTimer = new System.Timers.Timer(600000); // 10分钟
        _tokenRefreshTimer.Elapsed += async (_, _) => await TryRefreshTokenAsync();
    }

    public void SetToken(TokenInfo token)
    {
        _tokenInfo = token;
        UpdateCookies(token.Cookie);
        _tokenRefreshTimer.Start();
        AuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs { IsAuthenticated = true });
    }

    public async Task<IReadOnlyList<SongInfo>> SearchAsync(string keyword, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (_tokenInfo is null)
        {
            _logger.LogDebug("QQMusic search skipped: not authenticated");
            return [];
        }

        var request = new
        {
            module = "music.search.SearchCgiService",
            method = "DoSearchForQQMusicDesktop",
            platform = "desktop",
            param = new
            {
                search_id = QQ.Utils.GetSearchID(),
                search_type = 0,
                query = keyword,
                page_num = 1,
                page_id = 1,
                highlight = 0,
                num_per_page = limit,
                grp = 1
            }
        };

        var response = await SendRequestAsync(request, cancellationToken);
        var reqData = JsonHelper.Deserialize<ReqData>(response.Req.Data?.ToJsonString() ?? "{}");
        
        if (reqData?.Body.Songs.List is null)
            return [];

        return reqData.Body.Songs.List.Select(MapToSongInfo).ToList();
    }

    public async Task<string> GetPlayUrlAsync(string songId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var songs = await GetSongDataAsync(SongFileType.MP3_128, cancellationToken, songId);
        return songs.FirstOrDefault()?.PlayUrl ?? string.Empty;
    }

    public async Task<SongInfo?> GetSongDetailAsync(string songId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var request = CreateRequest("music.trackInfo.UniformRuleCtrl", "CgiGetTrackInfo", new
        {
            ids = new List<string>(),
            mids = new[] { songId },
            types = new[] { 0 },
            modify_stamp = new[] { 0 },
            client = 1,
            ctx = 0
        });

        var response = await SendRequestAsync(request, cancellationToken);
        var queryData = JsonHelper.Deserialize<QuerySongData>(response.Req.Data?.ToJsonString() ?? "{}");
        
        return queryData?.Tracks.FirstOrDefault() is { } track 
            ? MapToSongInfo(track) 
            : null;
    }

    public async Task<PlaylistInfo?> GetPlaylistAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var request = CreateRequest("music.srfDissInfo.DissInfo", "CgiGetDiss", new
        {
            disstid = long.Parse(playlistId),
            dirid = 0,
            tag = 0,
            song_num = 0,
            userinfo = 1,
            orderlist = 1
        });

        var response = await SendRequestAsync(request, cancellationToken);
        var playData = JsonHelper.Deserialize<PlayData>(response.Req.Data?.ToJsonString() ?? "{}");

        if (playData?.Dirinfo is null) return null;

        return new PlaylistInfo
        {
            Id = playlistId,
            Name = playData.Dirinfo.Title,
            Cover = playData.Dirinfo.Picurl,
            Creator = playData.Dirinfo.Creator?.Nick ?? string.Empty,
            Description = playData.Dirinfo.Desc,
            Songs = playData.Songlist.Select(MapToSongInfo).ToList()
        };
    }

    public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_tokenInfo is null) return false;

        try
        {
            var request = CreateRequest("QQConnectLogin.LoginServer", "QQLogin", new
            {
                refresh_key = _tokenInfo.RefreshKey,
                refresh_token = _tokenInfo.RefreshToken,
                musickey = _tokenInfo.Musickey,
                musicid = _tokenInfo.Musicid
            }, new { tmeLoginType = _tokenInfo.LoginType.ToString() });

            var response = await SendRequestAsync(request, cancellationToken);
            
            if (response.Req.Code != 0 || response.Req.Data is null)
                return false;

            var newToken = JsonHelper.Deserialize<TokenInfo>(response.Req.Data.ToJsonString());
            if (newToken is null) return false;

            newToken.Cookie = _cookieContainer.GetCookies(new Uri(QQMusicApi))
                .ToDictionary(c => c.Name, c => c.Value);
            
            _tokenInfo = newToken;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return false;
        }
    }

    private async Task TryRefreshTokenAsync()
    {
        if (_tokenInfo?.KeyExpiresIn < DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _tokenInfo.MusickeyCreateTime)
        {
            await RefreshTokenAsync();
        }
    }

    private async Task<List<SongData>> GetSongDataAsync(SongFileType type, CancellationToken cancellationToken, params string[] mids)
    {
        var request = CreateRequest("music.vkey.GetVkey", "UrlGetVkey", new
        {
            filename = mids.Select(m => $"{type.GetSongFormat()}{type}{type.GetSongExtension()}"),
            songmid = mids,
            guid = Guid.NewGuid().ToString("N"),
            songtype = mids.Select(_ => 0)
        });

        var response = await SendRequestAsync(request, cancellationToken);
        var songDict = mids.ToDictionary(m => m, _ => new SongData { Mid = _ });

        var array = response.Req.Data?["midurlinfo"]?.AsArray();
        if (array is null) return songDict.Values.ToList();

        const string domain = "https://ws.stream.qqmusic.qq.com/";
        foreach (var item in array)
        {
            var songmid = item?["songmid"]?.ToString();
            var url = item?["wifiurl"]?.ToString();
            if (!string.IsNullOrEmpty(songmid) && !string.IsNullOrEmpty(url) && songDict.ContainsKey(songmid))
            {
                songDict[songmid].PlayUrl = domain + url;
            }
        }

        return songDict.Values.ToList();
    }

    private static object CreateRequest(string module, string method, object param, object? extraCommon = null)
    {
        var request = new
        {
            module,
            method,
            param
        };

        if (extraCommon is not null)
        {
            return new { req = request, extra_common = extraCommon };
        }

        return request;
    }

    private async Task<Response> SendRequestAsync(object request, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();

        var qimei = await QimeiService.GetQimeiAsync(new Device(), "13.2.5.8");
        
        var payload = new
        {
            comm = new
            {
                ct = "11",
                cv = "13020508",
                v = "13020508",
                tmeAppID = "qqmusic",
                uid = "3931641530",
                format = "json",
                inCharset = "utf-8",
                outCharset = "utf-8",
                QIMEI36 = qimei.Q36,
                qq = _tokenInfo!.Musicid.ToString(),
                authst = _tokenInfo.Musickey,
                tmeLoginType = _tokenInfo.LoginType.ToString()
            },
            req = request
        };

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // 保持原样，不转换
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload, jsonOptions));
        var response = await _client.PostAsync(QQMusicApi, content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonHelper.Deserialize<Response>(json) 
            ?? throw new InvalidOperationException("Failed to parse response");
    }

    private void EnsureAuthenticated()
    {
        if (_tokenInfo is null)
            throw new InvalidOperationException("Not authenticated");
    }

    private void UpdateCookies(Dictionary<string, string> cookies)
    {
        var uri = new Uri(QQMusicApi);
        foreach (var (name, value) in cookies)
        {
            _cookieContainer.Add(uri, new Cookie(name, value));
        }
    }

    private static SongInfo MapToSongInfo(SongData data)
    {
        return new SongInfo
        {
            Id = data.Mid,
            Name = data.Title,
            Artists = data.Singer.Select(s => s.Name).ToList(),
            Album = data.Album.Name,
            AlbumCover = data.Album.Picture,
            Duration = data.Interval,
            PlayUrl = data.PlayUrl,
            Source = MusicSource.QQMusic
        };
    }

    public void Dispose()
    {
        _tokenRefreshTimer.Stop();
        _tokenRefreshTimer.Dispose();
        _client.Dispose();
    }

    private const string QQMusicApi = "https://u.y.qq.com/cgi-bin/musicu.fcg";
}
