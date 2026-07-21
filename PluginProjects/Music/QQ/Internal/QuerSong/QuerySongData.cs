using System.Text.Json.Serialization;
using Music.QQ.Internal.Search.Song;

namespace Music.QQ.Internal.QuerSong;

public class QuerySongData
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("tracks")]
    public List<SongData> Tracks { get; set; } = [];
}
