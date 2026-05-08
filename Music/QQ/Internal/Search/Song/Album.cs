using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Search.Song;

public class Album
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
    /// 七里香
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("pmid")]
    public string Pmid { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("time_public")]
    public string TimePublic { get; set; } = string.Empty;

    /// <summary>
    /// 七里香
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonIgnore]
    public string Picture
    {
        get => $"https://y.gtimg.cn/music/photo_new/T002R800x800M000{Mid}.jpg";
        set { }
    }
}
