using Lagrange.Core.Message;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Extension;
using Vortex.Bot.Utility;

namespace Daily;

[Command("绘图")]
[CommandType(CommandType.Group)]
[HelpText("根据提示词绘制图片")]
public static class DrawAIImageCommand
{
    private const string Url = "https://oiapi.net/api/AiDrawImage";

    private static readonly JsonSerializerOptions _option = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    [Main]
    [Flexible]
    public static async ValueTask ExecuteAsync(GroupCommandArgs args)
    {
        var prompt = args.Params.ToJoinedString("");
        if (string.IsNullOrEmpty(prompt))
        {
            await args.ReplyWithAtAsync("请至少给一个描述词");
            return;
        }

        var json = await HttpUtility.GetStringAsync(Url, new Dictionary<string, string>
        {
            ["prompt"] = prompt,
            ["style"] = "100",
            ["size"] = "2",
            ["llm"] = "true",
            ["type"] = "json"
        });

        var content = JsonSerializer.Deserialize<AIDrawResp>(json, _option)!;
        if(content.Code == 1)
        {
            var tasks = content.Data.Select(x => HttpUtility.GetByteAsync(x.Url));
            var r = await Task.WhenAll(tasks);
            var imsg = r.Select(x => BotMessage.CreateCustomGroup(args.GroupUin, args.SenderUin, args.Member?.Nickname ?? "", DateTime.Now, MessageBuilder.Create().Image(x).Build()));
            var pmsg = BotMessage.CreateCustomGroup(args.GroupUin, args.SenderUin, args.Member?.Nickname ?? "", DateTime.Now, MessageBuilder.Create().Text(prompt).Build());
            var msg = MessageBuilder.Create().MultiMsg([.. imsg.Prepend(pmsg)]).Build();
            await args.ReplyAsync(msg);
        }
    }
}

public class AIDrawResp
{
    public class ImageData
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("Url")]
        public string Url { get; set; } = string.Empty;

    }

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public ImageData[] Data { get; set; } = [];
}
