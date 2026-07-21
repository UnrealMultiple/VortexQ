using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Search.Song;

public class Pay
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("pay_down")]
    public int PayDown { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("pay_month")]
    public int PayMonth { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("pay_play")]
    public int PayPlay { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("pay_status")]
    public int PayStatus { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("price_album")]
    public int PriceAlbum { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("price_track")]
    public int PriceTrack { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("time_free")]
    public int TimeFree { get; set; }
}
