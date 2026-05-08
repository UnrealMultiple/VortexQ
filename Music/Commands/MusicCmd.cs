using Microsoft.Extensions.Logging;
using Music.Models;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;

namespace Music.Commands;

[Command("点歌", "music")]
[HelpText("搜索QQ音乐和网易云音乐")]
[CommandType(CommandType.Group)]
public static class MusicCmd
{
    [Main]
    public static async Task Execute(GroupCommandArgs args, [Param("歌曲名称")] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            await args.ReplyWithAtAsync("请输入歌曲名称");
            return;
        }

        try
        {
            var userSource = Config.Instance.GetUserSource(args.SenderUin.ToString());
            var songs = await Music.Instance.MusicService.SearchBySourceAsync(keyword, userSource);

            if (songs.Count == 0)
            {
                await args.ReplyWithAtAsync($"未找到与 \"{keyword}\" 相关的歌曲");
                return;
            }

            args.CreateVerification("music_select", nameof(Music), 60, new
            {
                musics = songs,
            });

            var message = FormatSearchResults(songs, userSource);
            await args.ReplyWithAtAsync(message);
            _ = args.StartVerificationTimeoutAsync("transfer", async (v) =>
            {
                await args.ReplyWithAtAsync($"点歌超时，操作已取消。");
            });
        }
        catch (Exception ex)
        {
            args.Logger.LogError(ex, "搜索歌曲失败: {Keyword}", keyword);
            await args.ReplyWithAtAsync("搜索失败，请稍后重试");
        }
    }

    private static string FormatSearchResults(IReadOnlyList<SongInfo> songs, MusicSource source)
    {
        var lines = new List<string>();
        
        var sourceText = source switch
        {
            MusicSource.QQMusic => "[QQ音乐] ",
            MusicSource.NetEase => "[网易云] ",
            _ => ""
        };
        
        lines.Add($"{sourceText}找到 {songs.Count} 首歌曲：");

        for (int i = 0; i < songs.Count; i++)
        {
            var song = songs[i];
            var prefix = song.Source switch
            {
                MusicSource.QQMusic => "[QQ] ",
                MusicSource.NetEase => "[网易] ",
                _ => ""
            };

            lines.Add($"{i + 1}. {prefix}{song.DisplayText}");
        }

        return string.Join("\n", lines);
    }
}
