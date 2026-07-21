using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Search.Song;

public class KSong
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("mid")]
    public string Mid { get; set; } = string.Empty;
}
