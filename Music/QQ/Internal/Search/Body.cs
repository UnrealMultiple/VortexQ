using System.Text.Json.Serialization;

namespace Music.QQ.Internal.Search;

public class Body
{
    ///// <summary>
    ///// 
    ///// </summary>
    //[JsonPropertyName("album")]
    //public Album Album { get; set; }

    ///// <summary>
    ///// 
    ///// </summary>
    //[JsonPropertyName("gedantip")]
    //public Gedantip Gedantip { get; set; }

    ///// <summary>
    ///// 
    ///// </summary>
    //[JsonPropertyName("mv")]
    //public Mv Mv { get; set; }

    ///// <summary>
    ///// 
    ///// </summary>
    //[JsonPropertyName("qc")]
    //public List<string> Qc { get; set; }

    ///// <summary>
    ///// 
    ///// </summary>
    //[JsonPropertyName("singer")]
    //public Singer Singer { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("song")]
    public Songs Songs { get; set; } = new();

    /// <summary>
    /// 
    ///// </summary>
    //[JsonPropertyName("songlist")]
    //public Songlist Songlist { get; set; }

    ///// <summary>
    ///// 
    ///// </summary>
    //[JsonPropertyName("user")]
    //public User User { get; set; }

    ///// <summary>
    ///// 
    ///// </summary>
    //[JsonPropertyName("zhida")]
    //public Zhida Zhida { get; set; }
}
