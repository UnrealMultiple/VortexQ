using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Search.Song;

public class Volume
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("gain")]
    public double Gain { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("lra")]
    public double Lra { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("peak")]
    public double Peak { get; set; }
}
