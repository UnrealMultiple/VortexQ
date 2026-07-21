using System.Text.Json.Serialization;
using Music.QQ.Internal.Search.Song;

namespace Music.QQ.Internal.Search;

public class Songs
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("list")]
    public List<SongData> List { get; set; } = [];
}
