namespace Bilibili.Services;

public class VideoUrlResolver
{
    public enum VideoIdType { B23, Bv, Av, None }

    public record struct VideoIdResult(VideoIdType Type, string Id);

    public VideoIdResult Detect(string text)
    {
        var b23 = BilibiliHelpers.B23_Regex().Match(text);
        if (b23.Success)
            return new(VideoIdType.B23, b23.Groups["B23"].Value);

        var bv = BilibiliHelpers.Bv_Regex().Match(text);
        if (bv.Success)
            return new(VideoIdType.Bv, bv.Groups["BVID"].Value);

        var av = BilibiliHelpers.Av_Regex().Match(text);
        if (av.Success)
            return new(VideoIdType.Av, av.Groups["AID"].Value);

        return new(VideoIdType.None, string.Empty);
    }

    public async Task<string> ResolveB23Async(string b23Code)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync($"https://{b23Code}");
        response.EnsureSuccessStatusCode();
        var finalUri = response.RequestMessage?.RequestUri?.OriginalString
                       ?? throw new Exception("无法解析 b23.tv 短链接");
        var match = BilibiliHelpers.BVIDRegex().Match(finalUri);
        return match.Success
            ? match.Groups["BVID"].Value
            : throw new Exception("b23.tv 链接中未找到 BV 号");
    }
}
