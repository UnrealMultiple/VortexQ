using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Playlists;

public class OrderlistItem
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("musicid")]
    public int Musicid { get; set; }

    /// <summary>
    /// 叾屾
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
    [JsonPropertyName("encrypt_uin")]
    public string EncryptUin { get; set; } = string.Empty;
}
