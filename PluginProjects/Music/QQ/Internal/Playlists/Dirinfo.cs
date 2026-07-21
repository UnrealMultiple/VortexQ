using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Playlists;

public class Dirinfo
{

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("host_uin")]
    public long HostUin { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("dirid")]
    public int Dirid { get; set; }

    /// <summary>
    /// 伤感DJ：你何必假装快乐
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("picurl")]
    public string Picurl { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("picid")]
    public int Picid { get; set; }

    /// <summary>
    /// 你何必假装快乐?我又何必真心难过.
    /// </summary>
    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("vec_tagid")]
    public List<int> VecTagid { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("vec_tagname")]
    public List<string> VecTagname { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ctime")]
    public int Ctime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("mtime")]
    public int Mtime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("listennum")]
    public int Listennum { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ordernum")]
    public int Ordernum { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("picmid")]
    public string Picmid { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("dirtype")]
    public int Dirtype { get; set; }

    /// <summary>
    /// 人间忧伤
    /// </summary>
    [JsonPropertyName("host_nick")]
    public string HostNick { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("songnum")]
    public int Songnum { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ordertime")]
    public int Ordertime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("show")]
    public int Show { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("picurl2")]
    public string Picurl2 { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("song_update_time")]
    public int SongUpdateTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("song_update_num")]
    public int SongUpdateNum { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("disstype")]
    public int Disstype { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ai_uin")]
    public long AiUin { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("dv2")]
    public int Dv2 { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("dir_show")]
    public int DirShow { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("encrypt_uin")]
    public string EncryptUin { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("encrypt_ai_uin")]
    public string EncryptAiUin { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("owndir")]
    public int Owndir { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("headurl")]
    public string Headurl { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("tag")]
    public List<string> Tag { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("creator")]
    public Creator Creator { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("edge_mark")]
    public string EdgeMark { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("layer_url")]
    public string LayerUrl { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ext1")]
    public string Ext1 { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ext2")]
    public string Ext2 { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("origin_title")]
    public string OriginTitle { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ad_tag")]
    public bool AdTag { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("aiToast")]
    public string AiToast { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("role")]
    public int Role { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("rl2")]
    public int Rl2 { get; set; }
}
