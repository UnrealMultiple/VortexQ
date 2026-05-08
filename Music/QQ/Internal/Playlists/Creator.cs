using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Playlists;

public class Creator
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("musicid")]
    public long Musicid { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("singerid")]
    public int Singerid { get; set; }

    /// <summary>
    /// 人间忧伤
    /// </summary>
    [JsonPropertyName("nick")]
    public string Nick { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("headurl")]
    public string Headurl { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ifpicurl")]
    public string Ifpicurl { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("encrypt_uin")]
    public string EncryptUin { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("isVip")]
    public int IsVip { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ai_uin")]
    public long AiUin { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("encrypt_ai_uin")]
    public string EncryptAiUin { get; set; } = string.Empty;
}
