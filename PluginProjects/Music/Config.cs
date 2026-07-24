using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Music.Kugou;
using Music.Models;
using Music.QQ.Internal.MusicToken;
using Vortex.Bot.Configuration;

namespace Music;

public class Config : JsonConfigBase<Config>
{
    public override string FileName => "Music";

    [JsonPropertyName("qqToken")]
    public TokenInfo? QQToken { get; set; }

    [JsonPropertyName("kugouToken")]
    public KugouToken? KugouToken { get; set; }

    [JsonPropertyName("defaultSource")]
    public string DefaultSource { get; set; } = "kugou";

    [JsonPropertyName("searchLimit")]
    public int SearchLimit { get; set; } = 10;

    [JsonPropertyName("autoRefreshToken")]
    public bool AutoRefreshToken { get; set; } = true;

    [JsonPropertyName("userSources")]
    public Dictionary<string, string> UserSources { get; set; } = new();

    private static ILogger? Logger => Music.Instance?.GetLogger();

    public void SetQQToken(TokenInfo token)
    {
        QQToken = token;
        Save();
        Logger?.LogInformation("[Music] QQ音乐令牌已更新");
    }

    public void SetKugouToken(KugouToken token)
    {
        KugouToken = token;
        Save();
        Logger?.LogInformation("[Music] 酷狗音乐令牌已更新");
    }

    public MusicSource GetUserSource(string userId)
    {
        if (UserSources.TryGetValue(userId, out var source))
        {
            return source.ToLower() switch
            {
                "qq" => MusicSource.QQMusic,
                "netease" => MusicSource.NetEase,
                "kugou" => MusicSource.Kugou,
                _ => MusicSource.Kugou
            };
        }
        return DefaultSource.ToLower() switch
        {
            "qq" => MusicSource.QQMusic,
            "netease" => MusicSource.NetEase,
            "kugou" => MusicSource.Kugou,
            _ => MusicSource.Kugou
        };
    }

    public void SetUserSource(string userId, string source)
    {
        UserSources[userId] = source.ToLower();
        Save();
    }
}
