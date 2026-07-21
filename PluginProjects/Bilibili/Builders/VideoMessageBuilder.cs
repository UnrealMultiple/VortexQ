using System.Text;
using Bilibili.Models;
using Lagrange.Core.Message;
using Vortex.Bot.Extension;
using Vortex.Bot.Utility;

namespace Bilibili.Builders;

public static class VideoMessageBuilder
{
    public static MessageBuilder Build(VideoInfo video, HotReplyInfo? hotReply)
    {
        var builder = MessageBuilder.Create();

        var sb = new StringBuilder();
        sb.AppendLine($"https://www.bilibili.com/video/{video.Id}");
        sb.AppendLine($"发布时间：{video.PublishTime:yyyy-MM-dd HH:mm:ss}");
        sb.Append($"UP主:{video.OwnerName} | ");
        sb.Append($"播放量:{video.View} | ");
        sb.Append($"点赞:{video.Like} | ");
        sb.Append($"投币:{video.Coin} | ");
        sb.Append($"收藏:{video.Favorite} | ");
        sb.Append($"转发:{video.Share} | ");
        sb.Append($"弹幕:{video.Danmaku} | ");
        sb.Append($"评论:{video.Reply} ");

        builder.Image(HttpUtility.GetByteAsync(video.Pic).Result);
        builder.Text(video.Title);
        builder.Text(sb.ToString().Trim());

        if (hotReply is not null)
        {
            var replySb = new StringBuilder();
            replySb.AppendLine("热评：");
            replySb.AppendLine(hotReply.Message);
            builder.Text(replySb.ToString().Trim());

            foreach (var imgUrl in hotReply.ImageUrls)
            {
                builder.Image(HttpUtility.GetByteAsync(imgUrl).Result);
            }
        }

        return builder;
    }
}
