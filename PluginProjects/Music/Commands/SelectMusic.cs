using Lagrange.Core.Message;
using Music.Models;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Extension;

namespace Music.Commands;

[Command("选")]
[CommandType(CommandType.Group)]
[HelpText("选择音乐")]
public static class SelectMusic
{
    [Main]
    public static async Task Execute(GroupCommandArgs args, int index)
    {
        var result = args.Verify("music_select");

        if (result.Success && result.Verification?.Data != null)
        {
            dynamic data = result.Verification.Data;
            IReadOnlyList<SongInfo> songs = data.musics;
            var userSource = Config.Instance.GetUserSource(args.SenderUin.ToString());
            if(index < 0 || index >= songs.Count)
            {
                await args.ReplyWithAtAsync("请输入一个正确的序列号");
                return;
            }

            var song = songs[index];
            var playUrl = await Music.Instance.MusicService.GetPlayUrlAsync(song.Id, userSource);
            if(playUrl == null)
            {
                await args.ReplyWithAtAsync("无法获取歌曲播放链接！");
                return;
            }
            var type = song.Source switch
            {
                MusicSource.QQMusic => "qq",
                MusicSource.NetEase => "163",
                _ => "kugou",
            };
            

            var json = await MusicSigner.Sign(new MusicSigSegment(type, song.PageUrl, playUrl, song.AlbumCover, song.Name, song.ArtistString));
            if(json == null)
            {
                await args.ReplyWithAtAsync("无法获取歌曲签名信息！");
                return;
            }
            await args.ReplyAsync(MessageBuilder.Create().LightApp(json).Build());
        }
        else
        {
            await args.ReplyWithAtAsync($"❌ {result.Message}");
        }
    }
}
