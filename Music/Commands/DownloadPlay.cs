using Microsoft.Extensions.Logging;
using Music.Models;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;

namespace Music.Commands;

/// <summary>
/// 下载命令
/// </summary>
[Command("下载", "download")]
[HelpText("下载指定歌曲")]
[CommandType(CommandType.Group)]
public static class DownloadPlay
{
    [Main]
    public static async Task Execute(
        GroupCommandArgs args, 
        [Param("歌曲来源 (qq/netease)")] string source,
        [Param("歌曲ID")] string id)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(id))
        {
            await args.ReplyWithAtAsync("请提供歌曲来源和ID");
            return;
        }

        try
        {
            //var sourceName = source.ToLower() switch
            //{
            //    "qq" => "QQ音乐",
            //    "netease" => "网易云音乐",
            //    _ => null
            //};

            //if (sourceName is null)
            //{
            //    await args.ReplyWithAtAsync("不支持的音源，请使用 qq 或 netease");
            //    return;
            //}

            //await args.ReplyWithAtAsync($"正在获取 {sourceName} 歌曲信息...");
            
            //// 使用静态实例获取歌曲信息
            //SongInfo? song = source.ToLower() switch
            //{
            //    "qq" => await Music.Instance.QQProvider.GetSongDetailAsync(id),
            //    "netease" => await Music.Instance.NetEaseProvider.GetSongDetailAsync(id),
            //    _ => null
            //};

            //if (song is null)
            //{
            //    await args.ReplyWithAtAsync("未找到该歌曲");
            //    return;
            //}

            //await args.ReplyWithAtAsync($"✅ 歌曲链接已获取\n{song.DisplayText}\n来源: {sourceName}");
        }
        catch (Exception ex)
        {
            args.Logger.LogError(ex, "下载歌曲失败: {Source} {Id}", source, id);
            await args.ReplyWithAtAsync("❌ 下载失败，请稍后重试");
        }
    }
}
