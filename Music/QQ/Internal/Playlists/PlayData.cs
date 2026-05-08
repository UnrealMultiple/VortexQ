using System.Text.Json.Serialization;
using Music.QQ.Internal.Search.Song;

namespace Music.QQ.Internal.Playlists;

public class PlayData
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("subcode")]
    public int Subcode { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("from_gedan_plaza")]
    public int FromGedanPlaza { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("accessed_plaza_cache")]
    public int AccessedPlazaCache { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("accessed_byfav")]
    public int AccessedByfav { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("optype")]
    public int Optype { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("filter_song_num")]
    public int FilterSongNum { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("sac_forbid")]
    public List<string> SacForbid { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("dirinfo")]
    public Dirinfo Dirinfo { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("songlist_size")]
    public int SonglistSize { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("songlist")]
    public List<SongData> Songlist { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("songtag")]
    public List<SongtagItem> Songtag { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("toplist_song")]
    public List<string> ToplistSong { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("toplist_nolimit")]
    public bool ToplistNolimit { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("login_uin")]
    public int LoginUin { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("invalid_song")]
    public List<string> InvalidSong { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("filtered_song")]
    public List<string> FilteredSong { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ad_list")]
    public List<string> AdList { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("total_song_num")]
    public int TotalSongNum { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("encrypt_login")]
    public string EncryptLogin { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ct")]
    public int Ct { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("cv")]
    public int Cv { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("orderlist")]
    public List<OrderlistItem> Orderlist { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("birthday")]
    public List<string> Birthday { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("aiExt")]
    public AiExt AiExt { get; set; } = new();

    ///// <summary>
    ///// 
    ///// </summary>
    //[JsonPropertyName("quickListenVid")]
    //public List<string> QuickListenVid { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("bitflag")]
    public int Bitflag { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("cmtURL_bykey")]
    public CmtURLBykey CmtURLBykey { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("srf_ip")]
    public string SrfIp { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("referer")]
    public string Referer { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("namedflag")]
    public int Namedflag { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("isAd")]
    public int IsAd { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("adTitle")]
    public string AdTitle { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("adUrl")]
    public string AdUrl { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("recomUgcValid")]
    public int RecomUgcValid { get; set; }
}
