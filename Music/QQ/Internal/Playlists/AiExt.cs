using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Playlists;

public class AiExt
{
    /// <summary>
    /// 
    /// </summary>
    public int CountdownTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool ISJoinExp { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("blkCntDnlist")]
    public List<string> BlkCntDnlist { get; set; } = [];
}
