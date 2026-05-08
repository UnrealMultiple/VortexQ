using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Search.Song;

public class Hotness
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("jump_type")]
    public int JumpType { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("jump_url")]
    public string JumpUrl { get; set; } = string.Empty;
}
