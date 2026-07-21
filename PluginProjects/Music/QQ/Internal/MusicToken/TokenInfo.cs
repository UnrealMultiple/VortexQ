using System.Text.Json.Serialization;

namespace Music.QQ.Internal.MusicToken;

public class TokenInfo
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("openid")]
    public string Openid { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("expired_at")]
    public int ExpiredAt { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("musicid")]
    public long Musicid { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("musickey")]
    public string Musickey { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("musickeyCreateTime")]
    public int MusickeyCreateTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("first_login")]
    public int FirstLogin { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("errMsg")]
    public string ErrMsg { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("sessionKey")]
    public string SessionKey { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("unionid")]
    public string Unionid { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("str_musicid")]
    public string StrMusicid { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("errtip")]
    public string Errtip { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("nick")]
    public string Nick { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("logo")]
    public string Logo { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("feedbackURL")]
    public string FeedbackURL { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("encryptUin")]
    public string EncryptUin { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("userip")]
    public string Userip { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("lastLoginTime")]
    public int LastLoginTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("keyExpiresIn")]
    public int KeyExpiresIn { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("refresh_key")]
    public string RefreshKey { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("loginType")]
    public int LoginType { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("prompt2bind")]
    public int Prompt2bind { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("logoffStatus")]
    public int LogoffStatus { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("otherAccounts")]
    public List<string> OtherAccounts { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("otherPhoneNo")]
    public string OtherPhoneNo { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("isPrized")]
    public int IsPrized { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("isShowDevManage")]
    public int IsShowDevManage { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("errTip2")]
    public string ErrTip2 { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("tip3")]
    public string Tip3 { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("encryptedPhoneNo")]
    public string EncryptedPhoneNo { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("phoneNo")]
    public string PhoneNo { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("bindAccountType")]
    public int BindAccountType { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("needRefreshKeyIn")]
    public int NeedRefreshKeyIn { get; set; }

    [JsonPropertyName("p_skey")]
    public string P_Skey { get; set; } = string.Empty;

    [JsonPropertyName("cookie")]
    public Dictionary<string, string> Cookie { get; set; } = [];
}
