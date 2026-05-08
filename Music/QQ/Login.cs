using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Music.QQ.Enums;
using Music.QQ.Internal;
using Music.QQ.Internal.MusicToken;

namespace Music.QQ;

public partial class Login
{
    private const string QrLoginApi = "https://ssl.ptlogin2.qq.com/ptqrshow";

    private const string CheckQrLoginApi = "https://ssl.ptlogin2.qq.com/ptqrlogin";

    private const string CheckSigApi = "https://ssl.ptlogin2.graph.qq.com/check_sig";

    private const string AuthorizeApi = "https://graph.qq.com/oauth2.0/authorize";

    internal const string QQMusicApi = "https://u.y.qq.com/cgi-bin/musicu.fcg";

    private static readonly CookieContainer cookie = new();

    private static readonly HttpClientHandler httpClientHandler = new()
    {
        CookieContainer = cookie,
        UseCookies = true
    };

    private static readonly HttpClient client = new(httpClientHandler)
    {
        DefaultRequestHeaders = { { "Referer", "https://xui.ptlogin2.qq.com/" } }
    };

    public static async Task<(string, byte[])> GetLoginQrcode()
    {
        // 创建一个随机数生成器
        Random random = new();

        // 定义字典并添加键值对
        var paramsDict = new Dictionary<string, string>
        {
            { "appid", "716027609" },
            { "e", "2" },
            { "l", "M" },
            { "s", "3" },
            { "d", "72" },
            { "v", "4" },
            { "t", random.NextDouble().ToString() }, // 生成一个0到1之间的随机数并转换为字符串
            { "daid", "383" },
            { "pt_3rd_aid", "100497308" }
        };

        var uri = new Uri(Utils.QueryUri(QrLoginApi, paramsDict));
        var res = await client.GetAsync(uri);
        var bytes = await res.Content.ReadAsByteArrayAsync();
        var qrsig = cookie.GetCookies(uri)["qrsig"] ?? throw new ArgumentNullException("获二维码失败!");
        return (qrsig.Value, bytes);
    }


    public static async Task<TokenInfo> GetQQMusicToken(string code, int gtk)
    {
        // 创建匿名类表示JSON对象
        var request = new
        {
            comm = new
            {
                g_tk = gtk,
                platform = "yqq",
                ct = 24,
                cv = 0
            },
            req = new
            {
                module = "QQConnectLogin.LoginServer",
                method = "QQLogin",
                param = new
                {
                    code
                }
            }
        };
        var url = new Uri(QQMusicApi);
        var content = new StringContent(JsonSerializer.Serialize(request));
        var response = await client.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();
        var responseObj = JsonSerializer.Deserialize<Response>(json);
        if (responseObj?.Req?.Data == null) throw new Exception("Token获取失败!");
        return JsonSerializer.Deserialize<TokenInfo>(responseObj.Req.Data.ToJsonString()) ?? throw new Exception("Token获取失败!");
    }

