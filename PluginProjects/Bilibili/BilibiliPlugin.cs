using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Events.EventArgs;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entities;
using System.Text.Json.Nodes;
using Bilibili.Builders;
using Bilibili.Services;
using Vortex.Bot.Extension;
using Vortex.Bot.Plugins;

namespace Bilibili;

[Plugin(Name = "Bilibili", Author = "VortexQ", Description = "Bilibili插件", Major = 1, Minor = 0)]
public class BilibiliPlugin : PluginBase
{
    private BilibiliApiService _apiService = null!;
    private VideoUrlResolver _urlResolver = null!;

    protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken)
    {
        _apiService = new BilibiliApiService();
        _urlResolver = new VideoUrlResolver();
        Vortex.BotContext.EventInvoker.RegisterEvent<BotMessageEvent>(OnMessage);
        return ValueTask.CompletedTask;
    }

    private async Task OnMessage(BotContext ctx, BotMessageEvent e)
    {
        if (e.Message.Type != MessageType.Group) return;
        if (e.Message.Contact is not BotGroupMember groupMember || groupMember.Uin == ctx.BotUin) return;

        var text = ExtractText(e.Message);
        var result = _urlResolver.Detect(text);
        if (result.Type == VideoUrlResolver.VideoIdType.None) return;

        try
        {
            var message = await HandleVideoAsync(result);
            if (message is not null)
                await Vortex.BotContext.SendGroupMessage(groupMember.Group.Uin, message.Build());
        }
        catch (Exception ex)
        {
            var errorMsg = MessageBuilder.Create().Text(ex.Message);
            await Vortex.BotContext.SendGroupMessage(groupMember.Group.Uin, errorMsg.Build());
        }
    }

    private static string ExtractText(BotMessage message)
    {
        var text = message.Entities.GetEnitys<TextEntity>()
            .Select(x => x.Text)
            .Aggregate(string.Empty, (current, next) => current + next);

        if (message.Entities.GetEnitys<LightAppEntity>().FirstOrDefault() is LightAppEntity json)
        {
            var data = JsonNode.Parse(json.Payload);
            var appid = data?["meta"]?["detail_1"]?["appid"];
            if (appid?.ToString() == "1109937557")
                text = data?["meta"]?["detail_1"]?["qqdocurl"]?.ToString() ?? text;
        }

        return text;
    }

    private async Task<MessageBuilder?> HandleVideoAsync(VideoUrlResolver.VideoIdResult result)
    {
        string? bvid = null;
        string? aid = null;

        switch (result.Type)
        {
            case VideoUrlResolver.VideoIdType.B23:
                bvid = await _urlResolver.ResolveB23Async(result.Id);
                break;
            case VideoUrlResolver.VideoIdType.Bv:
                bvid = result.Id;
                break;
            case VideoUrlResolver.VideoIdType.Av:
                aid = result.Id;
                break;
        }

        var videoInfo = bvid is not null
            ? await _apiService.GetVideoInfoAsync("bvid", bvid)
            : await _apiService.GetVideoInfoAsync("aid", aid!);

        var hotReply = await _apiService.GetHotReplyAsync(videoInfo.Aid);
        return VideoMessageBuilder.Build(videoInfo, hotReply);
    }

    protected override ValueTask OnShutdownAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
