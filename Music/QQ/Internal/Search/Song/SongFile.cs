using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Search.Song;

public class SongFile
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("b_30s")]
    public int B30s { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("e_30s")]
    public int E30s { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("hires_bitdepth")]
    public int HiresBitdepth { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("hires_sample")]
    public int HiresSample { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("media_mid")]
    public string MediaMid { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_128mp3")]
    public int Size128mp3 { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_192aac")]
    public int Size192aac { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_192ogg")]
    public int Size192ogg { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_24aac")]
    public int Size24aac { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_320mp3")]
    public int Size320mp3 { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_360ra")]
    public List<string> Size360ra { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_48aac")]
    public int Size48aac { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_96aac")]
    public int Size96aac { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_96ogg")]
    public int Size96ogg { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_ape")]
    public int SizeApe { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_dolby")]
    public int SizeDolby { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_dts")]
    public int SizeDts { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_flac")]
    public int SizeFlac { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_hires")]
    public int SizeHires { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_new")]
    public List<int> SizeNew { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("size_try")]
    public int SizeTry { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("try_begin")]
    public int TryBegin { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("try_end")]
    public int TryEnd { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
