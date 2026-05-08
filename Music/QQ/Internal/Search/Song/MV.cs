using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Search.Song;

public class MV
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("list")]
    public List<string> List { get; set; } = [];
}
