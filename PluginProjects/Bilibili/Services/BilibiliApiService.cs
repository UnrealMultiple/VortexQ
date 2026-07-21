using System.Text.Json.Nodes;
using Bilibili.Models;
using Vortex.Bot.Utility;

namespace Bilibili.Services;

public class BilibiliApiService
{
    public async Task<VideoInfo> GetVideoInfoAsync(string queryParam, string value)
    {
        var url = $"https://api.bilibili.com/x/web-interface/view?{queryParam}={value}";
        var response = await HttpUtility.GetStringAsync(url);
        var json = JsonNode.Parse(response) ?? throw new Exception("API 返回为空");

        var code = (int?)json["code"] ?? throw new Exception("API 返回 code 为空");
        if (code != 0)
            throw new Exception($"Bilibili API 返回错误 code: {code}");

        var data = json["data"] ?? throw new Exception("API 返回 data 为空");
        var stat = data["stat"] ?? throw new Exception("API 返回 stat 为空");
        var owner = data["owner"] ?? throw new Exception("API 返回 owner 为空");
        var ctime = data["ctime"]?.GetValue<long>() ?? throw new Exception("API 返回 ctime 为空");

        return new VideoInfo
        {
            Id = queryParam == "bvid" ? value : $"av{value}",
            Title = data["title"]?.GetValue<string>() ?? "未知标题",
            Pic = data["pic"]?.GetValue<string>() ?? "",
            OwnerName = owner["name"]?.GetValue<string>() ?? "未知",
            Aid = data["aid"]?.GetValue<long>() ?? 0,
            PublishTime = DateTimeOffset.FromUnixTimeSeconds(ctime).DateTime.ToLocalTime(),
            View = stat["view"]?.GetValue<int>() ?? 0,
            Like = stat["like"]?.GetValue<int>() ?? 0,
            Coin = stat["coin"]?.GetValue<int>() ?? 0,
            Favorite = stat["favorite"]?.GetValue<int>() ?? 0,
            Share = stat["share"]?.GetValue<int>() ?? 0,
            Danmaku = stat["danmaku"]?.GetValue<int>() ?? 0,
            Reply = stat["reply"]?.GetValue<int>() ?? 0,
        };
    }

    public async Task<HotReplyInfo?> GetHotReplyAsync(long aid)
    {
        try
        {
            var url = $"https://api.bilibili.com/x/v2/reply?type=1&sort=1&ps=1&oid={aid}";
            var response = await HttpUtility.GetStringAsync(url);
            var json = JsonNode.Parse(response);

            var code = (int?)json?["code"] ?? -1;
            if (code != 0) return null;

            var replies = json?["data"]?["replies"]?.AsArray();
            if (replies is null || replies.Count == 0) return null;

            var reply = replies[0];
            var content = reply?["content"];
            if (content is null) return null;

            var hotReply = new HotReplyInfo
            {
                Message = content["message"]?.GetValue<string>() ?? ""
            };

            var pictures = content["pictures"]?.AsArray();
            if (pictures is not null)
            {
                foreach (var picture in pictures)
                {
                    var imgSrc = picture?["img_src"]?.GetValue<string>();
                    if (imgSrc is not null)
                        hotReply.ImageUrls.Add(imgSrc);
                }
            }

            return hotReply;
        }
        catch
        {
            return null;
        }
    }
}