    public static async Task<string> Authorize(string pskey)
    {
        // 创建字典并填充静态值
        var parameters = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "client_id", "100497308" },
            { "redirect_uri", "https://y.qq.com/portal/wx_redirect.html?login_type=1&surl=https%3A%252F%252Fy.qq.com%252F" },
            { "scope", "get_user_info,get_app_friends" },
            { "state", "state" },
            { "switch", "" },
            { "from_ptlogin", "1" },
            { "src", "1" },
            { "update_auth", "1" },
            { "openapi", "1010_1030" },
            { "ui", Guid.NewGuid().ToString().ToUpper() },
            { "auth_time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() },
            { "g_tk", Utils.Hash33(pskey, 5381).ToString() }
        };
        var uri = new Uri(AuthorizeApi);
        var response = await client.PostAsync(uri, new FormUrlEncodedContent(parameters));
        var local = response.RequestMessage?.RequestUri?.AbsoluteUri ?? throw new ArgumentNullException("Authorize failed!");
        var match = RegexHelper.RegexCode().Match(local);
        return match.Success ? match.Value : throw new ArgumentNullException("Authorize failed!");
    }

    public static async Task<string> CheckSig(string uin, string sigx)
    {
        var Parameters = new Dictionary<string, string>
        {
            { "uin", uin },
            { "pttype", "1" },
            { "service", "ptqrlogin" },
            { "nodirect", "0" },
            { "ptsigx", sigx },
            { "s_url", "https://graph.qq.com/oauth2.0/login_jump" },
            { "ptlang", "2052" },
            { "ptredirect", "100" },
            { "aid", "716027609" },
            { "daid", "383" },
            { "j_later", "0" },
            { "low_login_hour", "0" },
            { "regmaster", "0" },
            { "pt_login_type", "3" },
            { "pt_aid", "0" },
            { "pt_aaid", "16" },
            { "pt_light", "0" },
            { "pt_3rd_aid", "100497308" }
        };
        var uri = new Uri(Utils.QueryUri(CheckSigApi, Parameters));
        await client.GetAsync(uri);
        return cookie.GetCookies(uri)["p_skey"]?.Value ?? throw new ArgumentNullException("CheckSig failed!");
    }

    public static async Task CheckLoginQrcode(string qrsig, int timeOut, Action<QrcodeLoginType, TokenInfo?> action)
    {
        using CancellationTokenSource cts = new();
        Task timerTask = RunTimerAsync(qrsig, action, cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(timeOut)).ConfigureAwait(false);
        cts.Cancel();
        try
        {
            await timerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            action(QrcodeLoginType.CANCEL, null);
        }
    }

    private static async Task RunTimerAsync(string qrsig, Action<QrcodeLoginType, TokenInfo?> action, CancellationToken token)
    {
        TimeSpan interval = TimeSpan.FromSeconds(3);
        while (!token.IsCancellationRequested)
        {
            var state = await CheckLoginQrcode(qrsig, action);
            if (state == QrcodeLoginType.DONE || state == QrcodeLoginType.REFUSE || state == QrcodeLoginType.OTHER)
            {
                return;
            }
            await Task.Delay(interval, token).ConfigureAwait(false);
        }
    }


    private static async Task<QrcodeLoginType> CheckLoginQrcode(string qrsig, Action<QrcodeLoginType, TokenInfo?> action)
    {
        // 创建并填充字典
        var parameters = new Dictionary<string, string>
        {
            { "u1", "https://graph.qq.com/oauth2.0/login_jump" },
            { "ptqrtoken", Utils.Hash33(qrsig).ToString() }, // 注意：这可能需要根据实际情况生成或获取
            { "ptredirect", "0" },
            { "h", "1" },
            { "t", "1" },
            { "g", "1" },
            { "from_ui", "1" },
            { "ptlang", "2052" },
            { "action", $"0-0-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" }, // 使用当前时间戳
            { "js_ver", "20102616" },
            { "js_type", "1" },
            { "pt_uistyle", "40" },
            { "aid", "716027609" },
            { "daid", "383" },
            { "pt_3rd_aid", "100497308" },
            { "has_onekey", "1" }
        };
        var url = Utils.QueryUri(CheckQrLoginApi, parameters);
        var res = await client.GetAsync(url);
        if (!res.IsSuccessStatusCode) throw new HttpRequestException("CheckLoginQrcode failed!");
        var results = await res.Content.ReadAsStringAsync();
        var match = RegexHelper.RegexState().Match(results);
        if (!match.Success) throw new Exception("CheckLoginQrcode failed!");
        var val = match.Groups[1].Value;
        var data = val.Split(",").Select(s => s.Trim('\'')).ToArray();
        var cookieuri = new Uri(QQMusicApi);
        var state = data[0] switch
        {
            "0" => QrcodeLoginType.DONE,
            "65" => QrcodeLoginType.TIMEOUT,
            "66" => QrcodeLoginType.SCAN,
            "67" => QrcodeLoginType.CONF,
            "68" => QrcodeLoginType.REFUSE,
            _ => QrcodeLoginType.OTHER
        };
        if (state == QrcodeLoginType.DONE)
        {
            var sigx = RegexHelper.RegexSigx().Match(data[2]).Groups[1].Value;
            var uin = RegexHelper.RegexUin().Match(data[2]).Groups[1].Value;
            var pskey = await CheckSig(uin, sigx);
            var code = await Authorize(pskey);
            var token = await GetQQMusicToken(code, Utils.Hash33(pskey));
            token.Cookie = cookie.GetCookies(cookieuri).ToDictionary(c => c.Name, c => c.Value);
            token.P_Skey = pskey;
            action(state, token);
        }
        else
        {
            action(state, null);
        }
        return state;
    }
}
