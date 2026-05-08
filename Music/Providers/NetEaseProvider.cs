using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Music.Abstractions;
using Music.Common;
using Music.Models;

namespace Music.Providers;

public sealed class NetEaseProvider : IMusicProvider
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    private const string EncSecKey = "5cab2b2073b315b420815a56c81f57f62b396655bd04d5005fb1de166811c631551b1da456fdc6eb45f87ae926b19c74a06b3fc6a04c5018a52d5960a44cae7eeafd543a2b1735f4a60f74c23ef487ed15c1f40f35af40fdb66ec4fc67fe991fdefd3266ea8f55cddbaabb0f75f8b19c1c7eca6447dec6938969045e04851929";
    private const string Iv = "ABxeF5MspBB0AbUJ";
    private const string Ig = "0CoJUm6Qyw8W8jud";
    private const string Key = "0102030405060708";

    private const string SearchApi = "https://music.163.com/weapi/cloudsearch/get/web?csrf_token=";
    private const string SongUrlApi = "https://music.163.com/weapi/song/enhance/player/url/v1?csrf_token=";
    private const string SongDetailApi = "https://music.163.com/weapi/v3/song/detail?csrf_token=";
    private const string PlaylistApi = "https://music.163.com/weapi/v6/playlist/detail?csrf_token=";

    public string Name => "网易云音乐";
    public MusicSource Source => MusicSource.NetEase;

    public NetEaseProvider(ILogger logger)
    {
        _logger = logger;
        _client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip
        });
        
        _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.79");
        _client.DefaultRequestHeaders.Add("Origin", "https://music.163.com");
        _client.DefaultRequestHeaders.Add("Referer", "https://music.163.com/");
        _client.DefaultRequestHeaders.Add("Cookie", GetDefaultCookie());
    }

    public async Task<IReadOnlyList<SongInfo>> SearchAsync(string keyword, int limit = 10, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            hlpretag = "<span class=\"s-fc7\">",
            hlposttag = "</span>",
            s = keyword,
            type = 1,
            offset = 0,
            total = "true",
            limit,
            csrf_token = ""
        };

        try
        {
            var result = await PostFormAsync(SearchApi, param, cancellationToken);
            if (string.IsNullOrEmpty(result))
                return [];

            var data = JsonNode.Parse(result);
            if (data?["code"]?.GetValue<int>() != 200)
            {
                _logger.LogWarning("NetEase search API returned code {Code}", data?["code"]?.GetValue<int>());
                return [];
            }

            var songs = data?["result"]?["songs"]?.AsArray();
            if (songs is null)
                return [];

            return songs.Select(ParseSongInfo).Where(s => s is not null).Cast<SongInfo>().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NetEase search failed");
            return [];
        }
    }

    public async Task<string> GetPlayUrlAsync(string songId, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            ids = $"[{songId}]",
            level = "standard",
            encodeType = "acc",
            csrf_token = ""
        };

        try
        {
            var result = await PostFormAsync(SongUrlApi, param, cancellationToken);
            if (string.IsNullOrEmpty(result))
                return string.Empty;

            var data = JsonNode.Parse(result);
            if (data?["code"]?.GetValue<int>() != 200)
                return string.Empty;

            var url = data?["data"]?.AsArray()?.FirstOrDefault()?["url"]?.GetValue<string>();
            return url ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NetEase get play url failed");
            return string.Empty;
        }
    }

    public async Task<SongInfo?> GetSongDetailAsync(string songId, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            id = songId,
            c = $"[{{\"id\":{songId}}}]",
            csrf_token = ""
        };

        try
        {
            var result = await PostFormAsync(SongDetailApi, param, cancellationToken);
            if (string.IsNullOrEmpty(result))
                return null;

            var data = JsonNode.Parse(result);
            if (data?["code"]?.GetValue<int>() != 200)
                return null;

            var song = data?["songs"]?.AsArray()?.FirstOrDefault();
            if (song is null) return null;

            var songInfo = ParseSongInfo(song);
            if (songInfo is null) return null;

            var url = await GetPlayUrlAsync(songId, cancellationToken);
            return songInfo with { PlayUrl = url };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NetEase get song detail failed");
            return null;
        }
    }

    public async Task<PlaylistInfo?> GetPlaylistAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            id = playlistId,
            n = 1000,
            csrf_token = ""
        };

        try
        {
            var result = await PostFormAsync(PlaylistApi, param, cancellationToken);
            if (string.IsNullOrEmpty(result))
                return null;

            var data = JsonNode.Parse(result);
            if (data?["code"]?.GetValue<int>() != 200)
                return null;

            var playlist = data?["playlist"];
            if (playlist is null) return null;

            var tracks = playlist["tracks"]?.AsArray();
            var songs = tracks?.Select(ParseSongInfo).Where(s => s is not null).Cast<SongInfo>().ToList() ?? new List<SongInfo>();

            return new PlaylistInfo
            {
                Id = playlistId,
                Name = playlist["name"]?.GetValue<string>() ?? string.Empty,
                Cover = playlist["coverImgUrl"]?.GetValue<string>() ?? string.Empty,
                Creator = playlist["creator"]?["nickname"]?.GetValue<string>() ?? string.Empty,
                Description = playlist["description"]?.GetValue<string>() ?? string.Empty,
                Songs = songs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NetEase get playlist failed");
            return null;
        }
    }

    private async Task<string> PostFormAsync(string url, object param, CancellationToken cancellationToken)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(param);
        var encText = Encrypt(json);
        
        var formData = new Dictionary<string, string>
        {
            { "params", encText },
            { "encSecKey", EncSecKey }
        };

        var content = new FormUrlEncodedContent(formData);
        var response = await _client.PostAsync(url, content, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string Encrypt(string text)
    {
        var first = AesEncrypt(text, Ig, Key);
        return AesEncrypt(first, Iv, Key);
    }

    private static string AesEncrypt(string plainText, string key, string iv)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var ivBytes = Encoding.UTF8.GetBytes(iv);
        
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    private static string GetDefaultCookie()
    {
        return "osver=undefined; deviceId=undefined; appver=8.10.05; versioncode=140; mobilename=undefined; buildver=1699788225; resolution=1920x1080; __csrf=; os=android; channel=undefined; requestId=1699788225483_0541;MUSIC_U=00DCE90E4414477F6CEB1D678986B3E798756DC0B3789AC24E863D0F1CDA8392E2191A215A72C87DD76D33AC15963F1D4581ADFAD71698E9CEE1F59205B30465327BD608B9A4C03E907A43561CC8BD9A21C0D400237A879F6E5CDEFED2B7ADD78FD44F6402E41100966CD15F655BE0C37A18D1103134FAAE42BE3D77AEB60D300BE1A2789E1B4F7EB956E1969D2CED89D57D629398263FB44214E8BF12D201B368A9DFF0B1AE062C24A80C57953E8D42B4FBDA2B11ADD2E8C87F230727EAB2D75DC85C3A8D033CF2ABD045131969431DF3BCC689B902402FF9A683CDF5C96EFF1FBFD2563BF50EDAFB2200C887A51F4FF10B4D14A5AA745BBD62DD7DB1C5EA183E3FE575795096A830BF3FA91D685B96E981718C1568BF95E2D9A146509FE4430570AF16B22DC144D77C61D654F90046F61DC210814E63661061EFA80136272A0DF51F97529AC412523D009391B77DAF29";
    }

    private static SongInfo? ParseSongInfo(JsonNode? node)
    {
        if (node is null) return null;

        var id = node["id"]?.GetValue<long>() ?? 0;
        var name = node["name"]?.GetValue<string>() ?? string.Empty;
        var picUrl = node["al"]?["picUrl"]?.GetValue<string>() ?? string.Empty;
        var album = node["al"]?["name"]?.GetValue<string>() ?? string.Empty;
        var duration = node["dt"]?.GetValue<int>() ?? 0;

        var artists = new List<string>();
        var arArray = node["ar"]?.AsArray();
        if (arArray is not null)
        {
            foreach (var ar in arArray)
            {
                var artistName = ar?["name"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(artistName))
                    artists.Add(artistName);
            }
        }

        return new SongInfo
        {
            Id = id.ToString(),
            Name = name,
            Artists = artists,
            Album = album,
            AlbumCover = picUrl,
            Duration = duration / 1000,
            Source = MusicSource.NetEase
        };
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
