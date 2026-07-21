using Lagrange.Core.Message;
using Lagrange.Core.Message.Entities;
using Microsoft.Extensions.Logging;
using Vortex.Bot;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;

namespace Reply;

[Command("image")]
[CommandType(CommandType.Group)]
[Permission("onebot.image.admin")]
[HelpText("图片管理 upload/list/del/url")]
public static class ImageCommand
{
    private static string ImagePath => Path.Combine(VortexContext.Path, "images");

    [Main]
    [Flexible]
    public static async Task Execute(GroupCommandArgs args)
    {
        if (args.Params.Count < 1)
        {
            await args.ReplyAsync($"语法错误，正确语法:" +
                $"\n{args.CommandPrefix}{args.CommandName} upload" +
                $"\n{args.CommandPrefix}{args.CommandName} list" +
                $"\n{args.CommandPrefix}{args.CommandName} url" +
                $"\n{args.CommandPrefix}{args.CommandName} del <文件名>");
            return;
        }
        switch (args.Params[0].ToLower())
        {
            case "upload": await Upload(args); break;
            case "list": await Images(args); break;
            case "del": await Delete(args); break;
            case "url": await GenerateUrl(args); break;
            default:
                await args.ReplyAsync("未知子命令");
                break;
        }
    }

    private static async Task GenerateUrl(GroupCommandArgs args)
    {
        var images = ReplyAdapter.GetImages(args.MessageChain!);
        if (images.Count == 0)
        {
            await args.ReplyWithAtAsync("请携带一张或多张图片!");
            return;
        }
        var lines = images.Select(i => i.FileUrl).Where(u => !string.IsNullOrEmpty(u));
        await args.ReplyAsync(string.Join("\n", lines));
    }

    private static async Task Delete(GroupCommandArgs args)
    {
        if (args.Params.Count < 2)
        {
            await args.ReplyWithAtAsync("请指定要删除的图片");
            return;
        }
        var name = args.Params[1];
        var path = Path.Combine(ImagePath, name);
        if (!File.Exists(path))
        {
            await args.ReplyWithAtAsync("图片不存在");
            return;
        }
        File.Delete(path);
        await args.ReplyWithAtAsync("删除成功");
    }

    private static async Task Images(GroupCommandArgs args)
    {
        if (!Directory.Exists(ImagePath))
        {
            await args.ReplyWithAtAsync("没有任何图片");
            return;
        }
        var images = Directory.GetFiles(ImagePath);
        if (images.Length == 0)
        {
            await args.ReplyWithAtAsync("没有任何图片");
            return;
        }
        var lines = images.Select(i => Path.Combine("images", Path.GetFileName(i)));
        await args.ReplyAsync("图片列表:\n" + string.Join("\n", lines));
    }

    private static async Task Upload(GroupCommandArgs args)
    {
        var images = ReplyAdapter.GetImages(args.MessageChain!);
        if (images.Count == 0)
        {
            await args.ReplyWithAtAsync("请携带一张或多张图片!");
            return;
        }
        if (!Directory.Exists(ImagePath))
            Directory.CreateDirectory(ImagePath);

        var saved = new List<string>();
        foreach (var img in images)
        {
            try
            {
                var name = img.FileName ?? $"{Guid.NewGuid():N}.png";
                var path = Path.Combine(ImagePath, name);
                var data = await DownloadImageAsync(img);
                if (data != null)
                {
                    await File.WriteAllBytesAsync(path, data);
                    saved.Add(path);
                }
            }
            catch { }
        }
        await args.ReplyWithAtAsync($"保存完成，共 {saved.Count}/{images.Count} 张");
    }

    private static async Task<byte[]?> DownloadImageAsync(ImageEntity img)
    {
        if (!string.IsNullOrEmpty(img.FileUrl))
        {
            using var client = new HttpClient();
            return await client.GetByteArrayAsync(img.FileUrl);
        }
        return null;
    }
}
