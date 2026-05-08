using System.Text.Json.Serialization;

namespace Music.QQ.Internal;

public class Response
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ts")]
    public long Ts { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("start_ts")]
    public long StartTs { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("traceid")]
    public string Traceid { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("req")]
    public Req Req { get; set; } = new();
}
