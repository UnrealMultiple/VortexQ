using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Playlists;

public class SongtagItem
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 昨日热播
    /// </summary>
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("tagid")]
    public int Tagid { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("from_type")]
    public int FromType { get; set; }
}
