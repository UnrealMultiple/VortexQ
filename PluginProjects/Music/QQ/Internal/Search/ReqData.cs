using System.Text.Json;
using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Search;

internal class ReqData
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("body")]
    public Body Body { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("feedbackURL")]
    public string FeedbackURL { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("meta")]
    public JsonElement? Meta { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ver")]
    public int Ver { get; set; }
}
