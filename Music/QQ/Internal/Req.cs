using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Music.QQ.Internal;

public class Req
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("data")]
    public JsonNode? Data { get; set; }
}
