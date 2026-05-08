using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

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

public class MusicSigSegment(string type, string url, string Audio, string image, string title, string content)
{
    public string Type { get; set; } = type;

    public string Url { get; set; } = url;

    public string Audio { get; set; } = Audio;

    public string Title { get; set; } = title;

    public string Image { get; set; } = image;

    public string Content { get; set; } = content;
}
