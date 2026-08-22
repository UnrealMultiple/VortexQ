using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Vortex.Bot.Utility;

namespace Music;

public class MusicSigner
{
    private static readonly string _signServer = "https://ss.xingzhige.com/music_card/card";

    private static readonly HttpClient _client = new();

    public static async Task<string?> Sign(MusicSigSegment musicSigSegment)
    {
        if (string.IsNullOrEmpty(_signServer)) return null;

        JsonObject payload;

        payload = new JsonObject()
        {
            { "type" , musicSigSegment.Type },
            { "url" , musicSigSegment.Url },
            { "audio" , musicSigSegment.Audio },
            { "title" , musicSigSegment.Title },
            { "image" , musicSigSegment.Image },
            { "singer" , musicSigSegment.Content },
        };
        try
        {
            HttpResponseMessage message = _client.PostAsJsonAsync(_signServer, payload).Result;
            return await message.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }
    }
}

//public class MusicSigner
//{
//    private static readonly string _signServer = "https://apii.xianyuw.cn/api/v1/qq-musicArk";

//    private static readonly HttpClient _client = new();

//    public static async Task<string?> Sign(MusicSigSegment musicSigSegment)
//    {
//        if (string.IsNullOrEmpty(_signServer)) return null;


//        var payload = new Dictionary<string, string>()
//        {
//            { "key", "sk-584b9698a5b96e88a06972575cd65a31" },
//            { "format" , musicSigSegment.Type },
//            { "jump" , musicSigSegment.Url },
//            { "url" , musicSigSegment.Audio },
//            { "song" , musicSigSegment.Title },
//            { "cover" , musicSigSegment.Image },
//            { "singer" , musicSigSegment.Content },
//        };
//        var responseText = await HttpUtility.GetStringAsync(_signServer, payload);
//        JsonNode? response = JsonNode.Parse(responseText);
//        if (response is not null && response["code"]?.GetValue<int>() == 200)
//        {
//            return response["data"]?.ToJsonString();
//        }
//        throw new Exception(response?["msg"]?.GetValue<string>() ?? "unknown error");
//    }
//}

public class MusicSigSegment(string type, string url, string Audio, string image, string title, string content)
{
    public string Type { get; set; } = type;

    public string Url { get; set; } = url;

    public string Audio { get; set; } = Audio;

    public string Title { get; set; } = title;

    public string Image { get; set; } = image;

    public string Content { get; set; } = content;
}
