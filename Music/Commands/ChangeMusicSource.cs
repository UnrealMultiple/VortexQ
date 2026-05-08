using Microsoft.Extensions.Logging;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;

namespace Music.Commands;

[Command("切换音源", "source")]
[HelpText("切换个人音乐搜索源 (qq/netease/all)")]
[CommandType(CommandType.Group)]
public static class ChangeMusicSource
{
    [Main]
    public static async Task Execute(
        GroupCommandArgs args,
        [Param("音源名称 (qq/netease/all)")] string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            await args.ReplyWithAtAsync("请指定音源 (qq/netease/all)");
            return;
        }

        try
        {
            var lowerSource = source.ToLower();
            var sourceName = lowerSource switch
            {
                "qq" => "QQ音乐",
                "netease" => "网易云音乐",
                _ => null
            };

            if (sourceName is null)
            {
                await args.ReplyWithAtAsync("不支持的音源，请使用 qq / netease");
                return;
            }

            var userId = args.Member?.Uin.ToString() ?? "0";
            Config.Instance.SetUserSource(userId, lowerSource);

            await args.ReplyWithAtAsync($"✅ 已设置个人音源为: {sourceName}");
        }
        catch (Exception ex)
        {
            args.Logger.LogError(ex, "切换音源失败");
            await args.ReplyWithAtAsync("❌ 切换音源失败，请稍后重试");
        }
    }
}
