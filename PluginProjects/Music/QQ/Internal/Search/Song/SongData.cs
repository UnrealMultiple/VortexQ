using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Search.Song;

public class SongData
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("act")]
    public int Act { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("album")]
    public Album Album { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("bpm")]
    public int Bpm { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("desc_hilight")]
    public string DescHilight { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("docid")]
    public string Docid { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("eq")]
    public int Eq { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("es")]
    public string Es { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("file")]
    public SongFile File { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("fnote")]
    public int Fnote { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("genre")]
    public int Genre { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("grp")]
    public List<string> Grp { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("hotness")]
    public Hotness Hotness { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("href3")]
    public string Href3 { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("index_album")]
    public int IndexAlbum { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("index_cd")]
    public int IndexCd { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("interval")]
    public int Interval { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("isonly")]
    public int Isonly { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ksong")]
    public KSong Ksong { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("language")]
    public int Language { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("lyric")]
    public string Lyric { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("lyric_hilight")]
    public string LyricHilight { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("mid")]
    public string Mid { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("mv")]
    public MV Mv { get; set; } = new();

    /// <summary>
    /// 搁浅
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("newStatus")]
    public int NewStatus { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ov")]
    public int Ov { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("pay")]
    public Pay Pay { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("protect")]
    public int Protect { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("sa")]
    public int Sa { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("singer")]
    public List<SingerItem> Singer { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("tag")]
    public int Tag { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("tid")]
    public int Tid { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("time_public")]
    public string TimePublic { get; set; } = string.Empty;

    /// <summary>
    /// 搁浅
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 搁浅
    /// </summary>
    [JsonPropertyName("title_hilight")]
    public string TitleHilight { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("vf")]
    public List<double> Vf { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("vi")]
    public List<int> Vi { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("volume")]
    public Volume Volume { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("vs")]
    public List<string> Vs { get; set; } = [];

    [JsonIgnore]
    public string PlayUrl { get; set; } = string.Empty;

    [JsonIgnore]
    public string PageUrl => $"https://y.qq.com/n/yqq/song/{Mid}.html";
}
