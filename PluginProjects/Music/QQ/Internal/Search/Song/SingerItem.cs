using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Search.Song;

public class SingerItem
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

    /// <summary>
    /// 周杰伦
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("pmid")]
    public string Pmid { get; set; } = string.Empty;

    /// <summary>
    /// 周杰伦
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("uin")]
    public long Uin { get; set; }
}
