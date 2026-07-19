using Lagrange.Core.Common.Interface;
using Lagrange.Core.Message.Entities;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Extension;
using Vortex.Bot.Utility;

namespace Decompile;

[Command("反")]
[CommandType(CommandType.Group)]
public static class DecompileCommand
{
    [Main]
    [Flexible]
    public static async Task Execute(GroupCommandArgs args)
    {
        if (args.Message?.Entities.GetEnitys<ReplyEntity>().FirstOrDefault() is ReplyEntity entity)
        {
            var file = MessageRecord.Query(entity.SrcUid)?.GetEnitys<GroupFileEntity>().FirstOrDefault();
            if(file != null)
            {
                await args.ReplyAsync($"正在获取{file.FileName}文件链接……");
                var url = await args.BotContext.GroupFSDownload(args.GroupUin, file.FileId);
                var buffer = await HttpUtility.GetByteAsync(url);
                await args.ReplyAsync($"反编译前准备，文件下载完成：{url}");
                using var decompiler = new DllDecompiler();
                var zipBytes = decompiler.LoadDecompileAndZip(buffer);
                if (zipBytes == null)
                {
                    await args.ReplyAsync($"反编译失败:{decompiler.LastError?.Message}");
                    return;
                }
                await args.BotContext.SendGroupFile(args.GroupUin, new MemoryStream(zipBytes), file.FileName + "-Decompile.zip");
            }
            else
            {
                await args.ReplyAsync("未找到可反编译的文件，请确保回复的消息中包含文件。");
            }
           
        }
    }
}
